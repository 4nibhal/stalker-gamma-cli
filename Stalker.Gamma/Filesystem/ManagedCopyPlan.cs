namespace Stalker.Gamma.Filesystem;

// An operation-owned, materialized write set, not a capability. The checked
// trees describe preparation-time state; execution must recheck paths near I/O.
internal sealed class ManagedCopyPlan
{
    internal sealed record DirectoryTarget(CheckedEntry Source, CheckedEntry Destination);
    internal sealed record FileTarget(CheckedEntry Source, CheckedEntry Destination);

    internal CheckedTree SourceTree { get; }
    internal CheckedTree DestinationTree { get; }
    internal FileSystemBoundary SourceBoundary => SourceTree.Boundary;
    internal FileSystemBoundary DestinationBoundary => DestinationTree.Boundary;
    internal IReadOnlyList<DirectoryTarget> Directories { get; }
    internal IReadOnlyList<FileTarget> Files { get; }
    internal int TotalFiles => Files.Count;
    internal bool Overwrite { get; }
    internal bool MoveFile { get; }
    internal string? FileFilter { get; }
    internal bool IsNoOp { get; }

    private ManagedCopyPlan(
        CheckedTree sourceTree, CheckedTree destinationTree,
        IEnumerable<DirectoryTarget> directories, IEnumerable<FileTarget> files,
        bool overwrite, string? fileFilter, bool moveFile, bool isNoOp)
    {
        SourceTree = sourceTree;
        DestinationTree = destinationTree;
        Directories = Array.AsReadOnly(directories.ToArray());
        Files = Array.AsReadOnly(files.ToArray());
        Overwrite = overwrite;
        FileFilter = fileFilter;
        MoveFile = moveFile;
        IsNoOp = isNoOp;
    }

    internal static ManagedCopyPlan Prepare(
        string sourceDir, string destDir, bool overwrite, string? fileFilter,
        bool moveFile, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Capture one explicit base for BOTH inputs, before any callback can
        // change CWD. Only the resulting canonical paths are used for I/O.
        var basePath = Directory.GetCurrentDirectory();
        var sourceBoundary = new FileSystemBoundary(sourceDir, basePath);
        var destinationBoundary = new FileSystemBoundary(destDir, basePath);
        var sourceTree = sourceBoundary.CheckTree(sourceBoundary.Root,
            ExpectedEntry.Directory, MissingPolicy.Reject, ct);
        var destinationTree = destinationBoundary.CheckTree(destinationBoundary.Root,
            ExpectedEntry.Directory, MissingPolicy.Suffix, ct);

        if (IsStrictDescendant(destinationBoundary.Root, sourceBoundary.Root))
            throw new UnsafePathException("Copy destination is inside its source.");

        // Filters and nonempty selection run only AFTER complete inspection of
        // both trees. Neither .git nor a filename filter can hide a link.
        var selectedDirectories = SelectDirectories(sourceTree, ct);
        var selectedFiles = SelectFiles(sourceTree, selectedDirectories, fileFilter, ct);
        var directories = new List<DirectoryTarget>();
        var files = new List<FileTarget>();
        foreach (var entry in selectedDirectories)
        {
            ct.ThrowIfCancellationRequested();
            var target = Path.GetFullPath(Path.GetRelativePath(sourceBoundary.Root, entry.FullPath),
                destinationBoundary.Root);
            directories.Add(new DirectoryTarget(entry, destinationBoundary.CheckPath(target,
                ExpectedEntry.Directory, MissingPolicy.Suffix, ct)));
        }
        foreach (var entry in selectedFiles)
        {
            ct.ThrowIfCancellationRequested();
            // Native member paths must NOT pass through instruction slash or
            // whitespace normalization (notably literal Linux backslashes).
            var target = Path.GetFullPath(Path.GetRelativePath(sourceBoundary.Root, entry.FullPath),
                destinationBoundary.Root);
            files.Add(new FileTarget(entry, destinationBoundary.CheckPath(target,
                ExpectedEntry.NonDirectory, MissingPolicy.Suffix, ct)));
        }
        ValidateMappedTargets(destinationBoundary.Root, directories, files, ct);
        var isNoOp = string.Equals(sourceBoundary.Root, destinationBoundary.Root, StringComparison.Ordinal)
            || sourceBoundary.Root.Contains(".git", StringComparison.Ordinal);
        ct.ThrowIfCancellationRequested();
        return new ManagedCopyPlan(sourceTree, destinationTree, directories, files,
            overwrite, fileFilter, moveFile, isNoOp);
    }

