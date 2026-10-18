using System.Text;
using Stalker.Gamma.Filesystem;
using Stalker.Gamma.GammaInstallerServices;
using Stalker.Gamma.Models;
using Stalker.Gamma.Utilities;

namespace LinuxRegression;

// Production-C# behavioral fixtures, written but NOT compiled or executed in
// this batch. Python checks inspect wiring only; they do not run this policy.
internal static class ManagedTreeCases
{
    internal static async Task RunAsync()
    {
        CopyRejectsLinksBeforeCallbacksHashOrCreation();
        CopyRejectsLinkedRootsParentsLeavesAndCycles();
        FiltersGitAndRetainedContentCannotHideLinks();
        MappedTypeConflictsPrecedeCallbacksAndMoves();
        IdenticalPathsAreCheckedNoOpsAndDescendantsReject();
        AncestorFlattenConsumesShallowerFilesFirst();
        SelectionPreservesFilterOverwriteAndNonemptyDirectories();
        PlanUsesCanonicalPathsAndOneSelectionSnapshot();
        CleanupPreservesItsOriginalSelection();
        CleanSharesPreflightBeforeChangingAnyModes();
        InspectionDenialIsNotSkippedAsAPermissionMutationFailure();
        InstructionsValidateTheWholeOrderedListBeforeMoving();
        InstructionsPrepareKnownTypesAndFutureConflicts();
        InstructionsRejectProducedSourceConflictsBeforeAnyMove();
        InstructionsReprepareSourcesProducedByEarlierSteps();
        InstructionsComposeSuccessiveProducedSources();
        InstructionsReuseConsumedProducedFilePathsAsDirectories();
        InstructionsStillRejectLiveProducedFileDirectoryConflicts();
        InstructionsChooseDestinationFromProducedGamedata();
        InstructionsDoNotProjectConsumedOrExcludedContents();
        EmptyAndBlankInstructionsCreateNothingButLiteralZeroExecutes();
        CancellationPrecedesNoOpsAndFollowsEveryCallback();
        NearIoRechecksRejectCallbackIntroducedLinks();
        MetadataGuardsAndPayloadArePreserved();
        await SeparatorGuardsAndPayloadArePreservedAsync();
        // Unexercised here: hostile concurrent replacement, POSIX special-file
        // classification and mid-enumeration failure injection. These fixtures
        // neither establish race resistance nor use user-owned filesystem paths.
    }

    private static void WithTree(Action<ScratchTree> body)
    {
        var tree = new ScratchTree();
        Check.WithCleanup(() => body(tree), tree.Dispose);
    }

    private static async Task WithTreeAsync(Func<ScratchTree, Task> body)
    {
        var tree = new ScratchTree();
        await Check.WithCleanupAsync(() => body(tree), tree.Dispose);
    }

    private static readonly UnixFileMode DirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        | UnixFileMode.GroupRead | UnixFileMode.GroupExecute;

    private static readonly UnixFileMode LegacyNormalizedDirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
        | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

