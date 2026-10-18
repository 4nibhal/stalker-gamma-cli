# Batch 4A.1 Filesystem Foundations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development`, with the user's explicit parallel-agent instruction overriding its default sequential dispatch. Steps use checkbox (`- [ ]`) syntax. Run only independent, exclusively owned tasks concurrently; the coordinator alone edits shared integration files and git state.

**Goal:** Implement the revised first batch's owned input policy, strict checked-state inspection and managed tree operations, then stop for source review.

**Architecture:** Separate raw instruction transport, role-aware preparation, immutable inspected entries and operation-specific mapped write sets. Retain root ownership and reject complete known unsafe sets before mutation; snapshots are not race-resistant capabilities. Cache integration follows in a separately reviewed batch.

**Tech Stack:** Existing C#/.NET 10 projects, System.IO, System.Text.Json source generation, the dependency-free C# regression executable and Python standard-library structural checks. No new packages.

## Global Constraints

- Keep C#, the CLI's NativeAOT configuration, the non-AOT companion server, Linux x64/arm64, Wine payload names/INI paths, and the online/offline/update/resume flows.
- Continue on `linux-hardening` in its existing worktree. Parallel agents must have disjoint file ownership and stable dependency contracts; shared-file integration and git commits belong to the coordinator.
- Private audit evidence stays ignored.
- No artifact/dependency downloads or installations, local SDK/compiler/build/restore/test commands, workflow dispatch, installers, containers, push, merge, publication or upstream disclosure.
- Prepare production-C# behavioral fixtures before implementation. They remain written, not compiled/executed. A Python structural red/green cycle is not behavioral TDD.
- No real HTTP, native curl/Git initialization, external extractor or game installation in fixtures.
- Reject unexpected links without deletion, replacement, repair or migration; generic operations have no link exemption.
- Concurrent replacement, extractor writes, hard links, mounts, special POSIX file classification, integrity, atomic downloads and restrictive-mode policy are not established by this batch.
- The approved GAMMA/cache layout is disjoint trees or the exact real `GAMMA/downloads` cache. Enforcement belongs to the next cache batch, not an unused helper here.
- Stop after Tasks 1–6 and their review/verification. Do not execute historical Tasks 4–10 from `2026-10-18-filesystem-confinement.md`.

---

## Authority, baseline and execution tracking

Normative behavior: `docs/superpowers/specs/2026-10-17-filesystem-confinement-design.md` plus the approved architecture review. This plan replaces historical Tasks 1–3; later historical tasks require reconciliation before execution.

Starting application/source HEAD: `a06783b`. Local baseline: 16 existing Python source/dry-run checks passed; no C# test was run. The coordinator records task briefs, reports, task-scoped review packages and progress in this plan's `.superpowers/sdd/2026-10-18-filesystem-foundations/` workspace. Agents do not stage/commit or start other agents. Review each task for both spec compliance and quality; resolve load-bearing findings before dependants start.

Dependency order: Tasks 1 and 2 in parallel; after review, Tasks 3 and 4 in parallel; Task 5 after Tasks 1/2/4; Task 6 integrates and reviews the entire batch. Task 3 need not block Task 5, because their files do not overlap.

| Owner | Exclusive production/test files |
|---|---|
| Task 1 — input foundations | `Stalker.Gamma/Filesystem/{InputPolicy,InstructionSet,UnsafePathException,OperationFailure}.cs`; `tests/LinuxRegression/InputPolicyCases.cs`; `tests/test_input_policy_sources.py` |
| Task 2 — fixture infrastructure | `tests/LinuxRegression/{Check,ScratchTree,ScratchTreeCases,WineIniCases}.cs`; `tests/test_fixture_sources.py` |
| Task 3 — record transport/roles | `Stalker.Gamma/Filesystem/RecordPathPolicy.cs`; `Stalker.Gamma/Models/ModPackMakerRecord.cs`; `Stalker.Gamma/Factories/{ModPackMakerRecordFactory,DownloadableRecordFactory,SeparatorsFactory}.cs`; `Stalker.Gamma/Extensions/DiffModPackMakerLists.cs`; `tests/LinuxRegression/PathPolicyCases.cs`; `tests/test_record_policy_sources.py` |
| Task 4 — inspection | `Stalker.Gamma/Filesystem/{FileSystemBoundary,CheckedEntry,CheckedTree}.cs`; `tests/LinuxRegression/FileSystemBoundaryCases.cs`; `tests/test_inspection_sources.py` |
| Task 5 — managed operations | `Stalker.Gamma/Filesystem/ManagedCopyPlan.cs`; `Stalker.Gamma/Utilities/DirUtils.cs`; `Stalker.Gamma/GammaInstallerServices/{ProcessInstructions,CleanExtractPath,WriteAddonMetaIni}.cs`; `Stalker.Gamma/Models/Separator.cs`; `tests/LinuxRegression/ManagedTreeCases.cs`; `tests/test_managed_tree_sources.py` |
| Coordinator — Task 6 and common integration | `tests/LinuxRegression/Program.cs`; `Stalker.Gamma/Properties/AssemblyInfo.cs`; `tests/test_foundation_integration.py`; this plan/spec/review status and `tests/FILESYSTEM-REGRESSIONS.md` |

