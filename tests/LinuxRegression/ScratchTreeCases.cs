namespace LinuxRegression;

// Written behavioral fixtures only: the local Python checks inspect this source,
// not the filesystem behavior of these C# helpers.
internal static class ScratchTreeCases
{
    internal static async Task RunAsync()
    {
        DetectsFileBytesAttributesAndModes();
        DetectsDirectoryModesAndInventories();
        CapturesAbsenceAndOwnsSnapshots();
        RejectsEscapingAndLinkedMutationPaths();
        CapturesLinkMetadataWithoutTargetModes();
        FailedBodyStillUnlinksRegisteredAndUnregisteredLinks();
        PreservesAssertionAndRealDisposalFailures();
        RestoresCurrentDirectoryBeforeDisposal();
        await PreservesAsyncAssertionAndCleanupFailures();
    }

    private static void WithTree(Action<ScratchTree> body)
    {
        var tree = new ScratchTree();
        Check.WithCleanup(() => body(tree), tree.Dispose);
    }

    private static void DetectsFileBytesAttributesAndModes() => WithTree(tree =>
    {
        var path = tree.File("bytes.bin", "initial");
        var bytes = new byte[] { 0, 1, 127, 255 };
        System.IO.File.WriteAllBytes(path, bytes);
        var snapshot = tree.Capture(path);
        System.IO.File.WriteAllBytes(path, [0, 2, 127, 255]);
        Check.Throws<InvalidOperationException>(() => tree.AssertUnchanged(snapshot));
        System.IO.File.WriteAllBytes(path, bytes);
        tree.AssertUnchanged(snapshot);

        var mode = System.IO.File.GetUnixFileMode(path);
        var attributes = System.IO.File.GetAttributes(path);
        Check.WithCleanup(() =>
        {
            System.IO.File.SetUnixFileMode(path, mode ^ UnixFileMode.GroupExecute);
            Check.Throws<InvalidOperationException>(() => tree.AssertUnchanged(snapshot));
        }, () => System.IO.File.SetUnixFileMode(path, mode));
        tree.AssertUnchanged(snapshot);
        Check.WithCleanup(() =>
        {
            System.IO.File.SetAttributes(path, attributes ^ FileAttributes.ReadOnly);
            Check.Throws<InvalidOperationException>(() => tree.AssertUnchanged(snapshot));
        }, () =>
        {
            System.IO.File.SetAttributes(path, attributes);
            System.IO.File.SetUnixFileMode(path, mode);
        });
        tree.AssertUnchanged(snapshot);
    });

    private static void DetectsDirectoryModesAndInventories() => WithTree(tree =>
    {
        var directory = tree.Dir("inventory/nested");
        tree.File("inventory/keep.txt", "kept");
        var snapshot = tree.Capture(Path.Join(tree.Root, "inventory"));
        var mode = System.IO.File.GetUnixFileMode(directory);
        Check.WithCleanup(() =>
        {
            System.IO.File.SetUnixFileMode(directory, mode ^ UnixFileMode.GroupExecute);
            Check.Throws<InvalidOperationException>(() => tree.AssertUnchanged(snapshot));
        }, () => System.IO.File.SetUnixFileMode(directory, mode));
        tree.AssertUnchanged(snapshot);

        var extraDirectory = tree.Dir("inventory/added-empty");
        Check.Throws<InvalidOperationException>(() => tree.AssertUnchanged(snapshot));
        Directory.Delete(extraDirectory);
        var extraFile = tree.File("inventory/nested/added.txt", "added");
        Check.Throws<InvalidOperationException>(() => tree.AssertUnchanged(snapshot));
        System.IO.File.Delete(extraFile);
        tree.AssertUnchanged(snapshot);
    });

    private static void CapturesAbsenceAndOwnsSnapshots() => WithTree(tree =>
    {
        var absent = tree.Capture(Path.Join(tree.Root, "absent", "leaf"));
        tree.AssertUnchanged(absent);
        tree.File("absent/leaf", "appeared");
        Check.Throws<InvalidOperationException>(() => tree.AssertUnchanged(absent));
        WithTree(other =>
        {
            var foreign = other.Capture(other.Root);
            Check.Throws<InvalidOperationException>(() => tree.AssertUnchanged(foreign));
            Check.Throws<ArgumentException>(() => tree.Capture(other.Root));
        });
        Check.Throws<ArgumentException>(() => tree.Capture("relative/path"));
    });

