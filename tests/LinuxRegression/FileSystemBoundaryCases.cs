using Stalker.Gamma.Filesystem;

namespace LinuxRegression;

// Written behavioral fixtures only. No C# compilation or execution is implied by
// the separate Python source/wiring checks.
internal static class FileSystemBoundaryCases
{
    internal static Task RunAsync()
    {
        ConstructionAndResolutionAreLexicalOnly();
        RelativeRootUsesAnExplicitStableBase();
        ContainmentIsOrdinalAndComponentAware();
        ReturnsCanonicalPathsWithoutRenamingNativeMembers();
        RejectsLinkedRootsAndAncestorsAboveTheBoundary();
        RejectsContainedExternalDanglingAndCyclicLinks();
        EnforcesDirectoryAndNonDirectoryExpectations();
        DistinguishesMissingLeafFromMissingSuffix();
        IncludesMissingStartAndOwnsEntryDescriptions();
        MaterializesCompleteTreesUsingCheckedKinds();
        SnapshotsAreEvidenceNotAuthorization();
        HonorsPreCancellationIncludingMissingResults();
        RejectsInspectionAndEnumerationErrorsWhenPermissionsPermit();
        return Task.CompletedTask;
    }

    private static void WithTree(Action<ScratchTree> body)
    {
        var tree = new ScratchTree();
        Check.WithCleanup(() => body(tree), tree.Dispose);
    }

    private static void ConstructionAndResolutionAreLexicalOnly() => WithTree(tree =>
    {
        var uncreated = Path.Join(tree.Root, "not-created", "root");
        var absent = tree.Capture(uncreated);
        var boundary = new FileSystemBoundary("not-created/root", tree.Root);
        Check.Equal(uncreated, boundary.Root);
        Check.Equal(Path.Join(uncreated, "folder", "child"), boundary.Resolve(@"folder\child"));
        tree.AssertUnchanged(absent);

        var dangling = tree.LinkDirectory("dangling-root", Path.Join(tree.Root, "absent-target"));
        var lexicalLink = new FileSystemBoundary(dangling, tree.Root);
        Check.Equal(Path.Join(dangling, "child"), lexicalLink.Resolve("child"));
        Check.Throws<UnsafePathException>(() => lexicalLink.CheckPath(lexicalLink.Root,
            ExpectedEntry.Any, MissingPolicy.Suffix));

        foreach (var root in new[] { "", " ", "\0" })
            Check.Throws<UnsafePathException>(() => new FileSystemBoundary(root, tree.Root));
        foreach (var basePath in new[] { "", " ", "relative-base", "\0" })
            Check.Throws<UnsafePathException>(() => new FileSystemBoundary("root", basePath));
        foreach (var relative in new[] { "", ".", "..", "../escape", "nested/../escape", "/absolute", @"C:\drive", @"\rooted" })
            Check.Throws<UnsafePathException>(() => boundary.Resolve(relative));
        Check.Throws<UnsafePathException>(() => boundary.CheckPath("\0"));
        Check.Throws<UnsafePathException>(() => boundary.CheckTree("\0"));
    });

