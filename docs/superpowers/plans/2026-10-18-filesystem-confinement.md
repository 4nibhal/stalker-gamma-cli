# Batch 4A Filesystem Boundaries Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task, inline without subagents. Steps use checkbox (`- [ ]`) syntax for tracking. The user has already selected sequential inline batches; do not dispatch agents or create another worktree.

**Goal:** Enforce the approved input and existing-filesystem link policies at managed installation, profile and cache operations without claiming a race-resistant or extractor sandbox.

**Architecture:** Share pure input parsing and a strict filesystem boundary in `Stalker.Gamma`; keep lexical containment separate from inspecting existing entries. Pass the configured downloads/cache boundary explicitly to its consumers, resolving their I/O to the checked real cache path. Preflight each affected operation's complete known source/destination set before its first mutation, retaining checks near I/O without treating them as atomic confinement.

**Tech Stack:** Existing C#/.NET 10 projects, System.IO, System.Text.Json source generation, existing LibGit2Sharp dependency, the dependency-free C# regression executable, and Python standard-library source contracts. No new packages or local .NET toolchain.

## Global Constraints

The normative specification is
`docs/superpowers/specs/2026-10-17-filesystem-confinement-design.md`.
The following requirements are copied from that specification:

- Keep C#, the CLI's NativeAOT configuration, the non-AOT companion server, Linux x64/arm64, Wine payload names/INI paths, and the online/offline/update/resume flows.
- Continue inline on `linux-hardening` in its existing worktree.
- Private audit evidence stays ignored.
- Required names must not be null, empty or whitespace-only. Reject `.` and `..`, `/` and `\` separators, control characters and rooted/drive/UNC forms.
- Preserve ordinary spaces, Unicode, apostrophes, extensions and ordinary dots inside valid names. Do not use an ASCII-only name allowlist.
- Missing optional fields retain their current meaning for the selected record role.
- A CLI settings profile's display label is not automatically a path component. Do not restrict it merely because the model calls it `ProfileName`.
- Preserve the current no-op meanings of absent/blank instructions and `0`.
- JSON array entries are checked individually; subsequent consumers use parsed entries rather than joining and reparsing them.
- Check existing components on the way to a root as well as the root itself and affected descendants.
- Linux comparisons are case-sensitive.
- A generic recursive walk must not gain permission to follow arbitrary encountered links.
- An unsafe input/tree stops the affected operation with a clear error.
- Do not silently skip a disallowed link, continue after a validation failure, mark a failed record complete, or invoke destructive cleanup as recovery.
- Existing permission-mode choices are not broadened or declared secure by this stage; only their traversal boundary is addressed.
- No claim that the security regression passes is allowed before separately authorized remote compilation/execution.

Additional execution gates carried forward from the approved work:

- This document is a proposal awaiting implementation approval. Writing it does not execute any task below.
- No artifact/dependency downloads or installations, local SDK/compiler/build/restore/test commands, workflow dispatch, installers, containers, push, merge, publication or upstream disclosure.
- Any remote build or acquisition of artifacts requires separate scoped consent and verified build inputs. Do not edit/dispatch CI to bypass this gate.
- Concurrent replacement, extractor writes, hard links, mounts, content authenticity, atomic downloads, restrictive-mode policy, subprocess/server policy and supply-chain pinning remain separate stages.
- Keep application/test source changes out of this documentation checkpoint. Do not stage ignored `docs/security/` evidence or build outputs.

---

## Starting evidence and execution checkpoints

The clean planning start is `f11e0c4`. Source anchors below refer to that revision,
not to line numbers after implementation. `Directory.Build.props:3-6` supplies
`net10.0`/AOT; the regression executable disables AOT in its own project.
The current runner only contains the Wine INI fixture
(`tests/LinuxRegression/Program.cs:8-40`). It has not been executed.

Use these approval/review batches:

| Batch | Tasks | Reviewable result |
|---|---|---|
| 4A.1 | 1–3 | Parsed input policy, strict existing-path/tree inspection and checked managed tree operations |
| 4A.2 | 4–6 | Exact cache alias, download/extraction entry guards and special-repository managed I/O |
| 4A.3 | 7–9 | Installer preparation, profile consumers, cache pruning and guarded resume/recovery |
| 4A.4 | 10 | Coverage/source verification and a precise remote-test handoff |

Stop for user review after each batch. Every task's local commit is independently
reviewable, but no intermediate batch is the completed 4A boundary. Preserve the
branch/worktree; no integration or publication is part of this plan.

### Test-first protocol under the remote-only restriction

For every task:

1. Add the production-C# behavioral fixture **before** implementation.
2. Add the indicated Python structural contract and run it locally to capture
   its expected failure against the unchanged production source.
3. Implement the task, rerun the source contract and the existing source suite.
4. Record C# fixtures as **written, not compiled or executed**. A source-contract
   red/green cycle is not a behavioral TDD result.

The future, separately authorized remote command is:

```text
dotnet run --project tests/LinuxRegression/LinuxRegression.csproj -c Release
```

This no-argument form runs every registered suite; each task also gives its
literal suite name for targeted execution. Neither this command nor a restore/build is
authorized locally or remotely by plan approval. Do not add a CI execution step
in this stage. The future runtime gate must include the complete runner and
available Linux architecture coverage; a single architecture cannot establish
the other's runtime behavior.

## File responsibilities and fixed interfaces

Keep existing service responsibilities and dependency registrations. New files
are small shared policy/operation units, not a replacement installer framework.

| New file | Responsibility / first task |
|---|---|
| `Stalker.Gamma/Filesystem/InputPolicy.cs` | Components and parsed instruction syntax; no I/O (1) |
| `Stalker.Gamma/Filesystem/RecordPathPolicy.cs` | Existing manifest-role selection and composed path names (1) |
| `Stalker.Gamma/Filesystem/UnsafePathException.cs` | Recognizable fail-closed exception, derived from `IOException` (1) |
| `Stalker.Gamma/Filesystem/FileSystemBoundary.cs` | Lexical containment, existing-component inspection, non-following tree preflight (2) |
| `Stalker.Gamma/Properties/AssemblyInfo.cs` | Only `InternalsVisibleTo("LinuxRegression")`, for narrow library-internal fixtures (1) |
| `Stalker.Gamma/Filesystem/DownloadCache.cs` | Configured cache, exact downloads alias and its scoped GAMMA-tree view (4) |
| `Stalker.Gamma/Services/GitBlobWriter.cs` | Preflight a complete managed Git blob write set before opening outputs (6) |
| `Stalker.Gamma/GammaInstallerServices/GammaInstaller/InstallerInputPreflight.cs` | Non-mutating validation of known installer inputs/targets, shared online/offline (7) |
| `Stalker.Gamma/Filesystem/CachePrunePlan.cs` | Snapshot/recheck an entire prune set before deleting its first file (9) |
| `tests/test_filesystem_boundaries.py` | Structural integration contracts only; no Python policy reimplementation (1 onward) |
| `tests/LinuxRegression/Check.cs` | Tiny assertions, no test dependency (1) |
| `tests/LinuxRegression/WineIniCases.cs` | Preserve the existing positive fixture (1) |
| `tests/LinuxRegression/PathPolicyCases.cs` | Pure parsing/role/serialization regressions (1) |
| `tests/LinuxRegression/ScratchTree.cs` | Owned synthetic trees, tracked links, sentinel snapshots and safe teardown (2) |
| `tests/LinuxRegression/FileSystemBoundaryCases.cs` | Paths, roots, siblings, missing entries and links (2) |
| `tests/LinuxRegression/ManagedTreeCases.cs` | Copy/move/count/chmod/cleanup/instruction operations (3) |
| `tests/LinuxRegression/DownloadCacheCases.cs` | Exact alias and strict cache descendants (4) |
| `tests/LinuxRegression/DownloadBoundaryCases.cs` | Existing I/O entry points with memory-only HTTP handlers (5) |
| `tests/LinuxRegression/SpecialRepoCases.cs` | Managed blob writes, multi-copy preflight and failure cleanup (6) |
| `tests/LinuxRegression/InstallerInputCases.cs` | Shared preparation and fixed-file operations, not a live installation (7) |
| `tests/LinuxRegression/ProfileCases.cs` | Library/CLI profile operations without real network requests (8) |
| `tests/LinuxRegression/CacheResumeCases.cs` | Prune and resume/rejection ordering (9) |
| `tests/FILESYSTEM-REGRESSIONS.md` | Case inventory, authorization gates and observed/deferred results (10) |

Existing-file changes are enumerated within each task. Do not create a second
copy of the policy in the CLI or tests. Public policy types below are consumed
by both projects; keep test-only helpers internal and use only the library's
friend assembly. No friend access to the CLI is required: test its public
commands/models, and put reusable prune behavior in the shared library.

### Input interfaces (Task 1)

```csharp
namespace Stalker.Gamma.Filesystem;

public sealed class UnsafePathException(string message, Exception? inner = null)
    : IOException(message, inner)
{
    public static bool IsUnsafe(Exception error);
}

public static class InputPolicy
{
    public static string RequireComponent(string? value, string field);
    public static string? OptionalComponent(string? value, string field);
    public static string RequireRelativePath(string value, string field);
    public static string[] ParseTsvInstructions(string? raw);
    public static string[] ValidateInstructions(IEnumerable<string?>? raw);
}

