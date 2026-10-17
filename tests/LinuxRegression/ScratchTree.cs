namespace LinuxRegression;

// This is independently owned test infrastructure, not production path policy.
// Managed metadata is evidence only; this helper makes no race-safety claim.
internal sealed class ScratchTree : IDisposable
{
    private readonly string containerRoot;
    private readonly string ownershipPath;
    private readonly string ownershipToken = Guid.NewGuid().ToString("N");
    private readonly object snapshotOwner = new();
    private bool disposed;

    internal string Root { get; }

    internal ScratchTree()
    {
        var temporaryParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        RequireAbsoluteDirectoryChain(temporaryParent);
        containerRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(
            Directory.CreateTempSubdirectory("stalker-linux-regression-").FullName));
        ownershipPath = Path.Join(containerRoot, ".scratch-owner");
        Root = Path.Join(containerRoot, "tree");
        RequireAbsoluteDirectoryChain(containerRoot);
        System.IO.File.WriteAllText(ownershipPath, ownershipToken);
        Directory.CreateDirectory(Root);
        // A constructor inspection/write error deliberately leaves the private
        // container for inspection rather than attempting a broad cleanup.
    }

    internal string Dir(string relative)
    {
        var path = MutationPath(relative);
        EnsureParents(path);
        var entry = ReadEntry(path);
        if (entry.Kind == EntryKind.Missing)
        {
            Directory.CreateDirectory(path);
            entry = ReadEntry(path);
        }
        RequireRealDirectory(entry);
        return path;
    }

    internal string File(string relative, string content)
    {
        var path = MutationPath(relative);
        EnsureParents(path);
        var entry = ReadEntry(path);
        if (entry.Kind is not (EntryKind.Missing or EntryKind.File))
            throw new IOException($"Scratch write requires an unlinked file: {path}");
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    internal string LinkFile(string relative, string target)
    {
        var path = MutationPath(relative);
        EnsureParents(path);
        RequireMissing(ReadEntry(path));
        System.IO.File.CreateSymbolicLink(path, target);
        return path;
    }

    internal string LinkDirectory(string relative, string target)
    {
        var path = MutationPath(relative);
        EnsureParents(path);
        RequireMissing(ReadEntry(path));
        Directory.CreateSymbolicLink(path, target);
        return path;
    }

    internal sealed class Snapshot
    {
        internal object Owner { get; }
        internal string AbsolutePath { get; }
        internal IReadOnlyList<EntryState> Entries { get; }

        internal Snapshot(object owner, string absolutePath, IEnumerable<EntryState> entries)
        {
            Owner = owner;
            AbsolutePath = absolutePath;
            Entries = Array.AsReadOnly(entries.ToArray());
        }
    }

    // Strings and value types only: no mutable byte arrays or inventory lists
    // escape a snapshot. Directory inventories use NUL (not a valid name byte).
    internal sealed record EntryState(
        string RelativePath, EntryKind Kind, FileAttributes? Attributes,
        UnixFileMode? Mode, string? Inventory, string? LinkTarget, string? Bytes);

    internal enum EntryKind { Missing, Directory, File, Link }

    private sealed record Entry(string Path, EntryKind Kind, FileAttributes Attributes);

    internal Snapshot Capture(string absolutePath)
    {
        var path = RequireOwnedAbsolutePath(absolutePath);
        var entries = new List<EntryState>();
        CaptureEntries(path, path, entries);
        return new Snapshot(snapshotOwner, path, entries);
    }

    internal void AssertUnchanged(Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!ReferenceEquals(snapshot.Owner, snapshotOwner))
            throw new InvalidOperationException("Snapshot belongs to another scratch tree.");
        var current = Capture(snapshot.AbsolutePath);
        Check.SequenceEqual(snapshot.Entries, current.Entries,
            $"Scratch state changed: {snapshot.AbsolutePath}");
    }

    private void CaptureEntries(string start, string path, List<EntryState> entries)
    {
        var entry = InspectOwnedPath(path);
        var relative = Path.GetRelativePath(start, path);
        if (entry.Kind == EntryKind.Missing)
        {
            entries.Add(new EntryState(relative, EntryKind.Missing, null, null, null, null, null));
            return;
        }
        if (entry.Kind == EntryKind.Link)
        {
            entries.Add(new EntryState(relative, EntryKind.Link, entry.Attributes,
                null, null, ReadLinkTarget(path, entry.Attributes), null));
            return;
        }

        var mode = System.IO.File.GetUnixFileMode(path);
        if (entry.Kind == EntryKind.Directory)
        {
            var children = OwnedChildren(path);
            var inventory = string.Join("\0", children.Select(child => Path.GetFileName(child)));
            entries.Add(new EntryState(relative, EntryKind.Directory, entry.Attributes,
                mode, inventory, null, null));
            foreach (var child in children)
                CaptureEntries(start, child, entries);
            return;
        }
        entries.Add(new EntryState(relative, EntryKind.File, entry.Attributes,
            mode, null, null, Convert.ToBase64String(System.IO.File.ReadAllBytes(path))));
    }

    private static string ReadLinkTarget(string path, FileAttributes attributes)
    {
        FileSystemInfo link = (attributes & FileAttributes.Directory) != 0
            ? new DirectoryInfo(path)
            : new FileInfo(path);
        return link.LinkTarget ?? throw new IOException($"Scratch link metadata disappeared: {path}");
    }

    private string MutationPath(string relative)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative)
            || relative.Split(Path.DirectorySeparatorChar).Any(part => part is "" or "." or ".."))
            throw new ArgumentException("Scratch mutation needs a nonempty, contained relative path.", nameof(relative));
        return RequireOwnedAbsolutePath(Path.GetFullPath(Path.Join(Root, relative)));
    }

    private string RequireOwnedAbsolutePath(string absolutePath)
    {
        ThrowIfDisposed();
        if (!Path.IsPathFullyQualified(absolutePath))
            throw new ArgumentException("Scratch inspection needs an absolute owned path.", nameof(absolutePath));
        var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(absolutePath));
        if (!string.Equals(path, Root, StringComparison.Ordinal)
            && !path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Path belongs to another tree.", nameof(absolutePath));
        return path;
    }

    private void EnsureParents(string path)
    {
        RequireRealDirectory(VerifyRoot(allowMissing: false));
        var parent = Path.GetDirectoryName(path)!;
        if (string.Equals(parent, Root, StringComparison.Ordinal))
            return;
        var current = Root;
        foreach (var part in Path.GetRelativePath(Root, parent).Split(Path.DirectorySeparatorChar))
        {
            current = Path.Join(current, part);
            var entry = ReadEntry(current);
            if (entry.Kind == EntryKind.Missing)
            {
                // Only one missing child beneath an already checked directory.
                Directory.CreateDirectory(current);
                entry = ReadEntry(current);
            }
            RequireRealDirectory(entry);
        }
    }

    private Entry InspectOwnedPath(string path)
    {
        path = RequireOwnedAbsolutePath(path);
        var root = VerifyRoot(allowMissing: true);
        if (root.Kind == EntryKind.Missing)
            return new Entry(path, EntryKind.Missing, default);
        if (string.Equals(path, Root, StringComparison.Ordinal))
            return root;
        var parts = Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar);
        var current = Root;
        for (var index = 0; index < parts.Length; index++)
        {
            current = Path.Join(current, parts[index]);
            var entry = ReadEntry(current);
            if (entry.Kind == EntryKind.Missing)
                return new Entry(path, EntryKind.Missing, default);
            if (index == parts.Length - 1)
                return entry;
            RequireRealDirectory(entry);
        }
        throw new IOException("Scratch path inspection did not reach its leaf.");
    }

    private string[] OwnedChildren(string path)
    {
        RequireRealDirectory(InspectOwnedPath(path));
        var children = Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly)
            .OrderBy(child => child, StringComparer.Ordinal).ToArray();
        foreach (var child in children)
        {
            RequireOwnedAbsolutePath(child);
            if (!string.Equals(Path.GetDirectoryName(child), path, StringComparison.Ordinal))
                throw new IOException("Scratch enumeration returned a non-child entry.");
        }
        return children;
    }

    private Entry VerifyRoot(bool allowMissing)
    {
        VerifyOwnership();
        var root = ReadEntry(Root);
        if (allowMissing && root.Kind == EntryKind.Missing)
            return root;
        RequireRealDirectory(root);
        return root;
    }

    private void VerifyOwnership()
    {
        ThrowIfDisposed();
        RequireAbsoluteDirectoryChain(containerRoot);
        var marker = ReadEntry(ownershipPath);
        if (marker.Kind != EntryKind.File
            || !string.Equals(System.IO.File.ReadAllText(ownershipPath), ownershipToken, StringComparison.Ordinal))
            throw new IOException($"Scratch ownership marker is not intact: {containerRoot}");
    }

    private static void RequireAbsoluteDirectoryChain(string path)
    {
        var current = Path.GetPathRoot(path)
            ?? throw new IOException("Scratch directory has no absolute root.");
        RequireRealDirectory(ReadEntry(current));
        foreach (var part in path[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Join(current, part);
            RequireRealDirectory(ReadEntry(current));
        }
    }

    // All callers check real directory parents before accepting a missing child.
    // Other inspection errors propagate; absence is never inferred from Exists.
    private static Entry ReadEntry(string path)
    {
        FileAttributes attributes;
        try
        {
            attributes = System.IO.File.GetAttributes(path);
        }
        catch (FileNotFoundException)
        {
            return new Entry(path, EntryKind.Missing, default);
        }
        catch (DirectoryNotFoundException)
        {
            return new Entry(path, EntryKind.Missing, default);
        }
        var kind = (attributes & FileAttributes.ReparsePoint) != 0 ? EntryKind.Link
            : (attributes & FileAttributes.Directory) != 0 ? EntryKind.Directory : EntryKind.File;
        return new Entry(path, kind, attributes);
    }

    private static void RequireRealDirectory(Entry entry)
    {
        if (entry.Kind != EntryKind.Directory)
            throw new IOException($"Scratch directory is missing, linked or not a directory: {entry.Path}");
    }

    private static void RequireMissing(Entry entry)
    {
        if (entry.Kind != EntryKind.Missing)
            throw new IOException($"Scratch link destination already exists: {entry.Path}");
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(ScratchTree));
    }

    public void Dispose()
    {
        if (disposed)
            return;
        var root = VerifyRoot(allowMissing: true);
        VerifyContainerInventory(rootMayExist: true);
        if (root.Kind != EntryKind.Missing)
        {
            // Finish inspection before any deletion. Every later delete checks
            // parents and the entry again; no error starts a broader fallback.
            var plan = new List<Entry>();
            InspectForCleanup(Root, plan);
            foreach (var entry in plan)
                DeleteChecked(entry);
        }
        VerifyOwnership();
        VerifyContainerInventory(rootMayExist: false);
        System.IO.File.Delete(ownershipPath);
        RequireAbsoluteDirectoryChain(containerRoot);
        if (Directory.EnumerateFileSystemEntries(containerRoot, "*", SearchOption.TopDirectoryOnly).Any())
            throw new IOException($"Scratch container is no longer empty: {containerRoot}");
        Directory.Delete(containerRoot);
        disposed = true;
    }

    private void VerifyContainerInventory(bool rootMayExist)
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(containerRoot, "*", SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(path, ownershipPath, StringComparison.Ordinal))
                continue;
            if (rootMayExist && string.Equals(path, Root, StringComparison.Ordinal))
                continue;
            throw new IOException($"Scratch container has an unexpected entry: {path}");
        }
    }

    private void InspectForCleanup(string path, List<Entry> plan)
    {
        var entry = InspectOwnedPath(path);
        if (entry.Kind == EntryKind.Missing)
            throw new IOException($"Scratch cleanup entry disappeared: {path}");
        if (entry.Kind == EntryKind.Directory)
        {
            foreach (var child in OwnedChildren(path))
                InspectForCleanup(child, plan);
        }
        // Links (registered or not) are terminal entries, never descent points.
        plan.Add(entry);
    }

    private void DeleteChecked(Entry planned)
    {
        var entry = InspectOwnedPath(planned.Path);
        if (entry.Kind != planned.Kind || entry.Attributes != planned.Attributes)
            throw new IOException($"Scratch cleanup entry changed: {planned.Path}");
        if (entry.Kind == EntryKind.Link)
        {
            UnlinkEntry(entry);
            return;
        }
        if (entry.Kind == EntryKind.Directory)
            Directory.Delete(entry.Path);
        else
            System.IO.File.Delete(entry.Path);
    }

    private static void UnlinkEntry(Entry entry)
    {
        if ((entry.Attributes & FileAttributes.Directory) != 0)
            Directory.Delete(entry.Path);
        else
            System.IO.File.Delete(entry.Path);
    }
}
