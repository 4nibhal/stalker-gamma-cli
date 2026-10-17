namespace Stalker.Gamma.Filesystem;

// NonDirectory is deliberately not a claim of POSIX regular-file type.
public enum EntryKind { Missing, Directory, NonDirectory }
public enum ExpectedEntry { Any, Directory, NonDirectory }
public enum MissingPolicy { Reject, Leaf, Suffix }

// Immutable descriptions of inspected state, not authority for later I/O.
public sealed record CheckedEntry(string FullPath, string RelativePath, EntryKind Kind);