### Fixed input/transport contracts

`InputPolicy` has public static `string RequireComponent(string? value, string field)`, `string? OptionalComponent(string? value, string field)`, and `string RequireRelativePath(string value, string field)`. No I/O. Check controls before trim/normalization, reject rooted/drive/UNC and dot/parent segments, require at least one relative component; normalize instruction `\` to `/`, collapse repeated interior separators, preserve ordinary interior spaces/Unicode/apostrophes. Do not use this backslash conversion to rename arbitrary Linux/Git filesystem entries.

`InstructionSet` is a public sealed owned value with `bool IsBlank`, `IReadOnlyList<string> Paths`, static `Blank`, `FromTsv(string?)`, `FromJsonTokens(IEnumerable<string?>?)`, `FromParsed(IEnumerable<string?>?)`, `IEquatable<InstructionSet>`, consistent hash, and `IReadOnlyList<string> ToJsonTokens()`. Never expose a mutable backing collection. All entry-edge ordinary whitespace is trimmed after raw controls/root prefixes are rejected; empty markers are wire encoding only. Equality compares `IsBlank` plus ordered canonical executable paths, ordinally. Redundant blank markers no longer produce a diff by themselves; blank field versus explicit no-op remains distinct because it affects roles.

| Input | IsBlank | Executable paths | Canonical JSON tokens |
|---|---:|---|---|
| TSV null/blank; JSON null or singleton blank | true | `[]` | `[""]` |
| TSV whole `0`; JSON `[]` or `["0"]` | false | `[]` | `[]` |
| TSV `:`; JSON `["", ""]` | false | `[]` | `[]` |
| TSV `0:`; JSON `["0", ""]`; parsed `["0"]` | false | `["0"]` | `["0", ""]` |
| JSON `["folder:child"]` | false | `["folder:child"]` | `["folder:child"]` |
| JSON `["C", "folder"]` | false | `["C", "folder"]` | `["C", "folder"]` |
| Parsed empty/null list | false | `[]` | `[]` |

TSV rejects drive ambiguity `(?:^|:)\s*[A-Za-z]:` before splitting. JSON never splits on `:`. Parsed ingress never interprets `0` as a sentinel. Null members and controls reject even when the whole value would otherwise be a no-op. Defensive ownership must survive caller-list mutation.

`UnsafePathException : IOException` has `(string message, Exception? inner = null)` and `public static bool IsUnsafe(Exception error)`. `OperationFailure` has `public static bool IsCancellation(Exception error)`. Both inspect ordinary inner exceptions and **all** aggregate members. Neither classifies an unrelated IOException as an unsafe/cancelled stop. Constant field labels, not complete records/URLs, identify invalid inputs.

### Fixed fixture contracts

All fixture classes are internal in the `LinuxRegression` namespace. Each suite exports `internal static Task RunAsync()`.

`Check`: `True(bool condition, string message)`, `Equal<T>(T expected, T actual, string message = "")`, `SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = "")`, `T Throws<T>(Action action) where T : Exception`, `Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception`.

`ScratchTree : IDisposable`: `string Root`, `string Dir(string relative)`, `string File(string relative, string content)`, `string LinkFile(string relative, string target)`, `string LinkDirectory(string relative, string target)`, nested immutable `Snapshot`, `Snapshot Capture(string absolutePath)`, `void AssertUnchanged(Snapshot snapshot)`. Capture records absence, inventories, file bytes/attributes/modes and directory modes recursively without following links. Link snapshots record link metadata, not target modes. Helpers restrict all mutation paths to their owned root and reject linked ancestors; link targets may be in another independently owned scratch tree.

Teardown is independent of production policy: inspect entries top-down, unlink links as entries, delete real directories bottom-up, never use recursive-delete fallback or follow unregistered links. Validate root ownership; if inspection/cleanup fails, preserve an owned leftover and the original assertion rather than widening deletion. Tests restore CWD before disposal and explicitly aggregate assertion/cleanup failures when using failure fixtures.

### Fixed inspection contracts

Public immutable data:

```csharp
public enum EntryKind { Missing, Directory, NonDirectory }
public enum ExpectedEntry { Any, Directory, NonDirectory }
public enum MissingPolicy { Reject, Leaf, Suffix }
public sealed record CheckedEntry(string FullPath, string RelativePath, EntryKind Kind);
```

`CheckedTree` exposes `FileSystemBoundary Boundary`, `string StartPath`, `IReadOnlyList<CheckedEntry> Entries`; internal constructor copies entries into a read-only collection. Include the start entry even when missing: one `Missing` entry, never an ambiguous empty snapshot.

```csharp
public sealed class FileSystemBoundary
{
    public FileSystemBoundary(string configuredRoot, string basePath);
    public string Root { get; }
    public string Resolve(string relativePath);
    public CheckedEntry CheckPath(string path, ExpectedEntry expected = ExpectedEntry.Any,
        MissingPolicy missing = MissingPolicy.Reject, CancellationToken ct = default);
    public CheckedTree CheckTree(string path, ExpectedEntry expected = ExpectedEntry.Any,
        MissingPolicy missing = MissingPolicy.Reject, CancellationToken ct = default);
}
```

Declarations describe the contract; implementation is required by Task 4. Constructor/Resolve are lexical only. Require an absolute explicit base; resolve relative roots once. Invalid/blank roots or normalization failures become UnsafePathException. CheckPath accepts canonical full paths or paths relative to Root; return the exact canonical FullPath to be used for I/O. Containment is ordinal and component-aware; `Resolve` validates untrusted relative input, while inspected native member paths are not instruction-normalized.

Inspect each prefix from `/`, including root/leaf, using throwing attributes inspection. Reject ReparsePoint before directory classification; every intermediate existing entry must be a directory. Only missing-file/directory exceptions **after checked directory parents** permit absence. `Leaf` permits only missing final entry; `Suffix` permits absent directory suffixes; all other inspection errors stop. NonDirectory is not a claim of POSIX regular-file classification. No invented P/Invoke/stat ABI.

CheckTree inspects before descent, enumerates only top-level entries, classifies using checked results (not `child is DirectoryInfo`), materializes a complete snapshot and wraps indeterminate enumeration creation/advancement errors as unsafe stops. Preserve cancellation separately and check it even for missing/no-op results. Metadata remains evidence of checked state only; near-I/O rechecks remain necessary.

## Task 1: Pure input, owned instructions and stop classification

**Source gate:** reviewed complete at `6b12bc8`; both task verdicts approved,
no findings. C# execution and dependent integration remain separate gates.

**Files/ownership:** Task 1 row above only. **Consumes:** no production prerequisites; tests use the fixed Check contract. **Produces:** the fixed input/error contracts above, available to Tasks 3/4/5.

- [x] Write `InputPolicyCases.cs` before production files, with literal expected values for components, relative paths, every transport-table row, source-list mutation, ordered equality/hash and aggregate stop classification.

```csharp
Check.Equal("d'Artagnan ñ.dll", InputPolicy.RequireComponent("d'Artagnan ñ.dll", "fixture"));
Check.Throws<UnsafePathException>(() => InputPolicy.RequireRelativePath(@"\root", "fixture"));
Check.Throws<UnsafePathException>(() => InstructionSet.FromTsv("folder:C:child"));
Check.SequenceEqual(new[] { "folder:child" }, InstructionSet.FromJsonTokens(["folder:child"]).Paths);
var literal = InstructionSet.FromParsed(["0"]);
Check.SequenceEqual(new[] { "0", "" }, literal.ToJsonTokens());
Check.True(literal.Equals(InstructionSet.FromJsonTokens(literal.ToJsonTokens())), "literal-zero round trip");
Check.True(!UnsafePathException.IsUnsafe(new IOException("ordinary")), "ordinary I/O remains ordinary");
```

- [x] Add `tests/test_input_policy_sources.py` (standard-library unittest, structural only). Read missing paths as empty text so missing feature assertions fail rather than import errors. Assert owned instruction transport and both recursive classifiers exist; never reimplement validation in Python.
- [x] Run `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests.test_input_policy_sources -v`; record expected structural failure before implementation.
- [x] Implement only the four Task 1 production files. Use a private copied string array wrapped with `Array.AsReadOnly`, `SequenceEqual(..., StringComparer.Ordinal)` and a matching HashCode accumulator. Raw ingress retains blankness; ToJsonTokens disambiguates literal zero. Parse/validation errors cannot print remote records.
- [x] Run the targeted Python command and all Python tests. Self-review raw-versus-parsed zero, controls before trimming, absolute-prefix whitespace, empty relative path rejection and defensive ownership. Report C# fixtures uncompiled/unexecuted; no behavior-pass claim. Full discovery at that point reported the pending coordinator runner failures, not a green batch.
- [x] Write the implementer report in the task workspace. Coordinator reviews spec/quality, resolves findings and commits exactly this task's paths; no agent git mutation.

## Task 2: Independent scratch/assertion infrastructure

**Source gate:** reviewed complete at `a5221f2`; both task verdicts approved,
no findings. C# execution and common runner integration remain separate gates.

**Files/ownership:** Task 2 row only. **Consumes:** only System.IO and its own assertions. **Produces:** fixed Check/ScratchTree/suite contracts above. No dependency on production policy; may run in parallel with Task 1.

- [x] Write `ScratchTreeCases.cs` first: mutate a captured file/directory mode/inventory and assert detection; reject helper `../escape` and linked-parent writes; deliberately fail inside a linked tree then independently dispose it and verify a second owned target tree unchanged.

```csharp
using var outside = new ScratchTree();
var sentinel = outside.File("keep.txt", "unchanged");
var before = outside.Capture(outside.Root);
var inside = new ScratchTree();
inside.LinkDirectory("external", outside.Root);
try { Check.True(false, "deliberate assertion"); }
catch (InvalidOperationException) { }
finally { inside.Dispose(); }
outside.AssertUnchanged(before);
Check.Equal("unchanged", System.IO.File.ReadAllText(sentinel));
```

Expand the example with an unregistered link created directly under the owned root, wrong-root helper paths and explicit preservation of original plus cleanup failures. No real user settings or outside directories.
- [x] Add `tests/test_fixture_sources.py`; observe targeted structural red using `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests.test_fixture_sources -v` before helpers exist.
- [x] Implement `Check.cs` and `ScratchTree.cs`; no production cleanup reuse and no `Directory.Delete(..., recursive: true)` fallback. Preserve the original Wine INI assertions in `WineIniCases.cs`, now using owned safe teardown. Do not edit Program.cs.
- [x] Run targeted Python tests plus the original baseline; full discovery deferred during peer/coordinator structural red. Source-review snapshot ownership, root/ancestor checks, directory modes, expected-absent snapshots, CWD restoration and link unlinking. C# remains unexecuted.
- [x] Write report; coordinator reviews and commits only owned paths, integrating Program separately.

## Task 3: Manifest transport and one role-aware preparation

**Source gate:** reviewed complete at `d8909ec`; both task verdicts approved,
no findings. C# execution and complete installer/runner integration are separate.

**Files/ownership:** Task 3 row only. **Consumes:** Task 1 InstructionSet/InputPolicy and Task 2 Check. **Produces:** typed `ModPackMakerRecord.Instructions`, converter, prepared record role/names, factory transport and instruction equality in both diffs. Direct downloadable constructors keep existing IList parameters in this batch; pass a fresh list from owned Paths. Their extraction entry guards/metadata lifecycle remain later integration, not claimed fixed here.

Internal `RecordRole` is Unsupported, Separator, StandardModDb, MetadataOnlyModDb or Github. Internal `PreparedRecord` contains selected role, `InstructionSet Instructions`, nullable `string OutputDirectoryName` and `string ArchiveName`. `RecordPathPolicy.Prepare(ModPackMakerRecord)` performs one pure role selection/validation; `ValidateAll(IReadOnlyList<ModPackMakerRecord>)` completes before factory output. Both factories consume preparation rather than separate inconsistent role predicates. Separator numbering remains list position, not Counter.

Selection precedence matches current factories: complete ModDB; metadata ModDB with addon/absent archive and optional patch; GitHub with required addon/patch (missing required fields throw); unsupported/incomplete ordinary rows stay unsupported. Comparison DTOs stay constructible without install-only requirements. Validate supplied archive fields and all selected raw/composed path contributors before prefix decoration can hide invalid components.

Patch decision: null/blank JSON patch remains null/blank, rather than manufacturing `"- "`; this deliberately corrects role selection for a blank required author. Validate nonblank raw patch before adding exactly one `"- "` display prefix. Writer removes at most that exact prefix, not a set of leading characters. TSV retains its existing ordinary-whitespace trim and display representation; no unrelated author-format rewrite. Record this intentional compatibility change in the report.

- [x] Write `PathPolicyCases.cs` first: JSON/TSV literal-zero round trips, blank versus explicit-zero separator roles, blank JSON author, prefix/author fidelity, selected optional roles, supplied unsafe archive, comparison DTOs, equal-value hash and both diff consumers retaining unrelated identity/hash/author rules.

```csharp
var records = new ModPackMakerRecordFactory().Create("Separator\t0\nSeparator\t");
Check.Equal(1, new SeparatorsFactory().Create(records).Count);
var parsed = InstructionSet.FromParsed(["0"]);
var record = new ModPackMakerRecord { DlLink = "label", Instructions = parsed };
var json = JsonSerializer.Serialize(record, ModPackMakerCtx.Default.ModPackMakerRecord);
var roundTrip = JsonSerializer.Deserialize(json, ModPackMakerCtx.Default.ModPackMakerRecord)!;
Check.True(parsed.Equals(roundTrip.Instructions), "production converter round trip");
```

- [x] Add/run `tests/test_record_policy_sources.py` for structural red before model/factory edits; prohibit old joining/reparsing and require owned transport/preparation wiring. No Python policy implementation.
- [x] Change Instructions to an InstructionSet value defaulting to Blank; converter handles JSON null explicitly, token-shape errors wrap UnsafePathException inside JsonException, and writes ToJsonTokens entry-by-entry. Update equality/hash consistently and only instruction comparisons in both diff methods.
- [x] TSV splits physical LF/CRLF lines without early TrimEntries; remove only terminal CRLF carriage return and inspect raw contributing fields before existing trims. Preserve absent trailing columns and ordinary blank physical lines. Feed raw instruction field to FromTsv.
- [x] Prepare the complete known list before returning selected records/separators. Remove the lossy private ProcessInstructions parser. Validate derived GitHub repository/archive components before construction; do not change network host selection or print rejected records.
- [x] Run targeted/all Python tests, inspect all eight pre-existing instruction references and JSON-context use; report changed equality/patch semantics explicitly. C#/source generation remains uncompiled/unexecuted. The discovery snapshot's peer/coordinator failures are recorded separately, not a green batch.
- [x] Write report; coordinator performs task review and scoped commit, reserving DownloadableRecordFactory to this owner until task completion.

## Task 4: Typed strict path/tree inspection

**Source gate:** reviewed complete at `fcac037`; both task verdicts approved,
no findings. Canonical-path use in mutating operations remains Task 5 work.

**Files/ownership:** Task 4 row only. **Consumes:** Task 1 InputPolicy/UnsafePathException and Task 2 ScratchTree/Check. **Produces:** fixed inspection contracts above for Task 5. No cache alias exception.

- [x] Write `FileSystemBoundaryCases.cs` first: lexical-only construction; relative configured root/stable base; sibling prefix/case differences; root/ancestor/leaf/intermediate contained/external/dangling/cyclic links; directory-versus-leaf expectations; missing-leaf versus suffix; tree start represented when missing; pre-cancellation and inaccessible/enumeration errors where test permissions permit.

```csharp
using var tree = new ScratchTree();
var root = tree.Dir("root");
var boundary = new FileSystemBoundary(root, tree.Root);
Check.Equal(EntryKind.Missing, boundary.CheckTree(boundary.Resolve("new/child"),
    ExpectedEntry.Directory, MissingPolicy.Suffix).Entries.Single().Kind);