    private static void RejectsEscapingAndLinkedMutationPaths() => WithTree(outside =>
    {
        var sentinel = outside.File("keep.txt", "unchanged");
        var before = outside.Capture(outside.Root);
        WithTree(tree =>
        {
            foreach (var bad in new[]
            {
                "../escape", "nested/../../escape", "./dot", ".", "", tree.Root,
                Path.Join(outside.Root, "wrong-root"), tree.Root + "-sibling/escape",
            })
            {
                Check.Throws<ArgumentException>(() => tree.Dir(bad));
                Check.Throws<ArgumentException>(() => tree.File(bad, "bad"));
                Check.Throws<ArgumentException>(() => tree.LinkFile(bad, sentinel));
                Check.Throws<ArgumentException>(() => tree.LinkDirectory(bad, outside.Root));
            }

            tree.LinkDirectory("registered", outside.Root);
            Directory.CreateSymbolicLink(Path.Join(tree.Root, "unregistered"), outside.Root);
            tree.LinkDirectory("dangling-parent", Path.Join(outside.Root, "absent"));
            tree.File("file-parent", "not a directory");
            foreach (var parent in new[] { "registered", "unregistered", "dangling-parent", "file-parent" })
            {
                Check.Throws<IOException>(() => tree.Dir(parent + "/new-directory"));
                Check.Throws<IOException>(() => tree.File(parent + "/keep.txt", "bad"));
                Check.Throws<IOException>(() => tree.LinkFile(parent + "/new-link", sentinel));
                Check.Throws<IOException>(() => tree.LinkDirectory(parent + "/new-link", outside.Root));
                Check.Throws<IOException>(() => tree.Capture(Path.Join(tree.Root, parent, "keep.txt")));
            }

            tree.LinkFile("linked-leaf", sentinel);
            Check.Throws<IOException>(() => tree.File("linked-leaf", "bad"));
            Check.Throws<IOException>(() => tree.Dir("registered"));
            Check.Throws<IOException>(() => tree.LinkFile("linked-leaf", sentinel));
            Check.Throws<IOException>(() => tree.LinkDirectory("registered", outside.Root));
        });
        outside.AssertUnchanged(before);
        Check.Equal("unchanged", System.IO.File.ReadAllText(sentinel));
    });

    private static void CapturesLinkMetadataWithoutTargetModes() => WithTree(outside =>
    {
        var target = outside.File("target.txt", "target");
        var otherTarget = outside.File("other.txt", "target");
        var directory = outside.Dir("target-directory");
        var before = outside.Capture(outside.Root);
        WithTree(inside =>
        {
            var link = inside.LinkFile("file-link", target);
            inside.LinkDirectory("directory-link", directory);
            inside.LinkFile("dangling-file", Path.Join(outside.Root, "missing-file"));
            inside.LinkDirectory("dangling-directory", Path.Join(outside.Root, "missing-directory"));
            var snapshot = inside.Capture(inside.Root);
            var fileMode = System.IO.File.GetUnixFileMode(target);
            var directoryMode = System.IO.File.GetUnixFileMode(directory);
            Check.WithCleanup(() =>
            {
                System.IO.File.SetUnixFileMode(target, fileMode ^ UnixFileMode.GroupExecute);
                System.IO.File.SetUnixFileMode(directory, directoryMode ^ UnixFileMode.GroupExecute);
                inside.AssertUnchanged(snapshot);
                Check.Throws<InvalidOperationException>(() => outside.AssertUnchanged(before));
            }, () =>
            {
                System.IO.File.SetUnixFileMode(target, fileMode);
                System.IO.File.SetUnixFileMode(directory, directoryMode);
            });
            inside.AssertUnchanged(snapshot);

            System.IO.File.Delete(link);
            System.IO.File.CreateSymbolicLink(link, otherTarget);
            Check.Throws<InvalidOperationException>(() => inside.AssertUnchanged(snapshot));
        });
        outside.AssertUnchanged(before);
    });