internal static class RecordPathPolicy
{
    internal static bool IsSeparator(ModPackMakerRecord record);
    internal static void Validate(ModPackMakerRecord record);
    internal static void ValidateAll(IReadOnlyList<ModPackMakerRecord> records);
    internal static string AddonDirectoryName(ModPackMakerRecord record);
}
```

These are signature inventories; the method bodies are supplied by the tasks.
Use the `Stalker.Gamma.Models` import for `ModPackMakerRecord`.
`UnsafePathException` messages identify a constant field label and record counter,
not the complete record/remote URL. `IsUnsafe` recognizes this exception through
ordinary inner exceptions and every inner member of an `AggregateException`;
it is used by retry/recovery filters so JSON-required wrapping cannot hide a
validation stop. Callers must not add record `ToString()` output or swallow the
failure as retryable.

### Filesystem interfaces (Task 2)

```csharp
public sealed class FileSystemBoundary
{
    public FileSystemBoundary(string configuredRoot, string basePath);
    public string Root { get; }
    public string Resolve(string relativePath);
    public string CheckPath(string path, bool allowMissing = false);
    public IReadOnlyList<FileSystemInfo> CheckTree(
        string path, bool allowMissing = false, CancellationToken ct = default);
}
```

- Constructor/`Resolve` are lexical only and create nothing. Require an absolute
  explicit `basePath`; normalize configured relative roots against it once.
  Reject null/blank roots and translate invalid-path normalization errors to
  `UnsafePathException` with a constant root label, so invalid configured input
  cannot be mistaken for a recoverable download failure.
- `Resolve` consumes validated relative child paths. Use `Root` for the root
  itself instead of passing `.` as untrusted child input.
- `CheckPath` accepts an absolute path or a path relative to `Root`, verifies
  containment and inspects every existing component from `/` through the leaf.
  Return the normalized absolute path used for the actual I/O.
- `allowMissing` permits a nonexistent suffix after its existing parents pass,
  not a linked, inaccessible or wrong-typed existing parent. Missing is not
  inferred from `File.Exists`/`Directory.Exists`.
- `CheckTree` handles either a regular file or directory and returns a fully
  materialized, read-only snapshot including the root. It uses top-level
  enumeration and checks an entry before descending. It never follows links,
  even contained ones, and produces no partial-result mutations.
- A snapshot is evidence of checked state, not an unforgeable capability or a
  guarantee against replacement. Recheck relevant paths before path-based I/O.

### Cache interfaces (Task 4)

```csharp
public sealed class DownloadCache
{
    public DownloadCache(string gammaDir, string cacheDir, string basePath);
    public FileSystemBoundary Gamma { get; }
    public FileSystemBoundary Cache { get; }
    public string DownloadsPath { get; }
    public string Resolve(string relativePath);
    public void Preflight();
    public void EnsureReady(CancellationToken ct = default);
    public string CheckPath(string relativePath, bool allowMissing = false);
    public IReadOnlyList<FileSystemInfo> CheckTree(
        string relativePath, bool allowMissing = false, CancellationToken ct = default);
    public IReadOnlyList<FileSystemInfo> CheckGammaTree(CancellationToken ct = default);
}
```

`Resolve` is lexical, for pure record construction/identity. `Preflight` inspects
the existing roots/entry without mutation and permits a wholly absent boundary;
it does not permit an existing dangling alias. `EnsureReady` first runs all
prechecks, creates only authorized missing real directories and an absent alias,
then verifies the resulting entry. No other method creates or repairs it.

`CheckPath`/`CheckTree` require an established valid boundary, then delegate to
strict real-cache operations. An empty `relativePath` in `CheckTree` means the
cache root. This explicit root case does not authorize an empty instruction.

`CheckGammaTree` requires the valid boundary, inspects the GAMMA tree and omits
only the exact verified downloads entry from its returned traversal snapshot.
It **does not descend into the cache**. Cache consumers must cross explicitly
through `CheckPath`/`CheckTree`. All other GAMMA entries, including other links,
remain strict. This scoped operation is needed for MO2 extraction/permission
follow-up on the GAMMA root; generic tree helpers retain strict rejection.

## Task 1: Pure input policy and lossless parsed instruction transport

**Files:**
- Create: input/record/exception files, library `Properties/AssemblyInfo.cs`, `Check.cs`, `WineIniCases.cs`, `PathPolicyCases.cs`, `tests/test_filesystem_boundaries.py` from the map.
- Modify: `tests/LinuxRegression/Program.cs:1-40`.
- Modify: `Stalker.Gamma/Models/ModPackMakerRecord.cs:12-58,70-135`.
- Modify: `Stalker.Gamma/Factories/ModPackMakerRecordFactory.cs:12-31`.
- Modify: `Stalker.Gamma/Factories/DownloadableRecordFactory.cs:169-289`.
- Modify: `Stalker.Gamma/Factories/SeparatorsFactory.cs:12-28`.
- Modify: `Stalker.Gamma/Extensions/DiffModPackMakerLists.cs:32,89`.

**Interfaces:** Consumes the existing model/factory signatures. Produces all input
interfaces above and `ModPackMakerRecord.Instructions: string[]` initialized to
`[]`; `HasSameInstructions(ModPackMakerRecord other): bool` compares entries
ordinally. Add `[assembly: InternalsVisibleTo("LinuxRegression")]` with
`using System.Runtime.CompilerServices;` in the new library assembly-info file
so pure role fixtures can call `RecordPathPolicy` without public API expansion.
Record constructors may continue accepting their current list types
in this task; factories pass validated array entries via `ToList()`.

- [ ] **Step 1: Add the pure behavioral suite and source-contract red test.**

Preserve the existing Wine INI code in `WineIniCases.RunAsync()`. Make the runner
register suite names in a dictionary and execute all suites when no arguments
are supplied; reject unknown suite names. Initially register `wine-ini` and
`inputs`. Add subsequent task registrations only with their implementation.

`Check` provides `True(bool, string)`, `Equal<T>(T, T)`,
`Throws<T>(Action)` and `ThrowsAsync<T>(Func<Task>)`, returning the caught
exception and failing on no exception or a different type. Never accept an
arbitrary exception as evidence of rejection.

```csharp
internal static class PathPolicyCases
{
    internal static Task RunAsync()
    {
        foreach (var value in new[] { "G.A.M.M.A", "d'Artagnan ñ", "addon.v2.7z" })
            Check.Equal(value, InputPolicy.RequireComponent(value, "fixture"));
        foreach (var value in new[] { "", " ", ".", "..", "a/b", @"a\b", "/root", @"\\host\share", "C:folder", "a\0b", "a\nb" })
            Check.Throws<UnsafePathException>(() => InputPolicy.RequireComponent(value, "fixture"));
        foreach (var raw in new[] { @"\root", "/root", "../outside", "a/../b", "a/./b", "C:folder", "folder:C:child" })
            Check.Throws<UnsafePathException>(() => InputPolicy.ParseTsvInstructions(raw));
        Check.True(InputPolicy.ParseTsvInstructions(@"folder\nested:other/path")
            .SequenceEqual(new[] { "folder/nested", "other/path" }), "TSV nesting");
        Check.True(InputPolicy.ValidateInstructions(new[] { "C", "folder" })
            .SequenceEqual(new[] { "C", "folder" }), "JSON entries stay separate");
        Check.True(InputPolicy.ValidateInstructions(new[] { "folder:child" })
            .SequenceEqual(new[] { "folder:child" }), "literal JSON colon stays one entry");
        return Task.CompletedTask;
    }
}
```

Add serialization round trips through `ModPackMakerCtx.Default.ModPackMakerRecord`,
including absent/null/empty/single-blank/single-`0` instructions, ordered arrays,
`["C", "folder"]`, a literal non-drive colon entry, and invalid null/non-string
array members. Preserve the distinction between the whole-field `0` sentinel
and a folder named `0` within a longer list: test TSV `0:` and JSON `["0", ""]`
through serialization and subsequent consumer validation. Compare record equality,
hash codes and both diff methods for equivalent separately allocated arrays.
Test `IsUnsafe` on direct, JSON-wrapped and aggregate-wrapped failures. Use
`DlLink = "fixture"` for pure model cases; a URL is unnecessary. Include all
role cases in the matrix below.

```python
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]

def source(path):
    return (ROOT / path).read_text(encoding="utf-8")

class InputContractTests(unittest.TestCase):
    def test_json_keeps_entries_and_tsv_uses_shared_parser(self):
        model = source("Stalker.Gamma/Models/ModPackMakerRecord.cs")
        self.assertIn("JsonConverter<string[]>", model)
        self.assertNotIn("string.Join(':', instructions)", model)
        self.assertIn("InputPolicy.ParseTsvInstructions", source(
            "Stalker.Gamma/Factories/ModPackMakerRecordFactory.cs"))
```

- [ ] **Step 2: Run the structural test before production changes.**

Run `python3 -m unittest tests.test_filesystem_boundaries.InputContractTests -v`.
Expected red: the current converter is `JsonConverter<string>` and TSV has no
shared parser. Record the actual output; do not run the C# `inputs` suite.

- [ ] **Step 3a: Implement pure components and relative-path validation.**

The component method returns valid input unchanged:

```csharp
public static string RequireComponent(string? value, string field)
{
    if (string.IsNullOrWhiteSpace(value)
        || value is "." or ".."
        || value.Any(char.IsControl)
        || value.Contains('/') || value.Contains('\\')
        || Path.IsPathRooted(value)
        || (value.Length >= 2 && char.IsAsciiLetter(value[0]) && value[1] == ':'))
        throw new UnsafePathException($"Unsafe filesystem component in {field}.");
    return value;
}
```

`OptionalComponent` leaves null/empty/whitespace-only optional values unchanged;
otherwise it calls `RequireComponent`. Reject control characters before any
trimming, including in an otherwise blank supplied field.

```csharp
public static string? OptionalComponent(string? value, string field)
{
    if (value?.Any(char.IsControl) == true)
        throw new UnsafePathException($"Unsafe filesystem component in {field}.");
    return string.IsNullOrWhiteSpace(value) ? value : RequireComponent(value, field);
}
```

`RequireRelativePath` rejects controls, a leading `/` or `\`, drive-like
prefixes and `.`/`..` segments before normalization. Validate each nonempty
segment with `RequireComponent`; then normalize ordinary `\` to `/`. Preserve
interior spaces/Unicode/apostrophes. Repeated interior separators may collapse
to one separator; they never remove a dot/parent/root prefix.

- [ ] **Step 3b: Parse TSV and JSON instruction values without reparsing consumers.**

`ParseTsvInstructions` checks controls before trimming and returns `[]` for a
whole absent/blank/`0` field. Before splitting, reject
`(?:^|:)\s*[A-Za-z]:` drive ambiguity. Split the remaining TSV list on `:` without
discarding empty members, trim item-edge whitespace and validate each raw item
before slash conversion. Validate both the raw entry and its trimmed form so
whitespace cannot hide a rooted prefix. Retain empty members as non-path list
markers; consumers skip them. This preserves the distinction between field `0`
and a literal folder `0` in field `0:` without joining/reparsing entries.

`ValidateInstructions` validates an **already parsed** list: accept an absent
list as `[]`, reject null/control-containing members, normalize/check nonblank
paths and retain blank members as empty strings. Do not reinterpret `0` as a
sentinel here; doing so would turn a valid parsed single folder into a no-op.
The raw JSON converter alone recognizes an empty/single-blank/single-`0` array
as its existing whole-value no-op shape, after checking controls. For all other
JSON arrays, retain their list boundaries/empty markers. Never split entries on
`:`. A non-drive colon such as `folder:child` stays one Linux relative component;
`C:child` is rejected. This implements the spec's no-reparse contract rather
than inventing a second list delimiter.

Change the converter to `JsonConverter<string[]>` with `HandleNull => true`.
For a whole null token return `[]`; otherwise require an array, collect strings
without joining, handle the whole-value sentinel shapes above, then return
`ValidateInstructions`. Translate policy errors to `JsonException` at the JSON
syntax boundary, keeping the unsafe exception as the inner cause. Invalid token
shape/member errors also carry an `UnsafePathException` cause. Write the validated
stored array entry-by-entry, retaining empty markers and writing `[]` for an
empty parsed list. Update the source-generated model context through the changed
property type, not reflection-based serializer calls.

Implement failure classification recursively, without treating unrelated network
or I/O errors as validated unsafe-path findings:

```csharp
public static bool IsUnsafe(Exception error) =>
    error is UnsafePathException
    || (error is AggregateException aggregate && aggregate.InnerExceptions.Any(IsUnsafe))
    || (error.InnerException is { } inner && IsUnsafe(inner));
```

- [ ] **Step 3c: Update model equality, hashing and both diff consumers.**

```csharp
public bool HasSameInstructions(ModPackMakerRecord other) =>
    (Instructions ?? []).SequenceEqual(other.Instructions ?? [], StringComparer.Ordinal);
```

Use that comparison in `Equals` and both diff methods. Replace array-reference
hashing with a `HashCode` accumulator over the same existing fields and each
instruction in order, using ordinal string comparison. Equivalent arrays must
have equal hash codes; do not change unrelated diff identity rules.

- [ ] **Step 3d: Wire the raw TSV and patch converter ingress.**

```csharp
private static string? ReadField(string? raw, string field)
{
    if (raw?.Any(char.IsControl) == true)
        throw new UnsafePathException($"Invalid control character in {field}.");
    return raw?.Trim();
}
```

For TSV transport, split physical lines without `TrimEntries`, remove only the
single terminal `\r` of a CRLF line and split fields on tabs. Check each raw
field for remaining control characters before its existing whitespace trim.
Pass the **untrimmed** instruction field into `ParseTsvInstructions`. Do not
hide raw controls with the old outer `.Trim()` calls. Preserve normal LF/CRLF
documents and blank lines.

For patch JSON, validate a supplied raw nonblank patch component before adding
the existing `- ` display prefix. Preserve absent/blank optional patch meaning;
do not let the prefix conceal raw `.`/`..` or separators.

- [ ] **Step 3e: Apply role-aware policy and composed names in both factories.**

```csharp
internal static string AddonDirectoryName(ModPackMakerRecord record)
{
    InputPolicy.RequireComponent(record.AddonName, "addon name");
    InputPolicy.OptionalComponent(record.Patch, "patch");
    return InputPolicy.RequireComponent(
        $"{record.Counter}- {record.AddonName} {record.Patch}", "addon output directory");
}
```

`RecordPathPolicy` centralizes the existing separator predicate with
`(Instructions?.Length ?? 0) == 0`, validates path-producing roles, validates supplied
archive names and composes/validates `$"{Counter}- {AddonName} {Patch}"`.
`ValidateAll` completes before factories return a selected list. Preserve these
role distinctions:

| Role | Required path-producing fields / compatibility |
|---|---|
| Separator | Validate label only when selected as separator and composed folder when produced; addon-only fields may be absent |
| Standard ModDB | Preserve current unsupported/incomplete-row `false` behavior; when selected, validate addon, patch, archive and composed output |
| Metadata-only ModDB | Addon/output/instructions validated now; absent archive and optional patch remain valid until metadata arrives |
| GitHub | Validate addon/patch, derived repository archive name, supplied archive field if present and composed output; preserve existing URL selection |
| Unsupported non-path-producing row | No invented addon/archive requirement and no filesystem mutation |

Validate both raw contributing fields and composed names; adding a numeric
prefix must not hide unsafe raw values. Remove the lossy private
`DownloadableRecordFactory.ProcessInstructions` implementation. Neither factory
may strip leading slashes or log the entire rejected record.

- [ ] **Step 4: Run local contracts and review the wire/role changes.**

Run the literal Step 2 command, then
`python3 -m unittest discover -s tests -v` and `git diff --check`.
Expected source green; C# suite `inputs` remains unexecuted. Confirm JSON remains
an array and no live manifest was fetched. Record unknown live compatibility.

- [ ] **Step 5: Commit only Task 1 paths.**

```bash
git add Stalker.Gamma/Filesystem/InputPolicy.cs Stalker.Gamma/Filesystem/RecordPathPolicy.cs Stalker.Gamma/Filesystem/UnsafePathException.cs Stalker.Gamma/Properties/AssemblyInfo.cs Stalker.Gamma/Models/ModPackMakerRecord.cs Stalker.Gamma/Factories/ModPackMakerRecordFactory.cs Stalker.Gamma/Factories/DownloadableRecordFactory.cs Stalker.Gamma/Factories/SeparatorsFactory.cs Stalker.Gamma/Extensions/DiffModPackMakerLists.cs tests/LinuxRegression/Program.cs tests/LinuxRegression/Check.cs tests/LinuxRegression/WineIniCases.cs tests/LinuxRegression/PathPolicyCases.cs tests/test_filesystem_boundaries.py
git diff --cached --name-only
git commit -m "feat: validate filesystem inputs and preserve parsed instructions"
```

## Task 2: Strict existing-component inspection and non-following tree preflight

**Files:**
- Create: `FileSystemBoundary.cs`, `ScratchTree.cs`, `FileSystemBoundaryCases.cs` from the map.
- Modify: `tests/LinuxRegression/Program.cs`, `WineIniCases.cs`, `tests/test_filesystem_boundaries.py`.

**Interfaces:** Consumes `InputPolicy`/`UnsafePathException`; produces the fixed
filesystem interface. Reuse the library's narrow friend-assembly access from
Task 1; no additional visibility grant is needed.

- [ ] **Step 1: Add boundary fixtures, safe teardown and structural red.**

`ScratchTree` exposes `Root: string`, `At(string): string`, `Dir(string): string`,
`File(string, string): string`, `LinkFile(string, string): void`,
`LinkDirectory(string, string): void`, `Capture(string): FileSnapshot` and
`AssertUnchanged(FileSnapshot): void`. `FileSnapshot` stores the direct owned
sentinel path, existence, bytes and `UnixFileMode`. Create both managed and
external-sentinel locations inside this fresh scratch root; “external” means
outside the tested managed root, not a real user file.

Teardown unlinks registered links first, never recursively traverses them, and
only changes modes on known owned real directories. A final top-level,
non-following cleanup walk must also detect any unregistered link before
descending. Never call production chmod/cleanup on a deliberately unsafe tree
to clean up a failed assertion. Migrate the Wine fixture to this helper.

```csharp
internal static class FileSystemBoundaryCases
{
    internal static Task RunAsync()
    {
        using var tree = new ScratchTree();
        var managed = tree.Dir("managed ñ space");
        var outside = tree.File("outside/sentinel.txt", "unchanged");
        var before = tree.Capture(outside);
        var boundary = new FileSystemBoundary(managed, tree.Root);
        tree.LinkFile("managed ñ space/linked", outside);
        Check.Throws<UnsafePathException>(() => boundary.CheckPath("linked"));
        tree.AssertUnchanged(before);
        Check.Throws<UnsafePathException>(() => boundary.CheckPath(tree.At("managed ñ space-other/file"), true));
        Check.Equal(tree.At("managed ñ space/new/leaf"), boundary.CheckPath("new/leaf", true));
        return Task.CompletedTask;
    }
}
```

Extend the suite with directory/intermediate/root/ancestor links, dangling file
and directory links, self/two-link cycles, contained links, a later unsafe
sibling, an existing file in a would-be parent position, case-sensitive sibling
paths, roots containing spaces/apostrophes/Unicode and relative configured roots
resolved against an explicit base even when the process directory changes.
Restore the process directory in a test `finally` before scratch teardown.

```python
class BoundaryContractTests(unittest.TestCase):
    def test_shared_guard_separates_containment_and_tree_checks(self):
        guard = source("Stalker.Gamma/Filesystem/FileSystemBoundary.cs")
        for marker in ("Path.GetRelativePath", "LinkTarget", "File.GetAttributes", "CheckTree"):
            self.assertIn(marker, guard)
        self.assertNotIn("SearchOption.AllDirectories", guard)
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_filesystem_boundaries.BoundaryContractTests -v`.**

Expected red: the boundary file is absent. C# suite name: `boundaries`, not run.

- [ ] **Step 3a: Implement explicit-base normalization and lexical containment.**

Use ordinal relative-path containment, including equality with the root:

```csharp
var absolute = Path.GetFullPath(path, Root);
var relative = Path.GetRelativePath(Root, absolute);
if (Path.IsPathRooted(relative) || relative == ".."
    || relative.StartsWith("../", StringComparison.Ordinal))
    throw new UnsafePathException("Path is outside the configured filesystem root.");
