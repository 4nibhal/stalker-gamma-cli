using Stalker.Gamma.Filesystem;

namespace LinuxRegression;

internal static class InputPolicyCases
{
    internal static Task RunAsync()
    {
        ComponentsPreserveOrdinaryNames();
        ComponentsRejectUnsafeSyntaxBeforeTrimming();
        RelativePathsNormalizeOnlyInstructionSeparators();
        RelativePathsRejectEmptyAndUnsafeComponents();
        RawTransportsPreserveBlankAndNoOpProvenance();
        RawTransportsValidateEveryMemberBeforeSentinelHandling();
        ParsedIngressNeverInterpretsWireMarkers();
        LiteralZeroSurvivesJsonRoundTrip();
        InstructionValuesOwnTheirCollections();
        EqualityUsesBlanknessAndOrderedOrdinalPaths();
        StopClassificationFindsNestedAndNonFirstAggregateMembers();
        ValidationMessagesDoNotEchoRemoteValues();
        return Task.CompletedTask;
    }

    // Break caught: replacing role validation with a restrictive filename allowlist.
    private static void ComponentsPreserveOrdinaryNames()
    {
        Check.Equal("d'Artagnan ñ.dll", InputPolicy.RequireComponent("d'Artagnan ñ.dll", "fixture"));
        Check.Equal("addon  ñ.1", InputPolicy.RequireComponent("  addon  ñ.1  ", "fixture"));
        Check.Equal("folder:child", InputPolicy.RequireComponent("folder:child", "fixture"));
        Check.Equal("C", InputPolicy.RequireComponent("C", "fixture"));
        Check.Equal<string?>(null, InputPolicy.OptionalComponent(null, "fixture"));
        Check.Equal<string?>(null, InputPolicy.OptionalComponent("  \u00a0  ", "fixture"));
        Check.Equal<string?>("d'Artagnan ñ.dll", InputPolicy.OptionalComponent(" d'Artagnan ñ.dll ", "fixture"));
    }

    // Break caught: trimming controls/root prefixes into acceptable required or optional names.
    private static void ComponentsRejectUnsafeSyntaxBeforeTrimming()
    {
        foreach (var invalid in new string?[]
        {
            null, "", "   ", ".", "..", " . ", " .. ", "a/b", @"a\b",
            "/root", @"\root", @"\\host\share", "C:child", " c:child",
            " /root", @" \root", "\tname", "name\r", "\n", "a\0b", "name\u007f", "\u0085",
        })
        {
            Check.Throws<UnsafePathException>(() => InputPolicy.RequireComponent(invalid, "fixture"));
        }

        foreach (var invalid in new[] { "\t", "name\n", " /root", @" \root", " C:child" })
        {
            Check.Throws<UnsafePathException>(() => InputPolicy.OptionalComponent(invalid, "fixture"));
        }
    }

    // Break caught: failing to normalize backslashes, or stripping ordinary interior spaces.
    private static void RelativePathsNormalizeOnlyInstructionSeparators()
    {
        Check.Equal("folder/child ñ.dll", InputPolicy.RequireRelativePath(@" folder\\child ñ.dll ", "fixture"));
        Check.Equal("folder/child", InputPolicy.RequireRelativePath("folder///child//", "fixture"));
        Check.Equal("folder  one/ child ñ.dll", InputPolicy.RequireRelativePath(@"  folder  one\ child ñ.dll  ", "fixture"));
        Check.Equal("d'Artagnan ñ.dll", InputPolicy.RequireRelativePath("d'Artagnan ñ.dll", "fixture"));
        Check.Equal("folder:child", InputPolicy.RequireRelativePath("folder:child", "fixture"));
        Check.Equal("C/folder", InputPolicy.RequireRelativePath("C/folder", "fixture"));
    }

