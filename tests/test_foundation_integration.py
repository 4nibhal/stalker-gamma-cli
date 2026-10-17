"""Runner/friend-assembly wiring only; not C# or filesystem behavioral tests."""

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]


def source(relative):
    path = ROOT / relative
    return path.read_text() if path.is_file() else ""


class FoundationIntegrationTests(unittest.TestCase):
    def test_library_exposes_internals_only_to_regression_assembly(self):
        assembly = source("Stalker.Gamma/Properties/AssemblyInfo.cs")
        self.assertIn('[assembly: InternalsVisibleTo("LinuxRegression")]', assembly)
        self.assertEqual(1, assembly.count("InternalsVisibleTo("))

    def test_linux_runner_registers_suites_without_inline_recursive_teardown(self):
        runner = source("tests/LinuxRegression/Program.cs")
        self.assertIn("OperatingSystem.IsLinux()", runner)
        suites = (
            ("inputs", "InputPolicyCases"),
            ("scratch", "ScratchTreeCases"),
            ("wine-ini", "WineIniCases"),
            ("records", "PathPolicyCases"),
            ("filesystem", "FileSystemBoundaryCases"),
            ("managed-tree", "ManagedTreeCases"),
        )
        for name, fixture in suites:
            with self.subTest(suite=name):
                self.assertIn(f'("{name}", {fixture}.RunAsync)', runner)
        self.assertIn("args.Length", runner)
        self.assertNotIn("Directory.Delete(", runner)

    def test_runner_selection_is_prevalidated_before_fixture_dispatch(self):
        runner = source("tests/LinuxRegression/Program.cs")
        # Source wiring only: runtime argument handling remains unexecuted.
        markers = (
            "if (!OperatingSystem.IsLinux())",
            "var suites =",
            "if (args.Length == 0)",
            "selected.AddRange(suites);",
            "foreach (var name in args)",
            "if (index < 0)",
            'throw new ArgumentException("Unknown Linux regression suite.", nameof(args));',
            "selected.Add(suites[index]);",
            "foreach (var suite in selected)",
            "await suite.Run();",
        )
        for marker in markers:
            self.assertIn(marker, runner)
        offsets = [runner.index(marker) for marker in markers]
        self.assertEqual(sorted(offsets), offsets)
        self.assertIn("StringComparison.Ordinal", runner)
        self.assertEqual(1, runner.count("await suite.Run();"))


if __name__ == "__main__":
    unittest.main()