```

- [ ] **Step 3b: Inspect existing absolute components without trusting existence predicates.**

```csharp
var info = new FileInfo(component);
if (info.LinkTarget is not null
    || (File.GetAttributes(component) & FileAttributes.ReparsePoint) != 0)
    throw new UnsafePathException("Symbolic links are not allowed in this filesystem boundary.");
```

Inspect absolute components one at a time from `/`. At each component, inspect
`FileSystemInfo.LinkTarget` and `File.GetAttributes` without first using an
existence predicate; reject nonnull link targets or `ReparsePoint`, including
the final file and dangling/cyclic links. Reject an existing non-directory
intermediate component. Only a genuine not-found exception after verified
parents establishes a missing suffix. Permission/I/O/inspection errors propagate
as failure, never as “missing” or “safe”. Do not resolve and accept a contained
target; strict rejection is deliberate.

Wrap an indeterminate inspection error in `UnsafePathException` with a constant
“cannot verify filesystem entry” message and its original cause. This is a
fail-closed classification, not a claim that the entry was proven malicious;
it prevents retry/recovery from treating an unverified tree as ordinary success.
Keep genuine allowed not-found handling separate from that catch.

- [ ] **Step 3c: Materialize the strict, cancellable tree snapshot.**

```csharp
foreach (var child in new DirectoryInfo(current).EnumerateFileSystemInfos())
{
    ct.ThrowIfCancellationRequested();
    CheckPath(child.FullName);
    entries.Add(child);
    if (child is DirectoryInfo)
        pending.Push(child.FullName);
}
```

Here `current` is the next already-checked directory from the method's local
`Stack<string> pending`; `entries` is its local `List<FileSystemInfo>` snapshot.
Initialize both only after checking the start path, include the start entry,
and return the snapshot only after the stack is empty.

Materialize `CheckTree` with checked top-level enumeration, not an
all-descendants API. Include the starting file/directory; propagate cancellation
using `ThrowIfCancellationRequested`. Return only after the entire requested
tree passes. Do not create directories or modify attributes/modes in inspection.

The current API references were consulted during planning through Context7 and
the canonical .NET 10 `File.GetAttributes`, `FileSystemInfo.LinkTarget`,
`ResolveLinkTarget` and explicit-base `Path.GetFullPath` pages. Their use here is
not runtime evidence: the dangling/cycle/permission cases remain mandatory for
the separately authorized runner. If the chosen API combination cannot reliably
distinguish links/missing/errors on a supported runtime, stop at this task's
review gate instead of inventing a no-follow guarantee or accepting ambiguity.

- [ ] **Step 4: Run the Task 2 contract, all Python contracts and `git diff --check`.**

Review all exception paths, root-prefix comparisons and teardown paths inline.
Expected structural green; report `boundaries` as written/unexecuted.

- [ ] **Step 5: Commit the boundary and its fixture support.**

```bash
git add Stalker.Gamma/Filesystem/FileSystemBoundary.cs tests/LinuxRegression/ScratchTree.cs tests/LinuxRegression/FileSystemBoundaryCases.cs tests/LinuxRegression/WineIniCases.cs tests/LinuxRegression/Program.cs tests/test_filesystem_boundaries.py
git diff --cached --name-only
git commit -m "feat: preflight managed filesystem paths without following links"
```

## Task 3: Checked copy/move/count/chmod/cleanup and sequential instructions

**Files:**
- Modify: `Stalker.Gamma/Utilities/DirUtils.cs:7-257`.
- Modify: `Stalker.Gamma/GammaInstallerServices/ProcessInstructions.cs:7-41`.
- Modify: `Stalker.Gamma/GammaInstallerServices/CleanExtractPath.cs:7-16`.
- Modify: `Stalker.Gamma/GammaInstallerServices/WriteAddonMetaIni.cs:7-40`.
- Modify: `Stalker.Gamma/Models/Separator.cs:15-22`.
- Create: `tests/LinuxRegression/ManagedTreeCases.cs`.
- Modify: runner registrations and `tests/test_filesystem_boundaries.py`.

**Interfaces:** Preserve public `DirUtils` signatures. `CountFiles` may become
`internal static int CountFiles(string sourceDir, string? fileFilter)` for the
friend fixture. Preserve `ProcessInstructions.Process(string, IList<string>,
CancellationToken)` and `CleanExtractPath.Clean(string)` as library-internal.
They consume the shared input/strict-tree interfaces, not a generic link flag.

- [ ] **Step 1: Add operation-ordering regressions and structural red.**

```csharp
internal static class ManagedTreeCases
{
    internal static Task RunAsync()
    {
        using var tree = new ScratchTree();
        var sourceDir = tree.Dir("source");
        var destination = tree.Dir("destination");
        var safe = tree.File("source/first.txt", "stay put");
        var outside = tree.File("outside/sentinel.txt", "do not touch");
        var safeBefore = tree.Capture(safe);
        var outsideBefore = tree.Capture(outside);
        tree.LinkFile("source/z-last.txt", outside);
        Check.Throws<UnsafePathException>(() => DirUtils.CopyDirectory(sourceDir, destination, moveFile: true));
        tree.AssertUnchanged(safeBefore);
        tree.AssertUnchanged(outsideBefore);
        Check.True(!System.IO.File.Exists(Path.Join(destination, "first.txt")), "preflight before first move");
        Check.Throws<UnsafePathException>(() => DirUtils.NormalizePermissions(sourceDir));
        tree.AssertUnchanged(safeBefore);
        tree.AssertUnchanged(outsideBefore);
        return Task.CompletedTask;
    }
}
```

Use separate fresh trees for these required cases:

| Operation | Fixture / assertion |
|---|---|
| Copy and move | Linked source file/directory/intermediate, linked destination leaf/parent/root; neither external sentinel nor earlier valid sibling changes |
| Progress/count/text logging | Exercise callbacks and direct `CountFiles`; no unchecked count/hash/open through a link before rejection |
| Selection | Existing `.git` substring skip, `fileFilter`, overwrite false/true and nonempty-directory behavior retained; filters never authorize an encountered link |
| Overlap | Safe identical source/destination is a no-op; destination below source is rejected; ancestor flattening does not enumerate newly written output |
| chmod and cleanup | Late link/cycle rejected before any sibling mode/attribute/delete change, including links under a retained `doNotMatch` directory |
| Cleanup selection | Preserve current dot-file removal and retained `gamedata/appdata/db/fomod` behavior; do not expand deletion to regular top-level files |
| Instructions | Valid nested slash/backslash source, source containing `gamedata`, plain source flattened into `gamedata`, and a source produced by an earlier instruction |
| Instruction ordering | Unsafe syntax in the second item or a link anywhere in the existing affected tree rejects before the first item moves files |
| Fixed metadata/separator writes | Directly constructed invalid names and linked `meta.ini` rejected before creating/writing related paths |
| Cancellation | Pre-cancelled operation throws and creates/writes nothing; cancellation during a callback does not mark processing complete |

Add a direct no-op instruction case using an absent extraction path: an empty
parsed list creates nothing and does not require that path to exist. Test a
list retaining blank markers too; validation must still reject control-bearing
members before recognizing a no-op.

```python
class ManagedTreeContractTests(unittest.TestCase):
    def test_no_unchecked_recursive_enumeration_or_prefix_repair(self):
        for path in ("Stalker.Gamma/Utilities/DirUtils.cs",
                     "Stalker.Gamma/GammaInstallerServices/ProcessInstructions.cs"):
            body = source(path)
            self.assertIn("CheckTree", body)
            self.assertNotIn("SearchOption.AllDirectories", body)
        self.assertNotIn("TrimStart", source(
            "Stalker.Gamma/GammaInstallerServices/ProcessInstructions.cs"))
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_filesystem_boundaries.ManagedTreeContractTests -v`.**

Expected red: current recursion uses all-descendants probes and lacks full
preflight. C# suite name: `managed-tree`, not run.

- [ ] **Step 3a: Preflight copy/move/count before callbacks or output creation.**

For copy/move, normalize source/destination once, inspect both roots and their
affected trees **before** progress counting, hash logging, destination creation
or the first move/copy. Snapshot the source traversal so destination changes
cannot expand it. Recheck a selected source and mapped destination near each
file operation. Preserve filters/overwrite/nonempty-directory selection while
checking every encountered entry. Thread the same cancellation token through
all recursion and throw rather than returning apparent success.

```csharp
cancellationToken.ThrowIfCancellationRequested();
var basePath = Directory.GetCurrentDirectory();
var sourceBoundary = new FileSystemBoundary(sourceDir, basePath);
var destinationBoundary = new FileSystemBoundary(destDir, basePath);
var sourceEntries = sourceBoundary.CheckTree(sourceBoundary.Root, ct: cancellationToken);
destinationBoundary.CheckTree(destinationBoundary.Root, allowMissing: true, ct: cancellationToken);
```

After those checks, reject a strict destination descendant of the source with
`Path.GetRelativePath`; an identical safe path is a no-op, and an ancestor is
permitted for existing flattening semantics. Counting/empty-directory selection
must use checked entries, not new all-descendants probes. Do not add a link
exception to `DirUtils`.

Before creating output, require an existing source to be a directory and any
existing directory destination to have that type. A wrong-typed root/parent is
a preflight failure, not something discovered after earlier siblings mutate.
Add this to the copy/count/cleanup fixture matrix. File-oriented operations in
Task 5 likewise reject an existing directory in an archive/output-file position.

- [ ] **Step 3b: Preflight permission normalization and selected recursive deletion.**

```csharp
var boundary = new FileSystemBoundary(dir, Directory.GetCurrentDirectory());
var entries = boundary.CheckTree(boundary.Root, allowMissing: true);
```

`NormalizePermissions` preflights the entire tree first, then applies the
**unchanged** existing mode/attribute choices to real entries. A boundary error
must escape existing `UnauthorizedAccessException` handling. Missing real paths
may retain the current no-op meaning after inspected-parent checks.
`RecursivelyDeleteDirectory` preflights before deleting the first selected
hidden file or directory; exclusions affect deletion selection, not link trust.

- [ ] **Step 3c: Preserve sequential instruction flattening through checked operations.**

For instructions, validate the complete parsed list first and preflight the
existing extraction/destination tree before its first mutation. Then resolve
each source against the extraction boundary immediately before use. Recheck
its current tree and destination; do not require a source created by an earlier
instruction to exist during the initial syntax/preflight phase.

```csharp
var parsed = InputPolicy.ValidateInstructions(instructions);
if (parsed.All(instruction => instruction.Length == 0))
    return;