    private static void CopyRejectsLinksBeforeCallbacksHashOrCreation() => WithTree(outside =>
    {
        var external = outside.File("keep.txt", "unchanged");
        File.SetUnixFileMode(external, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        var outsideBefore = outside.Capture(outside.Root);
        WithTree(tree =>
        {
            var source = tree.Dir("source");
            tree.File("source/safe.txt", "stay");
            tree.LinkFile("source/link.txt", external);
            File.SetUnixFileMode(source, DirectoryMode);
            var absent = Path.Join(tree.Root, "absent");
            var sourceBefore = tree.Capture(source);
            var destinationBefore = tree.Capture(absent);
            var calls = 0;
            Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, absent,
                onProgress: _ => calls++, txtProgress: _ => calls++, moveFile: true));
            Check.Equal(0, calls, "Known unsafe content must fail before callbacks/hash opens.");
            tree.AssertUnchanged(sourceBefore);
            tree.AssertUnchanged(destinationBefore);
            outside.AssertUnchanged(outsideBefore);
        });
    });

    private static void CopyRejectsLinkedRootsParentsLeavesAndCycles() => WithTree(outside =>
    {
        var externalDirectory = outside.Dir("target");
        var externalFile = outside.File("target/item.txt", "external");
        File.SetUnixFileMode(externalDirectory, DirectoryMode);
        var outsideBefore = outside.Capture(outside.Root);
        foreach (var scenario in new[]
        {
            "source-root", "source-parent", "source-file-root", "source-cycle",
            "destination-root", "destination-parent", "destination-leaf", "destination-cycle",
        })
        {
            // Each case owns an independent source/destination inventory.
            WithTree(tree =>
            {
                var source = tree.Dir("source");
                tree.File("source/item.txt", "source");
                var destination = Path.Join(tree.Root, "absent");
                switch (scenario)
                {
                    case "source-root": source = tree.LinkDirectory("linked", externalDirectory); break;
                    case "source-parent":
                        source = Path.Join(tree.LinkDirectory("linked", outside.Root), "target"); break;
                    case "source-file-root": source = tree.LinkFile("linked", externalFile); break;
                    case "source-cycle":
                        source = tree.LinkDirectory("cycle", Path.Join(tree.Root, "cycle")); break;
                    case "destination-root": destination = tree.LinkDirectory("linked", externalDirectory); break;
                    case "destination-parent":
                        destination = Path.Join(tree.LinkDirectory("linked", externalDirectory), "new"); break;
                    case "destination-leaf":
                        destination = tree.Dir("destination");
                        tree.LinkFile("destination/item.txt", externalFile); break;
                    case "destination-cycle":
                        destination = tree.LinkDirectory("cycle", Path.Join(tree.Root, "cycle")); break;
                }
                var before = tree.Capture(tree.Root);
                var absentBefore = tree.Capture(Path.Join(tree.Root, "absent"));
                Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, destination, moveFile: true));
                tree.AssertUnchanged(before);
                tree.AssertUnchanged(absentBefore);
                outside.AssertUnchanged(outsideBefore);
            });
        }
    });

    private static void FiltersGitAndRetainedContentCannotHideLinks() => WithTree(outside =>
    {
        var external = outside.File("keep", "unchanged");
        var outsideBefore = outside.Capture(outside.Root);
        foreach (var linkName in new[] { "skip.txt", ".git/hidden", "gamedata/retained/link", "junk/removed/link" })
        {
            WithTree(tree =>
            {
                var source = tree.Dir("source");
                tree.File("source/safe.txt", "stay");
                tree.LinkFile("source/" + linkName, external);
                File.SetUnixFileMode(source, DirectoryMode);
                var absent = Path.Join(tree.Root, "absent");
                var before = tree.Capture(tree.Root);
                var absentBefore = tree.Capture(absent);
                Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, absent, fileFilter: "skip.txt"));
                Check.Throws<UnsafePathException>(() => DirUtils.CountFiles(source, "skip.txt"));
                Check.Throws<UnsafePathException>(() => DirUtils.NormalizePermissions(source));
                Check.Throws<UnsafePathException>(() => DirUtils.RecursivelyDeleteDirectory(source, ["gamedata"]));
                Check.Throws<UnsafePathException>(() => CleanExtractPath.Clean(source));
                Check.Throws<UnsafePathException>(() => ProcessInstructions.Process(source, [""], default));
                tree.AssertUnchanged(before);
                tree.AssertUnchanged(absentBefore);
                outside.AssertUnchanged(outsideBefore);
            });
        }
        WithTree(tree =>
        {
            var source = tree.Dir("source.git-cache");
            tree.LinkFile("source.git-cache/hidden", external);
            var before = tree.Capture(tree.Root);
            Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, Path.Join(tree.Root, "absent")));
            Check.Throws<UnsafePathException>(() => DirUtils.CountFiles(source, null));
            tree.AssertUnchanged(before);
        });
        WithTree(tree =>
        {
            var source = tree.Dir("source");
            tree.File("source/item", "source");
            var destination = tree.Dir("destination");
            tree.File("destination/item", "keep with overwrite=false");
            tree.LinkFile("destination/unselected/hidden", external);
            var before = tree.Capture(tree.Root);
            Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, destination, overwrite: false));
            tree.AssertUnchanged(before);
        });
        outside.AssertUnchanged(outsideBefore);
    });

    private static void MappedTypeConflictsPrecedeCallbacksAndMoves()
    {
        foreach (var sourceIsFile in new[] { true, false })
        {
            WithTree(tree =>
            {
                var source = tree.Dir("source");
                tree.File("source/safe.txt", "must not move");
                var destination = tree.Dir("destination");
                if (sourceIsFile)
                {
                    tree.File("source/conflict", "file");
                    tree.Dir("destination/conflict");
                }
                else
                {
                    tree.File("source/conflict/child", "file");
                    tree.File("destination/conflict", "not a directory");
                }
                File.SetUnixFileMode(source, DirectoryMode);
                File.SetUnixFileMode(destination, DirectoryMode);
                var before = tree.Capture(tree.Root);
                var absent = tree.Capture(Path.Join(destination, "safe.txt"));
                var calls = 0;
                // Direct preparation plus zero callbacks proves the phase
                // boundary; merely naming the conflict last would not do so.
                Check.Throws<UnsafePathException>(() => DirUtils.PrepareCopy(source, destination, true, null, true, default));
                Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, destination,
                    onProgress: _ => calls++, txtProgress: _ => calls++, moveFile: true));
                Check.Equal(0, calls);
                tree.AssertUnchanged(before);
                tree.AssertUnchanged(absent);
            });
        }
    }

    private static void IdenticalPathsAreCheckedNoOpsAndDescendantsReject() => WithTree(tree =>
    {
        var source = tree.Dir("source");
        tree.Dir("source/empty");
        tree.File("source/item", "unchanged");
        File.SetUnixFileMode(source, DirectoryMode);
        var before = tree.Capture(source);
        var calls = 0;
        foreach (var move in new[] { false, true })
            DirUtils.CopyDirectory(source, Path.Join(source, "empty", ".."), moveFile: move,
                onProgress: _ => calls++, txtProgress: _ => calls++);
        Check.Equal(0, calls);
        tree.AssertUnchanged(before);
        foreach (var destination in new[] { Path.Join(source, "new"), Path.Join(source, "empty") })
        {
            var destinationBefore = tree.Capture(destination);
            Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, destination, moveFile: true));
            tree.AssertUnchanged(destinationBefore);
            tree.AssertUnchanged(before);
        }
        tree.LinkDirectory("source/cycle", source);
        var linkedBefore = tree.Capture(source);
        Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, source));
        tree.AssertUnchanged(linkedBefore);
    });

    private static void AncestorFlattenConsumesShallowerFilesFirst()
    {
        foreach (var move in new[] { false, true })
        {
            WithTree(tree =>
            {
                var destination = tree.Dir("destination");
                var source = tree.Dir("destination/nest");
                tree.File("destination/nest/item", "shallower-original");
                tree.File("destination/nest/nest/item", "deeper-original");
                var deepest = tree.File("destination/nest/nest/nest/item", "deepest-original");
                File.SetUnixFileMode(destination, DirectoryMode);
                File.SetUnixFileMode(source, DirectoryMode);
                var plan = DirUtils.PrepareCopy(source, destination, true, null, move, default);
                Check.Equal(3, plan.TotalFiles);
                DirUtils.ExecuteCopy(plan, null, null, default);
                Check.Equal("shallower-original", File.ReadAllText(Path.Join(destination, "item")));
                Check.Equal("deeper-original", File.ReadAllText(Path.Join(destination, "nest", "item")));
                Check.Equal("deepest-original", File.ReadAllText(Path.Join(destination, "nest", "nest", "item")));
                Check.Equal(!move, File.Exists(deepest));
                Check.Equal(DirectoryMode, File.GetUnixFileMode(source));
                Check.Equal(DirectoryMode, File.GetUnixFileMode(destination));
            });
        }
    }

    private static void SelectionPreservesFilterOverwriteAndNonemptyDirectories() => WithTree(tree =>
    {
        var source = tree.Dir("source");
        tree.File("source/existing.txt", "source");
        tree.File("source/new.txt", "new");
        tree.File("source/skip.txt", "filtered");
        tree.File(@"source/native\name.txt", "native Linux name");
        tree.File("source/nested/kept.txt", "nested");
        tree.File("source/filtered-only/skip.txt", "filtered but nonempty");
        tree.File("source/.git/hidden", "excluded");
        tree.File("source/prefix.git-cache/hidden", "excluded substring");
        tree.Dir("source/empty");
        tree.Dir("source/nested/empty");
        var destination = tree.Dir("source-sibling");
        tree.File("source-sibling/existing.txt", "destination");
        File.SetUnixFileMode(source, DirectoryMode);
        File.SetUnixFileMode(destination, DirectoryMode);
        var before = tree.Capture(source);
        var progress = new List<double>();
        Check.Equal(4, DirUtils.CountFiles(source, "skip.txt"));
        DirUtils.CopyDirectory(source, destination, overwrite: false, fileFilter: "skip.txt", onProgress: progress.Add);
        Check.Equal("destination", File.ReadAllText(Path.Join(destination, "existing.txt")));
        Check.Equal("new", File.ReadAllText(Path.Join(destination, "new.txt")));
        Check.Equal("native Linux name", File.ReadAllText(Path.Join(destination, @"native\name.txt")));
        Check.Equal("nested", File.ReadAllText(Path.Join(destination, "nested", "kept.txt")));
        Check.True(Directory.Exists(Path.Join(destination, "filtered-only")), "Filtered-only directory was originally nonempty.");
        foreach (var absent in new[] { "skip.txt", "filtered-only/skip.txt", ".git", "prefix.git-cache", "empty", "nested/empty" })
            Check.True(!Path.Exists(Path.Join(destination, absent)), $"Unexpected selected output: {absent}");
        Check.SequenceEqual(new[] { 0.25, 0.5, 0.75 }, progress);
        Check.Equal(DirectoryMode, File.GetUnixFileMode(destination));
        tree.AssertUnchanged(before);
        DirUtils.CopyDirectory(source, destination, overwrite: true, fileFilter: "skip.txt");
        Check.Equal("source", File.ReadAllText(Path.Join(destination, "existing.txt")));
        var excludedRoot = tree.Dir("excluded.git-cache");
        tree.File("excluded.git-cache/item", "not selected");
        var absentOutput = tree.Capture(Path.Join(tree.Root, "excluded-output"));
        DirUtils.CopyDirectory(excludedRoot, Path.Join(tree.Root, "excluded-output"));
        Check.Equal(0, DirUtils.CountFiles(excludedRoot, null));
        tree.AssertUnchanged(absentOutput);
    });

    private static void PlanUsesCanonicalPathsAndOneSelectionSnapshot() => WithTree(tree =>
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        Check.WithCleanup(() =>
        {
            var source = tree.Dir("source");
            tree.File("source/first", "first");
            tree.File("source/second", "second");
            var differentCwd = tree.Dir("different-cwd");
            Directory.SetCurrentDirectory(tree.Root);
            var plan = DirUtils.PrepareCopy("source", "destination", true, null, false, default);
            var changed = false;
            var progress = new List<double>();
            DirUtils.ExecuteCopy(plan, progress.Add, _ =>
            {
                if (changed) return;
                changed = true;
                tree.File("source/late-file", "not in the operation snapshot");
                Directory.SetCurrentDirectory(differentCwd);
            }, default);
            Check.True(changed, "Callback was not reached.");
            Check.Equal(2, plan.TotalFiles);
            Check.SequenceEqual(new[] { 0.5, 1.0 }, progress);
            Check.Equal("first", File.ReadAllText(Path.Join(tree.Root, "destination", "first")));
            Check.True(!Path.Exists(Path.Join(tree.Root, "destination", "late-file")), "Execution re-enumerated its source.");
            Check.True(!Path.Exists(Path.Join(differentCwd, "destination")), "I/O reused a relative input after CWD changed.");
            Check.Equal(source, plan.SourceBoundary.Root);
        }, () => Directory.SetCurrentDirectory(originalDirectory));
    });

    private static void CleanupPreservesItsOriginalSelection() => WithTree(tree =>
    {
        var root = tree.Dir("extract");
        tree.File("extract/.dot", "remove");
        var keepFile = tree.File("extract/readme.txt", "retain top-level ordinary file");
        tree.File("extract/junk/deep/.dot", "remove subtree");
        tree.File("extract/junk/deep/item", "remove subtree");
        var retained = tree.Dir("extract/GAMEDATA");
        tree.File("extract/GAMEDATA/.retained", "keep even dot files below retained dirs");
        File.SetUnixFileMode(root, DirectoryMode);
        File.SetUnixFileMode(retained, DirectoryMode);
        var retainedBefore = tree.Capture(retained);
        var fileBefore = tree.Capture(keepFile);
        DirUtils.RecursivelyDeleteDirectory(root, ["gamedata"]);
        Check.SequenceEqual(new[] { "GAMEDATA", "readme.txt" },
            Directory.GetFileSystemEntries(root).Select(path => Path.GetFileName(path)).OrderBy(name => name, StringComparer.Ordinal));
        Check.True(!Path.Exists(Path.Join(root, "junk")), "Selected subtree remains.");
        Check.True(!Path.Exists(Path.Join(root, ".dot")), "Selected dot file remains.");
        Check.Equal(DirectoryMode, File.GetUnixFileMode(root));
        tree.AssertUnchanged(retainedBefore);
        tree.AssertUnchanged(fileBefore);
    });

    private static void CleanSharesPreflightBeforeChangingAnyModes() => WithTree(outside =>
    {
        var external = outside.File("keep", "unchanged");
        var outsideBefore = outside.Capture(outside.Root);
        WithTree(tree =>
        {
            var root = tree.Dir("extract");
            var retained = tree.Dir("extract/gamedata");
            var removable = tree.Dir("extract/junk");
            tree.File("extract/.remove", "stay on failure");
            tree.LinkFile("extract/gamedata/retained-link", external);
            foreach (var directory in new[] { root, retained, removable })
                File.SetUnixFileMode(directory, DirectoryMode);
            var before = tree.Capture(root);
            Check.Throws<UnsafePathException>(() => CleanExtractPath.Clean(root));
            tree.AssertUnchanged(before); // includes all directory modes/inventories
            outside.AssertUnchanged(outsideBefore);
        });
        WithTree(tree =>
        {
            var root = tree.Dir("extract");
            var directories = new[] { root, tree.Dir("extract/gamedata"), tree.Dir("extract/appdata"),
                tree.Dir("extract/db"), tree.Dir("extract/fomod") };
            foreach (var directory in directories) File.SetUnixFileMode(directory, DirectoryMode);
            var retained = tree.File("extract/gamedata/keep", "keep");
            File.SetAttributes(retained, File.GetAttributes(retained) | FileAttributes.ReadOnly);
            tree.File("extract/junk/deep/item", "delete");
            tree.File("extract/.delete", "delete");
            tree.File("extract/readme", "keep");
            CleanExtractPath.Clean(root);
            foreach (var directory in directories)
                Check.Equal(LegacyNormalizedDirectoryMode, File.GetUnixFileMode(directory), "Mode policy is deliberately unchanged.");
            Check.True((File.GetAttributes(retained) & FileAttributes.ReadOnly) == 0, "Retained file attributes were not normalized.");
            Check.Equal("keep", File.ReadAllText(retained));
            Check.True(!Path.Exists(Path.Join(root, "junk")), "Cleanup did not use the selected checked entries.");
            Check.True(!Path.Exists(Path.Join(root, ".delete")), "Dot file remains.");
            Check.True(File.Exists(Path.Join(root, "readme")), "Ordinary root file was removed.");
        });
        WithTree(tree =>
        {
            var absent = Path.Join(tree.Root, "missing", "extract");
            var before = tree.Capture(absent);
            CleanExtractPath.Clean(absent);
            DirUtils.NormalizePermissions(absent);
            tree.AssertUnchanged(before);
        });
    });

    private static void InspectionDenialIsNotSkippedAsAPermissionMutationFailure() => WithTree(tree =>
    {
        var root = tree.Dir("extract");
        var blocked = tree.Dir("extract/gamedata/blocked");
        tree.File("extract/gamedata/blocked/item", "must remain");
        tree.File("extract/.remove", "must remain");
        File.SetUnixFileMode(root, DirectoryMode);
        var originalMode = File.GetUnixFileMode(blocked);
        var before = tree.Capture(root);
        Check.WithCleanup(() =>
        {
            File.SetUnixFileMode(blocked, UnixFileMode.UserExecute);
            try
            {
                Directory.EnumerateFileSystemEntries(blocked, "*", SearchOption.TopDirectoryOnly).ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                Check.Throws<UnsafePathException>(() => DirUtils.NormalizePermissions(root));
                Check.Throws<UnsafePathException>(() => CleanExtractPath.Clean(root));
                return;
            }
            Console.WriteLine("ManagedTreeCases: unexercised enumeration denial; current permissions bypass mode restriction.");
        }, () => File.SetUnixFileMode(blocked, originalMode));
        tree.AssertUnchanged(before);
    });

    private static void InstructionsValidateTheWholeOrderedListBeforeMoving()
    {
        foreach (var second in new string?[] { "../escape", "\t", null })
        {
            WithTree(tree =>
            {
                var root = tree.Dir("extract");
                tree.File("extract/first/safe", "must not move");
                var before = tree.Capture(root);
                var absent = tree.Capture(Path.Join(root, "gamedata"));
                IList<string> instructions = new List<string> { "first", second! };
                Check.Throws<UnsafePathException>(() => ProcessInstructions.Process(root, instructions, default));
                tree.AssertUnchanged(before);
                tree.AssertUnchanged(absent);
            });
        }
    }

    private static void InstructionsPrepareKnownTypesAndFutureConflicts()
    {
        foreach (var scenario in new[] { "source-file", "gamedata-file", "mapped-directory", "mapped-file", "future-target", "future-source" })
        {
            WithTree(tree =>
            {
                var root = tree.Dir("extract");
                tree.File("extract/first/safe", "must not move");
                switch (scenario)
                {
                    case "source-file": tree.File("extract/second", "not a directory"); break;
                    case "gamedata-file": tree.File("extract/second/gamedata", "not a directory"); break;
                    case "mapped-directory":
                        tree.File("extract/second/conflict", "file"); tree.Dir("extract/gamedata/conflict"); break;
                    case "mapped-file":
                        tree.File("extract/second/conflict/child", "file"); tree.File("extract/gamedata/conflict", "not a directory"); break;
                    case "future-target":
                        tree.File("extract/first/conflict", "future file");
                        tree.File("extract/second/conflict/child", "future directory"); break;
                    case "future-source":
                        tree.File("extract/first/second", "would create a file at the later selected source"); break;
                }
                File.SetUnixFileMode(root, DirectoryMode);
                var before = tree.Capture(root);
                var absent = tree.Capture(Path.Join(root, "gamedata", "safe"));
                IList<string> instructions = scenario == "future-source" ? ["first", "gamedata/second"] : ["first", "second"];
                // IList order, not filesystem enumeration order, makes the
                // first valid move precede a known-invalid second instruction.
                Check.Throws<UnsafePathException>(() => ProcessInstructions.Process(root, instructions, default));
                tree.AssertUnchanged(before);
                tree.AssertUnchanged(absent);
            });
        }
    }

    private static void InstructionsRejectProducedSourceConflictsBeforeAnyMove()
    {
        foreach (var composed in new[] { false, true })
        foreach (var sourceIsFile in new[] { true, false })
        {
            WithTree(tree =>
            {
                var root = tree.Dir("extract");
                // The two-step case is the review's exact ordinary-tree example;
                // the three-step case requires composing another produced source.
                var later = composed ? "stage-a/stage-b" : "later";
                var source = tree.Dir("extract/package/gamedata/" + later);
                var gamedata = tree.Dir("extract/gamedata");
                string file;
                string keep;
                string conflictDirectory;
                if (sourceIsFile)
                {
                    file = tree.File("extract/package/gamedata/" + later + "/conflict", "file");
                    keep = tree.File("extract/gamedata/conflict/keep", "keep");
                    conflictDirectory = Path.Join(gamedata, "conflict");
                }
                else
                {
                    file = tree.File("extract/package/gamedata/" + later + "/conflict/child", "file");
                    keep = tree.File("extract/gamedata/conflict", "not a directory");
                    conflictDirectory = Path.Join(source, "conflict");
                }
                foreach (var directory in new[] { root, source, gamedata, conflictDirectory })
                    File.SetUnixFileMode(directory, DirectoryMode);
                foreach (var member in new[] { file, keep })
                    File.SetUnixFileMode(member, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
                var before = tree.Capture(root); // all bytes, inventories, file AND directory modes
                IList<string> instructions = composed
                    ? ["package", "gamedata/stage-a", "gamedata/stage-b"]
                    : ["package", "gamedata/later"];
                Check.Throws<UnsafePathException>(() => ProcessInstructions.Process(root, instructions, default));
                tree.AssertUnchanged(before);
            });
        }
    }

    private static void InstructionsReprepareSourcesProducedByEarlierSteps() => WithTree(tree =>
    {
        var root = tree.Dir("extract");
        tree.File("extract/package/gamedata/later/data.txt", "produced by the first step");
        ProcessInstructions.Process(root, ["package", "gamedata/later"], default);
        Check.Equal("produced by the first step", File.ReadAllText(Path.Join(root, "gamedata", "data.txt")));
        Check.True(!File.Exists(Path.Join(root, "gamedata", "later", "data.txt")), "Later source was not freshly prepared/moved.");
        Check.True(!File.Exists(Path.Join(root, "package", "gamedata", "later", "data.txt")), "First source was not moved.");
        tree.File("extract/ordinary/extra.txt", "ordinary source goes into gamedata");
        ProcessInstructions.Process(root, ["ordinary"], default);
        Check.Equal("ordinary source goes into gamedata", File.ReadAllText(Path.Join(root, "gamedata", "extra.txt")));
    });

    private static void InstructionsComposeSuccessiveProducedSources() => WithTree(tree =>
    {
        var root = tree.Dir("extract");
        tree.File("extract/package/gamedata/stage-a/stage-b/data.txt", "composed across three moves");
        tree.File("extract/gamedata/data.txt", "overwrite this file");
        ProcessInstructions.Process(root, ["package", "gamedata/stage-a", "gamedata/stage-b"], default);
        Check.Equal("composed across three moves", File.ReadAllText(Path.Join(root, "gamedata", "data.txt")));
        foreach (var consumed in new[] { "package/gamedata/stage-a/stage-b/data.txt",
            "gamedata/stage-a/stage-b/data.txt", "gamedata/stage-b/data.txt" })
            Check.True(!File.Exists(Path.Join(root, consumed)), "A composed source file was not moved.");
    });

    private static void InstructionsReuseConsumedProducedFilePathsAsDirectories() => WithTree(tree =>
    {
        var root = tree.Dir("extract");
        tree.File("extract/package/gamedata/first/group/conflict", "first");
        tree.File("extract/package/gamedata/last/group/conflict/child", "last");
        var retained = tree.File("extract/unrelated/keep", "unchanged");
        File.SetUnixFileMode(retained, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        var retainedBefore = tree.Capture(retained);
        ProcessInstructions.Process(root, ["package", "gamedata/first", "gamedata/group", "gamedata/last"], default);
        Check.SequenceEqual(new byte[] { 0x66, 0x69, 0x72, 0x73, 0x74 },
            File.ReadAllBytes(Path.Join(root, "gamedata", "conflict")));
        var reused = Path.Join(root, "gamedata", "group", "conflict");
        Check.True(Directory.Exists(reused), "The consumed produced-file path was not reused as a directory.");
        Check.True(!File.Exists(reused), "The consumed produced-file path still names a file.");
        Check.SequenceEqual(new byte[] { 0x6c, 0x61, 0x73, 0x74 }, File.ReadAllBytes(Path.Join(reused, "child")));
        foreach (var consumed in new[] { "package/gamedata/first/group/conflict",
            "package/gamedata/last/group/conflict/child", "gamedata/first/group/conflict",
            "gamedata/last/group/conflict/child" })
            Check.True(!Path.Exists(Path.Join(root, consumed)), "An original or intermediate source file was not moved: " + consumed);
        tree.AssertUnchanged(retainedBefore);
    });

    private static void InstructionsStillRejectLiveProducedFileDirectoryConflicts() => WithTree(tree =>
    {
        var root = tree.Dir("extract");
        var first = tree.File("extract/package/gamedata/first/group/conflict", "first");
        var last = tree.File("extract/package/gamedata/last/group/conflict/child", "last");
        foreach (var directory in new[] { root, Path.GetDirectoryName(first)!, Path.GetDirectoryName(last)! })
            File.SetUnixFileMode(directory, DirectoryMode);
        foreach (var file in new[] { first, last })
            File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        var before = tree.Capture(root); // all bytes, inventories, file AND directory modes
        Check.Throws<UnsafePathException>(() => ProcessInstructions.Process(root,
            ["package", "gamedata/first", "gamedata/last"], default));
        tree.AssertUnchanged(before);
    });

    private static void InstructionsChooseDestinationFromProducedGamedata()
    {
        WithTree(tree =>
        {
            var root = tree.Dir("extract");
            tree.File("extract/package/gamedata/later/gamedata/data.txt", "produced child gamedata chooses root");
            tree.File(@"extract/package/gamedata/later/gamedata/native\é' name.txt", "native member name");
            tree.File("extract/package/gamedata/later/readme.txt", "overwrite at extraction root");
            tree.File("extract/readme.txt", "old root readme");
            var wrongDestination = tree.Dir("extract/gamedata/readme.txt");
            tree.File("extract/gamedata/readme.txt/keep", "wrong destination would conflict");
            var excluded = tree.File("extract/package/gamedata/later/gamedata/.git/hidden", "not moved");
            tree.Dir("extract/package/gamedata/later/gamedata/empty");
            var wrongBefore = tree.Capture(wrongDestination);
            var excludedBefore = tree.Capture(excluded);
            ProcessInstructions.Process(root, ["package", "gamedata/later"], default);
            Check.Equal("overwrite at extraction root", File.ReadAllText(Path.Join(root, "readme.txt")));
            Check.Equal("produced child gamedata chooses root", File.ReadAllText(Path.Join(root, "gamedata", "data.txt")));
            Check.Equal("native member name", File.ReadAllText(Path.Join(root, "gamedata", @"native\é' name.txt")));
            foreach (var absent in new[] { "gamedata/gamedata/data.txt", "gamedata/later/gamedata/data.txt",
                "package/gamedata/later/gamedata/data.txt", "gamedata/later/readme.txt",
                "package/gamedata/later/readme.txt", "gamedata/.git", "gamedata/empty" })
                Check.True(!Path.Exists(Path.Join(root, absent)), "Unexpected produced-source output: " + absent);
            tree.AssertUnchanged(wrongBefore);
            tree.AssertUnchanged(excludedBefore);
        });
        WithTree(tree =>
        {
            var root = tree.Dir("extract");
            tree.Dir("extract/package/gamedata/later/gamedata"); // empty child is NOT mapped
            tree.File("extract/package/gamedata/later/data.txt", "ordinary produced source chooses gamedata");
            var wrongDestination = tree.Dir("extract/data.txt");
            tree.File("extract/data.txt/keep", "root would be the wrong destination");
            var before = tree.Capture(wrongDestination);
            ProcessInstructions.Process(root, ["package", "gamedata/later"], default);
            Check.Equal("ordinary produced source chooses gamedata", File.ReadAllText(Path.Join(root, "gamedata", "data.txt")));
            Check.True(!Path.Exists(Path.Join(root, "gamedata", "later", "gamedata")), "An empty child was projected as a produced directory.");
            tree.AssertUnchanged(before);
        });
    }

    private static void InstructionsDoNotProjectConsumedOrExcludedContents()
    {
        WithTree(tree =>
        {
            var root = tree.Dir("extract");
            tree.File("extract/package/gamedata/later/nest/conflict", "consumed before its parent instruction");
            var retained = tree.Dir("extract/gamedata/nest/conflict");
            tree.File("extract/gamedata/nest/conflict/keep", "not a future file target");
            var before = tree.Capture(retained);
            ProcessInstructions.Process(root, ["package/gamedata/later/nest", "package", "gamedata/later"], default);
            Check.Equal("consumed before its parent instruction", File.ReadAllText(Path.Join(root, "gamedata", "conflict")));
            Check.True(!Path.Exists(Path.Join(root, "gamedata", "later")), "Consumed contents were projected into a later source.");
            Check.True(!File.Exists(Path.Join(root, "package", "gamedata", "later", "nest", "conflict")), "Original file was not moved.");
            tree.AssertUnchanged(before);
        });
        WithTree(tree =>
        {
            var root = tree.Dir("extract");
            tree.File("extract/package.git-cache/gamedata/later/conflict", "excluded source is a no-op");
            tree.File("extract/gamedata/conflict/keep", "must not become a file target");
            var before = tree.Capture(root);
            ProcessInstructions.Process(root, ["package.git-cache", "gamedata/later"], default);
            tree.AssertUnchanged(before);
        });
    }

    private static void EmptyAndBlankInstructionsCreateNothingButLiteralZeroExecutes()
    {
        foreach (IList<string> instructions in new IList<string>[] { new List<string>(), new List<string> { "", "  " } })
        {
            WithTree(tree =>
            {
                var missing = Path.Join(tree.Root, "missing", "extract");
                var before = tree.Capture(tree.Root);
                var absent = tree.Capture(missing);
                ProcessInstructions.Process(missing, instructions, default);
                tree.AssertUnchanged(before);
                tree.AssertUnchanged(absent);
            });
        }
        WithTree(tree =>
        {
            var root = tree.Dir("extract");
            tree.File("extract/0/item", "literal zero is executable");
            ProcessInstructions.Process(root, ["", "0", " "], default);
            Check.Equal("literal zero is executable", File.ReadAllText(Path.Join(root, "gamedata", "item")));
            Check.True(!File.Exists(Path.Join(root, "0", "item")), "Literal zero was treated as a sentinel.");
        });
        WithTree(tree =>
        {
            var root = tree.Dir("extract");
            ProcessInstructions.Process(root, ["missing-source"], default);
            Check.True(Directory.Exists(Path.Join(root, "gamedata")), "Preserve the legacy nonempty missing-source destination behavior.");
        });
    }

    private static void CancellationPrecedesNoOpsAndFollowsEveryCallback()
    {
        WithTree(tree =>
        {
            var source = tree.Dir("source.git-cache");
            tree.File("source.git-cache/item", "stay");
            var missing = Path.Join(tree.Root, "missing");
            var before = tree.Capture(tree.Root);
            using var stop = new CancellationTokenSource();
            stop.Cancel();
            Check.Throws<OperationCanceledException>(() => DirUtils.CopyDirectory(source, source, cancellationToken: stop.Token));
            Check.Throws<OperationCanceledException>(() => DirUtils.CopyDirectory(source, missing, cancellationToken: stop.Token));
            Check.Throws<OperationCanceledException>(() => DirUtils.CountFiles(source, null, stop.Token));
            Check.Throws<OperationCanceledException>(() => DirUtils.NormalizePermissions(missing, stop.Token));
            Check.Throws<OperationCanceledException>(() => CleanExtractPath.Clean(missing, stop.Token));
            Check.Throws<OperationCanceledException>(() => DirUtils.RecursivelyDeleteDirectory(source, [], stop.Token));
            Check.Throws<OperationCanceledException>(() => ProcessInstructions.Process(missing, [], stop.Token));
            Check.Throws<OperationCanceledException>(() => ProcessInstructions.Process(missing, ["", " "], stop.Token));
            tree.AssertUnchanged(before);
        });
        WithTree(tree =>
        {
            var source = tree.Dir("source");
            tree.File("source/item", "stay");
            var destination = Path.Join(tree.Root, "absent");
            var before = tree.Capture(tree.Root);
            using var stop = new CancellationTokenSource();
            var calls = 0;
            Check.Throws<OperationCanceledException>(() => DirUtils.CopyDirectory(source, destination,
                txtProgress: _ => { calls++; stop.Cancel(); }, cancellationToken: stop.Token));
            Check.Equal(1, calls, "No second callback or output creation after cancellation.");
            tree.AssertUnchanged(before);
        });
        WithTree(tree =>
        {
            var source = tree.Dir("source");
            tree.File("source/item", "move once");
            var destination = Path.Join(tree.Root, "destination");
            using var stop = new CancellationTokenSource();
            var followUp = false;
            var calls = 0;
            Check.Throws<OperationCanceledException>(() =>
            {
                DirUtils.CopyDirectory(source, destination, moveFile: true,
                    onProgress: _ => { calls++; stop.Cancel(); }, cancellationToken: stop.Token);
                followUp = true;
            });
            Check.Equal(1, calls);
            Check.True(!followUp, "Cancellation in the last progress callback must still throw.");
            Check.Equal("move once", File.ReadAllText(Path.Join(destination, "item")));
            Check.True(!File.Exists(Path.Join(source, "item")), "The already-completed move is not rolled back.");
        });
        WithTree(tree =>
        {
            var source = tree.Dir("empty-source");
            var destination = Path.Join(tree.Root, "destination");
            using var stop = new CancellationTokenSource();
            var followUp = false;
            Check.Throws<OperationCanceledException>(() =>
            {
                DirUtils.CopyDirectory(source, destination, txtProgress: text =>
                {
                    if (text.StartsWith("Created ", StringComparison.Ordinal)) stop.Cancel();
                }, cancellationToken: stop.Token);
                followUp = true;
            });
            Check.True(!followUp, "Cancellation in the final text callback must still throw.");
            Check.True(Directory.Exists(destination), "Only the directory creation preceding that callback completed.");
            Check.Equal(0, Directory.GetFileSystemEntries(destination).Length);
        });
        WithTree(tree =>
        {
            var source = tree.Dir("source");
            var file = tree.File("source/item", "must not move");
            var sourceBefore = tree.Capture(source);
            var destination = Path.Join(tree.Root, "destination");
            var target = Path.Join(destination, "item");
            ScratchTree.Snapshot? beforeWrite = null;
            using var stop = new CancellationTokenSource();
            Check.Throws<OperationCanceledException>(() => DirUtils.CopyDirectory(source, destination, moveFile: true,
                txtProgress: text =>
                {
                    if (text != $"Copying {file} to {target}") return;
                    beforeWrite = tree.Capture(destination);
                    stop.Cancel();
                }, cancellationToken: stop.Token));
            Check.True(beforeWrite is not null, "Final pre-write text callback was not reached.");
            tree.AssertUnchanged(beforeWrite!);
            tree.AssertUnchanged(sourceBefore);
            Check.True(!File.Exists(target), "Cancellation in the final text callback did not prevent the write.");
        });
    }

    private static void NearIoRechecksRejectCallbackIntroducedLinks() => WithTree(outside =>
    {
        var external = outside.File("keep", "unchanged");
        var outsideBefore = outside.Capture(outside.Root);
        foreach (var replaceSource in new[] { true, false })
        {
            WithTree(tree =>
            {
                var source = tree.Dir("source");
                var file = tree.File("source/item", "source bytes");
                var destination = Path.Join(tree.Root, "destination");
                ScratchTree.Snapshot? injectedState = null;
                var injected = false;
                Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, destination, moveFile: true,
                    txtProgress: text =>
                    {
                        if (injected || !text.Contains("SHA256 =", StringComparison.Ordinal)) return;
                        injected = true;
                        if (replaceSource)
                        {
                            File.Delete(file); // deliberate fixture mutation, not production policy
                            tree.LinkFile("source/item", external);
                        }
                        else tree.LinkFile("destination/item", external);
                        injectedState = tree.Capture(tree.Root);
                    }));
                Check.True(injected && injectedState is not null, "Hash callback injection was not reached.");
                tree.AssertUnchanged(injectedState!);
                outside.AssertUnchanged(outsideBefore);
            });
        }
    });

    private static void MetadataGuardsAndPayloadArePreserved() => WithTree(outside =>
    {
        var external = outside.File("meta.ini", "unchanged");
        var externalRoot = outside.Dir("directory");
        var outsideBefore = outside.Capture(outside.Root);
        foreach (var scenario in new[] { "invalid-archive", "linked-meta", "linked-root", "linked-parent" })
        {
            WithTree(tree =>
            {
                var root = tree.Dir("extract");
                var archive = "addon.7z";
                switch (scenario)
                {
                    case "invalid-archive": archive = "../escape"; break;
                    case "linked-meta": tree.LinkFile("extract/meta.ini", external); break;
                    case "linked-root": root = tree.LinkDirectory("linked", externalRoot); break;
                    case "linked-parent": root = Path.Join(tree.LinkDirectory("linked", outside.Root), "directory"); break;
                }
                var before = tree.Capture(tree.Root);
                Check.Throws<UnsafePathException>(() => WriteAddonMetaIni.Write(root, archive, "https://example.invalid/addon"));
                tree.AssertUnchanged(before);
                outside.AssertUnchanged(outsideBefore);
            });
        }
        WithTree(tree =>
        {
            var root = tree.Dir("extract");
            WriteAddonMetaIni.Write(root, "addon.7z", "https://example.invalid/addon");
            var expected = """
                [General]
                gameName=stalkeranomaly
                modid=0
                ignoredversion=addon.7z
                version=addon.7z
                newestversion=addon.7z
                category="-1,"
                nexusFileStatus=1
                installationFile=addon.7z
                repository=
                comments=
                notes=
                nexusDescription=
                url=https://example.invalid/addon
                hasCustomURL=true
                lastNexusQuery=
                lastNexusUpdate=
                nexusLastModified=2021-11-09T18:10:18Z
                converted=false
                validated=false
                color=@Variant(\0\0\0\x43\0\xff\xff\0\0\0\0\0\0\0\0)
                tracked=0

                [installedFiles]
                1\modid=0
                1\fileid=0
                size=1

                """.ReplaceLineEndings("\r\n");
            var path = Path.Join(root, "meta.ini");
            Check.Equal(expected, File.ReadAllText(path, Encoding.UTF8));
            Check.SequenceEqual(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(expected)), File.ReadAllBytes(path));
        });
    });

    private static async Task SeparatorGuardsAndPayloadArePreservedAsync()
    {
        foreach (var folder in new[] { "", " ", ".", "..", "../escape", "nested/path", @"nested\path", "C:drive", "bad\nname" })
        {
            await WithTreeAsync(async tree =>
            {
                var missing = Path.Join(tree.Root, "absent-gamma");
                var before = tree.Capture(tree.Root);
                var absent = tree.Capture(missing);
                var separator = new Separator { FolderName = folder, Name = "display label" };
                await Check.ThrowsAsync<UnsafePathException>(() => separator.WriteAsync(missing));
                tree.AssertUnchanged(before);
                tree.AssertUnchanged(absent);
            });
        }
        await WithTreeAsync(async outside =>
        {
            var external = outside.File("keep.ini", "unchanged");
            var externalDirectory = outside.Dir("directory");
            var outsideBefore = outside.Capture(outside.Root);
            foreach (var scenario in new[] { "mods", "selected", "meta", "gamma-parent" })
            {
                await WithTreeAsync(async tree =>
                {
                    var gamma = tree.Dir("gamma");
                    switch (scenario)
                    {
                        case "mods": tree.LinkDirectory("gamma/mods", externalDirectory); break;
                        case "selected": tree.LinkDirectory("gamma/mods/section", externalDirectory); break;
                        case "meta": tree.LinkFile("gamma/mods/section/meta.ini", external); break;
                        case "gamma-parent": gamma = Path.Join(tree.LinkDirectory("linked", outside.Root), "directory"); break;
                    }
                    var before = tree.Capture(tree.Root);
                    var separator = new Separator { FolderName = "section", Name = "display" };
                    await Check.ThrowsAsync<UnsafePathException>(() => separator.WriteAsync(gamma));
                    tree.AssertUnchanged(before);
                    outside.AssertUnchanged(outsideBefore);
                });
            }
        });
        await WithTreeAsync(async tree =>
        {
            var gamma = Path.Join(tree.Root, "new-gamma");
            var separator = new Separator { FolderName = "section", Name = "../display\nlabel is not a path" };
            await separator.WriteAsync(gamma);
            var expected = """
                [General]
                modid=0
                version=
                newestVersion=
                category=0
                installationFile=

                [installedFiles]
                size=0

                """.ReplaceLineEndings("\r\n");
            Check.SequenceEqual(Encoding.UTF8.GetBytes(expected), File.ReadAllBytes(Path.Join(gamma, "mods", "section", "meta.ini")));
            Check.True(!Path.Exists(Path.Join(tree.Root, "display")), "Name was used as a filesystem path.");
        });
    }
}
