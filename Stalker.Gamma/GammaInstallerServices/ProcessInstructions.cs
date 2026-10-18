using Stalker.Gamma.Filesystem;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices;

internal static class ProcessInstructions
{
    internal static void Process(
        string extractPath,
        IList<string> instructions,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(instructions);
        // Legacy IList contains blank wire markers. Check/copy EVERY raw member
        // before omitting blanks; FromParsed itself accepts executable paths
        // only, and must never reinterpret the literal path "0" as a sentinel.
        var values = instructions.Select(member => InputPolicy.PrepareValue(member, "instruction")).ToArray();
        var parsed = InstructionSet.FromParsed(values.Where(value => value.Length != 0));
        var boundary = new FileSystemBoundary(extractPath, Directory.GetCurrentDirectory());
        var existingTree = boundary.CheckTree(boundary.Root, ExpectedEntry.Directory,
            MissingPolicy.Suffix, cancellationToken);
        PrepareKnownInstructions(boundary, existingTree, parsed, cancellationToken);

        foreach (var path in parsed.Paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Earlier moves may have produced this source or its gamedata.
            // Preparation-time plans are deliberately NOT replayed here.
            var step = InspectInstruction(boundary, path, cancellationToken);
            if (step.Source.Kind == EntryKind.Missing)
            {
                // Preserve legacy nonempty missing-source behavior, but only
                // after whole-list preparation and a fresh destination check.
                boundary.CheckTree(step.Destination.FullPath, ExpectedEntry.Directory,
                    MissingPolicy.Suffix, cancellationToken);
                var destination = boundary.CheckPath(step.Destination.FullPath, ExpectedEntry.Directory,
                    MissingPolicy.Suffix, cancellationToken);
                Directory.CreateDirectory(destination.FullPath);
                cancellationToken.ThrowIfCancellationRequested();
                continue;
            }
            var plan = DirUtils.PrepareCopy(step.Source.FullPath, step.Destination.FullPath,
                true, null, true, cancellationToken);
            DirUtils.ExecuteCopy(plan, null, null, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static void PrepareKnownInstructions(
        FileSystemBoundary boundary, CheckedTree existingTree, InstructionSet parsed, CancellationToken ct)
    {
        // Instruction-only path/type projection, seeded from physical evidence.
        // Derived entries are NOT CheckedEntries or successful inspections. Only
        // selected mappings compose here; actual steps still prepare physically.
        var knownContents = existingTree.Entries.Where(entry => entry.Kind != EntryKind.Missing)
            .ToDictionary(entry => entry.FullPath, entry => entry.Kind, StringComparer.Ordinal);
        var requiredKinds = new Dictionary<string, EntryKind>(StringComparer.Ordinal);
        foreach (var path in parsed.Paths)
        {
            ct.ThrowIfCancellationRequested();
            var mappings = PrepareKnownMappings(boundary, path, knownContents, requiredKinds, ct);
            ComposeKnownMappings(boundary.Root, knownContents, mappings, ct);
        }
        ct.ThrowIfCancellationRequested();
    }

    private static IReadOnlyList<KnownInstructionMapping> PrepareKnownMappings(
        FileSystemBoundary boundary, string path, IReadOnlyDictionary<string, EntryKind> knownContents,
        Dictionary<string, EntryKind> requiredKinds, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var source = boundary.Resolve(path);
        var gamedata = Path.Join(source, "gamedata");
        var destination = knownContents.TryGetValue(gamedata, out var gamedataKind) && gamedataKind == EntryKind.Directory
            ? boundary.Root : boundary.Resolve("gamedata");
        RequireKnownKind(boundary.Root, knownContents, requiredKinds, source, EntryKind.Directory, ct);
        RequireKnownKind(boundary.Root, knownContents, requiredKinds, gamedata, EntryKind.Directory, ct);
        RequireKnownKind(boundary.Root, knownContents, requiredKinds, destination, EntryKind.Directory, ct);
        if (ManagedCopyPlan.IsStrictDescendant(destination, source))
            throw new UnsafePathException("Instruction destination is inside its source.");
        if (!knownContents.ContainsKey(source))
        {
            // A missing source creates only the destination, not the source or
            // its child gamedata. A role requirement is not produced content.
            ct.ThrowIfCancellationRequested();
            return new[] { new KnownInstructionMapping(source, destination, EntryKind.Directory) };
        }
        if (source == destination || source.Contains(".git", StringComparison.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            return Array.Empty<KnownInstructionMapping>();
        }

        // Materialize this step BEFORE adding outputs. Match managed-copy's
        // nonempty/.git selection without pretending this is a CheckedTree.
        var contents = knownContents.Where(entry => ManagedCopyPlan.IsAtOrBelow(entry.Key, source)).ToArray();
        var nonempty = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in contents)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.Value != EntryKind.NonDirectory)
                continue;
            for (var parent = Path.GetDirectoryName(entry.Key);
                parent is not null && ManagedCopyPlan.IsAtOrBelow(parent, source);
                parent = Path.GetDirectoryName(parent))
            {
                ct.ThrowIfCancellationRequested();
                if (!nonempty.Add(parent))
                    break;
            }
        }
        var directories = new HashSet<string>(StringComparer.Ordinal);
        var mappings = new List<KnownInstructionMapping>();
        foreach (var entry in contents.Where(entry => entry.Value == EntryKind.Directory)
            .OrderBy(entry => ManagedCopyPlan.Depth(entry.Key)).ThenBy(entry => entry.Key, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            if (entry.Key.Contains(".git", StringComparison.Ordinal) || (entry.Key != source && !nonempty.Contains(entry.Key)))
                continue;
            directories.Add(entry.Key);
            var target = Path.GetFullPath(Path.GetRelativePath(source, entry.Key), destination);
            RequireKnownKind(boundary.Root, knownContents, requiredKinds, target, EntryKind.Directory, ct);
            mappings.Add(new KnownInstructionMapping(entry.Key, target, EntryKind.Directory));
        }
        foreach (var entry in contents.Where(entry => entry.Value == EntryKind.NonDirectory)
            .OrderBy(entry => ManagedCopyPlan.Depth(entry.Key)).ThenBy(entry => entry.Key, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            if (!directories.Contains(Path.GetDirectoryName(entry.Key)!))
                continue;
            // Native member names are mapped as paths, never instruction-normalized.
            var target = Path.GetFullPath(Path.GetRelativePath(source, entry.Key), destination);
            RequireKnownKind(boundary.Root, knownContents, requiredKinds, target, EntryKind.NonDirectory, ct);
            mappings.Add(new KnownInstructionMapping(entry.Key, target, EntryKind.NonDirectory));
        }
        ct.ThrowIfCancellationRequested();
        return mappings;
    }

    private static void ComposeKnownMappings(
        string root, Dictionary<string, EntryKind> knownContents,
        IReadOnlyList<KnownInstructionMapping> mappings, CancellationToken ct)
    {
        // Directories remain; selected files move in the same shallow-first
        // order as managed-copy. Same-kind targets retain ordered overwrite.
        foreach (var mapping in mappings)
        {
            ct.ThrowIfCancellationRequested();
            if (mapping.Kind == EntryKind.NonDirectory)
                knownContents.Remove(mapping.Source);
            knownContents[mapping.Destination] = mapping.Kind;
            for (var parent = Path.GetDirectoryName(mapping.Destination);
                parent is not null && ManagedCopyPlan.IsAtOrBelow(parent, root);
                parent = Path.GetDirectoryName(parent))
            {
                ct.ThrowIfCancellationRequested();
                knownContents[parent] = EntryKind.Directory;
            }
        }
        ct.ThrowIfCancellationRequested();
    }

    private static PreparedInstruction InspectInstruction(FileSystemBoundary boundary, string path, CancellationToken ct)
    {
        var source = boundary.CheckPath(boundary.Resolve(path), ExpectedEntry.Directory, MissingPolicy.Suffix, ct);
        var gamedata = boundary.CheckPath(Path.Join(source.FullPath, "gamedata"), ExpectedEntry.Directory,
            MissingPolicy.Suffix, ct);
        var destination = boundary.CheckPath(gamedata.Kind == EntryKind.Directory
                ? boundary.Root : boundary.Resolve("gamedata"),
            ExpectedEntry.Directory, MissingPolicy.Suffix, ct);
        if (ManagedCopyPlan.IsStrictDescendant(destination.FullPath, source.FullPath))
            throw new UnsafePathException("Instruction destination is inside its source.");
        ct.ThrowIfCancellationRequested();
        return new PreparedInstruction(source, gamedata, destination);
    }

    private static void RequireKnownKind(
        string root, IReadOnlyDictionary<string, EntryKind> knownContents, Dictionary<string, EntryKind> requiredKinds,
        string path, EntryKind kind, CancellationToken ct)
    {
        // Every parent of a proposed file/directory is a directory requirement,
        // including a currently absent source to be produced by an earlier step.
        for (string? current = path; current is not null && ManagedCopyPlan.IsAtOrBelow(current, root);
            current = Path.GetDirectoryName(current))
        {
            ct.ThrowIfCancellationRequested();
            var expected = current == path ? kind : EntryKind.Directory;
            if ((knownContents.TryGetValue(current, out var actual) && actual != expected)
                || (requiredKinds.TryGetValue(current, out var proposed) && proposed != expected))
                throw new UnsafePathException("Incompatible known instruction entry roles.");
            requiredKinds[current] = expected;
        }
    }

    private sealed record KnownInstructionMapping(string Source, string Destination, EntryKind Kind);

    private sealed record PreparedInstruction(CheckedEntry Source, CheckedEntry Gamedata, CheckedEntry Destination);
}