var root = new FileSystemBoundary(extractPath, Directory.GetCurrentDirectory());
root.CheckTree(root.Root, ct: cancellationToken);
foreach (var instruction in parsed)
{
    cancellationToken.ThrowIfCancellationRequested();
    if (instruction.Length == 0)
        continue;
    var selected = root.CheckPath(root.Resolve(instruction), allowMissing: true);
    root.CheckTree(selected, allowMissing: true, ct: cancellationToken);
}
```

That loop shows the required check phase; the implementation retains the
existing two flattening branches, performs each move through checked
`CopyDirectory`, and checks/creates `gamedata` only after the operation's
preflight. No synthetic “destination inside source” copy or unchecked
`Path.Exists` probe may precede these checks.

- [ ] **Step 3d: Guard extraction cleanup and fixed metadata/separator writes.**

```csharp
InputPolicy.RequireComponent(FolderName, "separator folder");
var mods = new FileSystemBoundary(Path.Join(gammaPath, "mods"), Directory.GetCurrentDirectory());
var separatorPath = mods.CheckPath(mods.Resolve(FolderName), allowMissing: true);
mods.CheckPath(Path.Join(separatorPath, "meta.ini"), allowMissing: true);
```

`CleanExtractPath` checks the complete tree before chmod. `Separator.WriteAsync`
revalidates `FolderName` at the public write boundary, resolves it under
`GAMMA/mods`, and checks both directory and `meta.ini` before creating anything.
`WriteAddonMetaIni.Write` validates the archive component and checks the output
root and `meta.ini` leaf before its existing write. Preserve INI bytes/line
endings and do not validate the non-path display label as a directory name.

- [ ] **Step 4: Run the Task 3 contract, complete Python suite and `git diff --check`.**

Inspect failure/cancellation/filtered traversal paths and confirm no permission
mask was hardened or broadened. Record the C# fixtures as unexecuted.

- [ ] **Step 5: Commit Task 3 and stop at the first batch review gate.**

```bash
git add Stalker.Gamma/Utilities/DirUtils.cs Stalker.Gamma/GammaInstallerServices/ProcessInstructions.cs Stalker.Gamma/GammaInstallerServices/CleanExtractPath.cs Stalker.Gamma/GammaInstallerServices/WriteAddonMetaIni.cs Stalker.Gamma/Models/Separator.cs tests/LinuxRegression/ManagedTreeCases.cs tests/LinuxRegression/Program.cs tests/test_filesystem_boundaries.py
git diff --cached --name-only
git commit -m "fix: preflight managed tree operations before filesystem mutations"
```

## Task 4: Exact downloads/cache boundary and explicit path ownership

**Files:**
- Create: `Stalker.Gamma/Filesystem/DownloadCache.cs`, `tests/LinuxRegression/DownloadCacheCases.cs`.
- Modify: `Stalker.Gamma/Factories/DownloadableRecordFactory.cs`.
- Modify: `Stalker.Gamma/Models/IDownloadableRecord.cs`, `GithubRecord.cs`, `GithubRecordGroup.cs`, `ResumableRecord.cs`, `SkippedRecord.cs`, `SkipExtractWhenNotDownloadedRecord.cs`.
- Modify: `Stalker.Gamma/ModDb/Models/ModDbRecord.cs`, `ModDbRecordGetMetadata.cs`, `ModDbRecordGroup.cs`, `ModDbRecordGetMetadataGroup.cs`.
- Modify: all four `Stalker.Gamma/GammaInstallerServices/SpecialRepos/` record files listed in Task 6.
- Modify: `Stalker.Gamma/GammaInstallerServices/AnomalyInstaller.cs`.
- Modify: `Stalker.Gamma/GammaInstallerServices/GammaInstaller/GammaInstallerArgs.cs`, `GammaInstaller.cs`, `OfflineGammaInstaller.cs`.
- Modify: `stalker-gamma-cli/Commands/Cache.cs:159-179` and runner/source contracts.
- Modify: `tests/test_linux_only.py:84-97`, replacing the superseded unchecked-helper source contract with the checked boundary contract.
- Existing helper: inspect `Stalker.Gamma/Utilities/CreateSymbolicLinkUtility.cs`; remove its installation call sites, retaining no unchecked public replacement path.

**Interfaces:** Produces the cache interface above. Add to installer args:

```csharp
public string BasePath { get; init; } = Directory.GetCurrentDirectory();
public DownloadCache Downloads => new(Gamma, Cache, BasePath);
```

Replace the `string gammaDir` parameter with `DownloadCache downloads` in
`IDownloadableRecordFactory.TryCreate` and the four special-repo factory methods,
and in the corresponding addon/special-repo constructors. Keep other parameters
and their order. Use `downloads.Gamma.Root` for GAMMA paths and
`downloads.Resolve(...)` for cache identity paths. Extend the Anomaly factory to
`CreateAnomalyRecord(string downloadDirectory, string anomalyDir, DownloadCache? downloads = null)`
and add that optional parameter last in `AnomalyInstaller`'s constructor.
Standalone two-argument calls retain their meaning; full installation passes
the real configured cache root and its explicit boundary. If supplied, the
normalized download directory must equal that boundary's cache, not silently
substitute a different root.

Add these contracts to `IDownloadableRecord`:

```csharp
FileSystemBoundary DownloadRoot { get; }
string CheckDownloadPath(bool allowMissing = false);
```

Every leaf record supplies its explicit real cache/download root. Cache-aware
records implement `CheckDownloadPath` through `DownloadCache.CheckPath`, so
default archive operations cannot bypass alias validation by using only a real
cache path. Standalone Anomaly uses its strict explicit root; with the optional
boundary it also validates the alias. All groups/decorators forward both members
and the current `DownloadPath`/`ArchiveName`; never eagerly read or snapshot a
metadata-dependent filename/path while constructing a decorator.

- [ ] **Step 1: Write the alias matrix and source-contract red.**

```csharp
internal static class DownloadCacheCases
{
    internal static Task RunAsync()
    {
        using var tree = new ScratchTree();
        var gamma = tree.Dir("GAMMA ñ");
        var cache = tree.Dir("cache d'Artagnan");
        var downloads = new DownloadCache(gamma, cache, tree.Root);
        downloads.EnsureReady();
        var archive = tree.File("cache d'Artagnan/addon.7z", "synthetic");
        Check.Equal(archive, downloads.CheckPath("addon.7z"));
        Check.Throws<UnsafePathException>(() => new FileSystemBoundary(gamma, tree.Root).CheckTree(gamma));
        downloads.CheckGammaTree();
        return Task.CompletedTask;
    }
}
```

In separate trees test absolute/relative correct direct targets, missing entry,
wrong target, dangling target, self/two-link cycle, linked cache root/ancestor,
chain with eventual matching target, existing ordinary file, mismatched real
directory, same-path real directory, cache nested below downloads, and linked
descendants after crossing a correct alias. Assert invalid entries and their
existing contents/targets are not deleted/replaced/migrated, and a nested target
failure creates no directory. Add direct `ArchiveExists`/`DeleteArchive` cases
where a previously prepared record now sees a wrong alias: neither default
operation may bypass verification just because its identity path is canonical.
Register `cache-boundary`.

```python
class CacheBoundaryContractTests(unittest.TestCase):
    def test_both_bootstraps_use_explicit_cache_boundary(self):
        for path in ("Stalker.Gamma/GammaInstallerServices/GammaInstaller/GammaInstaller.cs",
                     "Stalker.Gamma/GammaInstallerServices/GammaInstaller/OfflineGammaInstaller.cs"):
            body = source(path)
            self.assertIn("EnsureReady", body)
            self.assertNotIn("CreateSymbolicLinkUtility.Create", body)
        self.assertIn("DownloadRoot", source("Stalker.Gamma/Models/IDownloadableRecord.cs"))
        self.assertIn("CheckDownloadPath", source("Stalker.Gamma/Models/IDownloadableRecord.cs"))
```

Replace the older `LinuxInstallerTests.test_both_installers_use_the_two_argument_link_helper`
before implementation; its exact unchecked-helper assertion must not remain
after this planned API change. Keep its Linux-only intent:

```python
def test_both_installers_use_checked_linux_cache_boundary(self):
    for name in ("GammaInstaller", "OfflineGammaInstaller"):
        body = source("Stalker.Gamma/GammaInstallerServices/GammaInstaller/" + name + ".cs")
        self.assertIn("downloads.EnsureReady(args.CancellationToken);", body)
        self.assertNotIn("CreateSymbolicLinkUtility.Create", body)
    boundary = source("Stalker.Gamma/Filesystem/DownloadCache.cs")
    self.assertIn("Directory.CreateSymbolicLink", boundary)
    self.assertNotIn("OperatingSystem.IsWindows", boundary)
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_filesystem_boundaries.CacheBoundaryContractTests -v`.**

Expected red: unchecked helper call sites and no `DownloadRoot` contract.

- [ ] **Step 3a: Implement non-mutating alias inspection and guarded establishment.**

```csharp
var resolvedTarget = Path.GetFullPath(rawTarget, Path.GetDirectoryName(DownloadsPath)!);
if (!string.Equals(resolvedTarget, Cache.Root, StringComparison.Ordinal))
    throw new UnsafePathException("GAMMA downloads does not target the configured cache.");
```

`rawTarget` is the nonnull `LinkTarget` of the exact downloads entry after its
parents/cache have passed inspection. Add a private `RequireReady(): void`
helper that repeats `Preflight` and additionally requires established real
roots/the valid downloads entry; reuse it in consumer methods.

Reject a cache target strictly below the downloads entry lexically before any
directory creation. Inspect the GAMMA root/ancestors without traversing the
downloads entry, inspect the cache root strictly, then inspect that entry.
For a symlink, compare its **direct** raw target normalized against the link
parent with `Cache.Root`, ordinally. Never use final-target resolution to bless
a chain. Existing dangling, cyclic, file-valued or wrong-target entries fail.

For an ordinary directory, accept only `DownloadsPath == Cache.Root`; otherwise
fail. For absence, `EnsureReady` creates checked real roots and then uses
`Directory.CreateSymbolicLink` only if an alias is needed. Never delete an
appearing/existing entry; creation failure is a failure, not a repair trigger.
Recheck the resulting boundary. Creation is still not race-resistant.

Existing GAMMA/cache roots must be real directories, not ordinary file entries.
Reject a wrong-typed root before creating another missing root. Apply that same
expected-directory check to the configured Anomaly root in Task 7.

- [ ] **Step 3b: Migrate the two bootstrap establishment calls.**

```csharp
var downloads = args.Downloads;
downloads.Preflight();
downloads.EnsureReady(args.CancellationToken);
```

Replace both bootstrap helper calls with this operation. Full known-manifest
ordering is completed in Task 7; this intermediate task is not the completed
installer preflight. Remove the old helper if no callers remain; if a caller is
found, enumerate and migrate it rather than leaving an unchecked public API.

- [ ] **Step 3c: Implement the scoped GAMMA snapshot without following cache.**

```csharp
RequireReady();
foreach (var child in new DirectoryInfo(Gamma.Root).EnumerateFileSystemInfos())
{
    ct.ThrowIfCancellationRequested();
    if (string.Equals(child.FullName, DownloadsPath, StringComparison.Ordinal))
    {
        RequireReady();
        continue;
    }
    Gamma.CheckTree(child.FullName, ct: ct);
}
```

Accumulate the checked root/child snapshots into the returned list; never add
the omitted alias itself for downstream chmod to act on.

Implement `CheckGammaTree` as the only scoped GAMMA tree partition: verify and
omit exactly `DownloadsPath`, check everything else strictly, never descend
through the alias and never give `DirUtils` a general exemption. Tests must
show the same tree is rejected by generic `CheckTree` but accepted through this
specific boundary.

- [ ] **Step 3d: Propagate configured real roots through records, factories and consumers.**

Update every listed constructor/factory/group/decorator and installer/cache
factory call in the same commit. The factory is given the configured cache;
it must not infer trust from whatever downloads happens to point to.
For example:

```csharp
public string DownloadPath => downloads.Resolve(ArchiveName);
public FileSystemBoundary DownloadRoot => downloads.Cache;
public string CheckDownloadPath(bool allowMissing = false) =>
    downloads.CheckPath(ArchiveName, allowMissing);
