"""Task 3 structural contracts only; no Python policy implementation or C# execution."""

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
MODEL = "Stalker.Gamma/Models/ModPackMakerRecord.cs"
POLICY = "Stalker.Gamma/Filesystem/RecordPathPolicy.cs"
FACTORIES = "Stalker.Gamma/Factories/"


def source(path):
    file = ROOT / path
    return file.read_text(encoding="utf-8") if file.is_file() else ""


class RecordPolicySourceTests(unittest.TestCase):
    def test_model_owns_instruction_value_and_hashes_its_semantics(self):
        text = source(MODEL)
        self.assertRegex(text, r"public InstructionSet Instructions\s*\{\s*get;\s*set;\s*\}\s*=\s*InstructionSet.Blank;")
        self.assertIn("Instructions.Equals(other.Instructions)", text)
        self.assertRegex(text, r"HashCode.Combine\([^;]*\bInstructions\b")
        self.assertNotRegex(text, r"Instructions\s*==\s*other.Instructions")
        self.assertIn("[JsonSerializable(typeof(ModPackMakerRecord))]", text)
        self.assertIn("[JsonSerializable(typeof(List<ModPackMakerRecord>))]", text)

    def test_json_converter_preserves_raw_tokens_and_explicit_null(self):
        text = source(MODEL).split("public class ModPackMakerPatchConverter", 1)[0]
        self.assertIn("JsonConverter<InstructionSet>", text)
        self.assertIn("public override bool HandleNull => true;", text)
        self.assertIn("JsonTokenType.Null", text)
        self.assertIn("InstructionSet.FromJsonTokens(null)", text)
        self.assertIn("InstructionSet.FromJsonTokens(tokens)", text)
        self.assertIn("tokens.Add(reader.GetString())", text)
        self.assertIn(".ToJsonTokens()", text)
        self.assertIn("writer.WriteStringValue(token)", text)
        self.assertNotIn("string.Join", text)
        self.assertNotIn(".Split(", text)
        self.assertNotIn("FromTsv(", text)
        self.assertNotIn("FromParsed(", text)

    def test_instruction_shape_errors_keep_the_unsafe_inner_exception(self):
        text = source(MODEL).split("public class ModPackMakerPatchConverter", 1)[0]
        self.assertRegex(text, r"throw new UnsafePathException\(\"Invalid instruction token\.\"\)")
        self.assertRegex(text, r"catch \(UnsafePathException \w+\)")
        self.assertRegex(text, r'throw new JsonException\("Invalid instruction\.", \w+\)')

    def test_patch_checks_raw_author_and_removes_only_one_exact_display_prefix(self):
        text = source(MODEL).split("public class ModPackMakerPatchConverter", 1)[1]
        self.assertIn('InputPolicy.OptionalComponent(value, "patch")', text)
        self.assertIn("string.IsNullOrWhiteSpace(value)", text)
        self.assertLess(text.index('InputPolicy.OptionalComponent(value, "patch")'), text.index('$"- {value}"'))
        self.assertIn('value.StartsWith("- ", StringComparison.Ordinal)', text)
        self.assertIn("value[2..]", text)
        self.assertNotIn("TrimStart", text)
        self.assertNotIn(".Trim()", text)

    def test_one_pure_preparation_defines_roles_and_owned_names(self):
        text = source(POLICY)
        self.assertIn("internal enum RecordRole", text)
        for role in ("Unsupported", "Separator", "StandardModDb", "MetadataOnlyModDb", "Github"):
            self.assertIn(role, text)
        self.assertRegex(text, r"internal sealed record PreparedRecord\(")
        for field in ("RecordRole Role", "InstructionSet Instructions", "string? OutputDirectoryName", "string? ArchiveName"):
            self.assertIn(field, text)
        self.assertIn("Prepare(ModPackMakerRecord record)", text)
        self.assertIn("ValidateAll(IReadOnlyList<ModPackMakerRecord> records)", text)
        self.assertIn("record.Instructions.IsBlank", text)
        self.assertNotRegex(text, r"\b(?:File|Directory|FileInfo|DirectoryInfo|HttpClient|GitService|Console)\s*[.(]")
        self.assertNotIn("record.Instructions.Paths.Count", text)
        self.assertNotIn("{record}", text)

    def test_raw_path_contributors_precede_composition_and_github_suffix(self):
        text = source(POLICY)
        self.assertIn('InputPolicy.OptionalComponent(record.ZipName, "archive")', text)
        self.assertIn('InputPolicy.RequireComponent(record.AddonName, "addon")', text)
        self.assertIn('InputPolicy.RequireComponent(record.Patch, "patch")', text)
        self.assertIn('InputPolicy.OptionalComponent(record.Patch, "patch")', text)
        self.assertIn('InputPolicy.RequireComponent(record.DlLink, "separator label")', text)
        self.assertIn('InputPolicy.RequireComponent(segments.ElementAtOrDefault(4), "repository")', text)
        self.assertLess(text.index('"repository")'), text.index('$"{repository}.zip"'))
        self.assertLess(text.index('"addon")'), text.index('$"{record.Counter}- {addon} {patch}"'))
        self.assertIn('InputPolicy.RequireComponent($"{repository}.zip", "archive")', text)
        self.assertIn('InputPolicy.RequireComponent($"{record.Counter}- {addon} {patch}", "output directory")', text)

    def test_tsv_validates_raw_columns_before_ordinary_trims(self):
        text = source(FACTORIES + "ModPackMakerRecordFactory.cs")
        self.assertIn("modpackMakerTxt.Split('\\n')", text)
        self.assertNotIn("TrimEntries", text)
        self.assertNotIn("RemoveEmptyEntries", text)
        self.assertIn("index < lines.Length - 1", text)
        self.assertIn("line.EndsWith('\\r')", text)
        self.assertIn("line[..^1]", text)
        self.assertIn("string.IsNullOrWhiteSpace(line)", text)
        self.assertIn("Instructions = InstructionSet.FromTsv(columns.ElementAtOrDefault(1))", text)
        self.assertIn("RecordPathPolicy.ValidateAll(records)", text)
        self.assertLess(text.index("RecordPathPolicy.ValidateAll(records)"), text.index(".Trim()"))
        self.assertIn("Counter = records.Count + 1", text)

    def test_download_factory_consumes_prepared_values_without_lossy_reparse(self):
        text = source(FACTORIES + "DownloadableRecordFactory.cs")
        self.assertIn("RecordPathPolicy.Prepare(record)", text)
        for role in ("StandardModDb", "MetadataOnlyModDb", "Github"):
            self.assertIn("case RecordRole." + role + ":", text)
        self.assertEqual(3, text.count("prepared.Instructions.Paths.ToList()"))
        self.assertGreaterEqual(text.count("prepared.OutputDirectoryName!"), 3)
        self.assertGreaterEqual(text.count("prepared.ArchiveName!"), 2)
        self.assertNotIn("ProcessInstructions", text)
        self.assertNotIn("TryParse", text)
        self.assertNotIn('Contains("moddb")', text)
        self.assertNotIn('Contains("github")', text)
        self.assertNotIn("Invalid record: {record}", text)

    def test_separator_factory_validates_the_complete_list_before_selecting(self):
        text = source(FACTORIES + "SeparatorsFactory.cs")
        self.assertIn("var snapshot = records.ToArray();", text)
        self.assertIn("RecordPathPolicy.ValidateAll(snapshot)", text)
        self.assertIn("RecordRole.Separator", text)
        self.assertIn("FolderName = prepared[index].OutputDirectoryName!", text)
        self.assertLess(text.index("RecordPathPolicy.ValidateAll(snapshot)"), text.index("new Separator"))
        self.assertNotIn("IsNullOrWhiteSpace", text)
        self.assertNotIn(".Counter", text)
        policy = source(POLICY)
        self.assertIn('$"{index + 1}- {item.OutputDirectoryName}"', policy)

    def test_both_diffs_change_only_instruction_comparison(self):
        text = source("Stalker.Gamma/Extensions/DiffModPackMakerLists.cs")
        self.assertEqual(2, text.count("!matchingNewRec.Instructions.Equals(old.Instructions)"))
        self.assertNotIn("matchingNewRec.Instructions != old.Instructions", text)
        self.assertEqual(3, text.count("x.AddonName == "))
        self.assertEqual(2, text.count("StringComparison.OrdinalIgnoreCase"))
        self.assertEqual(2, text.count('PatchRx().Match(matchingNewRec.Patch ?? "").Groups["author"].Value'))
        self.assertIn('[GeneratedRegex(@"^-\\s+(?<author>.+)")]', text)

    def test_csharp_fixtures_call_production_transport_roles_and_both_diff_consumers(self):
        text = source("tests/LinuxRegression/PathPolicyCases.cs")
        self.assertIn("internal static async Task RunAsync()", text)
        for case in (
            "ManifestTransportsRoundTripLiteralZero", "InstructionTokenErrorsRetainUnsafeCause",
            "BlankAndExplicitNoOpSelectDifferentRoles", "BlankJsonPatchDoesNotBecomeRequiredAuthor",
            "JsonPatchPrefixAndAuthorAreFaithful", "SelectedRolesKeepOptionalFieldsAndPrecedence",
            "SelectedRolesValidateRawContributorsBeforeDecoration", "SuppliedUnsafeArchiveIsNotIgnored",
            "GithubRepositoryIsValidatedBeforeArchiveDecoration", "TsvPreservesPhysicalColumnsAndChecksBeforeTrimming",
            "CompleteListsAreValidatedBeforeFactoryOutput", "DownloadableFactoryConsumesOnlyPreparedValues",
            "ComparisonDtosRemainConstructibleAndHashByValue", "DiffConsumersRetainUnrelatedRulesAsync",
        ):
            self.assertIn(case + "(", text)
        for consumer in ("ModPackMakerCtx.Default.ModPackMakerRecord", "ModPackMakerCtx.Default.ListModPackMakerRecord", "RecordPathPolicy.Prepare(", "RecordPathPolicy.ValidateAll(", ".Diff(", ".DiffAsync("):
            self.assertIn(consumer, text)

    def test_fixtures_cannot_fall_through_to_native_or_live_network_sinks(self):
        text = source("tests/LinuxRegression/PathPolicyCases.cs")
        self.assertIn("new HttpClient(Handler)", text)
        self.assertIn("class RejectingHandler : HttpMessageHandler", text)
        self.assertIn('throw new InvalidOperationException("Path-policy fixtures must not send HTTP.")', text)
        self.assertIn("new DownloadableRecordFactory(clients, new GammaProgress(), null!, null!, null!, null!, null!)", text)
        self.assertNotRegex(text, r"\bnew\s+(?:GitService|NetworkServiceFactory|ModDbService|ArchiveService|SevenZipService|UnzipService)\b")
        self.assertNotRegex(text, r"\.(?:DownloadAsync|ExtractAsync|WriteAsync|GetStringAsync|SendAsync|SaveAsync)\(")
        self.assertNotRegex(text, r"\b(?:File|Directory|Process)\.")
        self.assertNotIn("StalkerGammaSettings", text)


if __name__ == "__main__":
    unittest.main()