    private static void FailedBodyStillUnlinksRegisteredAndUnregisteredLinks() => WithTree(outside =>
    {
        var sentinel = outside.File("keep.txt", "unchanged");
        outside.File("nested/keep.bin", "also unchanged");
        var before = outside.Capture(outside.Root);
        var inside = new ScratchTree();
        // Even setup failure must dispose this independently owned inner tree.
        var failure = Check.Throws<InvalidOperationException>(() => Check.WithCleanup(() =>
        {
            inside.LinkDirectory("external", outside.Root);
            inside.LinkFile("external-file", sentinel);
            Directory.CreateSymbolicLink(Path.Join(inside.Root, "unregistered-directory"), outside.Root);
            System.IO.File.CreateSymbolicLink(Path.Join(inside.Root, "unregistered-file"), sentinel);
            Directory.CreateSymbolicLink(Path.Join(inside.Root, "unregistered-dangling"),
                Path.Join(outside.Root, "absent"));
            Check.True(false, "deliberate assertion");
        }, inside.Dispose));
        Check.Equal("deliberate assertion", failure.Message);
        inside.Dispose(); // Successful disposal is idempotent.
        Check.Throws<IOException>(() => System.IO.File.GetAttributes(inside.Root));
        outside.AssertUnchanged(before);
        Check.Equal("unchanged", System.IO.File.ReadAllText(sentinel));
    });

    private static void PreservesAssertionAndRealDisposalFailures() => WithTree(outside =>
    {
        outside.File("keep.txt", "unchanged");
        var before = outside.Capture(outside.Root);
        var inside = new ScratchTree();
        var rootRemoved = false;
        var rootLinked = false;
        Check.WithCleanup(() =>
        {
            Directory.Delete(inside.Root); // Empty, owned root; force an ownership failure.
            rootRemoved = true;
            Directory.CreateSymbolicLink(inside.Root, outside.Root);
            rootLinked = true;
            var assertion = new InvalidOperationException("original assertion");
            var both = Check.Throws<AggregateException>(() => Check.WithCleanup(
                () => throw assertion, inside.Dispose));
            Check.Equal(2, both.InnerExceptions.Count);
            Check.True(ReferenceEquals(assertion, both.InnerExceptions[0]), "Original assertion was replaced.");
            Check.True(both.InnerExceptions[1] is IOException, "Ownership cleanup failure was not preserved.");
            outside.AssertUnchanged(before);
        }, () =>
        {
            // Recovery is explicit fixture repair, never a Dispose fallback.
            if (rootLinked)
                Directory.Delete(inside.Root);
            if (rootRemoved)
                Directory.CreateDirectory(inside.Root);
            inside.Dispose();
        });
        outside.AssertUnchanged(before);

        var cleanup = new IOException("cleanup only");
        var observed = Check.Throws<IOException>(() => Check.WithCleanup(() => { }, () => throw cleanup));
        Check.True(ReferenceEquals(cleanup, observed), "Cleanup-only failure changed identity.");
        var bodyOnly = new InvalidOperationException("body only");
        var bodyObserved = Check.Throws<InvalidOperationException>(() =>
            Check.WithCleanup(() => throw bodyOnly, () => { }));
        Check.True(ReferenceEquals(bodyOnly, bodyObserved), "Body-only failure changed identity.");
    });

    private static void RestoresCurrentDirectoryBeforeDisposal()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var tree = new ScratchTree();
        Check.WithCleanup(() =>
        {
            var working = tree.Dir("working");
            Directory.SetCurrentDirectory(working);
            Check.Equal(working, Directory.GetCurrentDirectory());
        }, () =>
        {
            Directory.SetCurrentDirectory(originalDirectory);
            tree.Dispose();
        });
        Check.Equal(originalDirectory, Directory.GetCurrentDirectory());
    }

    private static async Task PreservesAsyncAssertionAndCleanupFailures()
    {
        var assertion = new InvalidOperationException("async assertion");
        var cleanup = new IOException("async cleanup");
        var both = await Check.ThrowsAsync<AggregateException>(() => Check.WithCleanupAsync(
            () => Task.FromException(assertion), () => throw cleanup));
        Check.Equal(2, both.InnerExceptions.Count);
        Check.True(ReferenceEquals(assertion, both.InnerExceptions[0]), "Async assertion was replaced.");
        Check.True(ReferenceEquals(cleanup, both.InnerExceptions[1]), "Async cleanup failure was replaced.");
        Check.SequenceEqual(new Exception[] { assertion, cleanup }, both.InnerExceptions);
    }
}