```

The four special repos use `$"{Name}.git"` and `Name` relative to that same
object. `BuildAnomalyRecord` passes `args.Downloads.Cache.Root`, `args.Anomaly`
and `args.Downloads` to the Anomaly factory; the standalone CLI's explicit cache
root remains supported. Wire both default archive operations through
`CheckDownloadPath` before their current existence/delete logic in this task;
Task 5 adds their complete recursive descendant preflight. For cache commands
construct `DownloadCache` from the active profile's `Gamma` and
`Cache`, with one captured base path. No alias is established by prune/check.

- [ ] **Step 4: Run Task 4/all source contracts and inspect interface propagation.**

Run `python3 -m unittest tests.test_filesystem_boundaries.CacheBoundaryContractTests -v`,
`python3 -m unittest discover -s tests -v` and `git diff --check`.
Search all `IDownloadableRecord` implementers and changed factory call sites;
verify every concrete record supplies/forwards `DownloadRoot` and
`CheckDownloadPath`. C# is unexecuted.

- [ ] **Step 5: Commit the explicit cache plumbing.**

Stage the exact Task 4 files and the old helper only if actually removed. Use
`git diff --cached --name-only` to exclude unrelated paths, then:

```bash
git commit -m "fix: scope downloads alias trust to the configured cache boundary"
```

## Task 5: Guard record/download/hash/extraction entry points and late filenames

**Files:**
- Modify: `Stalker.Gamma/Models/IDownloadableRecord.cs:19-31`, `GithubRecord.cs:36-105`.
- Modify: `Stalker.Gamma/ModDb/Models/ModDbRecord.cs:39-113`, `ModDbRecordGetMetadata.cs:26-126`, `ModDbRecordGetMetadataGroup.cs:23-33`.
- Modify: `Stalker.Gamma/GammaInstallerServices/AnomalyInstaller.cs:36-116`.
- Modify: `Stalker.Gamma/Utilities/DownloadFileFast.cs:13-68`, `HashUtils.cs:14-15`, `DirUtils.cs` for the scoped method below.
- Modify: `Stalker.Gamma/Services/NetworkService.cs:28-42`, `ArchiveService.cs:7-97`.
- Modify: `Stalker.Gamma/Proxies/StalkerGammaServerProxy.cs:31-46`.
- Modify: `Stalker.Gamma/ModDb/Services/ModDbService.cs:16-151`.
- Modify: `Stalker.Gamma/ModOrganizer/DownloadModOrganizer/DownloadModOrganizerService.cs:11-267`.
- Modify: the two installers' MO2 extraction call sites.
- Modify: `tests/test_linux_only.py:126-130`, retaining generic archive follow-up assertions and adding the explicit GAMMA follow-up assertion.
- Create: `tests/LinuxRegression/DownloadBoundaryCases.cs`; modify runner/source contracts.

**Interfaces:** Preserve existing generic sink/hash/archive signatures; their
callers supply canonical paths already checked against the intended domain
root. Generic final-path sinks additionally check their own existing parents
and leaf, rather than treating those caller checks as permanent trust.
Add these narrowly scoped production operations:

```csharp
public static void NormalizeGammaPermissions(DownloadCache downloads);

// ArchiveService
public Task ExtractGammaAsync(string archivePath, DownloadCache downloads,
    Action<double> pct, CancellationToken ct);

// IDownloadModOrganizerService and implementation
public Task ExtractGammaAsync(string version, DownloadCache downloads,
    CancellationToken cancellationToken = default);
```

The generic archive/MO2 entry points stay strict. They do not infer an exception
from a directory named downloads. Only the explicit overloads use
`CheckGammaTree` and skip the checked alias in permission follow-up.

- [ ] **Step 1: Add direct-boundary rejection and memory-only receipt tests.**

Use an `HttpMessageHandler` overriding `SendAsync`, returning an in-memory
`HttpResponseMessage`; it must have no sockets/client fallback. A simple handler
can count calls. An `IHttpClientFactory` fixture returns only this client.

```csharp
using var tree = new ScratchTree();
var outside = tree.File("outside/sentinel", "original");
var before = tree.Capture(outside);
tree.Dir("cache");
tree.LinkFile("cache/archive.7z", outside);
var handler = new MemoryResponseHandler();
using var client = new HttpClient(handler);
await Check.ThrowsAsync<UnsafePathException>(() => DownloadFileFast.DownloadAsync(
    client, "https://fixture.invalid/archive", tree.At("cache/archive.7z")));
Check.Equal(0, handler.Calls);
tree.AssertUnchanged(before);
```

Define `MemoryResponseHandler` inside the test file: return a fixed byte-array
content with `200 OK` and increment `Calls`; add a configured delegate for the
MO2 release JSON response. Use no real HTTP endpoints. Test a valid destination
receives the exact synthetic bytes without changing publication/integrity policy.

```csharp
internal sealed class MemoryResponseHandler : HttpMessageHandler
{
    internal int Calls { get; private set; }
    internal Func<HttpRequestMessage, HttpResponseMessage> Response { get; init; } =
        _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        return Task.FromResult(Response(request));
    }
}
```

Add these cases to the `download-boundaries` suite:

| Boundary | Required case |
|---|---|
| Direct records | Invalid output/archive/instruction value rejected before a related path is created, even without a factory |
| Metadata-only record | Unresolved archive is allowed during construction, cannot be used as a cache path; assigning a valid filename works, traversal/separator/root/control assignment fails |
| Metadata receipt | The actual production filename setter performs validation; `GetModDbAddonMetadataAsync` and metadata-group propagation use that setter before download/hash/path use |
| Cached archives | Linked archive (including `.tar.zst` alternative), linked cache parent and directory archive containing a late link fail before hash/existence/delete/extract operations |
| Extract entry | Linked archive source or existing destination rejected before header opening/extractor dispatch; unsupported synthetic header uses no external extractor |
| MO2 receipt | In-memory release JSON with an unsafe release-derived archive name rejected before creating a destination or requesting its asset |
| MO2 removal set | Unsafe later cleanup folder/file rejects before an earlier valid component is chmodded/deleted |
| Error handling | `UnsafePathException` and cancellation propagate, are not wrapped as retryable record/network failures, and do not start post-extraction chmod/metadata/cleanup |

Do not execute 7z/unzip/Anomaly/MO2 installers or create an archive that requires
external extraction in the runner. Positive extractor behavior remains a later
authorized integration test, not something this suite claims.

```python
class DownloadBoundaryContractTests(unittest.TestCase):
    def test_download_guard_precedes_request_and_archive_has_scoped_entry(self):
        download = source("Stalker.Gamma/Utilities/DownloadFileFast.cs")
        self.assertLess(download.index("CheckPath"), download.index("hc.GetAsync"))
        archive = source("Stalker.Gamma/Services/ArchiveService.cs")
        self.assertIn("ExtractGammaAsync", archive)
        self.assertIn("CheckGammaTree", archive)
        self.assertNotIn("finally", archive)
```

Keep the existing unzip/Wine-payload assertions in `LinuxRuntimeTests`; retain
the generic `DirUtils.NormalizePermissions(destinationDir);` assertion and add:

```python
self.assertIn("DirUtils.NormalizeGammaPermissions(downloads);", archive)
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_filesystem_boundaries.DownloadBoundaryContractTests -v`.**

Expected red: the download opens without the guard and archive chmod is in
`finally`. C# suite remains unexecuted.

- [ ] **Step 3a: Guard direct record operations and late filename assignment.**

Each record mutation revalidates known names/instructions, verifies its exact
downloads boundary, and checks its real download/extraction paths. Direct
construction cannot bypass these checks. For late filenames use a backing
field with a validating setter; the path getter must fail clearly until the
filename exists instead of returning the downloads directory as an archive.

```csharp
private string? _archiveName;
public string ArchiveName
{
    get => _archiveName ?? throw new UnsafePathException("Archive filename metadata is required.");
    set => _archiveName = InputPolicy.RequireComponent(value, "metadata filename");
}
```

Metadata receipt assigns through this setter before hashes/progress/download
paths; grouped metadata records do the same. Groups/decorators forward the live
filename/path. Do not introduce a new metadata HTTP abstraction merely for tests.

Default `ArchiveExists`/`DeleteArchive` first obtain the current canonical path
through `CheckDownloadPath`, then use `DownloadRoot` to preflight the whole
selected directory tree before existence checks, chmod or deletion. Missing
real entries preserve their no-op meaning; dangling links do not count as absent.
For two alternative Anomaly archives, check the complete considered source/delete
set before choosing/deleting the first entry.

- [ ] **Step 3b: Guard generic download/hash/network leaf I/O.**

For generic full-path utilities, capture a base once, compute the absolute path,
and construct an explicit parent boundary for leaf inspection. This complements,
not replaces, the record/service's check against its intended configured root:

```csharp
var basePath = Directory.GetCurrentDirectory();
var absolute = Path.GetFullPath(downloadPath, basePath);
var parent = new FileSystemBoundary(Path.GetDirectoryName(absolute)!, basePath);
absolute = parent.CheckPath(absolute, allowMissing: true);
```

Perform this before the HTTP request and again before the existing `FileStream`
creation. Retain final-path publication behavior for the later atomic-download
stage; do not claim authenticity, exclusive temp files or failure cleanup fixed.
`NetworkService` and the server proxy validate their `fileName` argument as one
component before joining; inspect the output parent/leaf before dispatch.
`ModDbService` checks its supplied output before retry work and must not retry
`UnsafePathException`/cancellation. `HashUtils.HashFile` checks the path before
`File.OpenRead`. Do not modify native curl bindings or their static initializer.

- [ ] **Step 3c: Separate generic and explicitly cache-scoped archive follow-up.**

```csharp
ct.ThrowIfCancellationRequested();
downloads.CheckGammaTree(ct);
DirUtils.NormalizeGammaPermissions(downloads);
```

These are the successful scoped follow-up operations. Complete the same
scoped preflight before header opening/dispatch; no follow-up runs when the
awaited extraction throws.

Generic `ArchiveService.ExtractAsync` checks archive source plus the full existing
destination tree before opening the header. After **successful** extraction,
check the resulting tree before any chmod/follow-up. Remove chmod from `finally`;
on extraction failure/cancellation no cleanup/permission normalization starts.
The explicit `ExtractGammaAsync` uses the checked GAMMA snapshot, verifies the
alias again after extraction, then calls `NormalizeGammaPermissions`, which
operates only on the materialized GAMMA entries and never chmods cache through
the alias. Shared private dispatch may avoid duplicating archive mappings; it
must not be a public unchecked bypass.

- [ ] **Step 3d: Guard the complete MO2 cleanup set and release-derived path.**

```csharp
var destination = new FileSystemBoundary(extractPath, Directory.GetCurrentDirectory());
var cleanupTargets = _foldersToDelete.Concat(_filesToDelete)
    .Select(name => destination.Resolve(name)).ToArray();
foreach (var target in cleanupTargets)
    destination.CheckTree(target, allowMissing: true, ct: cancellationToken);
```

For the scoped entry, `extractPath` is `downloads.Gamma.Root`; also finish
`CheckGammaTree` before deleting any selected target.

MO2 validates the requested version, the release-derived name and composed
archive filename **before** related directory creation. In extraction, inspect
the archive and **all** `_foldersToDelete`/`_filesToDelete` targets before the
first removal; replace its all-descendants attribute walk with checked entries.
The two installers use the explicit cache-scoped MO2 method. Preserve existing
payload filenames and fixed deletion selection; mode and hash policy are unchanged.

- [ ] **Step 3e: Propagate security/cancellation stops without retry wrapping.**

Exclude direct or wrapped unsafe-path errors and cancellation from broad record
exception wrappers and retry catches, for example:

```csharp
catch (Exception e) when (!UnsafePathException.IsUnsafe(e)
    && e is not OperationCanceledException and not CloudflareChallengeException)
```

Use the corresponding existing exception type at each site; do not add a
Cloudflare dependency to unrelated utilities. Unsafe messages must not acquire
full URLs by being wrapped with record dumps.

- [ ] **Step 4: Run Task 5/all Python contracts and `git diff --check`.**

Review generic versus explicit GAMMA entry points and every failure path. Verify
all download utility call sites still receive checked domain paths. C# remains
unexecuted; do not describe the fixture source as a successful reproduction.

- [ ] **Step 5: Stage exactly the Task 5 files, inspect the index and commit.**

```bash
git diff --cached --name-only
git commit -m "fix: validate late filenames and guard download extraction boundaries"
```

## Task 6: Managed Git output and special-repository multi-operation preflight

**Files:**
- Create: `Stalker.Gamma/Services/GitBlobWriter.cs`, `tests/LinuxRegression/SpecialRepoCases.cs`.
- Modify: `Stalker.Gamma/Services/GitService.cs:7-120,145-227`.
- Modify: `Stalker.Gamma/GammaInstallerServices/SpecialRepos/GammaSetup.cs:26-115`.
- Modify: `Stalker.Gamma/GammaInstallerServices/SpecialRepos/GammaLargeFiles.cs:29-119`.
- Modify: `Stalker.Gamma/GammaInstallerServices/SpecialRepos/StalkerGammaRepo.cs:28-131`.
- Modify: `Stalker.Gamma/GammaInstallerServices/SpecialRepos/TeivazAnomalyGunslingerRepo.cs:33-129`.
- Modify: runner/source contracts.

**Interfaces:** Consume `DownloadCache` and strict shared operations. Add internal
managed blob writer/test seam, not a new public Git API:

```csharp
internal sealed record GitBlobWrite(string RelativePath, Func<Stream> OpenContent);

