namespace Stalker.Gamma.Filesystem;

// A materialized description is evidence only, not a security token. Consumers
// must recheck canonical paths near I/O; replacement races are not prevented.
public sealed class CheckedTree
{
    public FileSystemBoundary Boundary { get; }
    public string StartPath { get; }
    public IReadOnlyList<CheckedEntry> Entries { get; }

    internal CheckedTree(
        FileSystemBoundary boundary, string startPath, IEnumerable<CheckedEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(startPath);
        ArgumentNullException.ThrowIfNull(entries);
        Boundary = boundary;
        StartPath = startPath;
        Entries = Array.AsReadOnly(entries.Select(entry =>
            new CheckedEntry(entry.FullPath, entry.RelativePath, entry.Kind)).ToArray());
    }
}
