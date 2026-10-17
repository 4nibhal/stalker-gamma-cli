"""Structural contracts only; production C# fixtures are not compiled or executed here."""

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
FILESYSTEM = "Stalker.Gamma/Filesystem/"


def source(path):
    file = ROOT / path
    return file.read_text(encoding="utf-8") if file.is_file() else ""


class InputPolicySourceTests(unittest.TestCase):
    def test_pure_input_policy_exposes_required_and_optional_contracts(self):
        text = source(FILESYSTEM + "InputPolicy.cs")
        self.assertRegex(text, r"public static class InputPolicy\b")
        for declaration in (
            r"public static string RequireComponent\(string\? value, string field\)",
            r"public static string\? OptionalComponent\(string\? value, string field\)",
            r"public static string RequireRelativePath\(string value, string field\)",
        ):
            with self.subTest(declaration=declaration):
                self.assertRegex(text, declaration)
        self.assertNotRegex(text, r"\b(?:File|Directory|FileInfo|DirectoryInfo)\s*[.(]")

    def test_owned_instruction_value_exposes_all_distinct_ingresses(self):
        text = source(FILESYSTEM + "InstructionSet.cs")
        self.assertRegex(text, r"public sealed class InstructionSet\s*:\s*IEquatable<InstructionSet>")
        for declaration in (
            r"public bool IsBlank\s*\{\s*get;\s*\}",
            r"public IReadOnlyList<string> Paths\b",
            r"public static InstructionSet Blank\b",
            r"public static InstructionSet FromTsv\(string\? value\)",
            r"public static InstructionSet FromJsonTokens\(IEnumerable<string\?>\? tokens\)",
            r"public static InstructionSet FromParsed\(IEnumerable<string\?>\? paths\)",
            r"public IReadOnlyList<string> ToJsonTokens\(\)",
        ):
            with self.subTest(declaration=declaration):
                self.assertRegex(text, declaration)

    def test_instruction_storage_is_copied_and_read_only(self):
        text = source(FILESYSTEM + "InstructionSet.cs")
        self.assertRegex(text, r"private readonly ReadOnlyCollection<string>\s+\w+")
        self.assertIn("Array.AsReadOnly(", text)
        self.assertIn(".ToArray()", text)
        self.assertNotRegex(text, r"public\s+(?:readonly\s+)?string\[\]")
        self.assertNotRegex(text, r"public\s+(?:List|IList)<string>")

    def test_instruction_equality_and_hash_use_the_same_ordinal_value(self):
        text = source(FILESYSTEM + "InstructionSet.cs")
        self.assertRegex(text, r"public bool Equals\(InstructionSet\? other\)")
        self.assertRegex(text, r"IsBlank\s*==\s*other\.IsBlank")
        self.assertRegex(text, r"\.SequenceEqual\([^;]*StringComparer\.Ordinal")
        self.assertRegex(text, r"public override int GetHashCode\(\)")
        self.assertIn("new HashCode()", text)
        self.assertRegex(text, r"\.Add\(IsBlank\)")
        self.assertRegex(text, r"\.Add\([^,]+, StringComparer\.Ordinal\)")

    def test_unsafe_stop_classifier_traverses_aggregate_and_ordinary_inners(self):
        text = source(FILESYSTEM + "UnsafePathException.cs")
        self.assertRegex(text, r"public (?:sealed )?class UnsafePathException\s*:\s*IOException")
        self.assertRegex(text, r"public UnsafePathException\(string message, Exception\? inner = null\)")
        self.assertRegex(text, r"public static bool IsUnsafe\(Exception error\)")
        self.assertIn("error is UnsafePathException", text)
        self.assertIn("AggregateException", text)
        self.assertRegex(text, r"\.InnerExceptions\.Any\(IsUnsafe\)")
        self.assertRegex(text, r"error\.InnerException[^;]*IsUnsafe\(")

    def test_cancellation_classifier_traverses_aggregate_and_ordinary_inners(self):
        text = source(FILESYSTEM + "OperationFailure.cs")
        self.assertRegex(text, r"public static class OperationFailure\b")
        self.assertRegex(text, r"public static bool IsCancellation\(Exception error\)")
        self.assertIn("error is OperationCanceledException", text)
        self.assertIn("AggregateException", text)
        self.assertRegex(text, r"\.InnerExceptions\.Any\(IsCancellation\)")
        self.assertRegex(text, r"error\.InnerException[^;]*IsCancellation\(")

    def test_real_csharp_fixtures_use_the_frozen_check_contract(self):
        text = source("tests/LinuxRegression/InputPolicyCases.cs")
        self.assertIn("namespace LinuxRegression;", text)
        self.assertIn("internal static Task RunAsync()", text)
        for consumer in (
            "InputPolicy.RequireComponent(",
            "InputPolicy.OptionalComponent(",
            "InputPolicy.RequireRelativePath(",
            "InstructionSet.FromTsv(",
            "InstructionSet.FromJsonTokens(",
            "InstructionSet.FromParsed(",
            "UnsafePathException.IsUnsafe(",
            "OperationFailure.IsCancellation(",
        ):
            with self.subTest(consumer=consumer):
                self.assertIn(consumer, text)
        self.assertIn("Check.SequenceEqual(", text)
        self.assertIn("Check.Throws<UnsafePathException>(", text)


if __name__ == "__main__":
    unittest.main()