internal static class GitBlobWriter
{
    internal static Task WriteAsync(string outputDir,
        IReadOnlyList<GitBlobWrite> blobs, CancellationToken ct,
        Action<double>? onProgress = null);
}
```

`GitService.ExtractAsync` gathers the selected tree's complete relative blob
write set, then passes it to this helper while the repository remains open.
Keep existing branch lookup and blob-as-regular-file behavior; do not add
symlink materialization, new checkout semantics or unrelated Git mode changes.

- [ ] **Step 1: Write managed-write/multi-copy failure fixtures and structural red.**

```csharp
using var tree = new ScratchTree();
var output = tree.Dir("git-output");
var outside = tree.File("outside/sentinel", "unchanged");
var before = tree.Capture(outside);
tree.LinkFile("git-output/z-last.txt", outside);
var opened = 0;
Stream OpenContent()
{
    opened++;
    return new MemoryStream(new byte[] { 1, 2, 3 });
}
await Check.ThrowsAsync<UnsafePathException>(() => GitBlobWriter.WriteAsync(output,
    new[] { new GitBlobWrite("first.txt", OpenContent), new GitBlobWrite("z-last.txt", OpenContent) },
    CancellationToken.None));
Check.Equal(0, opened);
Check.True(!System.IO.File.Exists(Path.Join(output, "first.txt")), "all targets checked before first blob");
tree.AssertUnchanged(before);
```

Add invalid relative Git paths, linked intermediate output and a valid nested
write set with exact bytes. No local repository/native Git load is needed for
this seam. Special-repo fixtures instantiate records against synthetic prepared
cache trees and call only managed `ExtractAsync` paths, never download/expand.
Test a link in Stalker_GAMMA's **second** source/destination/version-file set
before the first copy, Teivaz's late `gamedata` tree, safe successful cleanup,
and failure/cancellation leaving temp/sentinel trees untouched. Register
`special-repos`.

```python
class SpecialRepoContractTests(unittest.TestCase):
    def test_special_repos_have_no_destructive_finally_or_unchecked_recursive_probe(self):
        directory = ROOT / "Stalker.Gamma/GammaInstallerServices/SpecialRepos"
        for name in ("GammaSetup.cs", "GammaLargeFiles.cs", "StalkerGammaRepo.cs", "TeivazAnomalyGunslingerRepo.cs"):
            body = (directory / name).read_text(encoding="utf-8")
            self.assertNotIn("finally", body)
            self.assertNotIn("SearchOption.AllDirectories", body)
            self.assertIn("CheckTree", body)
        self.assertIn("GitBlobWriter.WriteAsync", source("Stalker.Gamma/Services/GitService.cs"))
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_filesystem_boundaries.SpecialRepoContractTests -v`.**

Expected red: current special-repo cleanup is in `finally`, Teivaz enumerates
all descendants and Git writes outputs directly.

- [ ] **Step 3a: Preflight repository entry points and managed blob write sets.**

Before Git repository reads/fetches/clones, inspect the existing repository/output
path and its affected tree, allowing missing clone destinations only after parent
checks. These are prechecks, not a sandbox for native LibGit2Sharp internals.
For managed extraction, validate every raw blob relative path and the complete
existing output tree before any output directory/content stream opens. Resolve
each blob under the explicit output root and recheck before its write.

```csharp
ct.ThrowIfCancellationRequested();
var root = new FileSystemBoundary(outputDir, Directory.GetCurrentDirectory());
var outputs = blobs.Select(blob => (Blob: blob,
    Path: root.Resolve(InputPolicy.RequireRelativePath(blob.RelativePath, "Git blob path"))))
    .ToArray();
root.CheckTree(root.Root, allowMissing: true, ct: ct);
foreach (var output in outputs)
    root.CheckPath(output.Path, allowMissing: true);
```

Only after this phase create directories/open streams/write. Keep the source
repository alive while stream factories are used. Propagate cancellation and
unsafe paths; do not count a failed/cancelled blob write as completed.

- [ ] **Step 3b: Preflight each special repository's complete managed operation set.**

```csharp
var ordered = downloads.CheckTree(Name, ct: cancellationToken)
    .OfType<DirectoryInfo>().Where(directory => directory.Name == "gamedata")
    .Select(directory => directory.FullName).Order().ToList();
```

This is Teivaz's checked replacement for its recursive directory probe; the
other three records use their explicit source/destination sets below.

For all four special repos, verify `downloads` and use checked real-cache
repository/temp paths. Preflight **all** intended source/destination operations
and the temp cleanup tree before the first move/copy, not merely each call
after an earlier move succeeded. Stalker_GAMMA includes the addons tree, patches
tree, version source file and GAMMA `version.txt` destination in that set.
Replace `mods/../version.txt` with a canonical GAMMA-root resolution.

For Teivaz, obtain the matching directories from a checked snapshot, preserving
the existing sorted order; do not call `GetDirectories(...AllDirectories)`.
Recheck each source and destination immediately before its move.

- [ ] **Step 3c: Remove destructive failure/cancellation cleanup and false completion.**

```csharp
cancellationToken.ThrowIfCancellationRequested();
downloads.CheckTree(Name, ct: cancellationToken);
DirUtils.NormalizePermissions(TempDir);
Directory.Delete(TempDir, recursive: true);
```

This belongs after all successful copies, not in a catch/finally, and completion
is reported only after it returns.

Move temp chmod/delete out of `finally` to the successful path, after cancellation
checks and another strict cleanup preflight. On rejection/cancellation, leave
the temp tree for diagnosis; do not repair/delete it and do not increment the
completed-mod counter. Previous unrelated successful operations are not rolled
back. Do not alter remote URLs, branch policy or mutable-input pinning here.

- [ ] **Step 4: Run Task 6/all source contracts and `git diff --check`.**

Review managed write ordering and all repo cleanup paths inline. Record the
managed fixture scope separately from untested native Git/fetch behavior.

- [ ] **Step 5: Stage Task 6 files, inspect the index, commit and stop for batch review.**

```bash
git diff --cached --name-only
git commit -m "fix: preflight special repository writes and preserve rejected trees"
```

## Task 7: Shared known-input installer preflight and fixed-file operations

**Files:**
- Create: `Stalker.Gamma/GammaInstallerServices/GammaInstaller/InstallerInputPreflight.cs`, `tests/LinuxRegression/InstallerInputCases.cs`.
- Modify: `GammaInstallerArgs.cs`, `GammaInstallerArgsBuilder.cs`, `GammaInstaller.cs:42-120,156-407,412-419,430-503`, `OfflineGammaInstaller.cs:43-247,252-271` in that installer directory.
- Modify: `Stalker.Gamma/ModOrganizer/InstallModOrganizerGammaProfile.cs`, `WriteModOrganizerIni.cs`, `DisableNexusModHandlerLink.cs`.
- Modify: `Stalker.Gamma/GammaInstallerServices/PreserveUserLtxSettingsService.cs`, `PreserveMcmSettings.cs`, `UserLtxForceBorderless.cs`, `DeleteReshadeDlls.cs`, `DeleteShaderCache.cs`.
- Modify: runner/source contracts.

**Interfaces:**

```csharp
// GammaInstallerArgs: internal raw manifest snapshot, not a trust flag.
internal string? ManifestText { get; set; }

internal static class InstallerInputPreflight
{
    internal static void Validate(GammaInstallerArgs args,
        IReadOnlyList<ModPackMakerRecord> records, IReadOnlyList<ISeparator> separators);
}
```

`GetModpackMakerTxt` reuses `ManifestText` after obtaining the selected source
once. Parse/validate that raw snapshot for record builders, diff/update and
installation; do not use two different remote manifest responses for known
record construction and later writes. Caller-supplied local manifest/modlist
paths remain explicit configured inputs; normalize/check existing components
and the final file before reading, not as manifest child components.

- [ ] **Step 1: Add non-mutating preparation/fixed-file tests and structural red.**

```csharp
using var tree = new ScratchTree();
var args = new GammaInstallerArgs
{
    Anomaly = tree.At("new-anomaly"), Gamma = tree.At("new-gamma"),
    Cache = tree.At("new-cache"), BasePath = tree.Root, Mo2Profile = "../outside"
};
Check.Throws<UnsafePathException>(() => InstallerInputPreflight.Validate(args,
    Array.Empty<ModPackMakerRecord>(), Array.Empty<ISeparator>()));
Check.True(!Directory.Exists(args.Gamma), "profile validated before creating GAMMA");
Check.True(!Directory.Exists(args.Cache), "profile validated before creating cache");
```

Add a later invalid manifest row/separator with valid earlier siblings, linked
fixed profile files and root ancestors. Verify shared preparation creates
nothing and leaves sentinels/modes unchanged. Test the actual fixed-file helpers
for read, write and delete rejection; retain a valid positive Wine INI test.
No fixture calls a live `GammaInstaller.InstallAsync`, downloader or external
extractor. Register `installer-inputs`.

```python
class InstallerPreflightContractTests(unittest.TestCase):
    def test_each_bootstrap_preflights_before_alias_or_directory_mutation(self):
        for name in ("GammaInstaller.cs", "OfflineGammaInstaller.cs"):
            body = source("Stalker.Gamma/GammaInstallerServices/GammaInstaller/" + name)
            start = body.index("Task InstallAsync(")
            install = body[start:]
            check = install.index("InstallerInputPreflight.Validate")
            self.assertLess(check, install.index("EnsureReady"))
            if "Directory.CreateDirectory" in install:
                self.assertLess(check, install.index("Directory.CreateDirectory"))
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_filesystem_boundaries.InstallerPreflightContractTests -v`.**

Expected red: current installers create roots before parsing their known
manifest/profile values. Source assertion is not an end-to-end installer test.

- [ ] **Step 3a: Capture a stable base and one raw manifest snapshot.**

Capture `BasePath` once at argument construction; retain it through the builder.
Normalize Anomaly/GAMMA/cache against that base, using canonical values for I/O.
At both installer entry points, fetch/read the selected manifest, parse it,
build separators, validate every known record/profile/path and the complete
known target set, then establish roots/cache. Do this before readiness polling,
preservation reads and separator writes.

```csharp
args.CancellationToken.ThrowIfCancellationRequested();
var modpackMakerTxt = await GetModpackMakerTxt(args);
var modpackMakerRecords = ModListRecordFactory.Create(modpackMakerTxt);
var separators = SeparatorsFactory.Create(modpackMakerRecords);
InstallerInputPreflight.Validate(args, modpackMakerRecords, separators);
var downloads = args.Downloads;
downloads.EnsureReady(args.CancellationToken);
```

- [ ] **Step 3b: Validate the complete known input/target set for both bootstraps.**

```csharp
RecordPathPolicy.ValidateAll(records);
InputPolicy.RequireComponent(args.Mo2Profile, "MO2 profile");
args.Downloads.Preflight();
var gamma = args.Downloads.Gamma;
foreach (var file in new[] { "settings.txt", "modlist.txt", "modpack_maker_list.txt", "modpack_maker_list.json" })
    gamma.CheckPath(gamma.Resolve($"profiles/{args.Mo2Profile}/{file}"), allowMissing: true);