    internal static IReadOnlyList<CheckedEntry> SelectDirectories(CheckedTree tree, CancellationToken ct)
    {
        var nonempty = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in tree.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.Kind != EntryKind.NonDirectory)
                continue;
            var parent = Path.GetDirectoryName(entry.FullPath);
            while (parent is not null && IsAtOrBelow(parent, tree.StartPath))
            {
                ct.ThrowIfCancellationRequested();
                if (!nonempty.Add(parent))
                    break; // That parent's ancestors have already been marked.
                parent = Path.GetDirectoryName(parent);
            }
        }
        var selected = new List<CheckedEntry>();
        foreach (var entry in tree.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.Kind == EntryKind.Directory
                && !entry.FullPath.Contains(".git", StringComparison.Ordinal)
                && (entry.FullPath == tree.StartPath || nonempty.Contains(entry.FullPath)))
                selected.Add(entry);
        }
        var result = Array.AsReadOnly(selected.OrderBy(entry => Depth(entry.FullPath))
            .ThenBy(entry => entry.FullPath, StringComparer.Ordinal).ToArray());
        ct.ThrowIfCancellationRequested();
        return result;
    }

    internal static IReadOnlyList<CheckedEntry> SelectFiles(
        CheckedTree tree, IReadOnlyList<CheckedEntry> directories, string? fileFilter, CancellationToken ct)
    {
        var selectedDirectories = directories.Select(entry => entry.FullPath).ToHashSet(StringComparer.Ordinal);
        var selected = new List<CheckedEntry>();
        foreach (var entry in tree.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.Kind == EntryKind.NonDirectory
                && selectedDirectories.Contains(Path.GetDirectoryName(entry.FullPath)!)
                && (string.IsNullOrWhiteSpace(fileFilter) || Path.GetFileName(entry.FullPath) != fileFilter))
                selected.Add(entry);
        }
        // Load-bearing for ancestor flattening: a deeper output can overwrite
        // an original shallower source. Consume ALL shallower sources first.
        var result = Array.AsReadOnly(selected.OrderBy(entry => Depth(entry.FullPath))
            .ThenBy(entry => entry.FullPath, StringComparer.Ordinal).ToArray());
        ct.ThrowIfCancellationRequested();
        return result;
    }

    private static void ValidateMappedTargets(
        string destinationRoot, IEnumerable<DirectoryTarget> directories,
        IEnumerable<FileTarget> files, CancellationToken ct)
    {
        var kinds = new Dictionary<string, EntryKind>(StringComparer.Ordinal);
        foreach (var directory in directories)
        {
            ct.ThrowIfCancellationRequested();
            if (!kinds.TryAdd(directory.Destination.FullPath, EntryKind.Directory))
                throw new UnsafePathException("Duplicate canonical copy target.");
        }
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            if (!kinds.TryAdd(file.Destination.FullPath, EntryKind.NonDirectory))
                throw new UnsafePathException("Duplicate canonical copy target.");
        }
        foreach (var target in kinds.Keys)
        {
            ct.ThrowIfCancellationRequested();
            for (var parent = Path.GetDirectoryName(target);
                parent is not null && IsAtOrBelow(parent, destinationRoot);
                parent = Path.GetDirectoryName(parent))
            {
                ct.ThrowIfCancellationRequested();
                if (kinds.TryGetValue(parent, out var kind) && kind == EntryKind.NonDirectory)
                    throw new UnsafePathException("Proposed copy file is also a target parent.");
            }
        }
        ct.ThrowIfCancellationRequested();
    }

    internal static int Depth(string path) => path.Count(character => character == Path.DirectorySeparatorChar);

    internal static bool IsAtOrBelow(string path, string parent) =>
        string.Equals(path, parent, StringComparison.Ordinal) || IsStrictDescendant(path, parent);

    internal static bool IsStrictDescendant(string path, string parent) =>
        path.StartsWith(Path.EndsInDirectorySeparator(parent) ? parent : parent + Path.DirectorySeparatorChar,
            StringComparison.Ordinal) && !string.Equals(path, parent, StringComparison.Ordinal);
}