    private static void RelativeRootUsesAnExplicitStableBase()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var tree = new ScratchTree();
        Check.WithCleanup(() =>
        {
            var root = tree.Dir("stable/root");
            var file = tree.File("stable/root/leaf.txt", "bytes");
            var boundary = new FileSystemBoundary("stable/root", tree.Root);
            Directory.SetCurrentDirectory(tree.Dir("different-working-directory"));
            var constructedAfterCwdChange = new FileSystemBoundary("stable/root", tree.Root);
            Check.Equal(root, boundary.Root);
            Check.Equal(root, constructedAfterCwdChange.Root);
            Check.Equal(file, boundary.Resolve("leaf.txt"));
            Check.Equal(file, boundary.CheckPath("leaf.txt", ExpectedEntry.NonDirectory).FullPath);
            Check.Equal(file, constructedAfterCwdChange.CheckPath("leaf.txt").FullPath);
        }, () =>
        {
            Directory.SetCurrentDirectory(originalDirectory);
            tree.Dispose();
        });
        Check.Equal(originalDirectory, Directory.GetCurrentDirectory());
    }

    private static void ContainmentIsOrdinalAndComponentAware() => WithTree(tree =>
    {
        var root = tree.Dir("TreeRoot");
        var boundary = new FileSystemBoundary(root + "/", tree.Root);
        Check.Equal(root, boundary.Root);
        Check.Equal(".", boundary.CheckPath(root, ExpectedEntry.Directory).RelativePath);
        var sibling = tree.File("TreeRoot-sibling/file", "sibling");
        var differentCase = tree.File("treeroot/file", "case-distinct");
        foreach (var outside in new[] { sibling, differentCase, Path.Join(root, "..", "escape") })
        {
            Check.Throws<UnsafePathException>(() => boundary.CheckPath(outside,
                ExpectedEntry.Any, MissingPolicy.Suffix));
            Check.Throws<UnsafePathException>(() => boundary.CheckTree(outside,
                ExpectedEntry.Any, MissingPolicy.Suffix));
        }

        var filesystemRoot = Path.GetPathRoot(tree.Root)!;
        var wholeRoot = new FileSystemBoundary(filesystemRoot, tree.Root);
        Check.Equal(filesystemRoot, wholeRoot.Root);
        Check.Equal(EntryKind.Directory, wholeRoot.CheckPath(filesystemRoot).Kind);
        Check.Equal(root, wholeRoot.CheckPath(root).FullPath);
    });

    private static void ReturnsCanonicalPathsWithoutRenamingNativeMembers() => WithTree(tree =>
    {
        var root = tree.Dir("root");
        tree.Dir("root/nested");
        var file = tree.File("root/leaf.txt", "bytes");
        var nativeBackslash = tree.File(@"root/native\name.txt", "backslash is a Linux name byte");
        var nativeSpaces = tree.File("root/ leading ñ d'Artagnan ", "spaces are native name bytes");
        var boundary = new FileSystemBoundary(root, tree.Root);
        var canonical = boundary.CheckPath(Path.Join(root, "nested", "..", "leaf.txt"));
        Check.Equal(file, canonical.FullPath);
        Check.Equal("leaf.txt", canonical.RelativePath);
        Check.Equal(file, boundary.CheckPath("nested/../leaf.txt").FullPath);
        Check.Equal(nativeBackslash, boundary.CheckPath(nativeBackslash).FullPath);
        Check.Equal(nativeSpaces, boundary.CheckPath(nativeSpaces).FullPath);
        var snapshot = boundary.CheckTree(root);
        Check.True(snapshot.Entries.Any(entry => entry.FullPath == nativeBackslash), "Native backslash member was renamed.");
        Check.True(snapshot.Entries.Any(entry => entry.FullPath == nativeSpaces), "Native spaces/Unicode member was renamed.");
    });

    private static void RejectsLinkedRootsAndAncestorsAboveTheBoundary() => WithTree(outside =>
    {
        var external = outside.Dir("target/subtree");
        outside.File("target/subtree/sentinel", "unchanged");
        var before = outside.Capture(outside.Root);
        WithTree(tree =>
        {
            var contained = tree.Dir("real/subtree");
            tree.File("real/subtree/sentinel", "unchanged");
            var containedLink = tree.LinkDirectory("contained-root", Path.GetDirectoryName(contained)!);
            var externalLink = tree.LinkDirectory("external-root", Path.GetDirectoryName(external)!);
            var danglingLink = tree.LinkDirectory("dangling-root", Path.Join(tree.Root, "absent"));
            var cycleLink = tree.LinkDirectory("cyclic-root", Path.Join(tree.Root, "cyclic-root"));
            var snapshot = tree.Capture(tree.Root);
            foreach (var link in new[] { containedLink, externalLink, danglingLink, cycleLink })
            {
                // The link is either the configured root or an ancestor ABOVE
                // it. A boundary-only ancestor walk would miss the second case.
                foreach (var configuredRoot in new[] { link, Path.Join(link, "subtree") })
                {
                    var boundary = new FileSystemBoundary(configuredRoot, tree.Root);
                    Check.Throws<UnsafePathException>(() => boundary.CheckPath(boundary.Root,
                        ExpectedEntry.Any, MissingPolicy.Suffix));
                    Check.Throws<UnsafePathException>(() => boundary.CheckPath(boundary.Resolve("missing/leaf"),
                        ExpectedEntry.Any, MissingPolicy.Suffix));
                    Check.Throws<UnsafePathException>(() => boundary.CheckTree(boundary.Root,
                        ExpectedEntry.Any, MissingPolicy.Suffix));
                }
            }
            tree.AssertUnchanged(snapshot);
        });
        outside.AssertUnchanged(before);
    });

    private static void RejectsContainedExternalDanglingAndCyclicLinks() => WithTree(outside =>
    {
        var externalFile = outside.File("sentinel.txt", "unchanged");
        var externalDirectory = outside.Dir("directory");
        outside.File("directory/sentinel.txt", "also unchanged");
        var before = outside.Capture(outside.Root);
        WithTree(tree =>
        {
            var root = tree.Dir("root");
            var containedFile = tree.File("root/real-file", "bytes");
            var containedDirectory = tree.Dir("root/real-directory");
            var links = new[]
            {
                tree.LinkFile("root/contained-file", containedFile),
                tree.LinkDirectory("root/contained-directory", containedDirectory),
                tree.LinkFile("root/external-file", externalFile),
                tree.LinkDirectory("root/external-directory", externalDirectory),
                tree.LinkFile("root/dangling-file", Path.Join(root, "absent-file")),
                tree.LinkDirectory("root/dangling-directory", Path.Join(root, "absent-directory")),
                tree.LinkFile("root/cyclic-file", Path.Join(root, "cyclic-file")),
                tree.LinkDirectory("root/cyclic-directory", Path.Join(root, "cyclic-directory")),
                tree.LinkDirectory("root/cycle-a", Path.Join(root, "cycle-b")),
                tree.LinkDirectory("root/cycle-b", Path.Join(root, "cycle-a")),
            };
            var snapshot = tree.Capture(tree.Root);
            var boundary = new FileSystemBoundary(root, tree.Root);
            foreach (var link in links)
            {
                Check.Throws<UnsafePathException>(() => boundary.CheckPath(link,
                    ExpectedEntry.Any, MissingPolicy.Suffix));
                Check.Throws<UnsafePathException>(() => boundary.CheckTree(link,
                    ExpectedEntry.Any, MissingPolicy.Suffix));
                Check.Throws<UnsafePathException>(() => boundary.CheckPath(Path.Join(link, "missing", "child"),
                    ExpectedEntry.Any, MissingPolicy.Suffix));
                Check.Throws<UnsafePathException>(() => boundary.CheckTree(Path.Join(link, "missing", "child"),
                    ExpectedEntry.Any, MissingPolicy.Suffix));
            }
            Check.Throws<UnsafePathException>(() => boundary.CheckTree(root));
            tree.AssertUnchanged(snapshot);
        });
        outside.AssertUnchanged(before);
        Check.Equal("unchanged", System.IO.File.ReadAllText(externalFile));
    });

    private static void EnforcesDirectoryAndNonDirectoryExpectations() => WithTree(tree =>
    {
        var root = tree.Dir("root");
        var file = tree.File("root/file", "not a directory");
        var directory = tree.Dir("root/directory");
        var boundary = new FileSystemBoundary(root, tree.Root);
        Check.Equal(EntryKind.NonDirectory, boundary.CheckPath(file, ExpectedEntry.NonDirectory).Kind);
        Check.Equal(EntryKind.Directory, boundary.CheckPath(directory, ExpectedEntry.Directory).Kind);
        Check.Throws<UnsafePathException>(() => boundary.CheckPath(file, ExpectedEntry.Directory));
        Check.Throws<UnsafePathException>(() => boundary.CheckTree(file, ExpectedEntry.Directory));
        Check.Throws<UnsafePathException>(() => boundary.CheckPath(directory, ExpectedEntry.NonDirectory));
        Check.Throws<UnsafePathException>(() => boundary.CheckTree(directory, ExpectedEntry.NonDirectory));
        foreach (var missing in new[] { MissingPolicy.Reject, MissingPolicy.Leaf, MissingPolicy.Suffix })
        {
            Check.Throws<UnsafePathException>(() => boundary.CheckPath(Path.Join(file, "child"),
                ExpectedEntry.Any, missing));
            Check.Throws<UnsafePathException>(() => boundary.CheckTree(Path.Join(file, "child"),
                ExpectedEntry.Any, missing));
        }
        var fileRoot = new FileSystemBoundary(file, tree.Root);
        Check.Equal(EntryKind.NonDirectory, fileRoot.CheckPath(fileRoot.Root).Kind);
        Check.Throws<UnsafePathException>(() => fileRoot.CheckPath(fileRoot.Resolve("child"),
            ExpectedEntry.Any, MissingPolicy.Suffix));
        Check.Throws<UnsafePathException>(() => boundary.CheckPath(root, (ExpectedEntry)999));
        Check.Throws<UnsafePathException>(() => boundary.CheckPath(root, missing: (MissingPolicy)999));
    });

    private static void DistinguishesMissingLeafFromMissingSuffix() => WithTree(tree =>
    {
        var root = tree.Dir("root");
        var boundary = new FileSystemBoundary(root, tree.Root);
        var leaf = boundary.Resolve("new");
        var suffix = boundary.Resolve("new/child");
        var before = tree.Capture(tree.Root);
        Check.Throws<UnsafePathException>(() => boundary.CheckPath(leaf));
        Check.Throws<UnsafePathException>(() => boundary.CheckPath(suffix));
        foreach (var expected in new[] { ExpectedEntry.Any, ExpectedEntry.Directory, ExpectedEntry.NonDirectory })
        {
            Check.Equal(EntryKind.Missing, boundary.CheckPath(leaf, expected, MissingPolicy.Leaf).Kind);
            Check.Throws<UnsafePathException>(() => boundary.CheckPath(suffix, expected, MissingPolicy.Leaf));
            var missing = boundary.CheckPath(suffix, expected, MissingPolicy.Suffix);
            Check.Equal(EntryKind.Missing, missing.Kind);
            Check.Equal(suffix, missing.FullPath);
            Check.Equal("new/child", missing.RelativePath);
        }
        var missingRoot = new FileSystemBoundary(Path.Join(root, "absent-root"), tree.Root);
        Check.Equal(EntryKind.Missing, missingRoot.CheckPath(missingRoot.Root,
            ExpectedEntry.Directory, MissingPolicy.Leaf).Kind);
        var missingAncestor = new FileSystemBoundary(Path.Join(root, "absent-parent", "root"), tree.Root);
        Check.Throws<UnsafePathException>(() => missingAncestor.CheckPath(missingAncestor.Root,
            ExpectedEntry.Directory, MissingPolicy.Leaf));
        Check.Equal(EntryKind.Missing, missingAncestor.CheckPath(missingAncestor.Root,
            ExpectedEntry.Directory, MissingPolicy.Suffix).Kind);
        tree.AssertUnchanged(before);
    });

    private static void IncludesMissingStartAndOwnsEntryDescriptions() => WithTree(tree =>
    {
        var root = tree.Dir("root");
        var boundary = new FileSystemBoundary(root, tree.Root);
        var start = boundary.Resolve("new/child");
        var snapshot = boundary.CheckTree(start, ExpectedEntry.Directory, MissingPolicy.Suffix);
        Check.True(ReferenceEquals(boundary, snapshot.Boundary), "Snapshot lost its explicit boundary.");
        Check.Equal(start, snapshot.StartPath);
        Check.Equal(1, snapshot.Entries.Count);
        Check.Equal(new CheckedEntry(start, "new/child", EntryKind.Missing), snapshot.Entries.Single());
        Check.Throws<UnsafePathException>(() => boundary.CheckTree(start, ExpectedEntry.Directory, MissingPolicy.Leaf));
        Check.Throws<UnsafePathException>(() => boundary.CheckTree(start));
        var missingRoot = new FileSystemBoundary(Path.Join(root, "absent"), tree.Root);
        Check.Equal(new CheckedEntry(missingRoot.Root, ".", EntryKind.Missing),
            missingRoot.CheckTree(missingRoot.Root, ExpectedEntry.Directory, MissingPolicy.Leaf).Entries.Single());

        var entry = snapshot.Entries.Single();
        var callerEntries = new List<CheckedEntry> { entry };
        var owned = new CheckedTree(boundary, start, callerEntries);
        callerEntries[0] = entry with { Kind = EntryKind.Directory };
        callerEntries.Clear();
        Check.Equal(entry, owned.Entries.Single());
        Check.True(!ReferenceEquals(entry, owned.Entries.Single()), "Entry descriptions were not copied.");
        Check.True(owned.Entries is not CheckedEntry[], "Mutable array escaped.");
        if (owned.Entries is IList<CheckedEntry> list)
            Check.Throws<NotSupportedException>(() => list[0] = entry with { Kind = EntryKind.NonDirectory });
    });

    private static void MaterializesCompleteTreesUsingCheckedKinds() => WithTree(tree =>
    {
        var root = tree.Dir("root");
        tree.Dir("root/empty");
        tree.File("root/alpha/bytes", "bytes");
        tree.File("root/alpha/deeper/second", "second");
        var leaf = tree.File("root/leaf", "leaf");
        tree.File("root/.hidden", "hidden");
        var boundary = new FileSystemBoundary(root, tree.Root);
        var before = tree.Capture(tree.Root);
        var snapshot = boundary.CheckTree(Path.Join(root, "alpha", ".."), ExpectedEntry.Directory);
        Check.Equal(root, snapshot.StartPath);
        Check.Equal(new CheckedEntry(root, ".", EntryKind.Directory), snapshot.Entries[0]);
        Check.SequenceEqual(new[] { ".", ".hidden", "alpha", "alpha/bytes", "alpha/deeper", "alpha/deeper/second", "empty", "leaf" },
            snapshot.Entries.Select(entry => entry.RelativePath).OrderBy(path => path, StringComparer.Ordinal));
        foreach (var entry in snapshot.Entries)
        {
            Check.Equal(Path.GetFullPath(entry.RelativePath, root), entry.FullPath);
            Check.Equal(boundary.CheckPath(entry.FullPath), entry);
        }
        Check.Equal(snapshot.Entries.Count, snapshot.Entries.Select(entry => entry.FullPath).Distinct(StringComparer.Ordinal).Count());
        var leafSnapshot = boundary.CheckTree(leaf, ExpectedEntry.NonDirectory);
        Check.Equal(new CheckedEntry(leaf, "leaf", EntryKind.NonDirectory), leafSnapshot.Entries.Single());
        var empty = boundary.Resolve("empty");
        Check.Equal(new CheckedEntry(empty, "empty", EntryKind.Directory),
            boundary.CheckTree(empty, ExpectedEntry.Directory).Entries.Single());
        var subtree = boundary.CheckTree(boundary.Resolve("alpha/deeper"), ExpectedEntry.Directory);
        Check.SequenceEqual(new[] { "alpha/deeper", "alpha/deeper/second" }, subtree.Entries.Select(entry => entry.RelativePath));
        tree.AssertUnchanged(before);

        tree.LinkDirectory("root/alpha/deeper/unexpected-link", root);
        Check.Throws<UnsafePathException>(() => boundary.CheckTree(root));
    });

    private static void SnapshotsAreEvidenceNotAuthorization() => WithTree(tree =>
    {
        var root = tree.Dir("root");
        var file = tree.File("root/entry", "bytes");
        var boundary = new FileSystemBoundary(root, tree.Root);
        var snapshot = boundary.CheckTree(file);
        System.IO.File.Delete(file);
        tree.Dir("root/entry");
        Check.Equal(EntryKind.NonDirectory, snapshot.Entries.Single().Kind);
        Check.Equal(EntryKind.Directory, boundary.CheckPath(file, ExpectedEntry.Directory).Kind);
        Check.Throws<UnsafePathException>(() => boundary.CheckPath(snapshot.StartPath, ExpectedEntry.NonDirectory));
    });

    private static void HonorsPreCancellationIncludingMissingResults() => WithTree(tree =>
    {
        var root = tree.Dir("root");
        var file = tree.File("root/file", "bytes");
        var boundary = new FileSystemBoundary(root, tree.Root);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        foreach (var path in new[] { root, file, boundary.Resolve("missing"), boundary.Resolve("missing/child") })
        {
            var pathError = Check.Throws<OperationCanceledException>(() => boundary.CheckPath(path,
                ExpectedEntry.Any, MissingPolicy.Suffix, cancellation.Token));
            var treeError = Check.Throws<OperationCanceledException>(() => boundary.CheckTree(path,
                ExpectedEntry.Any, MissingPolicy.Suffix, cancellation.Token));
            Check.Equal(cancellation.Token, pathError.CancellationToken);
            Check.Equal(cancellation.Token, treeError.CancellationToken);
            Check.True(!UnsafePathException.IsUnsafe(pathError), "Cancellation was converted to an unsafe stop.");
            Check.True(!UnsafePathException.IsUnsafe(treeError), "Tree cancellation was converted to an unsafe stop.");
        }
    });

    private static void RejectsInspectionAndEnumerationErrorsWhenPermissionsPermit() => WithTree(tree =>
    {
        var root = tree.Dir("root");
        var blockedSearch = tree.Dir("root/no-search");
        var inaccessibleFile = tree.File("root/no-search/child", "bytes");
        var blockedRead = tree.Dir("root/no-read");
        tree.File("root/no-read/child", "bytes");
        var boundary = new FileSystemBoundary(root, tree.Root);
        var before = tree.Capture(tree.Root);
        var searchMode = System.IO.File.GetUnixFileMode(blockedSearch);
        var readMode = System.IO.File.GetUnixFileMode(blockedRead);
        Check.WithCleanup(() =>
        {
            System.IO.File.SetUnixFileMode(blockedSearch, UnixFileMode.None);
            if (ProbeDeniesAccess(() => System.IO.File.GetAttributes(inaccessibleFile)))
            {
                var error = Check.Throws<UnsafePathException>(() => boundary.CheckPath(inaccessibleFile,
                    ExpectedEntry.Any, MissingPolicy.Suffix));
                Check.True(error.InnerException is UnauthorizedAccessException, "Inspection error evidence was lost.");
                Check.Throws<UnsafePathException>(() => boundary.CheckTree(inaccessibleFile,
                    ExpectedEntry.Any, MissingPolicy.Suffix));
            }
            else
                Console.WriteLine("FileSystemBoundaryCases: unexercised inspection denial; current permissions bypass mode restriction.");

            // Search is allowed so checked metadata can succeed, but directory
            // inventory is unreadable for a process without a permission bypass.
            System.IO.File.SetUnixFileMode(blockedRead, UnixFileMode.UserExecute);
            Check.Equal(EntryKind.Directory, boundary.CheckPath(blockedRead, ExpectedEntry.Directory).Kind);
            if (ProbeDeniesAccess(() => Directory.EnumerateFileSystemEntries(blockedRead, "*", SearchOption.TopDirectoryOnly).ToArray()))
            {
                var error = Check.Throws<UnsafePathException>(() => boundary.CheckTree(blockedRead, ExpectedEntry.Directory));
                Check.True(error.InnerException is UnauthorizedAccessException, "Enumeration error evidence was lost.");
            }
            else
                Console.WriteLine("FileSystemBoundaryCases: unexercised enumeration denial; current permissions bypass mode restriction.");
        }, () => Check.WithCleanup(
            () => System.IO.File.SetUnixFileMode(blockedSearch, searchMode),
            () => System.IO.File.SetUnixFileMode(blockedRead, readMode)));
        tree.AssertUnchanged(before);
    });

    private static bool ProbeDeniesAccess(Action probe)
    {
        try
        {
            probe();
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }
}