```

`InstallerInputPreflight.Validate` is non-mutating. It validates all known
record/separator/profile data before filesystem preflight, then checks root
ancestors, the proposed downloads boundary, each known separator output,
selected profile files, `ModOrganizer.ini`, `nxmhandler.ini`, `version.txt`,
resume paths and applicable preservation/cleanup targets. Fixed descendant
trees are checked before their related operations; do not generic-walk the
GAMMA root through its alias. Late metadata is checked at receipt, not guessed.

Before any related group/status work, the public record-building/update methods
validate all current input records and `Mo2Profile`; a filtered update subset
must not hide invalid known data. Preserve the existing update selection and
resume keys. Do not use a cached parse or “validated” boolean to bypass checks
after mutable model fields change.

- [ ] **Step 3c: Guard the fixed-file helper operations and installer outputs.**

For fixed-file helpers, use this pattern before their existing I/O:

```csharp
var root = new FileSystemBoundary(gammaPath, Directory.GetCurrentDirectory());
InputPolicy.RequireComponent(selectedProfile, "MO2 profile");
var ini = root.CheckPath(root.Resolve("ModOrganizer.ini"), allowMissing: true);
```

The exact helper/target mapping is:

| Existing operation | Preflight requirement |
|---|---|
| `InstallModOrganizerGammaProfile.InstallAsync` | Validate profile, profiles ancestry, selected directory and `settings.txt` before mkdir/write |
| `WriteModOrganizerIni.WriteAsync` | Validate selected profile and supplied separator folder components; check GAMMA/Anomaly roots and existing final INI before writing; preserve `Z:`/Wine payload output |
| `DisableNexusModHandlerLink.DisableAsync` | Check `nxmhandler.ini` leaf and parents before write |
| Preservation read/write helpers | Check `user.ltx` / `axr_options.ltx` before reads, writes and timestamp setters; retain explicit roots for later writes |
| `UserLtxForceBorderless` | Check existing `user.ltx` before read and again before conditional write |
| `DeleteReshadeDlls` | Preflight both fixed DLL leaves before deleting either; preserve payload/delete selection |
| `DeleteShaderCache` | Check complete selected cache tree before chmod/cleanup |
| Profile/manifest output at installer end | Check all selected fixed output leaves before the first mkdir/write in that output operation |

Pass real checked-cache sources instead of downloads-alias strings to profile
setup/special-repo consumers. Do not revive its commented-out modlist copy.
Check supplied local manifest/modlist files before `File.Exists`/read operations.

- [ ] **Step 3d: Stop retry/follow-up work on rejection or cancellation.**

```csharp
catch (Exception e) when (!UnsafePathException.IsUnsafe(e)
    && e is not OperationCanceledException and not CloudflareChallengeException)
{
    brokenAddons.Add(grs);
}
```

Exclude `UnsafePathException.IsUnsafe(e)` from `ProcessAddonsAsync`'s broken-addon
retry catch, including JSON/aggregate-wrapped stops.
Both online/offline orchestration must propagate rejection and cancellation,
cancel the batch token, and skip retries, subsequent extraction/profile writes,
minimal-cache deletion and success reporting. In-flight concurrent work is not
rolled back; this is not a transaction. Include cancellation in the offline
`ParallelOptions` and pass/check the callback token rather than dropping it.

- [ ] **Step 4: Run Task 7/all source contracts and `git diff --check`.**

Review both bootstrap orderings, update/standalone entry points, fixed outputs
and retry handling. Do not claim a full installer integration run.

- [ ] **Step 5: Stage Task 7 files, inspect staged scope and commit.**

```bash
git diff --cached --name-only
git commit -m "fix: validate known installer inputs before bootstrap mutations"
```

## Task 8: CLI MO2 profile read/write/activation/delete boundaries

**Files:**
- Modify: `stalker-gamma-cli/Commands/Mo2.cs:17-232`.
- Modify: `stalker-gamma-cli/Models/CliProfile.cs:54-81,109-112`.
- Modify: `stalker-gamma-cli/Utilities/ProfileUtility.cs:5-18`, `ModListUtility.cs:5-25`.
- Modify: `stalker-gamma-cli/Commands/Config.cs` only where activation/profile-setting errors are surfaced.
- Modify: `tests/LinuxRegression/LinuxRegression.csproj` to reference `../../stalker-gamma-cli/stalker-gamma-cli.csproj` in addition to the existing library reference.
- Create: `tests/LinuxRegression/ProfileCases.cs`; modify runner/source contracts.

**Interfaces:** Preserve public command names/signatures and CLI display-label
semantics. Add shared-use CLI convenience methods:

```csharp
public static string GetProfileDirectory(string gamma, string profile, bool allowMissing = false);
public static string GetProfileFile(string gamma, string profile, string fileName, bool allowMissing = false);
```

They live on existing `ProfileUtility`, validate the profile/fixed file
components and return canonical checked paths. No new CLI friend assembly,
NuGet package or test framework. Fixtures instantiate public CLI types without
calling its entry point; keep the runner's existing non-AOT override and leave
the application project properties unchanged.

- [ ] **Step 1: Add direct public CLI/model regressions and structural red.**

```csharp
using var tree = new ScratchTree();
var gamma = tree.Dir("gamma");
tree.Dir("gamma/profiles");
var sentinel = tree.File("outside/keep.txt", "keep");
var before = tree.Capture(sentinel);
var settings = new CliSettings
{
    Profiles = [new CliProfile { Active = true, Gamma = gamma, Mo2Profile = "G.A.M.M.A" }]
};
var command = new Mo2Cmds(new Serilog.LoggerConfiguration().CreateLogger(), settings);
Check.Throws<UnsafePathException>(() => command.DeleteProfile("../outside"));
tree.AssertUnchanged(before);
```

For every relevant public operation, create selected-profile directory/file/
parent links and assert rejection with unchanged external bytes/modes. Cover
`GetProfile`, `SetProfile`, `ListProfiles`, `ListMods`, `DeleteProfile`, `Status`,
`Enable`, `Disable`, `Delete`, `CliProfile.SetActiveAsync` and direct modlist
read/save. A linked fixed `ModOrganizer.ini` or `modlist.txt` must fail even
with a valid profile name. Positive cases use pre-created local files/profiles
so activation performs no request; a valid new profile's settings-file behavior
is covered by Task 7's library fixture. Assert `ProfileName = "label/with:punctuation"`
remains a display label and is not rejected merely by path policy. Register
`profiles`.

```python
class ProfileContractTests(unittest.TestCase):
    def test_profile_operations_use_checked_paths(self):
        commands = source("stalker-gamma-cli/Commands/Mo2.cs")
        self.assertIn("ProfileUtility.GetProfileFile", commands)
        self.assertIn("ProfileUtility.GetProfileDirectory", commands)
        self.assertIn("InputPolicy.RequireComponent", source("stalker-gamma-cli/Models/CliProfile.cs"))
        self.assertIn("CheckPath", source("stalker-gamma-cli/Utilities/ModListUtility.cs"))
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_filesystem_boundaries.ProfileContractTests -v`.**

Expected red: current profile consumers construct unchecked path strings.

- [ ] **Step 3a: Implement checked profile-path utility methods.**

```csharp
public static string GetProfileFile(string gamma, string profile,
    string fileName, bool allowMissing = false)
{
    var directory = GetProfileDirectory(gamma, profile, allowMissing);
    var root = new FileSystemBoundary(directory, Directory.GetCurrentDirectory());
    return root.CheckPath(root.Resolve(InputPolicy.RequireComponent(fileName, "profile file")), allowMissing);
}
```

`ValidateProfileExists` checks existing GAMMA/profiles ancestry before existence
predicates. `GetProfileDirectory` validates the profile, resolves it under the
explicit `GAMMA/profiles` root and checks existing parents/leaf. Listing scans
the affected profile tree before listing names; a linked profile must not be
silently reported as an ordinary directory. Selected-file reads/edits check the
specific fixed leaf; profile deletion preflights the **whole** selected tree
before `NormalizePermissions` or delete.

- [ ] **Step 3b: Route command/modlist read-write-delete operations through guards.**

```csharp
var modListPath = ProfileUtility.GetProfileFile(gamma, profile, "modlist.txt");
var modList = await ModListUtility.GetModListAsync(modListPath);
```

`ModListUtility` retains its explicit full-path API but performs strict
parent/leaf checks before both reads and writes. Do not change mod status
parsing/serialization or unrelated blank-line behavior in this stage.

- [ ] **Step 3c: Validate activation destinations before fetching or changing active state.**

```csharp
InputPolicy.RequireComponent(Mo2Profile, "MO2 profile");
var root = new FileSystemBoundary(Gamma, Directory.GetCurrentDirectory());
var modOrganizerIniPath = root.CheckPath(root.Resolve("ModOrganizer.ini"), allowMissing: true);
var mo2ProfilePath = ProfileUtility.GetProfileDirectory(root.Root, Mo2Profile, allowMissing: true);
ProfileUtility.GetProfileFile(root.Root, Mo2Profile, "modlist.txt", allowMissing: true);
```

`SetActiveAsync` validates `Mo2Profile`, root/profiles/INI/selected fixed-file
paths and enumeration before any related mkdir/fetch/write. Delay `Active = true`
until those checks pass. Existing missing-INI behavior remains a valid
configuration activation without installation; no network is added to that
branch. A missing selected profile may retain existing fetching behavior only
after all its destination checks; tests must not exercise real HTTP.

- [ ] **Step 3d: Surface component-setting errors without restricting display labels.**

```csharp
case "mo2profile":
    try
    {
        Mo2Profile = InputPolicy.RequireComponent(value, "MO2 profile");
        error = null;
        return true;
    }
    catch (UnsafePathException e)
    {
        error = e.Message;
        return false;
    }
```

`TrySet("mo2profile", ...)` reports a clear validation error before assigning;
other direct assignment/public mutation paths still revalidate independently.
Do not apply the component validator to `ProfileName` or the `mod` display-name
argument, which is looked up in modlist data rather than joined into paths.
Keep command names, normal valid punctuation and logging semantics.

- [ ] **Step 4: Run Task 8/all Python contracts, XML parsing and `git diff --check`.**

Confirm the CLI project reference resolves on disk and no CLI entry point is
called by fixture code. Compilation and source-generator/AOT results remain
unverified until authorized remote execution.

- [ ] **Step 5: Stage Task 8 files, inspect the index and commit.**

```bash
git diff --cached --name-only
git commit -m "fix: enforce filesystem boundaries for MO2 profile commands"
```

## Task 9: Cached manifests, prune planning and resume/recovery safety

**Files:**
- Create: `Stalker.Gamma/Filesystem/CachePrunePlan.cs`, `tests/LinuxRegression/CacheResumeCases.cs`.
- Modify: `Stalker.Gamma/GammaInstallerServices/GetStalkerModsFromLocal.cs:13-31`.
- Modify: `stalker-gamma-cli/Commands/Cache.cs:85-179,218-275`.
- Modify: `Stalker.Gamma/Models/ResumeState.cs:38-75,96-115`, `ResumableRecord.cs:21-48`.
- Modify: `stalker-gamma-cli/Utilities/ResumeUtility.cs:19-75`.
- Modify: `stalker-gamma-cli/Commands/FullInstall.cs:128-136`, `Update.cs:259-267` for recovery exclusion.
- Modify: runner/source contracts.

**Interfaces:**

```csharp
public sealed class CachePrunePlan
{
    public static CachePrunePlan Create(DownloadCache downloads,
        IEnumerable<string> keepNames, IReadOnlySet<string> ignoreFragments);
    public IReadOnlyList<FileInfo> Files { get; }
    public void Apply(CancellationToken cancellationToken = default);
}

// ResumeState internal helper, shared with its status update/write code.
internal static void ValidateWritePath(string path);
```

The prune plan preserves the CLI's current fixed ignore-fragment selection and
top-level file deletion scope. It is not a generic recursive cache remover.
`Create` requires a valid established boundary and never creates an alias/root.

Within the existing `Commands/Cache.cs` classes, change the plan-carrying
interfaces together so apply mode cannot lose its checked selection:

```csharp
// PruneLogic and its two derived handlers
private Task<CachePrunePlan> GetPrunePlanAsync(CancellationToken cancellationToken);
protected abstract void HandlePrunableArchives(CachePrunePlan plan,
    int maxPathLen, int maxMbLen, CancellationToken cancellationToken);
private static (int maxPathLen, int maxMbLen) CalculateMaxLengths(IReadOnlyList<FileInfo> files);
protected static long MbToReclaim(IReadOnlyList<FileInfo> files);
```

Replace the old `GetAddonsArchivesToDelete` flow with `GetPrunePlanAsync`.
`ExecuteAsync` reads `plan.Files` for the no-candidates/formatting paths and
passes the complete plan plus its token to the selected handler.

- [ ] **Step 1: Add cached-read/prune/resume side-effect regressions and structural red.**

```csharp
using var tree = new ScratchTree();
var gamma = tree.Dir("gamma");
var outside = tree.File("outside/sentinel", "original");
var before = tree.Capture(outside);
tree.LinkFile("gamma/resume.txt.tmp", outside);
var state = new ResumeState { Command = "fixture", FilePath = tree.At("gamma/resume.txt") };
state.Register("record");
Check.Throws<UnsafePathException>(() => state.SetStatus("record", ResumeStatus.Complete));
Check.True(!state.IsComplete("record"), "unsafe resume path does not publish completion");
Check.True(!System.IO.File.Exists(tree.At("gamma/resume.txt")), "no resume publication");
tree.AssertUnchanged(before);
```

Add linked `resume.txt`, `.tmp`, parent/root, direct `ReadAsync`/`Write`, and
CLI load/write/delete cases. Test a wrapped fake record that throws
`UnsafePathException` or cancellation and ensure its resume state is not marked
complete. Implement that test `IDownloadableRecord` with an explicit scratch
`DownloadRoot`, a `CheckDownloadPath` method using that strict root, and no
network/extractor implementation.

Prune cases: valid direct alias retains expected archives and removes only the
selected top-level files; wrong/missing/dangling/chained aliases are not repaired;
a late linked candidate or any disallowed encountered cache link prevents all
candidate deletions and size logging reads; ignored fragments do not authorize
links. A metadata-only unresolved archive set stops pruning rather than deleting
files it cannot identify. Cached profile JSON uses valid parsed instruction
arrays and rejects linked filename/parent/profile targets before reading.
Register `cache-resume`.

```python
class CacheResumeContractTests(unittest.TestCase):
    def test_prune_and_resume_use_shared_preflight(self):
        self.assertIn("CachePrunePlan.Create", source("stalker-gamma-cli/Commands/Cache.cs"))
        self.assertIn("ValidateWritePath", source("Stalker.Gamma/Models/ResumeState.cs"))
        for path in ("stalker-gamma-cli/Commands/FullInstall.cs", "stalker-gamma-cli/Commands/Update.cs"):
            self.assertIn("UnsafePathException", source(path))
        self.assertIn("CheckPath", source("Stalker.Gamma/GammaInstallerServices/GetStalkerModsFromLocal.cs"))
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_filesystem_boundaries.CacheResumeContractTests -v`.**

Expected red: current prune/resume/cached-read paths do not use this preflight.

- [ ] **Step 3a: Guard cached profile-manifest reads and invalid JSON classification.**

```csharp
InputPolicy.RequireComponent(mo2Profile, "MO2 profile");
var root = new FileSystemBoundary(gammaPath, Directory.GetCurrentDirectory());
var pathToModPackMakerList = root.CheckPath(
    root.Resolve($"profiles/{mo2Profile}/modpack_maker_list.json"), allowMissing: true);
