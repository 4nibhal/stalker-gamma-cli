"""Managed-operation source contracts only: NOT C# behavioral execution/TDD.

The production-C# fixtures are intentionally uncompiled/unexecuted. These tests
inspect preparation/execution wiring and unsafe-walk removal, not path-policy
behavior, marker counts, or a Python reimplementation of filesystem policy.
"""

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]


def source(relative):
    path = ROOT / relative
    if not path.is_file():
        raise AssertionError(f"Missing managed-operation source: {relative}")
    return path.read_text(encoding="utf-8")


def section(text, start, end):
    if start not in text or end not in text:
        raise AssertionError(f"Missing source section: {start} ... {end}")
    return text.split(start, 1)[1].split(end, 1)[0]


DIR = "Stalker.Gamma/Utilities/DirUtils.cs"
PLAN = "Stalker.Gamma/Filesystem/ManagedCopyPlan.cs"
SERVICES = "Stalker.Gamma/GammaInstallerServices/"


class ManagedTreeSourceTests(unittest.TestCase):
    def test_public_copy_prepares_before_execution(self):
        text = source(DIR)
        wrapper = section(text, "public static void CopyDirectory(", "internal static ManagedCopyPlan PrepareCopy(")
        self.assertIn("bool overwrite = true", wrapper)
        self.assertIn("string? fileFilter = null", wrapper)
        self.assertIn("bool moveFile = false", wrapper)
        self.assertIn("CancellationToken cancellationToken = default", wrapper)
        self.assertLess(wrapper.index("PrepareCopy("), wrapper.index("ExecuteCopy("))
        for forbidden in ("Directory.CreateDirectory", "File.OpenRead", ".Invoke(", "CountFiles("):
            self.assertNotIn(forbidden, wrapper)

    def test_copy_plan_materializes_both_trees_before_mapping_and_execution(self):
        text = source(PLAN)
        self.assertIn("internal sealed class ManagedCopyPlan", text)
        prepare = section(text, "internal static ManagedCopyPlan Prepare(", "internal static IReadOnlyList<CheckedEntry> SelectDirectories(")
        for marker in ("sourceBoundary.CheckTree(", "destinationBoundary.CheckTree(", "ValidateMappedTargets(", "new ManagedCopyPlan("):
            self.assertIn(marker, prepare)
        self.assertLess(prepare.index("sourceBoundary.CheckTree("), prepare.index("SelectDirectories("))
        self.assertLess(prepare.index("destinationBoundary.CheckTree("), prepare.index("SelectDirectories("))
        self.assertLess(prepare.index("ValidateMappedTargets("), prepare.index("new ManagedCopyPlan("))
        for forbidden in ("Directory.CreateDirectory", "File.OpenRead", "File.Copy", "File.Move", ".Invoke(", "CountFiles("):
            self.assertNotIn(forbidden, prepare)

    def test_plan_owns_snapshots_mappings_and_read_only_selection(self):
        text = source(PLAN)
        for declaration in (
            "internal CheckedTree SourceTree { get; }", "internal CheckedTree DestinationTree { get; }",
            "internal FileSystemBoundary SourceBoundary => SourceTree.Boundary;",
            "internal FileSystemBoundary DestinationBoundary => DestinationTree.Boundary;",
            "internal IReadOnlyList<DirectoryTarget> Directories { get; }",
            "internal IReadOnlyList<FileTarget> Files { get; }", "internal int TotalFiles => Files.Count;",
            "Array.AsReadOnly(directories.ToArray())", "Array.AsReadOnly(files.ToArray())",
        ):
            self.assertIn(declaration, text)
        self.assertNotIn("set;", text)

    def test_selection_is_snapshot_only_and_ancestor_reads_are_depth_ordered(self):
        text = source(PLAN)
        selection = section(text, "internal static IReadOnlyList<CheckedEntry> SelectDirectories(", "private static void ValidateMappedTargets(")
        self.assertIn("tree.Entries", selection)
        self.assertIn("nonempty", selection)
        self.assertIn('Contains(".git", StringComparison.Ordinal)', selection)
        self.assertIn("Path.GetFileName(entry.FullPath)", selection)
        self.assertIn("fileFilter", selection)
        self.assertIn("OrderBy(entry => Depth(entry.FullPath))", selection)
        for forbidden in ("GetFiles(", "GetDirectories(", "EnumerateFiles(", "File.OpenRead", "Directory.Exists"):
            self.assertNotIn(forbidden, selection)

    def test_mapping_validates_future_roles_duplicates_and_overlap(self):
        text = source(PLAN)
        for marker in (
            "IsStrictDescendant(destinationBoundary.Root, sourceBoundary.Root)",
            "string.Equals(sourceBoundary.Root, destinationBoundary.Root, StringComparison.Ordinal)",
            "Dictionary<string, EntryKind>(StringComparer.Ordinal)", "TryAdd(",
            "EntryKind.NonDirectory", "Path.GetDirectoryName(", "MissingPolicy.Suffix",
            "ExpectedEntry.Directory", "ExpectedEntry.NonDirectory",
        ):
            self.assertIn(marker, text)
        self.assertNotIn("Resolve(Path.GetRelativePath", text)

    def test_execute_consumes_plan_and_rechecks_canonical_paths_near_io(self):
        text = source(DIR)
        execute = section(text, "internal static void ExecuteCopy(", "internal static int CountFiles(")
        for marker in ("plan.Directories", "plan.Files", "plan.TotalFiles", "plan.SourceBoundary.CheckPath(",
                       "plan.DestinationBoundary.CheckPath(", "Directory.CreateDirectory(destination.FullPath)",
                       "File.OpenRead(source.FullPath)", "File.Move(source.FullPath, destination.FullPath, plan.Overwrite)",
                       "File.Copy(source.FullPath, destination.FullPath, plan.Overwrite)"):
            self.assertIn(marker, execute)
        for forbidden in ("Enumerate", "GetFiles(", "GetDirectories(", "CountFiles(", "Directory.Exists", "File.Exists"):
            self.assertNotIn(forbidden, execute)
        self.assertNotIn(".Invoke(", execute)
        self.assertIn("ReportText(", execute)
        self.assertIn("ReportProgress(", execute)

    def test_callbacks_use_post_invocation_cancellation_checks(self):
        text = source(DIR)
        text_callback = section(text, "private static void ReportText(", "private static void ReportProgress(")
        progress_callback = text.split("private static void ReportProgress(", 1)[1]
        for callback in (text_callback, progress_callback):
            self.assertLess(callback.index("callback?.Invoke("), callback.index("ct.ThrowIfCancellationRequested();", callback.index("callback?.Invoke(")))
        self.assertNotIn("IsCancellationRequested", text)
        self.assertIn("if (plan.IsNoOp)", text)

    def test_count_checks_the_whole_tree_before_snapshot_selection(self):
        text = source(DIR)
        count = section(text, "internal static int CountFiles(", "internal static CheckedTree PrepareDirectoryTree(")
        self.assertLess(count.index("PrepareDirectoryTree("), count.index("SelectDirectories("))
        self.assertLess(count.index("SelectFiles("), count.index("return files.Count;"))
        self.assertIn("ct.ThrowIfCancellationRequested();", count)

    def test_managed_operations_have_no_unchecked_recursive_walks_or_exists_probes(self):
        for path in (DIR, PLAN, SERVICES + "ProcessInstructions.cs", SERVICES + "CleanExtractPath.cs"):
            text = source(path)
            for forbidden in ("SearchOption.AllDirectories", "EnumerateFiles(", ".GetFiles(", ".GetDirectories(",
                              "Directory.Exists(", "File.Exists(", "Path.Exists(", ".Delete(true)", "recursive: true"):
                self.assertNotIn(forbidden, text, f"Unchecked traversal/probe in {path}")

    def test_normalization_preflights_and_keeps_legacy_mode_policy(self):
        text = source(DIR)
        public = section(text, "public static void NormalizePermissions(string dir)", "internal static void NormalizePermissions(CheckedTree tree,")
        self.assertLess(public.index("PrepareDirectoryTree("), public.index("NormalizePermissions(tree,"))
        normalize = section(text, "internal static void NormalizePermissions(CheckedTree tree,", "public static void RecursivelyDeleteDirectory(")
        self.assertIn("tree.Entries", normalize)
        self.assertIn("ReadCheckedAttributes(", normalize)
        self.assertIn("File.SetUnixFileMode(directory.FullPath,", normalize)
        for mode in ("UserRead", "UserWrite", "UserExecute", "GroupRead", "GroupWrite", "GroupExecute", "OtherRead", "OtherExecute"):
            self.assertIn("UnixFileMode." + mode, normalize)
        self.assertNotIn("UnixFileMode.OtherWrite", normalize)
        self.assertIn("catch (UnauthorizedAccessException)", normalize)
        attributes = section(text, "private static FileAttributes ReadCheckedAttributes(", "private static void ReportText(")
        self.assertIn("FileAttributes.ReparsePoint", attributes)
        self.assertIn("throw new UnsafePathException(", attributes)

    def test_cleanup_is_selected_from_checked_snapshot_and_nonrecursive(self):
        text = source(DIR)
        selection = section(text, "internal static IReadOnlyList<CheckedEntry> PrepareCleanup(", "internal static void ExecuteCleanup(")
        self.assertIn("tree.Entries", selection)
        self.assertIn("StringComparer.OrdinalIgnoreCase", selection)
        self.assertIn('StartsWith(".", StringComparison.Ordinal)', selection)
        self.assertIn("OrderByDescending(entry => ManagedCopyPlan.Depth(entry.FullPath))", selection)
        execute = section(text, "internal static void ExecuteCleanup(", "public static void CopyDirectory(")
        self.assertIn("tree.Boundary.CheckPath(", execute)
        self.assertIn("Directory.Delete(current.FullPath);", execute)
        self.assertIn("File.Delete(current.FullPath);", execute)

    def test_clean_uses_one_operation_owned_preflight_before_chmod_and_cleanup(self):
        text = source(SERVICES + "CleanExtractPath.cs")
        self.assertIn("CancellationToken cancellationToken = default", text)
        self.assertLess(text.index("PrepareDirectoryTree("), text.index("PrepareCleanup("))
        self.assertLess(text.index("PrepareCleanup("), text.index("NormalizePermissions(tree,"))
        self.assertLess(text.index("NormalizePermissions(tree,"), text.index("ExecuteCleanup(tree,"))
        self.assertNotIn("NormalizePermissions(extractPath)", text)
        self.assertNotIn("RecursivelyDeleteDirectory(extractPath", text)

    def test_legacy_instruction_adapter_checks_every_raw_member_before_omitting_markers(self):
        text = source(SERVICES + "ProcessInstructions.cs")
        process = section(text, "internal static void Process(", "private static void PrepareKnownInstructions(")
        self.assertIn('InputPolicy.PrepareValue(member, "instruction")', process)
        self.assertIn(".ToArray();", process)
        self.assertLess(process.index("InputPolicy.PrepareValue("), process.index("InstructionSet.FromParsed("))
        self.assertIn("Where(value => value.Length != 0)", process)
        self.assertNotIn("FromTsv", text)
        self.assertNotIn("FromJsonTokens", text)
        self.assertNotIn('== "0"', text)
        self.assertNotIn("IsNullOrWhiteSpace", text)

    def test_instructions_prepare_full_existing_tree_and_known_roles_before_steps(self):
        text = source(SERVICES + "ProcessInstructions.cs")
        process = section(text, "internal static void Process(", "private static void PrepareKnownInstructions(")
        self.assertLess(process.index("boundary.CheckTree("), process.index("PrepareKnownInstructions("))
        self.assertLess(process.index("PrepareKnownInstructions("), process.index("foreach (var path in parsed.Paths)"))
        step = process.split("foreach (var path in parsed.Paths)", 1)[1]
        self.assertLess(step.index("InspectInstruction("), step.index("DirUtils.PrepareCopy("))
        self.assertLess(step.index("DirUtils.PrepareCopy("), step.index("DirUtils.ExecuteCopy("))
        known = section(text, "private static void PrepareKnownInstructions(", "private static PreparedInstruction InspectInstruction(")
        self.assertIn("existingTree.Entries", known)
        self.assertIn("PrepareKnownMappings(", known)
        self.assertIn("ComposeKnownMappings(", known)
        self.assertIn("RequireKnownKind(", known)
        self.assertNotIn("ExecuteCopy(", known)
        self.assertNotIn("Directory.CreateDirectory", known)
        roles = section(text, "private static void RequireKnownKind(", "private sealed record PreparedInstruction(")
        self.assertIn("ManagedCopyPlan.IsAtOrBelow(current, root)", roles)
        self.assertIn("current = Path.GetDirectoryName(current)", roles)
        self.assertIn("current == path ? kind : EntryKind.Directory", roles)
        self.assertIn("knownContents.TryGetValue(current, out var actual)", roles)
        self.assertIn("requiredKinds.TryGetValue(current, out var proposed)", roles)
        self.assertIn("throw new UnsafePathException(", roles)

    def test_instruction_preflight_composes_known_mappings_without_fabricating_checked_evidence(self):
        text = source(SERVICES + "ProcessInstructions.cs")
        known = section(text, "private static void PrepareKnownInstructions(", "private static PreparedInstruction InspectInstruction(")
        # Structural wiring only: losing the composition edge caused I1. Actual
        # conflicts/destination choices are asserted by the unexecuted C# cases.
        self.assertIn("var knownContents =", known)
        self.assertIn("PrepareKnownMappings(", known)
        self.assertIn("ComposeKnownMappings(", known)
        loop = section(known, "foreach (var path in parsed.Paths)", "private static IReadOnlyList<KnownInstructionMapping> PrepareKnownMappings(")
        self.assertIn("PrepareKnownMappings(boundary, path, knownContents, requiredKinds, ct)", loop)
        self.assertIn("ComposeKnownMappings(boundary.Root, knownContents, mappings, ct)", loop)
        self.assertLess(loop.index("PrepareKnownMappings("), loop.index("ComposeKnownMappings("))
        mappings = section(known, "private static IReadOnlyList<KnownInstructionMapping> PrepareKnownMappings(", "private static void ComposeKnownMappings(")
        self.assertIn("knownContents.TryGetValue(gamedata,", mappings)
        self.assertIn("? boundary.Root : boundary.Resolve(\"gamedata\")", mappings)
        self.assertIn("ManagedCopyPlan.IsAtOrBelow(", mappings)
        self.assertIn("Path.GetRelativePath(source,", mappings)
        self.assertIn("nonempty", mappings)
        self.assertIn('Contains(".git", StringComparison.Ordinal)', mappings)
        composition = known.split("private static void ComposeKnownMappings(", 1)[1]
        self.assertIn("knownContents.Remove(mapping.Source)", composition)
        self.assertIn("knownContents[mapping.Destination] = mapping.Kind", composition)
        for forbidden in ("new CheckedEntry(", "new CheckedTree(", "ExecuteCopy(", "Directory.CreateDirectory"):
            self.assertNotIn(forbidden, known)

    def test_metadata_and_separator_guard_all_targets_before_io(self):
        metadata = source(SERVICES + "WriteAddonMetaIni.cs")
        self.assertIn("InputPolicy.RequireComponent(", metadata)
        self.assertLess(metadata.index("InputPolicy.RequireComponent("), metadata.index("File.WriteAllText("))
        self.assertLess(metadata.index("ExpectedEntry.NonDirectory"), metadata.index("File.WriteAllText("))
        self.assertIn("meta.FullPath", metadata)
        self.assertIn('ReplaceLineEndings("\\r\\n")', metadata)
        self.assertIn("encoding: Encoding.UTF8", metadata)
        separator = source("Stalker.Gamma/Models/Separator.cs")
        self.assertIn("InputPolicy.RequireComponent(", separator)
        self.assertLess(separator.index("InputPolicy.RequireComponent("), separator.index("Directory.CreateDirectory("))
        self.assertLess(separator.index("ExpectedEntry.NonDirectory"), separator.index("Directory.CreateDirectory("))
        self.assertIn("Directory.CreateDirectory(selected.FullPath)", separator)
        self.assertIn("meta.FullPath", separator)
        self.assertNotIn("RequireComponent(Name", separator)

    def test_written_csharp_fixtures_use_owned_snapshots_and_real_production_operations(self):
        text = source("tests/LinuxRegression/ManagedTreeCases.cs")
        for fixture in (
            "CopyRejectsLinksBeforeCallbacksHashOrCreation", "CopyRejectsLinkedRootsParentsLeavesAndCycles",
            "FiltersGitAndRetainedContentCannotHideLinks", "MappedTypeConflictsPrecedeCallbacksAndMoves",
            "IdenticalPathsAreCheckedNoOpsAndDescendantsReject", "AncestorFlattenConsumesShallowerFilesFirst",
            "SelectionPreservesFilterOverwriteAndNonemptyDirectories", "PlanUsesCanonicalPathsAndOneSelectionSnapshot",
            "CleanupPreservesItsOriginalSelection", "CleanSharesPreflightBeforeChangingAnyModes",
            "InspectionDenialIsNotSkippedAsAPermissionMutationFailure",
            "InstructionsValidateTheWholeOrderedListBeforeMoving", "InstructionsPrepareKnownTypesAndFutureConflicts",
            "InstructionsRejectProducedSourceConflictsBeforeAnyMove", "InstructionsComposeSuccessiveProducedSources",
            "InstructionsChooseDestinationFromProducedGamedata", "InstructionsDoNotProjectConsumedOrExcludedContents",
            "InstructionsReprepareSourcesProducedByEarlierSteps", "EmptyAndBlankInstructionsCreateNothingButLiteralZeroExecutes",
            "CancellationPrecedesNoOpsAndFollowsEveryCallback", "NearIoRechecksRejectCallbackIntroducedLinks",
            "MetadataGuardsAndPayloadArePreserved", "SeparatorGuardsAndPayloadArePreservedAsync",
        ):
            self.assertIn(fixture, text)
        for marker in ("Check.WithCleanup(", "Check.WithCleanupAsync(", "outside.AssertUnchanged(outsideBefore)",
                       "tree.AssertUnchanged(destinationBefore)", "File.GetUnixFileMode(", "File.ReadAllBytes(",
                       "DirUtils.PrepareCopy(", "DirUtils.ExecuteCopy(", "ProcessInstructions.Process(",
                       "Directory.SetCurrentDirectory(originalDirectory)", "NOT compiled or executed"):
            self.assertIn(marker, text)
        self.assertIn("unexercised enumeration denial", text)


if __name__ == "__main__":
    unittest.main()