var ordinary = tree.File("root/file", "bytes");
Check.Throws<UnsafePathException>(() => boundary.CheckPath(Path.Join(ordinary, "child"),
    ExpectedEntry.Any, MissingPolicy.Suffix));
tree.LinkFile("root/link", ordinary);
Check.Throws<UnsafePathException>(() => boundary.CheckTree(root));
```

- [x] Add/run `tests/test_inspection_sources.py` for structural red before production files exist; classify these as wiring checks only.
- [x] Implement immutable entry/tree data and FileSystemBoundary. Check every existing prefix from `/`; reject ReparsePoint first. Normalize once and use returned canonical paths; do not use File.Exists/Directory.Exists to infer absence. Directory parents are required even when suffix creation is allowed.
- [x] Implement top-level-only tree materialization with inspected-kind descent; catch inspection and enumeration errors without swallowing cancellation. No public skip-link predicate, native ABI or claim that NonDirectory proves regular-file type. CheckTree includes root or explicit missing root.
- [x] Run targeted/all Python checks; compare new C# parser diagnostics against baseline with the installed parse-only interpreter, without compiling. Report target-runtime semantics unverified. Selected source/dry-run checks passed; full runner wiring remains a later gate.
- [x] Write report; coordinator reviews/commits owned paths before Task 5 consumes this contract.

## Task 5: Complete managed-operation preparation and execution

**Source gate:** approved after `b9eeaef`, I1 correction `fe60971` and the
user-authorized B1 correction `8830dff`. The final B1 scoped review approves
source-spec and quality with no new findings; I1 and B1 are source-addressed.
C# remains uncompiled/unexecuted. Helper-local stops are not whole-installer
propagation, and no later integration/security batch is declared complete.

**Files/ownership:** Task 5 row only. **Consumes:** Tasks 1/2/4; independent of Task 3 files. **Produces:** strict public DirUtils operations, narrow internal ManagedCopyPlan, guarded instructions/cleanup/metadata/separator writes.

Preserve the public CopyDirectory signature, NormalizePermissions(string), RecursivelyDeleteDirectory(string,IReadOnlyList<string>) and Separator interface. Add internal token-aware overloads where useful; `CountFiles(string, string?, CancellationToken = default)` may be internal. ProcessInstructions.Process(string,IList<string>,CancellationToken) validates/copies via InstructionSet.FromParsed, never reinterpreting literal zero. CleanExtractPath.Clean may add an optional CancellationToken without breaking existing calls.

The reviewed `FromParsed` ingress accepts executable paths, not blank markers.
For the legacy IList instruction boundary, first copy/check **every** member with
internal `InputPolicy.PrepareValue(member, "instruction")`, omit empty results,
then call FromParsed. Controls/null members still reject before no-op handling;
literal `0` is never omitted. This adapter preserves the approved blank-marker
no-op behavior without weakening the owned domain value.

`ManagedCopyPlan` is internal and owns canonical source/destination boundaries, checked trees, selected/countable files, mapped directory/file targets and their execution order. `DirUtils.PrepareCopy(string sourceDir,string destDir,bool overwrite,string? fileFilter,bool moveFile,CancellationToken ct)` returns it; `DirUtils.ExecuteCopy(ManagedCopyPlan,Action<double>?,Action<string>?,CancellationToken)` executes. Public CopyDirectory calls those in order. Do not introduce a general operation DSL.

- [x] Write `ManagedTreeCases.cs` first, with separate owned trees per scenario. Capture source/destination **directory** modes/inventories plus external file bytes/modes and expected-absent outputs. Include deterministic later-item instruction/type failures; filesystem names alone do not prove traversal order.

```csharp
using var outside = new ScratchTree();
var external = outside.File("keep.txt", "unchanged");
using var tree = new ScratchTree();
var source = tree.Dir("source");
tree.File("source/safe.txt", "stay");
tree.LinkFile("source/link.txt", external);
var absent = Path.Join(tree.Root, "absent");
var destinationBefore = tree.Capture(absent);
var sourceBefore = tree.Capture(source);
var outsideBefore = outside.Capture(outside.Root);
Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(source, absent, moveFile: true));
tree.AssertUnchanged(destinationBefore);
tree.AssertUnchanged(sourceBefore);
outside.AssertUnchanged(outsideBefore);
```

Required other cases: source/destination leaf/parent/root links and cycles; links in filtered/.git/retained cleanup content; later file→directory/directory→file mapped conflicts; safe identical paths; descendant rejection; ancestor flattening whose deeper output would overwrite an unread shallower source; overwrite/filter/nonempty selection; no unchecked count/hash; original cleanup dot-file/retained-directory selection; empty/no-op parsed instructions create nothing but check cancellation; later sources produced by earlier instructions; invalid second instruction before first move; linked meta.ini/invalid direct separator; callback cancellation including last callback prevents follow-up.

- [x] Add/run `tests/test_managed_tree_sources.py` for structural red before production edits. Check preparation-before-execution wiring and removal of unchecked AllDirectories walks, not behavior by marker count.
- [x] Prepare source/destination existing trees and **all mapped targets** before callbacks, hash opens or output creation. Validate target types, proposed file/parent conflicts, duplicate canonical mappings and overlap. Reject descendant destinations; check identical paths before safe no-op. For ancestor flattening order source files by increasing source depth so original shallower content is consumed before deeper mappings overwrite it. Never enumerate new outputs into source traversal.
- [x] Derive progress counts, filters and nonempty-directory selection from the same snapshot. Preserve `.git` substring selection, fileFilter, overwrite and cleanup selection without letting any filter hide a link. Recheck source/destination immediately before each I/O; token checks precede no-op returns and follow every progress/text callback, including the final callback.
- [x] Permission normalization preflights the whole tree before any mode/attribute change; retain the existing directory mode mask and file attribute behavior. Cleanup preflights retained and removed entries, deletes selected checked entries bottom-up without unchecked recursive fallback. CleanExtractPath shares one operation-owned preflight across chmod/selection; inspection failure cannot become a skipped permission error.
- [x] Instructions validate whole syntax/list and the existing extraction tree before mutation, including currently known selected source/destination types. I1's preceding-mapping composition is source-addressed in `fe60971`; B1's step-local proposed-role lifetime is source-approved in `8830dff`. Then prepare each instruction from current state, preserving sources created by earlier moves. Distinguish source-containing-gamedata flattening to root from ordinary-source flattening to root/gamedata; no destination inside source. Empty parsed lists/blank markers create nothing. This checkbox records source review, not executed behavior.
- [x] WriteAddonMetaIni validates archive component and checks root plus meta.ini leaf before its unchanged UTF-8/CRLF write. Separator validates FolderName at its public entry and checks mods/selected directory/meta.ini before creating anything; Name remains a display label. Keep existing INI content.
- [x] Run targeted/all Python checks and parse-only diagnostics. Review actual source ordering and snapshots; record unchanged mode policy and unexecuted C# behavior explicitly. Do not modify record extraction/alias/bootstrap code or claim installation compatibility in this intermediate batch.
- [x] Write report; coordinator reviews and commits only this task's paths.

## Task 6: Coordinator integration, evidence and first-batch stop

**Files/ownership:** Coordinator row only. **Consumes:** reviewed Tasks 1–5. **Produces:** registered runner, internal access, exact source-verification and remote-test handoff.

- [x] Before integration edits, add/run `tests/test_foundation_integration.py` for source-only red: narrow friend assembly and Linux/named-suite runner dispatch without inline recursive teardown. Register suites only after their owner is reviewed; runner checks can remain explicitly pending until then.
- [x] Add `Stalker.Gamma/Properties/AssemblyInfo.cs` containing only `using System.Runtime.CompilerServices;` and `[assembly: InternalsVisibleTo("LinuxRegression")]`; no CLI friend access.
- [x] Replace the old top-level runner with a Linux guard and named suite dispatch: `inputs`, `scratch`, `wine-ini`, `records`, `filesystem`, `managed-tree`. No arguments runs all registered suites; unknown arguments reject; do not execute it. Register only existing reviewed suite classes while tasks are integrating, so no references to future files enter commits.

```csharp
var suites = new (string Name, Func<Task> Run)[]
{
    ("inputs", InputPolicyCases.RunAsync), ("scratch", ScratchTreeCases.RunAsync),
    ("wine-ini", WineIniCases.RunAsync), ("records", PathPolicyCases.RunAsync),
    ("filesystem", FileSystemBoundaryCases.RunAsync), ("managed-tree", ManagedTreeCases.RunAsync),
};
```

- [x] Record each actual structural red/green command/output separately from uncompiled/unexecuted C# fixture coverage in `tests/FILESYSTEM-REGRESSIONS.md`. Include the original 16-test baseline, task review findings/resolutions, intentional instruction equality/patch compatibility choices, missing alias integration and remaining inspector/runtime limitations.
- [x] Run `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -v`, `git diff --check`, XML parses and the installed tree-sitter comparison from `2026-10-17-linux-only-fork.md:955–995`. Observed 76 Python source/dry-run methods OK before broad review, 77 after I1 and 78 after B1; the coordinator's final discovery uses `-q`. Across 147 C# files there are no added diagnostic files over six original baseline diagnostic files; six XML files parse. Parse-only is not compilation.
- [x] Generate task-scoped review packages and a final first-batch diff package including new files, with a fresh read-only reviewer checking spec **and** quality. The broad review and subsequent scoped corrections identify I1 and B1 as source-addressed; final B1 spec/quality verdicts both approve, with no new findings. No runtime/native/network command was allowed during review, and no broad audit was repeated for the minimal B1 change.
- [x] Inspect exact staged paths before local commits; preserve ignored security evidence, existing worktree and unrelated files. Update task checkboxes/ledger only from real outputs and reviews. Integration/evidence commits remain separate from production corrections.
- [x] Original stop for user review: the final re-review left Important B1 load-bearing. The user has now approved the separate B1-only cycle below. Report source implementation/review and local structural results, not C# correctness, full 4A completion or personal-use readiness. Preserve the branch/worktree/evidence. Cache/extractor/metadata/resume/installer integrations are later batches. Remote verification still needs separate consent and authenticated pinned build inputs.

## Authorized B1-only continuation

After the blocked handoff at `4f2307d`, the user explicitly authorized another
B1-only round and requested several subagents and faster execution. This
supersedes the previous one-wave stop for this correction only. The approved
functional design is unchanged: preserve sequential produced-source semantics,
reject live/same-step type conflicts before mutation, and physically prepare each
actual step. All original execution/publication constraints remain binding.

**Design:** distinguish current composed path types from one instruction's
proposed roles. Prefer a fresh `requiredKinds` dictionary per instruction; do not
discard `knownContents`, weaken kind checks, add a simulator or rewrite selection.
Clearing the shared dictionary is less explicit; retiring only consumed-file
requirements would retain other historical reservations. Independent source
analysis must check the minimal direction before implementation.

**Exclusive ownership and gates:**

| Worker | Files | Gate |
|---|---|---|
| Fixture author | `tests/LinuxRegression/ManagedTreeCases.cs`, `tests/test_managed_tree_sources.py` | Write real C# expectations and observe Python structural red before production edits |
| Production analyst/implementer | `Stalker.Gamma/GammaInstallerServices/ProcessInstructions.cs` | Analysis/report only in parallel with fixtures; edit only after the coordinator releases the red gate |
| Coordinator/reviewer | Existing plan/evidence/ledger and read-only correction review | Coordinator alone commits; no source ownership overlap |

- [x] In parallel, write the B1 fixtures and independently analyze the lifetime
  distinction. Use the exact four-step counterexample in
  `.superpowers/sdd/2026-10-18-filesystem-foundations/final-fix-review.md:41–63`.
  Expect successful final bytes `first` at `gamedata/conflict` and `last` at
  `gamedata/group/conflict/child`, with original/intermediate files consumed.
  The three-step variant omitting `gamedata/group` must still reject the live
  file/directory conflict with the whole extraction snapshot unchanged.
- [x] Add a structural check that `requiredKinds` is declared inside the
  instruction loop, before `PrepareKnownMappings`, not before the loop. Run
  `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests.test_managed_tree_sources -v`
  and record actual red. Do not execute the C# fixtures or simulate their policy
  in Python. Retain I1's exact/reverse and produced-source cases.
- [x] After red and independent analysis, make the minimum production change:

  ```csharp
  foreach (var path in parsed.Paths)
  {
      ct.ThrowIfCancellationRequested();
      var requiredKinds = new Dictionary<string, EntryKind>(StringComparer.Ordinal);
      var mappings = PrepareKnownMappings(boundary, path, knownContents, requiredKinds, ct);
      ComposeKnownMappings(boundary.Root, knownContents, mappings, ct);
  }
  ```

  Remove the old pre-loop declaration. If source analysis finds this insufficient,
  stop with the concrete counterexample rather than expand the architecture.
- [x] Run targeted and full Python source/dry-run checks, installed baseline
  parser/XML verification and whitespace checks. Commit only the three correction
  paths; give a fresh scoped reviewer the complete range from `4f2307d` plus the
  B1 report and I1 retention requirements. No repeated broad review or duplicate
  suite runs by reviewers.
- [x] Close the source-review gate: `8830dff` receives both approvals and zero
  new Critical/Important/Minor findings. I1 and B1 have no unresolved source
  findings. Final status/evidence is kept separate from production corrections.
  Stop for the user; do not start later hardening batches automatically.