```

`GetStalkerModsFromLocal.GetMods` validates the filesystem profile name and
checks the selected JSON path's ancestry/leaf before existence/read. Validate
deserialized path-producing records using the shared policy. Treat a malformed
cached JSON document as invalid managed input: translate its `JsonException` to
`UnsafePathException` with a constant field/document label and the original
inner exception, preventing unsafe-input recovery from being misclassified.
Preserve the existing missing-file error and source-generated JSON context.

- [ ] **Step 3b: Materialize and recheck the complete cache-prune set.**

`CachePrunePlan.Create` validates every supplied keep-name component, verifies
the exact downloads boundary and checks the affected cache tree before reading
file lengths/times or producing a plan. Select only top-level real files, using
the existing ignore-fragment behavior. Match by canonical real-cache paths,
not one alias form against one real-path form. `Apply` preflights the boundary
and **all** candidates before deleting the first, then rechecks each path near
deletion. Cancellation throws before follow-up reporting.

```csharp
var keepNames = addonRecords.Select(record =>
    InputPolicy.RequireComponent(record.ArchiveName, "cache archive")).ToArray();
var plan = CachePrunePlan.Create(downloads, keepNames, _ignoreAddons);
```

- [ ] **Step 3c: Integrate check/apply modes and stop on unresolved archive identities.**

```csharp
cancellationToken.ThrowIfCancellationRequested();
plan.Apply(cancellationToken);
```

Apply mode uses the previously created complete plan; check mode never calls
this mutation block.

The CLI must materialize/validate all known archive identities before asking for
a prune plan. Metadata-only rows still construct successfully, but an unresolved
archive cannot safely participate in a prune set: fail clearly without fetching
new metadata or deleting anything. Check/report mode never invokes `Apply`.
Use `plan.Files` for existing formatting; apply mode uses the plan's mutation
operation rather than raw `FileInfo.Delete`. Do not log a successful prune after
rejection. Failure logs are allowed; filesystem recovery is not.

- [ ] **Step 3d: Guard resume reads, temp/final writes and status publication.**

`ResumeState.ValidateWritePath` normalizes the explicit path and checks its
existing parents, final file and sibling `$"{path}.tmp"` before directory
creation, writing or publication. Recheck before the final move. Retain the
existing file format/temp naming; exclusive temporary publication is not claimed
as part of this link-precheck stage.

Call that helper **before** changing `_statuses` in `SetStatus` when `FilePath`
is set. Because `UnsafePathException` derives from `IOException`, exclude it
from the existing best-effort catch explicitly:

```csharp
catch (Exception e) when (!UnsafePathException.IsUnsafe(e)
    && (e is IOException or UnauthorizedAccessException))
```

- [ ] **Step 3e: Exclude unsafe/cancelled resume recovery at CLI boundaries.**

```csharp
if (resumeState is not null && !UnsafePathException.IsUnsafe(e)
    && e is not OperationCanceledException)
    ResumeUtility.Write(_logger, resumeState, gamma);
```

`ReadAsync`, `ResumeUtility.LoadAsync`, `Write` and `Delete` inspect existing
parents/leaf before any predicate/read/write/delete. `Write` must not swallow
the unsafe-path exception as a harmless logging error. In wrappers, check
cancellation after awaited record operations before status changes. In CLI
full-install/update recovery, do not call `ResumeUtility.Write` when
`UnsafePathException.IsUnsafe(e)` is true or failure is cancellation; no new
filesystem recovery begins after those stops. Preserve already safely persisted progress and ordinary
non-security error recovery.

- [ ] **Step 4: Run Task 9/all source contracts and `git diff --check`.**

Review `catch`/`finally`/minimal/delete/report paths across installer, record and
CLI boundaries. Verify validation failures are not retried, swallowed or marked
complete and have no automatic alias repair. C# runtime results remain absent.

- [ ] **Step 5: Stage Task 9 files, inspect staged scope, commit and stop for batch review.**

```bash
git diff --cached --name-only
git commit -m "fix: preflight cache pruning and stop unsafe resume recovery"
```

## Task 10: Coverage review, local verification and remote-execution handoff

**Files:**
- Create: `tests/FILESYSTEM-REGRESSIONS.md`.
- Modify: this plan's task/evidence checkboxes and, only for status/limitations, `README.md` and the approved specification/discovery tracker.
- Tests: the full existing/new Python source suite and all C# fixture sources.

**Interfaces:** No new runtime API. Consumes the completed task interfaces and
records exact verification evidence. Do not change CI or run .NET to close a
verification gap.

- [ ] **Step 1: Verify every required regression family has an explicit production operation.**

Use this traceability table; record concrete fixture method names in
`tests/FILESSYSTEM-REGRESSIONS.md` rather than claiming coverage from file count:

| Specification section at design checkpoint | Planned task(s) |
|---|---|
| Component/role/error contracts, lines 77–94 | 1, 3, 5, 7, 8 |
| Raw TSV versus JSON/list semantics, lines 96–125 | 1, 3 |
| Configured roots and case-sensitive containment, lines 127–139 | 2, 4, 7 |
| Source/destination/ancestor/leaf preflight, lines 141–153 | 2, 3, 5–9 |
| Exact alias and no repair, lines 155–177 | 4–7, 9 |
| Factories/late metadata/managed extraction/cleanup, lines 181–189 | 1, 3, 5, 6 |
| Both bootstraps/profile/CLI/fixed files, lines 190–194 | 7–9 |
| Known-data ordering, retries/status/finally, lines 196–206 | 3, 5–9 |
| Sentinel modes/bytes and safe fixture teardown, lines 208–237 | 2–9 |
| Concurrent/extractor/hard-link/mount limitations | Global constraints and final handoff; not implemented here |

Manually inspect the complete diff and remaining managed filesystem calls, not
only regex assertions. Check every `IDownloadableRecord` implementation,
factory/metadata assignment, `SearchOption.AllDirectories`, `.GetFiles`,
`File.Open*`, writes, deletes, permission setters, catches and finalizers in the
affected paths. Classify each remaining unchecked-looking call explicitly as
guarded, unrelated or outside this stage; do not silently broaden the scope.

- [ ] **Step 2: Run all allowed local checks and record actual output.**

```bash
python3 -m unittest discover -s tests -v
git diff --check
bash -n build/update-pkgbuild.sh
sh -n build/linux/stalker-gamma.AppDir/AppRun
sh -n build/linux/stalker-gamma-server.AppDir/AppRun
shellcheck build/update-pkgbuild.sh build/linux/stalker-gamma.AppDir/AppRun build/linux/stalker-gamma-server.AppDir/AppRun
```

Parse project/property XML and verify project references using only Python's
standard library:

```bash
python3 - <<'PY'
from pathlib import Path
import xml.etree.ElementTree as ET
paths = sorted(Path('.').rglob('*.csproj')) + [Path('Directory.Build.props')]
paths = [p for p in paths if not {'bin', 'obj', '.git'}.intersection(p.parts)]
for path in paths:
    root = ET.parse(path).getroot()
    for reference in root.findall('.//ProjectReference'):
        target = path.parent / reference.attrib['Include'].replace('\\', '/')
        if not target.is_file():
            raise SystemExit(f'Unresolved project reference: {path}: {target}')
print(f'{len(paths)} XML files parsed; project references resolve on disk')
PY
```

Run the existing installed parse-only comparison from
`docs/superpowers/plans/2026-10-17-linux-only-fork.md:955-995`, setting its
comparison revision to `f11e0c4`. Keep the same application directories and
`tests/LinuxRegression` inclusion. New files must have no parser diagnostics;
do not suppress/rewrite baseline generated-client/inferred-`ref` grammar
diagnostics or call parsing “compilation”. If the installed interpreter/module
is unavailable, record that check as unavailable; do not install it.

Verify stage/index scope separately; use `git diff --cached --check` after
staging. Nothing under ignored `docs/security/` or `build/` is newly staged.

- [ ] **Step 3: Review limitations and document the future runtime gate.**

Record four separate statuses in `tests/FILESYSTEM-REGRESSIONS.md`:

```text
Local Python source-contract red/green: actual command/output per task.
Local parser/XML/shell/whitespace checks: actual command/output or unavailable.
C# behavior and compilation: not run; remote authorization/input verification required.
Live manifests/native Git/extractors/installers and x64/arm64 runtime: not verified.
```

List literal runner suites: `wine-ini`, `inputs`, `boundaries`, `managed-tree`,
`cache-boundary`, `download-boundaries`, `special-repos`, `installer-inputs`,
`profiles`, `cache-resume`. Explain that no-argument execution runs the complete
suite and that no fixture may use real network, external extractors or a live
installation. The eventual remote build/run requires its own approval, including
any SDK/NuGet/input acquisition; plan approval does not provide that consent.

Document strict-link compatibility costs, unknown live-manifest compatibility,
checked-state-only guarantees, unisolated extractor writes and unfinished later
security stages. Neither output checksums nor a future source-test pass prove
download authenticity, race resistance or readiness for personal installation.

- [ ] **Step 4: Commit the verification handoff and present the batch results.**

Stage only the exact documentation/status paths changed, inspect
`git diff --cached --name-only`, run `git diff --cached --check`, then:

```bash
git commit -m "docs: record filesystem source verification and remote test gates"
git status --short --branch
```

Report completed source changes, actual local checks and unexecuted runtime
checks separately. Keep the worktree/branch and stop; do not merge, push,
dispatch CI, download a build, execute an installer or publish audit details.

## Plan-author self-review and approval state

### Planning checkpoint evidence

This is documentation-only preparation from the clean `f11e0c4` worktree, not
execution of the task checklists. The change set is this plan, the discovery
tracker and the specification's planning-authorization status. Production/test
source remains unchanged; no new C# behavioral fixture has been implemented,
compiled or executed at this checkpoint.

The 16 existing tests pass via `python3 -m unittest discover -s tests -v`.
Local document checks verify ten task sections, paired code fences, no flagged
placeholder phrases, parseable Python snippets in memory and unchanged
production/test scope. `git diff --check` reports no whitespace errors.
These checks say nothing about the proposed security behavior at runtime.

Inline plan review covered every specification section and reconciled parsed
array equality/zero sentinels, narrow test visibility, legacy source-contract
updates, live filename forwarding, default archive alias verification, wrapped
validation errors and resume/recovery ordering. No subagent review ran.

### Documentation references used for the plan

These are canonical read-only API references, not downloaded build inputs or
runtime verification of this project's use of them:

- `https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath?view=net-10.0`
- `https://learn.microsoft.com/en-us/dotnet/api/system.io.file.getattributes?view=net-10.0`
- `https://learn.microsoft.com/en-us/dotnet/api/system.io.filesysteminfo.linktarget?view=net-10.0`
- `https://learn.microsoft.com/en-us/dotnet/api/system.io.filesysteminfo.resolvelinktarget?view=net-10.0`
- Context7 `/dotnet/docs`, custom System.Text.Json converter documentation:
  `https://github.com/dotnet/docs/blob/main/docs/standard/serialization/system-text-json/converters-how-to.md`.

- [x] Every specification requirement maps to a concrete task or explicit exclusion.
- [x] Code steps contain concrete examples, case matrices and literal local commands.
- [x] Shared method/type/property names and factory propagation agree across tasks.
- [x] Alias-scoped GAMMA traversal does not grant a generic link exemption.
- [x] Every planned mutation set has preflight before its first mutation, including cleanup/recovery.
- [x] C# behavioral fixtures and local structural checks are distinguished throughout.
- [x] Documentation-only planning checkpoint verified; production/tests unchanged.
- [ ] User approves implementation of the first inline batch (Tasks 1–3).

No implementation task is checked complete when this plan is authored.
