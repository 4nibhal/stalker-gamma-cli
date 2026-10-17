namespace Stalker.Gamma.Filesystem;

// Lexical containment plus strict inspected-state evidence. Neither this object
// nor its snapshots make later path-based I/O atomic or race resistant.
public sealed class FileSystemBoundary
{
    private readonly string rootPrefix;

    public string Root { get; }

    public FileSystemBoundary(string configuredRoot, string basePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuredRoot))
                throw new UnsafePathException("Invalid configured filesystem root.");
            if (string.IsNullOrWhiteSpace(basePath) || !Path.IsPathFullyQualified(basePath))
                throw new UnsafePathException("Filesystem base path must be explicitly absolute.");

            var absoluteBase = Path.GetFullPath(basePath);
            Root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configuredRoot, absoluteBase));
            rootPrefix = Path.EndsInDirectorySeparator(Root) ? Root : Root + Path.DirectorySeparatorChar;
        }
        catch (Exception error) when (!OperationFailure.IsCancellation(error) && !UnsafePathException.IsUnsafe(error))
        {
            throw new UnsafePathException("Cannot normalize configured filesystem root.", error);
        }
    }

    public string Resolve(string relativePath) =>
        CanonicalContainedPath(InputPolicy.RequireRelativePath(relativePath, "boundary relative path"));

    public CheckedEntry CheckPath(
        string path, ExpectedEntry expected = ExpectedEntry.Any,
        MissingPolicy missing = MissingPolicy.Reject, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (expected is not (ExpectedEntry.Any or ExpectedEntry.Directory or ExpectedEntry.NonDirectory))
            throw new UnsafePathException("Invalid expected filesystem entry kind.");
        if (missing is not (MissingPolicy.Reject or MissingPolicy.Leaf or MissingPolicy.Suffix))
            throw new UnsafePathException("Invalid filesystem missing-entry policy.");

        var fullPath = CanonicalContainedPath(path);
        var current = Path.GetPathRoot(fullPath)
            ?? throw new UnsafePathException("Filesystem inspection requires an absolute path.");
        var parts = fullPath[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var directoryParentChecked = false;

        // Index -1 inspects the filesystem root itself, not merely Root. Every
        // existing intermediate prefix must be an unlinked directory; the leaf
        // is also inspected, with its own explicit kind expectation.
        for (var index = -1; index < parts.Length; index++)
        {
            ct.ThrowIfCancellationRequested();
            if (index >= 0)
                current = Path.Join(current, parts[index]);
            var kind = ReadKind(current);
            ct.ThrowIfCancellationRequested();
            var isLeaf = index == parts.Length - 1;
            if (kind == EntryKind.Missing)
            {
                if (!directoryParentChecked || missing == MissingPolicy.Reject
                    || (missing == MissingPolicy.Leaf && !isLeaf))
                    throw new UnsafePathException("Required filesystem entry is missing.");

                // A permitted absent suffix begins only below checked directory
                // parents. Describe the requested leaf, not the first gap.
                ct.ThrowIfCancellationRequested();
                return Describe(fullPath, EntryKind.Missing);
            }
            if (!isLeaf)
            {
                if (kind != EntryKind.Directory)
                    throw new UnsafePathException("Filesystem parent is not a directory.");
                directoryParentChecked = true;
                continue;
            }
            if ((expected == ExpectedEntry.Directory && kind != EntryKind.Directory)
                || (expected == ExpectedEntry.NonDirectory && kind != EntryKind.NonDirectory))
                throw new UnsafePathException("Filesystem entry has an unexpected kind.");

            ct.ThrowIfCancellationRequested();
            return Describe(fullPath, kind);
        }
        throw new UnsafePathException("Filesystem inspection did not reach its requested entry.");
    }

    public CheckedTree CheckTree(
        string path, ExpectedEntry expected = ExpectedEntry.Any,
        MissingPolicy missing = MissingPolicy.Reject, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var start = CheckPath(path, expected, missing, ct);
        // Include even a missing start: an empty collection is never used to
        // ambiguously represent either absence or an empty existing directory.
        var entries = new List<CheckedEntry> { start };
        var pending = new Stack<CheckedEntry>();
        if (start.Kind == EntryKind.Directory)
            pending.Push(start);
        while (pending.Count != 0)
        {
            ct.ThrowIfCancellationRequested();
            var directory = CheckPath(pending.Pop().FullPath, ExpectedEntry.Directory,
                MissingPolicy.Reject, ct);
            foreach (var childPath in ReadChildren(directory, ct))
            {
                ct.ThrowIfCancellationRequested();
                // Native names are not instruction-normalized. Only canonical,
                // inspected paths are used for this traversal's actual I/O.
                var child = CheckPath(childPath, ExpectedEntry.Any, MissingPolicy.Reject, ct);
                if (!string.Equals(Path.GetDirectoryName(child.FullPath), directory.FullPath, StringComparison.Ordinal))
                    throw new UnsafePathException("Filesystem enumeration returned a non-child entry.");
                entries.Add(child);
                if (child.Kind == EntryKind.Directory)
                    pending.Push(child);
            }
        }
        ct.ThrowIfCancellationRequested();
        return new CheckedTree(this, start.FullPath, entries);
    }

    private string CanonicalContainedPath(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
                throw new UnsafePathException("Invalid filesystem entry path.");
            var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path, Root));
            if (!string.Equals(fullPath, Root, StringComparison.Ordinal)
                && !fullPath.StartsWith(rootPrefix, StringComparison.Ordinal))
                throw new UnsafePathException("Filesystem entry is outside its boundary.");
            return fullPath;
        }
        catch (Exception error) when (!OperationFailure.IsCancellation(error) && !UnsafePathException.IsUnsafe(error))
        {
            throw new UnsafePathException("Cannot normalize filesystem entry path.", error);
        }
    }

    private CheckedEntry Describe(string fullPath, EntryKind kind) =>
        new CheckedEntry(fullPath, Path.GetRelativePath(Root, fullPath), kind);

    private static string[] ReadChildren(CheckedEntry directory, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var children = new List<string>();
            // Explicitly request all native members at this level and no
            // ignored access errors. Creation, GetEnumerator, MoveNext and
            // Dispose are inside this try; checked kinds alone drive descent.
            foreach (var child in Directory.EnumerateFileSystemEntries(
                directory.FullPath, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = false,
                    IgnoreInaccessible = false,
                    AttributesToSkip = 0,
                    ReturnSpecialDirectories = false,
                }))
            {
                ct.ThrowIfCancellationRequested();
                children.Add(child);
            }
            ct.ThrowIfCancellationRequested();
            return children.OrderBy(child => child, StringComparer.Ordinal).ToArray();
        }
        catch (Exception error) when (!OperationFailure.IsCancellation(error) && !UnsafePathException.IsUnsafe(error))
        {
            throw new UnsafePathException("Cannot enumerate filesystem directory.", error);
        }
    }

    private static EntryKind ReadKind(string path)
    {
        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(path);
        }
        catch (FileNotFoundException error) when (!OperationFailure.IsCancellation(error))
        {
            return EntryKind.Missing;
        }
        catch (DirectoryNotFoundException error) when (!OperationFailure.IsCancellation(error))
        {
            return EntryKind.Missing;
        }
        catch (Exception error) when (!OperationFailure.IsCancellation(error) && !UnsafePathException.IsUnsafe(error))
        {
            throw new UnsafePathException("Cannot inspect filesystem entry.", error);
        }
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new UnsafePathException("Linked filesystem entries are not allowed.");
        if ((attributes & FileAttributes.Directory) != 0)
            return EntryKind.Directory;
        return EntryKind.NonDirectory;
    }
}
