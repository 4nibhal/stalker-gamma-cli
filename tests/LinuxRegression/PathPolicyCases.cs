using System.Text.Json;
using Stalker.Gamma.Extensions;
using Stalker.Gamma.Factories;
using Stalker.Gamma.Filesystem;
using Stalker.Gamma.GammaInstallerServices;
using Stalker.Gamma.ModDb.Models;
using Stalker.Gamma.Models;

namespace LinuxRegression;

// Production-C# behavioral fixtures only: written, not compiled or executed in this batch.
internal static class PathPolicyCases
{
    internal static async Task RunAsync()
    {
        ManifestTransportsRoundTripLiteralZero();
        InstructionTokenErrorsRetainUnsafeCause();
        BlankAndExplicitNoOpSelectDifferentRoles();
        BlankJsonPatchDoesNotBecomeRequiredAuthor();
        JsonPatchPrefixAndAuthorAreFaithful();
        SelectedRolesKeepOptionalFieldsAndPrecedence();
        SelectedRolesValidateRawContributorsBeforeDecoration();
        SuppliedUnsafeArchiveIsNotIgnored();
        GithubRepositoryIsValidatedBeforeArchiveDecoration();
        TsvPreservesPhysicalColumnsAndChecksBeforeTrimming();
        CompleteListsAreValidatedBeforeFactoryOutput();
        DownloadableFactoryConsumesOnlyPreparedValues();
        ComparisonDtosRemainConstructibleAndHashByValue();
        await DiffConsumersRetainUnrelatedRulesAsync();
    }

    private static void ManifestTransportsRoundTripLiteralZero()
    {
        var records = new ModPackMakerRecordFactory().Create("Label\t0:\nLabel\t0\nLabel\t\r\nLabel\tfolder\\child::");
        Check.SequenceEqual(new[] { "0" }, records[0].Instructions.Paths);
        Check.Equal(false, records[1].Instructions.IsBlank);
        Check.Equal(0, records[1].Instructions.Paths.Count);
        Check.Equal(true, records[2].Instructions.IsBlank);
        Check.SequenceEqual(new[] { "folder/child" }, records[3].Instructions.Paths);

        foreach (var record in records)
        {
            Check.True(record.Instructions.Equals(RoundTrip(record).Instructions), "production TSV/JSON converter round trip");
        }
        var listJson = JsonSerializer.Serialize(records, ModPackMakerCtx.Default.ListModPackMakerRecord);
        var listRoundTrip = JsonSerializer.Deserialize(listJson, ModPackMakerCtx.Default.ListModPackMakerRecord)!;
        Check.SequenceEqual(records.Select(x => x.Instructions), listRoundTrip.Select(x => x.Instructions), "production source-generated list context");

        var parsed = InstructionSet.FromParsed(["0"]);
        var literal = new ModPackMakerRecord { DlLink = "label", Instructions = parsed };
        var json = JsonSerializer.Serialize(literal, ModPackMakerCtx.Default.ModPackMakerRecord);
        using var document = JsonDocument.Parse(json);
        Check.SequenceEqual(new[] { "0", "" }, document.RootElement.GetProperty("instructions").EnumerateArray().Select(x => x.GetString()!));
        Check.True(parsed.Equals(RoundTrip(literal).Instructions), "production converter preserves parsed literal zero");

        var colon = Read("""{"dlLink":"label","instructions":["folder:child"]}""");
        Check.SequenceEqual(new[] { "folder:child" }, RoundTrip(colon).Instructions.Paths);
        var separate = Read("""{"dlLink":"label","instructions":["C","folder"]}""");
        Check.SequenceEqual(new[] { "C", "folder" }, RoundTrip(separate).Instructions.Paths);
        Check.Equal(true, Read("""{"dlLink":"label","instructions":null}""").Instructions.IsBlank);
        Check.Equal(true, Read("""{"dlLink":"label"}""").Instructions.IsBlank);
        Check.Equal(false, Read("""{"dlLink":"label","instructions":[]}""").Instructions.IsBlank);
    }

