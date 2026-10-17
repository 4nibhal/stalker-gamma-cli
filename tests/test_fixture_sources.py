"""Structural fixture contracts only; never compile/run C# or model its policy.

These checks locate required source shapes, not filesystem security guarantees.
Behavioral assertions live in the unexecuted C# regression suites.
"""

from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]
FIXTURES = ROOT / "tests/LinuxRegression"


def source(name):
    path = FIXTURES / name
    # Missing implementation is an assertion failure during structural red.
    if not path.is_file():
        raise AssertionError(f"Missing fixture source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


class FixtureSourceTests(unittest.TestCase):
    def test_check_keeps_fixed_assertion_contracts(self):
        text = source("Check.cs")
        for declaration in (
            "namespace LinuxRegression;",
            "internal static class Check",
            "True(bool condition, string message)",
            'Equal<T>(T expected, T actual, string message = "")',
            'SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = "")',
            "T Throws<T>(Action action)",
            "Task<T> ThrowsAsync<T>(Func<Task> action)",
            "where T : Exception",
        ):
            with self.subTest(declaration=declaration):
                self.assertIn(declaration, text)

    def test_body_disposal_wrappers_preserve_both_failures(self):
        text = source("Check.cs")
        self.assertIn("WithCleanup(Action body, Action cleanup)", text)
        self.assertIn("WithCleanupAsync(Func<Task> body, Action cleanup)", text)
        self.assertIn("ExceptionDispatchInfo.Capture", text)
        self.assertIn("new AggregateException(bodyFailure, cleanupFailure)", text)
        cases = source("ScratchTreeCases.cs")
        for marker in (
            "PreservesAssertionAndRealDisposalFailures",
            "PreservesAsyncAssertionAndCleanupFailures",
            "both.InnerExceptions[0]",
            "both.InnerExceptions[1]",
            "inside.Dispose",
        ):
            self.assertIn(marker, cases)

    def test_scratch_keeps_fixed_api_and_no_production_dependency(self):
        text = source("ScratchTree.cs")
        for declaration in (
            "namespace LinuxRegression;",
            "internal sealed class ScratchTree : IDisposable",
            "string Root { get; }",
            "string Dir(string relative)",
            "string File(string relative, string content)",
            "string LinkFile(string relative, string target)",
            "string LinkDirectory(string relative, string target)",
            "sealed class Snapshot",
            "Snapshot Capture(string absolutePath)",
            "void AssertUnchanged(Snapshot snapshot)",
            "void Dispose()",
        ):
            with self.subTest(declaration=declaration):
                self.assertIn(declaration, text)
        for forbidden in ("Stalker.Gamma", "FileSystemBoundary", "DirUtils", "DllImport", "LibraryImport"):
            self.assertNotIn(forbidden, text)

    def test_snapshot_shape_is_owned_immutable_and_includes_absence_inventory_modes(self):
        text = source("ScratchTree.cs")
        for marker in (
            "ReferenceEquals(snapshot.Owner, snapshotOwner)",
            "Array.AsReadOnly(entries.ToArray())",
            "internal object Owner { get; }",
            "internal string AbsolutePath { get; }",
            "internal IReadOnlyList<EntryState> Entries { get; }",
            "EntryKind.Missing",
            "string? Inventory",
            "FileAttributes? Attributes",
            "UnixFileMode? Mode",
            "string? LinkTarget",
            "Convert.ToBase64String(System.IO.File.ReadAllBytes(path))",
            "System.IO.File.GetUnixFileMode(path)",
        ):
            with self.subTest(marker=marker):
                self.assertIn(marker, text)
        self.assertNotRegex(text, r"(?:public|internal)\s+(?:byte\[\]|List<[^>]+>)\s+\w+\s*\{")

    def test_link_snapshot_branch_returns_before_mode_or_content_reads(self):
        text = source("ScratchTree.cs")
        match = re.search(
            r"if \(entry.Kind == EntryKind.Link\)\s*\{(?P<link>.*?)\n        \}",
            text,
            re.S,
        )
        self.assertIsNotNone(match, "Separate link snapshot branch missing")
        link = match.group("link")
        self.assertIn("ReadLinkTarget(path, entry.Attributes)", link)
        self.assertIn("return;", link)
        self.assertNotIn("GetUnixFileMode", link)
        self.assertNotIn("ReadAllBytes", link)
        self.assertNotIn("ResolveLinkTarget", text)

    def test_mutation_sources_have_containment_and_ancestor_checks(self):
        text = source("ScratchTree.cs")
        for marker in (
            "Path.IsPathRooted(relative)",
            'part is "" or "." or ".."',
            "StringComparison.Ordinal",
            "RequireOwnedAbsolutePath",
            "EnsureParents",
            "VerifyOwnership",
            "RequireRealDirectory",
            "FileAttributes.ReparsePoint",
            "Path.GetPathRoot",
        ):
            self.assertIn(marker, text)
        for forbidden in ("Directory.Exists(", "System.IO.File.Exists(", "SearchOption.AllDirectories"):
            self.assertNotIn(forbidden, text)
        guard = "RequireAbsoluteDirectoryChain(temporaryParent);"
        self.assertTrue(guard in text, "Temp parent must be inspected before creating the owned container")
        self.assertLess(text.index(guard), text.index("Directory.CreateTempSubdirectory("))

    def test_cleanup_is_inspection_driven_nonrecursive_and_not_link_registry_driven(self):
        text = source("ScratchTree.cs")
        for marker in (
            "InspectForCleanup",
            "DeleteChecked",
            "UnlinkEntry",
            "SearchOption.TopDirectoryOnly",
            "VerifyContainerInventory",
            "ReadAllText(ownershipPath)",
            "Directory.Delete(containerRoot);",
            "Directory.Delete(entry.Path);",
        ):
            self.assertIn(marker, text)
        self.assertNotRegex(text, r"Directory\.Delete\([^;]*(?:recursive\s*:|,\s*true)")
        for forbidden in ("registeredLinks", "catch (Exception", "SetUnixFileMode", "SetAttributes"):
            self.assertNotIn(forbidden, text)
        catches = re.findall(r"catch \(([^)]+)\)", text)
        self.assertEqual(catches, ["FileNotFoundException", "DirectoryNotFoundException"])

    def test_scratch_behavioral_fixtures_cover_owned_isolation_and_cwd_restoration(self):
        cases = source("ScratchTreeCases.cs")
        for marker in (
            "internal static async Task RunAsync()",
            "DetectsFileBytesAttributesAndModes",
            "DetectsDirectoryModesAndInventories",
            "CapturesAbsenceAndOwnsSnapshots",
            '"../escape"',
            '"wrong-root"',
            '"registered"',
            '"unregistered"',
            'inside.LinkDirectory("external", outside.Root)',
            "Directory.CreateSymbolicLink(Path.Join(inside.Root",
            "System.IO.File.CreateSymbolicLink(Path.Join(inside.Root",
            "outside.AssertUnchanged(before)",
            'Check.True(false, "deliberate assertion")',
            "Directory.GetCurrentDirectory()",
            "Directory.SetCurrentDirectory(originalDirectory);\n            tree.Dispose();",
        ):
            with self.subTest(marker=marker):
                self.assertIn(marker, cases)
        self.assertNotIn("recursive: true", cases)

    def test_wine_ini_preserves_original_assertions_with_owned_cleanup(self):
        text = source("WineIniCases.cs")
        for marker in (
            "internal static Task RunAsync()",
            "new ScratchTree()",
            "Check.WithCleanupAsync",
            "scratch.Dispose",
            '"GAMMA ñ space"',
            '"Anomaly d\'Artagnan"',
            'WriteModOrganizerIni.WriteAsync(gamma, anomaly, "v2.5.2", [])',
            'var wineAnomaly = "Z:" + Path.GetFullPath(anomaly);',
            r'''var escapedAnomaly = wineAnomaly.TrimEnd('/').Replace("/", @"\\");''',
            '$"gamePath=@ByteArray({escapedAnomaly})"',
            r'$"1\\binary={wineAnomaly}/AnomalyLauncher.exe"',
            r'$"2\\binary={wineAnomaly}/bin/AnomalyDX11AVX.exe"',
            '"selected_profile=@ByteArray(G.A.M.M.A)"',
            '"version=2.5.2"',
            "ini.Contains(expected, StringComparison.Ordinal)",
        ):
            with self.subTest(marker=marker):
                self.assertIn(marker, text)
        self.assertNotIn("Directory.Delete", text)


if __name__ == "__main__":
    unittest.main()
