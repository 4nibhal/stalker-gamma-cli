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
        // Temporary role/type requirements for the known list, NOT a virtual
        // filesystem or a general instruction DSL. Same-kind targets across
        // steps retain ordered overwrite behavior; incompatible roles reject.
        var existing = existingTree.Entries.ToDictionary(entry => entry.FullPath, entry => entry.Kind,
            StringComparer.Ordinal);
        var requiredKinds = new Dictionary<string, EntryKind>(StringComparer.Ordinal);
        foreach (var path in parsed.Paths)
        {
            ct.ThrowIfCancellationRequested();
            var step = InspectInstruction(boundary, path, ct);
            RequireKnownKind(boundary.Root, existing, requiredKinds, step.Source.FullPath, EntryKind.Directory, ct);
            RequireKnownKind(boundary.Root, existing, requiredKinds, step.Gamedata.FullPath, EntryKind.Directory, ct);
            RequireKnownKind(boundary.Root, existing, requiredKinds, step.Destination.FullPath, EntryKind.Directory, ct);
            if (step.Source.Kind == EntryKind.Missing)
            {
                ct.ThrowIfCancellationRequested();
                continue;
            }
            var plan = DirUtils.PrepareCopy(step.Source.FullPath, step.Destination.FullPath, true, null, true, ct);
            foreach (var directory in plan.Directories)
                RequireKnownKind(boundary.Root, existing, requiredKinds, directory.Destination.FullPath, EntryKind.Directory, ct);
            foreach (var file in plan.Files)
                RequireKnownKind(boundary.Root, existing, requiredKinds, file.Destination.FullPath, EntryKind.NonDirectory, ct);
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
        string root, IReadOnlyDictionary<string, EntryKind> existing, Dictionary<string, EntryKind> requiredKinds,
        string path, EntryKind kind, CancellationToken ct)
    {
        // Every parent of a proposed file/directory is a directory requirement,
        // including a currently absent source to be produced by an earlier step.
        for (string? current = path; current is not null && ManagedCopyPlan.IsAtOrBelow(current, root);
            current = Path.GetDirectoryName(current))
        {
            ct.ThrowIfCancellationRequested();
            var expected = current == path ? kind : EntryKind.Directory;
            if ((existing.TryGetValue(current, out var actual) && actual != EntryKind.Missing && actual != expected)
                || (requiredKinds.TryGetValue(current, out var proposed) && proposed != expected))
                throw new UnsafePathException("Incompatible known instruction entry roles.");
            requiredKinds[current] = expected;
        }
    }

    private sealed record PreparedInstruction(CheckedEntry Source, CheckedEntry Gamedata, CheckedEntry Destination);
}