    private static void InstructionTokenErrorsRetainUnsafeCause()
    {
        foreach (var token in new[]
        {
            "\"folder\"", "{}", "true", "1", "[null]", "[\"folder\",1]",
            "[\"../private-token\"]", "[\"folder\\n\"]", "[\"C:child\"]",
        })
        {
            var error = Check.Throws<JsonException>(() => Read("{\"dlLink\":\"label\",\"instructions\":" + token + "}"));
            Check.True(UnsafePathException.IsUnsafe(error), "JSON token/path errors retain the unsafe cause");
            Check.True(!error.ToString().Contains("private-token", StringComparison.Ordinal), "errors do not echo remote fields");
        }
    }

    private static void BlankAndExplicitNoOpSelectDifferentRoles()
    {
        var records = new ModPackMakerRecordFactory().Create("Separator\t0\nSeparator\t");
        records[1].Counter = 90;
        var separators = new SeparatorsFactory().Create(records);
        Check.Equal(1, separators.Count);
        Check.Equal("2- Separator_separator", separators[0].FolderName, "list position, not Counter");
        Check.Equal(RecordRole.Unsupported, RecordPathPolicy.Prepare(records[0]).Role);
        Check.Equal(RecordRole.Separator, RecordPathPolicy.Prepare(records[1]).Role);
        Check.Equal(RecordRole.Unsupported, RecordPathPolicy.Prepare(new ModPackMakerRecord
        {
            DlLink = "label", Instructions = InstructionSet.FromParsed([]),
        }).Role);
        Check.Equal(0, new SeparatorsFactory().Create([Read("""{"dlLink":"label","instructions":[]}""")]).Count);
        Check.Equal(1, new SeparatorsFactory().Create([Read("""{"dlLink":"label","instructions":[""]}""")]).Count);
    }

