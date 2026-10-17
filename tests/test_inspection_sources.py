"""Inspection source/wiring contracts only, not filesystem behavioral tests.

No C# compilation/execution or Python implementation of path/entry policy occurs
here. The behavioral cases are written in FileSystemBoundaryCases.cs.
"""

from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]
FILESYSTEM = ROOT / "Stalker.Gamma/Filesystem"


def source(path):
    if not path.is_file():
        raise AssertionError(f"Missing inspection source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def boundary():
    return source(FILESYSTEM / "FileSystemBoundary.cs")


class InspectionSourceTests(unittest.TestCase):
    def test_public_kind_and_entry_contracts_are_exact(self):
        text = source(FILESYSTEM / "CheckedEntry.cs")
        for declaration in (
            "namespace Stalker.Gamma.Filesystem;",
            "public enum EntryKind { Missing, Directory, NonDirectory }",
            "public enum ExpectedEntry { Any, Directory, NonDirectory }",
            "public enum MissingPolicy { Reject, Leaf, Suffix }",
            "public sealed record CheckedEntry(string FullPath, string RelativePath, EntryKind Kind);",
        ):
            self.assertIn(declaration, text)

    def test_boundary_keeps_fixed_public_api(self):
        text = boundary()
        for declaration in (
            r"public sealed class FileSystemBoundary\b",
            r"public FileSystemBoundary\(string configuredRoot, string basePath\)",
            r"public string Root\s*\{\s*get;\s*\}",
            r"public string Resolve\(string relativePath\)",
            r"public CheckedEntry CheckPath\(\s*string path,\s*ExpectedEntry expected = ExpectedEntry.Any,\s*MissingPolicy missing = MissingPolicy.Reject,\s*CancellationToken ct = default\)",
            r"public CheckedTree CheckTree\(\s*string path,\s*ExpectedEntry expected = ExpectedEntry.Any,\s*MissingPolicy missing = MissingPolicy.Reject,\s*CancellationToken ct = default\)",
        ):
            self.assertRegex(text, declaration)

    def test_construction_resolution_and_normalization_are_lexical(self):
        text = boundary()
        lexical = text.split("public FileSystemBoundary(", 1)[1].split("public CheckedEntry CheckPath(", 1)[0]
        self.assertIn("Path.IsPathFullyQualified(basePath)", lexical)
        self.assertIn("Path.GetFullPath(configuredRoot,", lexical)
        self.assertIn("InputPolicy.RequireRelativePath(relativePath,", lexical)
        self.assertIn("string.IsNullOrWhiteSpace(configuredRoot)", lexical)
        self.assertNotRegex(lexical, r"\b(?:File|Directory|FileInfo|DirectoryInfo)\s*[.(]")
        self.assertNotIn("ReadKind(", lexical)
        self.assertNotIn("Directory.GetCurrentDirectory", text)
        self.assertNotIn(".Replace(", text)

    def test_containment_is_canonical_ordinal_and_component_aware(self):
        text = boundary()
        for marker in (
            "Path.GetFullPath(path, Root)",
            "Path.TrimEndingDirectorySeparator(",
            "Path.EndsInDirectorySeparator(Root)",
            "Root + Path.DirectorySeparatorChar",
            "string.Equals(fullPath, Root, StringComparison.Ordinal)",
            "fullPath.StartsWith(rootPrefix, StringComparison.Ordinal)",
            "Path.GetRelativePath(Root, fullPath)",
        ):
            self.assertIn(marker, text)
        self.assertNotIn("OrdinalIgnoreCase", text)

    def test_inspection_walks_from_filesystem_root_and_requires_directory_parents(self):
        text = boundary()
        for marker in (
            "Path.GetPathRoot(fullPath)",
            "var directoryParentChecked = false;",
            "for (var index = -1; index < parts.Length; index++)",
            "ReadKind(current)",
            "Path.Join(current, parts[index])",
            "kind != EntryKind.Directory",
            "directoryParentChecked = true;",
        ):
            self.assertIn(marker, text)
        self.assertNotIn("Path.GetRelativePath(Root, fullPath).Split", text)

    def test_throwing_inspection_rejects_reparse_before_directory_classification(self):
        text = boundary()
        read_kind = text.split("private static EntryKind ReadKind(", 1)[1]
        self.assertIn("File.GetAttributes(path)", read_kind)
        self.assertLess(read_kind.index("FileAttributes.ReparsePoint"), read_kind.index("FileAttributes.Directory"))
        for marker in ("catch (FileNotFoundException error)", "catch (DirectoryNotFoundException error)"):
            self.assertIn(marker, read_kind)
        self.assertIn("return EntryKind.NonDirectory", read_kind)
        for forbidden in ("File.Exists(", "Directory.Exists(", "ResolveLinkTarget", "LinkTarget", "DllImport", "LibraryImport", "stat("):
            self.assertNotIn(forbidden, text)

    def test_missing_modes_are_explicit_and_do_not_bypass_checked_parents(self):
        text = boundary()
        for marker in (
            "!directoryParentChecked",
            "missing == MissingPolicy.Reject",
            "missing == MissingPolicy.Leaf && !isLeaf",
            "EntryKind.Missing",
            "ExpectedEntry.Directory",
            "ExpectedEntry.NonDirectory",
        ):
            self.assertIn(marker, text)

    def test_checked_tree_copies_immutable_descriptions_to_read_only_storage(self):
        text = source(FILESYSTEM / "CheckedTree.cs")
        for declaration in (
            "public sealed class CheckedTree",
            "public FileSystemBoundary Boundary { get; }",
            "public string StartPath { get; }",
            "public IReadOnlyList<CheckedEntry> Entries { get; }",
            "internal CheckedTree(",
            "Array.AsReadOnly(",
            "new CheckedEntry(entry.FullPath, entry.RelativePath, entry.Kind)",
            ".ToArray()",
        ):
            self.assertIn(declaration, text)
        self.assertNotRegex(text, r"public\s+(?:CheckedEntry\[\]|List<CheckedEntry>|IList<CheckedEntry>)")
        self.assertNotIn("set;", text)
        self.assertIn("evidence", text)
        self.assertIn("recheck", text)

    def test_tree_includes_start_and_descends_only_using_checked_kinds(self):
        text = boundary()
        for marker in (
            "var start = CheckPath(path, expected, missing, ct);",
            "var entries = new List<CheckedEntry> { start };",
            "if (start.Kind == EntryKind.Directory)",
            "if (child.Kind == EntryKind.Directory)",
            "new CheckedTree(this, start.FullPath, entries)",
            "Path.GetDirectoryName(child.FullPath)",
        ):
            self.assertIn(marker, text)
        self.assertNotRegex(text, r"\bis\s+(?:DirectoryInfo|FileInfo)\b")
        self.assertNotIn("SearchOption.AllDirectories", text)

    def test_enumeration_uses_checked_full_path_and_wraps_creation_and_advancement_errors(self):
        text = boundary()
        children = text.split("private static string[] ReadChildren(", 1)[1].split("private static EntryKind ReadKind(", 1)[0]
        self.assertRegex(children, r"try\s*\{[\s\S]*foreach\s*\(var child in Directory.EnumerateFileSystemEntries\(\s*directory.FullPath,\s*\"\*\",")
        self.assertIn("children.Add(child);", children)
        self.assertIn("catch (Exception error)", children)
        self.assertIn("!OperationFailure.IsCancellation(error)", children)
        self.assertIn("throw new UnsafePathException(", children)
        self.assertNotIn("return [];", children)
        self.assertNotIn("yield", children)

    def test_enumeration_explicitly_disables_skipping_and_recursion(self):
        text = boundary()
        for setting in (
            "new EnumerationOptions",
            "RecurseSubdirectories = false",
            "IgnoreInaccessible = false",
            "AttributesToSkip = 0",
            "ReturnSpecialDirectories = false",
        ):
            self.assertTrue(setting in text, f"Explicit enumeration setting missing: {setting}")

    def test_cancellation_is_checked_and_not_wrapped_as_an_unsafe_stop(self):
        text = boundary()
        self.assertGreaterEqual(text.count("ct.ThrowIfCancellationRequested();"), 7)
        catches = re.findall(r"catch \(Exception error\)\s*when\s*\(([^\n]+)\)", text)
        self.assertGreaterEqual(len(catches), 3)
        for filter_text in catches:
            self.assertIn("!OperationFailure.IsCancellation(error)", filter_text)

    def test_csharp_fixtures_cover_required_inspection_cases_and_safe_cleanup(self):
        text = source(ROOT / "tests/LinuxRegression/FileSystemBoundaryCases.cs")
        for marker in (
            "internal static Task RunAsync()",
            "ConstructionAndResolutionAreLexicalOnly",
            "RelativeRootUsesAnExplicitStableBase",
            "ContainmentIsOrdinalAndComponentAware",
            "ReturnsCanonicalPathsWithoutRenamingNativeMembers",
            "RejectsLinkedRootsAndAncestorsAboveTheBoundary",
            "RejectsContainedExternalDanglingAndCyclicLinks",
            "EnforcesDirectoryAndNonDirectoryExpectations",
            "DistinguishesMissingLeafFromMissingSuffix",
            "IncludesMissingStartAndOwnsEntryDescriptions",
            "MaterializesCompleteTreesUsingCheckedKinds",
            "SnapshotsAreEvidenceNotAuthorization",
            "HonorsPreCancellationIncludingMissingResults",
            "RejectsInspectionAndEnumerationErrorsWhenPermissionsPermit",
            "Check.WithCleanup",
            "tree.Dispose",
            "outside.AssertUnchanged(before)",
            "Directory.SetCurrentDirectory(originalDirectory);\n            tree.Dispose();",
            "unexercised inspection denial",
            "unexercised enumeration denial",
            "ProbeDeniesAccess",
            "new CheckedTree(boundary, start, callerEntries)",
            'tree.LinkDirectory("root/cycle-a", Path.Join(root, "cycle-b"))',
            'tree.LinkDirectory("root/cycle-b", Path.Join(root, "cycle-a"))',
        ):
            self.assertIn(marker, text)
        self.assertNotIn("recursive: true", text)


if __name__ == "__main__":
    unittest.main()