    // Break caught: allowing an empty, rooted, drive-like or traversal path through lexical validation.
    private static void RelativePathsRejectEmptyAndUnsafeComponents()
    {
        foreach (var invalid in new[]
        {
            "", "   ", ".", "..", "folder/./child", "folder/../child", @"folder\..\child",
            "folder/ ../child", "/root", @"\root", "//host/share", @"\\host\share",
            " /root", @" \root", " C:child", "c:/child", "folder/C:child",
            "folder/\tchild", "folder\n", "\r", "folder/\0child",
        })
        {
            Check.Throws<UnsafePathException>(() => InputPolicy.RequireRelativePath(invalid, "fixture"));
        }
        Check.Throws<UnsafePathException>(() => InputPolicy.RequireRelativePath(null!, "fixture"));
    }

    // Break caught: deriving IsBlank from path count, splitting JSON on colons, or losing TSV provenance.
    private static void RawTransportsPreserveBlankAndNoOpProvenance()
    {
        var cases = new (InstructionSet Actual, bool Blank, string[] Paths, string[] Tokens)[]
        {
            (InstructionSet.Blank, true, [], [""]),
            (InstructionSet.FromTsv(null), true, [], [""]),
            (InstructionSet.FromTsv(""), true, [], [""]),
            (InstructionSet.FromTsv("  "), true, [], [""]),
            (InstructionSet.FromJsonTokens(null), true, [], [""]),
            (InstructionSet.FromJsonTokens([""]), true, [], [""]),
            (InstructionSet.FromJsonTokens(["  "]), true, [], [""]),
            (InstructionSet.FromTsv("0"), false, [], []),
            (InstructionSet.FromTsv(" 0 "), false, [], []),
            (InstructionSet.FromJsonTokens([]), false, [], []),
            (InstructionSet.FromJsonTokens(["0"]), false, [], []),
            (InstructionSet.FromJsonTokens([" 0 "]), false, [], []),
            (InstructionSet.FromTsv(":"), false, [], []),
            (InstructionSet.FromJsonTokens(["", ""]), false, [], []),
            (InstructionSet.FromTsv("0:"), false, ["0"], ["0", ""]),
            (InstructionSet.FromJsonTokens(["0", ""]), false, ["0"], ["0", ""]),
            (InstructionSet.FromParsed(["0"]), false, ["0"], ["0", ""]),
            (InstructionSet.FromTsv(@"0\"), false, ["0"], ["0", ""]),
            (InstructionSet.FromJsonTokens(["0//"]), false, ["0"], ["0", ""]),
            (InstructionSet.FromJsonTokens(["folder:child"]), false, ["folder:child"], ["folder:child"]),
            (InstructionSet.FromJsonTokens(["C", "folder"]), false, ["C", "folder"], ["C", "folder"]),
            (InstructionSet.FromParsed([]), false, [], []),
            (InstructionSet.FromParsed(null), false, [], []),
            (InstructionSet.FromTsv(@" folder\\child : d'Artagnan ñ.dll : "), false,
                ["folder/child", "d'Artagnan ñ.dll"], ["folder/child", "d'Artagnan ñ.dll"]),
        };

        foreach (var item in cases)
        {
            Check.Equal(item.Blank, item.Actual.IsBlank);
            Check.SequenceEqual(item.Paths, item.Actual.Paths);
            Check.SequenceEqual(item.Tokens, item.Actual.ToJsonTokens());
        }
    }

    // Break caught: accepting a sentinel before inspecting nulls, controls, roots or TSV drive ambiguity.
    private static void RawTransportsValidateEveryMemberBeforeSentinelHandling()
    {
        foreach (var invalid in new[]
        {
            "C:folder", " C:folder", "folder:C:child", "folder: c:child", "folder:Z:",
            "\t0", "0\n", "\t", "folder:\n", @"\root", " /root", @"folder: \root",
        })
        {
            Check.Throws<UnsafePathException>(() => InstructionSet.FromTsv(invalid));
        }

        foreach (var invalid in new string?[][]
        {
            [null], ["", null], ["0", null], ["\t"], ["0\r"], ["0", "\n"],
            ["", " /root"], ["0", @" \root"], ["folder", "../child"], ["C:child"],
        })
        {
            Check.Throws<UnsafePathException>(() => InstructionSet.FromJsonTokens(invalid));
        }
    }

    // Break caught: reparsing an already-parsed literal 0, or admitting empty wire markers as paths.
    private static void ParsedIngressNeverInterpretsWireMarkers()
    {
        var parsed = InstructionSet.FromParsed([" 0 ", @"folder\child"]);
        Check.Equal(false, parsed.IsBlank);
        Check.SequenceEqual(new[] { "0", "folder/child" }, parsed.Paths);
        Check.SequenceEqual(new[] { "0", "folder/child" }, parsed.ToJsonTokens());

        foreach (var invalid in new string?[][] { [null], [""], [" "], ["0", null], ["0", "\t"], ["/root"] })
        {
            Check.Throws<UnsafePathException>(() => InstructionSet.FromParsed(invalid));
        }
    }

    // Break caught: serializing a singleton literal zero in the raw JSON no-op encoding.
    private static void LiteralZeroSurvivesJsonRoundTrip()
    {
        var literal = InstructionSet.FromParsed(["0"]);
        Check.SequenceEqual(new[] { "0", "" }, literal.ToJsonTokens());
        var reread = InstructionSet.FromJsonTokens(literal.ToJsonTokens());
        Check.Equal(false, reread.IsBlank);
        Check.SequenceEqual(new[] { "0" }, reread.Paths);
        Check.True(literal.Equals(reread), "literal-zero round trip");
        Check.True(!literal.Equals(InstructionSet.FromJsonTokens(["0"])), "literal zero is not raw no-op");
    }

    // Break caught: retaining a caller list/array, exposing it as IReadOnlyList, or leaking JSON arrays.
    private static void InstructionValuesOwnTheirCollections()
    {
        var parsedSource = new List<string?> { "folder", "child" };
        var parsed = InstructionSet.FromParsed(parsedSource);
        var originalHash = parsed.GetHashCode();
        parsedSource[0] = "changed";
        parsedSource.Add("added");
        Check.SequenceEqual(new[] { "folder", "child" }, parsed.Paths);
        Check.Equal(originalHash, parsed.GetHashCode());

        var jsonSource = new string?[] { "folder", "child" };
        var json = InstructionSet.FromJsonTokens(jsonSource);
        jsonSource[0] = "changed";
        Check.SequenceEqual(new[] { "folder", "child" }, json.Paths);

        AssertCannotMutate(parsed.Paths);
        AssertCannotMutate(parsed.ToJsonTokens());
        AssertCannotMutate(InstructionSet.Blank.Paths);
        AssertCannotMutate(InstructionSet.Blank.ToJsonTokens());
        AssertCannotMutate(InstructionSet.FromJsonTokens([]).ToJsonTokens());
        AssertCannotMutate(InstructionSet.FromParsed(["0"]).ToJsonTokens());
        Check.SequenceEqual(new[] { "folder", "child" }, parsed.Paths);
        Check.SequenceEqual(new[] { "" }, InstructionSet.Blank.ToJsonTokens());
    }

    private static void AssertCannotMutate(IReadOnlyList<string> values)
    {
        Check.True(values is not string[], "no mutable array exposure");
        if (values is IList<string> list)
        {
            Check.True(list.IsReadOnly, "exposed collection must be read-only");
            if (list.Count != 0)
            {
                Check.Throws<NotSupportedException>(() => list[0] = "changed");
            }
            Check.Throws<NotSupportedException>(() => list.Add("added"));
        }
    }

    // Break caught: comparing unordered/case-insensitive paths, losing provenance, or hashing raw markers.
    private static void EqualityUsesBlanknessAndOrderedOrdinalPaths()
    {
        var canonical = InstructionSet.FromParsed(["folder/child", "d'Artagnan ñ.dll"]);
        var raw = InstructionSet.FromTsv(@" folder\\child : :d'Artagnan ñ.dll:");
        Check.True(canonical.Equals(raw) && raw.Equals(canonical), "canonical equality is symmetric");
        Check.True(canonical.Equals((object)raw), "object equality matches typed equality");
        Check.Equal(canonical.GetHashCode(), raw.GetHashCode());
        Check.True(new HashSet<InstructionSet> { canonical }.Contains(raw), "equal values work as hash keys");
        Check.True(!canonical.Equals(InstructionSet.FromParsed(["d'Artagnan ñ.dll", "folder/child"])), "order matters");
        Check.True(!canonical.Equals(InstructionSet.FromParsed(["Folder/child", "d'Artagnan ñ.dll"])), "ordinal case matters");
        Check.True(!canonical.Equals(InstructionSet.FromParsed(["folder/child"])), "path count matters");
        Check.True(!canonical.Equals((InstructionSet?)null), "null is not equal");
        Check.True(!canonical.Equals("folder/child"), "unrelated objects are not equal");

        var noOp = InstructionSet.FromTsv("0");
        var redundantMarkers = InstructionSet.FromJsonTokens(["", ""]);
        Check.True(noOp.Equals(redundantMarkers), "redundant blank markers do not change no-op equality");
        Check.Equal(noOp.GetHashCode(), redundantMarkers.GetHashCode());
        Check.True(InstructionSet.Blank.Equals(InstructionSet.FromJsonTokens([" "])), "blank transports agree");
        Check.True(!InstructionSet.Blank.Equals(noOp), "blank and explicit no-op remain distinct");
    }

    // Break caught: looking only at the first aggregate member, or treating ordinary I/O as a stop class.
    private static void StopClassificationFindsNestedAndNonFirstAggregateMembers()
    {
        var ordinary = new IOException("ordinary");
        var unsafePath = new UnsafePathException("unsafe");
        var cancelled = new OperationCanceledException("cancelled");
        var cases = new (Exception Error, bool Unsafe, bool Cancellation)[]
        {
            (ordinary, false, false),
            (new Exception("ordinary wrapper", ordinary), false, false),
            (unsafePath, true, false),
            (cancelled, false, true),
            (new TaskCanceledException(), false, true),
            (new Exception("unsafe wrapper", unsafePath), true, false),
            (new IOException("cancellation wrapper", cancelled), false, true),
            (new AggregateException(ordinary, unsafePath), true, false),
            (new AggregateException(ordinary, cancelled), false, true),
            (new AggregateException(ordinary, new AggregateException(ordinary, unsafePath, cancelled)), true, true),
            (new Exception("outer", new AggregateException(ordinary, new Exception("inner", unsafePath))), true, false),
            (new Exception("outer", new AggregateException(ordinary, new Exception("inner", cancelled))), false, true),
            (new AggregateException(ordinary, new IOException("also ordinary")), false, false),
            (new AggregateException(), false, false),
            (new UnsafePathException("both", cancelled), true, true),
        };

        foreach (var item in cases)
        {
            Check.Equal(item.Unsafe, UnsafePathException.IsUnsafe(item.Error));
            Check.Equal(item.Cancellation, OperationFailure.IsCancellation(item.Error));
        }
    }

    // Break caught: embedding an untrusted remote value in validation errors or inner exceptions.
    private static void ValidationMessagesDoNotEchoRemoteValues()
    {
        const string remote = "../private-record?token=do-not-echo";
        var componentError = Check.Throws<UnsafePathException>(() => InputPolicy.RequireComponent(remote, "archive"));
        Check.True(componentError.Message.Contains("archive", StringComparison.Ordinal), "field label identifies input");
        Check.True(!componentError.ToString().Contains("do-not-echo", StringComparison.Ordinal), "component value is private");

        var instructionError = Check.Throws<UnsafePathException>(() => InstructionSet.FromJsonTokens([remote]));
        Check.True(!instructionError.ToString().Contains("do-not-echo", StringComparison.Ordinal), "JSON value is private");
        var tsvError = Check.Throws<UnsafePathException>(() => InstructionSet.FromTsv("folder:C:do-not-echo"));
        Check.True(!tsvError.ToString().Contains("do-not-echo", StringComparison.Ordinal), "TSV value is private");
    }
}