    private static void BlankJsonPatchDoesNotBecomeRequiredAuthor()
    {
        foreach (var token in new[] { "null", "\"\"", "\"  \"" })
        {
            var record = Read("{\"dlLink\":\"https://moddb.com/addons/mod\",\"addonName\":\"Mod\",\"zipName\":\"mod.zip\",\"patch\":" + token + "}");
            Check.True(string.IsNullOrWhiteSpace(record.Patch), "blank JSON author stays blank, not '- '");
            Check.Equal(RecordRole.Unsupported, RecordPathPolicy.Prepare(record).Role, "complete ModDB requires a real patch");
            Check.Equal(token == "null" ? null : token == "\"\"" ? "" : "  ", RoundTrip(record).Patch);
            record.ZipName = null;
            Check.Equal(RecordRole.MetadataOnlyModDb, RecordPathPolicy.Prepare(record).Role);
            record.DlLink = "https://github.com/owner/repository/archive/main.zip";
            Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(record));
        }
    }

    private static void JsonPatchPrefixAndAuthorAreFaithful()
    {
        var record = Read("""{"dlLink":"label","patch":"- - d'Artagnan ñ"}""");
        Check.Equal("- - - d'Artagnan ñ", record.Patch);
        Check.Equal(record.Patch, RoundTrip(record).Patch);
        using (var document = JsonDocument.Parse(JsonSerializer.Serialize(record, ModPackMakerCtx.Default.ModPackMakerRecord)))
        {
            Check.Equal("- - d'Artagnan ñ", document.RootElement.GetProperty("patch").GetString());
        }
        var spaced = Read("""{"dlLink":"label","patch":"  Author  "}""");
        Check.Equal("-   Author  ", spaced.Patch, "ordinary author whitespace is not rewritten");
        Check.Equal(spaced.Patch, RoundTrip(spaced).Patch);
        var unprefixed = new ModPackMakerRecord { DlLink = "label", Patch = "Author" };
        using (var document = JsonDocument.Parse(JsonSerializer.Serialize(unprefixed, ModPackMakerCtx.Default.ModPackMakerRecord)))
        {
            Check.Equal("Author", document.RootElement.GetProperty("patch").GetString());
        }
        foreach (var token in new[] { "\"/root\"", "\"..\"", "\"C:author\"", "\"\\tAuthor\"", "\"Author\\r\"", "\"\\t\"" })
        {
            var error = Check.Throws<JsonException>(() => Read("{\"dlLink\":\"label\",\"patch\":" + token + "}"));
            Check.True(UnsafePathException.IsUnsafe(error), "raw author checked before display prefix");
        }
    }

    private static void SelectedRolesKeepOptionalFieldsAndPrecedence()
    {
        var complete = ModDbRecordDto();
        var prepared = RecordPathPolicy.Prepare(complete);
        Check.Equal(RecordRole.StandardModDb, prepared.Role);
        Check.Equal("7- Mod - Author", prepared.OutputDirectoryName);
        Check.Equal("mod.zip", prepared.ArchiveName);
        Check.True(ReferenceEquals(complete.Instructions, prepared.Instructions), "owned value is transported, not reparsed");

        complete.ZipName = null;
        complete.Patch = null;
        var metadata = RecordPathPolicy.Prepare(complete);
        Check.Equal(RecordRole.MetadataOnlyModDb, metadata.Role);
        Check.Equal("7- Mod", metadata.OutputDirectoryName);
        Check.Equal<string?>(null, metadata.ArchiveName);
        complete.Patch = " - Optional ";
        Check.Equal("7- Mod - Optional", RecordPathPolicy.Prepare(complete).OutputDirectoryName);

        complete.DlLink = "https://moddb.com/github/owner/repository";
        complete.ZipName = "mod.zip";
        Check.Equal(RecordRole.StandardModDb, RecordPathPolicy.Prepare(complete).Role, "ModDB precedence is unchanged");
        complete.ZipName = " ";
        Check.Equal(RecordRole.MetadataOnlyModDb, RecordPathPolicy.Prepare(complete).Role);
        complete.DlLink = "https://github.com/owner/repository/archive/main.zip";
        var github = RecordPathPolicy.Prepare(complete);
        Check.Equal(RecordRole.Github, github.Role);
        Check.Equal("repository.zip", github.ArchiveName);

        var incomplete = ModDbRecordDto();
        incomplete.Patch = null;
        Check.Equal(RecordRole.Unsupported, RecordPathPolicy.Prepare(incomplete).Role);
        Check.Equal(RecordRole.Unsupported, RecordPathPolicy.Prepare(new ModPackMakerRecord { DlLink = "ordinary", AddonName = "comparison" }).Role);
    }

    private static void SelectedRolesValidateRawContributorsBeforeDecoration()
    {
        foreach (var invalid in new[] { ".", "..", "/root", @"\root", " C:child", "Mod\n", "\vMod", "part/child", @"part\child" })
        {
            var addon = ModDbRecordDto();
            addon.AddonName = invalid;
            Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(addon));
            var patch = ModDbRecordDto();
            patch.Patch = invalid;
            Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(patch));
            var separator = new ModPackMakerRecord { DlLink = invalid };
            Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(separator));
        }
        var metadata = ModDbRecordDto();
        metadata.ZipName = null;
        metadata.Patch = "\r";
        Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(metadata));
        var github = ModDbRecordDto();
        github.DlLink = "https://github.com/owner/repository/archive/main.zip";
        github.AddonName = null;
        Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(github));
        github.AddonName = "Mod";
        github.Patch = null;
        Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(github));
    }

    private static void SuppliedUnsafeArchiveIsNotIgnored()
    {
        foreach (var invalid in new[] { "../mod.zip", "/mod.zip", @"C:\mod.zip", ".", "..", "mod.zip\r", "\r" })
        {
            var complete = ModDbRecordDto();
            complete.ZipName = invalid;
            Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(complete));
            complete.DlLink = "https://github.com/owner/repository/archive/main.zip";
            Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(complete));
            var unsupported = new ModPackMakerRecord { DlLink = "ordinary", ZipName = invalid };
            Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(unsupported));
        }
    }

    private static void GithubRepositoryIsValidatedBeforeArchiveDecoration()
    {
        foreach (var repository in new[] { "", ".", "..", "C:repo", @"repo\child", "repo\r" })
        {
            var record = ModDbRecordDto();
            record.DlLink = $"https://github.com/owner/{repository}/archive/main.zip";
            Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(record));
        }
        var missing = ModDbRecordDto();
        missing.DlLink = "github";
        Check.Throws<UnsafePathException>(() => RecordPathPolicy.Prepare(missing));
        var ordinary = ModDbRecordDto();
        ordinary.DlLink = "https://github.com/owner/d'Artagnan ñ/archive/main.zip";
        Check.Equal("d'Artagnan ñ.zip", RecordPathPolicy.Prepare(ordinary).ArchiveName);
    }

    private static void TsvPreservesPhysicalColumnsAndChecksBeforeTrimming()
    {
        var factory = new ModPackMakerRecordFactory();
        var records = factory.Create(" Label \t0\r\n \t \r\n Label \t\r\n\nLabel\t0\t - - Author \t Mod \t nice \t mod.zip \t ABC ");
        Check.Equal(3, records.Count, "ordinary blank physical lines are skipped");
        Check.Equal("Label", records[0].DlLink);
        Check.Equal(false, records[0].Instructions.IsBlank);
        Check.Equal(true, records[1].Instructions.IsBlank, "trailing instruction column survives CRLF framing");
        Check.Equal("- - Author", records[2].Patch, "TSV retains its existing display spelling and trim");
        Check.Equal("Mod", records[2].AddonName);
        Check.Equal("ABC", records[2].Md5ModDb);
        Check.Equal(3, records[2].Counter);
        Check.Equal(true, factory.Create("Label")[0].Instructions.IsBlank, "absent trailing columns remain absent");

        foreach (var text in new[]
        {
            "\v", "\f", "\r", "\vLabel\t", "Label\t\v0", "Label\t0\r",
            "https://moddb.com/addon\t0\t- Author\t\vMod\tnice\tmod.zip\tABC",
            "https://moddb.com/addon\t0\t- Author\r\tMod\tnice\tmod.zip\tABC",
            "https://moddb.com/addon\t0\t- Author\tMod\tnice\t\fmod.zip\tABC",
        })
        {
            Check.Throws<UnsafePathException>(() => factory.Create(text));
        }
    }

    private static void CompleteListsAreValidatedBeforeFactoryOutput()
    {
        Check.Throws<UnsafePathException>(() => new ModPackMakerRecordFactory().Create("Safe\t\n../private-token\t"));
        var invalid = ModDbRecordDto();
        invalid.AddonName = "../private-token";
        var records = new List<ModPackMakerRecord> { new() { DlLink = "Safe" }, invalid };
        var error = Check.Throws<UnsafePathException>(() => new SeparatorsFactory().Create(records));
        Check.True(!error.ToString().Contains("private-token", StringComparison.Ordinal), "complete-list rejection does not print records");
        Check.Throws<UnsafePathException>(() => RecordPathPolicy.ValidateAll(records));
    }

    private static void DownloadableFactoryConsumesOnlyPreparedValues()
    {
        using var clients = new MemoryClients();
        // Null sink dependencies are intentional: no curl/Git/extractor service can be reached.
        // Only constructors and in-memory properties/ToString are used; no record operation is invoked.
        var factory = new DownloadableRecordFactory(clients, new GammaProgress(), null!, null!, null!, null!, null!);
        var record = ModDbRecordDto();
        record.Instructions = InstructionSet.FromParsed(["0"]);
        Check.True(factory.TryCreate("/unused/gamma", record, out var moddb), "standard ModDB selected");
        Check.True(moddb is ModDbRecord, "standard role has the existing concrete type");
        Check.Equal("/unused/gamma/downloads/mod.zip", moddb!.DownloadPath);
        Check.True(moddb.ToString()!.Contains("Instructions: 0", StringComparison.Ordinal), "literal zero reaches the IList constructor");

        record.ZipName = null;
        record.Patch = null;
        Check.True(factory.TryCreate("/unused/gamma", record, out var metadata), "optional metadata role selected");
        Check.True(metadata is ModDbRecordGetMetadata, "metadata role has the existing concrete type");
        Check.Equal("/unused/gamma/mods/7- Mod", ((ModDbRecordGetMetadata)metadata!).ExtractPath);

        record.DlLink = "https://github.com/owner/repository/archive/main.zip";
        record.Patch = "- Author";
        Check.True(factory.TryCreate("/unused/gamma", record, out var github), "GitHub selected");
        Check.True(github is GithubRecord, "GitHub role has the existing concrete type");
        Check.Equal("repository.zip", github!.ArchiveName);
        Check.True(github.ToString()!.Contains("Instructions: 0", StringComparison.Ordinal), "GitHub does not reinterpret literal zero");
        Check.Equal(1, clients.CreateCalls);

        record.AddonName = "../private-token";
        Check.Throws<UnsafePathException>(() => factory.TryCreate("/unused/gamma", record, out _));
        Check.Equal(1, clients.CreateCalls, "validation precedes HTTP-client construction");
        Check.Equal(false, factory.TryCreate("/unused/gamma", new ModPackMakerRecord { DlLink = "label" }, out var unsupported));
        Check.True(unsupported is null, "separator is not a downloadable record");
        Check.Equal(0, clients.Handler.Requests, "even accidental HTTP is rejected in memory");
    }

    private static void ComparisonDtosRemainConstructibleAndHashByValue()
    {
        var first = ComparisonRecord(InstructionSet.FromParsed(["folder/child"]));
        var second = ComparisonRecord(InstructionSet.FromJsonTokens([@"folder\child", "", ""]));
        first.AddonName = second.AddonName = "../comparison-only";
        second.Counter = 999;
        Check.True(first.Equals(second) && first.Equals((object)second), "comparison DTOs do not require install preparation");
        Check.Equal(first.GetHashCode(), second.GetHashCode());
        Check.True(new HashSet<ModPackMakerRecord> { first }.Contains(second), "owned instruction value participates in record hashing");
        second.Md5ModDb = "abcd";
        Check.Equal(false, first.Equals(second), "record equality keeps its case-sensitive non-instruction fields");
    }

    private static async Task DiffConsumersRetainUnrelatedRulesAsync()
    {
        var old = ComparisonRecord(InstructionSet.FromParsed(["folder/child"]));
        var equal = ComparisonRecord(InstructionSet.FromJsonTokens([@"folder\child", "", ""]));
        equal.Md5ModDb = "abcd";
        equal.Patch = "-   Author";
        equal.ModDbUrl = "unrelated comparison URL";
        equal.ZipName = "other.zip";
        equal.Counter = 999;
        await AssertBothDiffsAsync(old, equal, []);

        var changedInstructions = ComparisonRecord(InstructionSet.FromParsed(["Folder/child"]));
        await AssertBothDiffsAsync(old, changedInstructions, [DiffType.Modified]);
        var changedHash = ComparisonRecord(old.Instructions);
        changedHash.Md5ModDb = "different";
        await AssertBothDiffsAsync(old, changedHash, [DiffType.Modified]);
        var changedAuthor = ComparisonRecord(old.Instructions);
        changedAuthor.Patch = "- Another Author";
        await AssertBothDiffsAsync(old, changedAuthor, [DiffType.Modified]);

        var noOp = ComparisonRecord(InstructionSet.FromTsv("0"));
        await AssertBothDiffsAsync(noOp, ComparisonRecord(InstructionSet.FromJsonTokens(["", ""])), []);
        await AssertBothDiffsAsync(noOp, ComparisonRecord(InstructionSet.Blank), [DiffType.Modified]);

        var differentIdentity = ComparisonRecord(old.Instructions);
        differentIdentity.AddonName = "Another Mod";
        Check.SequenceEqual(new[] { DiffType.Removed, DiffType.Added }, new List<ModPackMakerRecord> { old }.Diff([differentIdentity]).Select(x => x.DiffType));
        Check.SequenceEqual(new[] { DiffType.Removed }, (await new List<ModPackMakerRecord> { old }.DiffAsync(Remote([differentIdentity]).WithCancellation(CancellationToken.None))).Select(x => x.DiffType), "async diff still reports only removed/modified");
        differentIdentity.AddonName = old.AddonName;
        differentIdentity.DlLink = "different link";
        Check.SequenceEqual(new[] { DiffType.Removed, DiffType.Added }, new List<ModPackMakerRecord> { old }.Diff([differentIdentity]).Select(x => x.DiffType));
        Check.SequenceEqual(new[] { DiffType.Removed }, (await new List<ModPackMakerRecord> { old }.DiffAsync(Remote([differentIdentity]).WithCancellation(CancellationToken.None))).Select(x => x.DiffType));
    }

    private static async Task AssertBothDiffsAsync(ModPackMakerRecord old, ModPackMakerRecord next, DiffType[] expected)
    {
        Check.SequenceEqual(expected, new List<ModPackMakerRecord> { old }.Diff([next]).Select(x => x.DiffType));
        var asynchronous = await new List<ModPackMakerRecord> { old }.DiffAsync(Remote([next]).WithCancellation(CancellationToken.None));
        Check.SequenceEqual(expected, asynchronous.Select(x => x.DiffType));
    }

    private static async IAsyncEnumerable<Task<ModPackMakerRecord>> Remote(IEnumerable<ModPackMakerRecord> records)
    {
        foreach (var record in records)
        {
            await Task.CompletedTask;
            yield return Task.FromResult(record);
        }
    }

    private static ModPackMakerRecord ModDbRecordDto() => new()
    {
        Counter = 7, DlLink = "https://moddb.com/addons/mod", AddonName = "Mod",
        Patch = "- Author", ZipName = "mod.zip", Instructions = InstructionSet.FromTsv("0"),
    };

    private static ModPackMakerRecord ComparisonRecord(InstructionSet instructions) => new()
    {
        DlLink = "comparison link", AddonName = "Mod", Patch = "- Author",
        Md5ModDb = "ABCD", ModDbUrl = "comparison URL", ZipName = "mod.zip", Instructions = instructions,
    };

    private static ModPackMakerRecord Read(string json) =>
        JsonSerializer.Deserialize(json, ModPackMakerCtx.Default.ModPackMakerRecord)!;

    private static ModPackMakerRecord RoundTrip(ModPackMakerRecord record) =>
        Read(JsonSerializer.Serialize(record, ModPackMakerCtx.Default.ModPackMakerRecord));

    private sealed class MemoryClients : IHttpClientFactory, IDisposable
    {
        internal RejectingHandler Handler { get; } = new();
        private readonly HttpClient _client;
        internal int CreateCalls { get; private set; }

        internal MemoryClients() => _client = new HttpClient(Handler);

        public HttpClient CreateClient(string name)
        {
            CreateCalls++;
            return _client;
        }

        public void Dispose() => _client.Dispose();
    }

    private sealed class RejectingHandler : HttpMessageHandler
    {
        internal int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            throw new InvalidOperationException("Path-policy fixtures must not send HTTP.");
        }
    }
}
