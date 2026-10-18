# Filesystem foundation regression evidence

## Status and authorization

**The first-batch source implementation/review gate is complete.** After the
initial task gates, the broad review identified I1 and its correction introduced
B1. I1 is source-addressed in `fe60971`; the user-authorized B1 correction
`8830dff` receives both source-spec and quality approvals with no new findings.
The final scoped review leaves no unresolved I1/B1 source finding. This is not
compilation, runtime security, full hardening or installation approval. The plan is
`docs/superpowers/plans/2026-10-18-filesystem-foundations.md`.
This file distinguishes source/wiring checks from production-C# behavioral
fixtures. No new behavioral result is claimed by planning or writing a fixture.

- Source baseline: `a06783b`; approved architecture/first-batch plan: `132d0d8`.
- Local baseline command:
  `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -v`.
  Observed **16 existing source/dry-run tests, OK**, before foundation source edits.
- No local SDK/compiler/build/restore/runtime-test invocation is authorized.
- No remote execution, downloads/packages, native initialization, network fixture,
  extractor, installer, container, push, merge or publication is authorized.

## Suites and source-review gates

**All C# fixtures below remain uncompiled and unexecuted.** Task completion means
the specified source-review gate, not filesystem behavior or installer readiness.

| Suite | Intended production-C# observations | Current source status |
|---|---|---|
| `inputs` | Owned transport, raw validation, blank/no-op distinction, literal-zero round trips, wrapped stop classification | `6b12bc8`: spec/quality review approved, no findings; registered |
| `scratch` | Owned helpers, snapshots, non-following teardown, independent external-tree preservation and failure retention | `a5221f2`: spec/quality review approved, no findings; registered |
| `wine-ini` | Existing Wine paths, executable payload names and INI expectations with owned teardown | Same Task 2 approval; registered |
| `records` | Actual converter/factory roles, separator provenance and both diff consumers | `d8909ec`: spec/quality review approved, no findings; registered |
| `filesystem` | Typed inspection, missing semantics, parents, links, inventories and cancellation | `fcac037`: spec/quality review approved, no findings; registered |
| `managed-tree` | Complete mapped preflight, copy/move ordering, counting, chmod/cleanup, instructions and fixed-file writes | Initial `b9eeaef` gate plus I1/B1 corrections through `8830dff` source-approved; no unresolved findings; registered |

At each closed task, a scoped `git diff --exit-code <reviewed-commit> -- <owned-paths>`
confirmed that its reviewed sources were unchanged. No deferred source findings
were reported for Tasks 1–5. Their compiler/platform and downstream-integration
unknowns remain open below.

## Structural evidence — not behavioral TDD

Python checks locate source/wiring contracts; they do not reimplement the C#
policy. Marker checks and a syntax parser do not prove runtime operation ordering,
link handling or filesystem confinement.

### Fixture-first source red/green

The implementer reports record these initial outputs before production/helper
edits. The final diff alone cannot independently prove chronology.

| Command prefix: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest` | Initial source red | Targeted source green |
|---|---|---|
| `tests.test_input_policy_sources -v` | 7 methods, 6 missing-feature failures | 7 methods, OK |
| `tests.test_fixture_sources -v` | 9 methods, 7 missing-helper failures | 9 methods, OK; temp-parent preflight had a further single-failure red/green |
| `tests.test_record_policy_sources -v` | 12 methods, 10 missing-feature failures | 12 methods, OK |
| `tests.test_inspection_sources -v` | 12 methods, 11 missing-source failures | 13 final methods, OK; explicit no-skip enumeration had a further red/green |
| `tests.test_managed_tree_sources -v` | 16 methods, 15 missing-feature failures after correcting a Python missing-marker lookup error | 16 methods, OK |

### Independent coordinator checks before task review

| Command prefix as above / check | Observed result |
|---|---|
| `tests.test_input_policy_sources tests.test_linux_only -v` | 23 source/dry-run methods, OK |
| `tests.test_fixture_sources tests.test_linux_only -v` | 25 source/dry-run methods, OK |
| `tests.test_record_policy_sources tests.test_linux_only -v` | 28 source/dry-run methods, OK |
| `tests.test_inspection_sources tests.test_input_policy_sources tests.test_fixture_sources tests.test_linux_only -v` | 45 source/dry-run methods, OK |
| Scoped installed tree-sitter parse of Tasks 1/2/3/4 | 5/4/7/4 C# files respectively, each with 0 diagnostics |
| Coordinator Program/friend-assembly syntax parse | 2 C# files, 0 diagnostics |
| `tests.test_managed_tree_sources tests.test_input_policy_sources tests.test_fixture_sources tests.test_inspection_sources tests.test_record_policy_sources tests.test_linux_only -v` | 73 source/dry-run methods, OK |
| Scoped installed tree-sitter parse of Task 5 | 7 C# files, 0 diagnostics |
| Exact staged-path inspection and `git diff --cached --check` per task commit | Only task-owned paths; no whitespace diagnostics |

Do not add those test counts: the commands share the same 16 baseline methods.
The checks independently confirm source state, not the historical red or C#
execution. The Task 3 discovery snapshot had 14 peer/coordinator failure entries
across 58 methods (11 inspection-source failures, 3 runner-registration subtests)
while those implementations were incomplete; it was not a full-green result.

### Coordinator runner source integration

Before integration edits,
`PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests.test_foundation_integration -v`
observed two methods with eight assertion/subtest failures: absent friend access,
six absent suite registrations and absent argument dispatch. The targeted
friend-only method subsequently passed (one method, OK).

The runner now registers all six reviewed suites, validates all requested
names before invoking any fixture, runs all registered suites when no arguments
are supplied, retains its Linux guard and has no inline recursive teardown.
The last registration was added only after Task 5's clean source review. Full
Python discovery and whole-batch parser/XML checks then passed as recorded below;
the broad source review subsequently identified I1, recorded below. No C# runner
was executed.

The registration source assertion now requires exact name/class-RunAsync pairs.
An additional source-order method checks the guard, all-suite selection, requested
name validation and later dispatch loop. The targeted friend/selection command
(`tests.test_foundation_integration.FoundationIntegrationTests.test_library_exposes_internals_only_to_regression_assembly`
and `tests.test_foundation_integration.FoundationIntegrationTests.test_runner_selection_is_prevalidated_before_fixture_dispatch`,
using the same unittest prefix and `-v`) observed 2 methods, OK. At that snapshot
all-suite integration was pending; these two checks do not establish executed
argument handling.

Immediately before Task 5 review, fresh coordinator full discovery observed
76 methods with exactly one failing subtest: the intentionally pending
`("managed-tree", ManagedTreeCases.RunAsync)` registration. All other methods
passed. Only Task 5's eight files entered `b9eeaef`; its task review was active
at that snapshot. This historical run does not establish any C# behavior.

### Integrated source verification before broad review

After the final reviewed suite registration, the coordinator actually ran:

| Command/check | Observed result |
|---|---|
| `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -v` | 76 source/dry-run methods, OK; no remaining registration failure |
| Installed tree-sitter baseline comparison from the plan's existing parse-only protocol | 147 C# files; six original baseline diagnostic files; zero files with additional diagnostics |
| XML parser over Directory.Build.props and all five projects | Six XML files parsed |
| `git diff --check` | Exit 0, no whitespace diagnostics |

Task 5's initial returned review approved both source-level spec compliance and
quality with no findings. A scoped diff at that task's closure confirmed its
eight files unchanged since `b9eeaef`. Fixture-first chronology remains
report-supported, not independently proven by the final diff. The next gate at that
snapshot was the broad first-batch review, not C# execution, merge or installation.

### Broad review and single final correction

The broad source review of `a06783b..9c6853d` returned 0 Critical, 1 Important
and 0 Minor findings. I1 identified missing preparation of derivable mapped
targets when preceding instructions produce a later source. It was a source/
control-flow finding, not a reproduced C# runtime failure.

One consolidated fix wave produced `fe60971`, changing only ProcessInstructions,
ManagedTreeCases and their Python structural check. Its report records
fixture-first exact/reverse conflicts with unchanged whole-extraction snapshots, longer
produced-source chains and destination-choice/consumed-content expectations.
Those C# fixtures remain written, uncompiled and unexecuted.

The implementer recorded structural red with 17 methods: 16 passed and one
missing-composition assertion failed; targeted green then passed all 17. The
coordinator freshly ran full Python discovery after the fix: **77 source/dry-run
methods, OK**. Whole-batch parsing still observed 147 C# files, six original
baseline diagnostic files and zero additional diagnostic files; all six XML
files parsed. Working and cached whitespace checks returned 0, and the cached
path list contained only those three fix paths. No behavioral green is inferred.

The single scoped re-review of `9c6853d..fe60971` found I1 source-addressed, but
reported new B1 (0 Critical / 1 Important / 0 Minor). The coordinator checked the
source: composition removes consumed files from `knownContents` at
`ProcessInstructions.cs:151–153`, but the shared `requiredKinds` table persists
across instructions and still rejects a later directory at the now-absent file
path (`:57–65,190–193`). This is a compatibility rejection in source/control flow,
not an executed C# reproduction or a demonstrated escape/destructive outcome.

The four-step counterexample produces a file at `gamedata/group/conflict`, moves
it to `gamedata/conflict`, then needs the former path as a directory. The stale
NonDirectory requirement rejects that reuse. This violates the intended ordered
produced-source behavior in the functional spec at lines 130–139. The coordinator
classified B1 as load-bearing and did not park it. At that checkpoint both
source-spec and quality approval were withheld despite passing structural checks.

At that handoff, the one final fix wave and its scoped re-review were exhausted;
implementation stopped for the user. The user then explicitly authorized a B1-only
continuation: parallel fixture writing and read-only production analysis, followed
by a gated minimal production correction and scoped review. Baseline at `4f2307d`
was clean; fresh `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -q`
observed 77 source/dry-run methods, OK. This does not resolve B1 or authorize later
batches. The branch, worktree and ignored reports remain preserved.

### B1 continuation — fixture-first source red/green

The fixture author added two production-C# cases before production changes:
successful directory reuse after consuming a produced file, and rejection of the
same still-live file/directory conflict when the consuming move is omitted. Both
are registered; all C# fixtures remain uncompiled/unexecuted.

The targeted source command
`PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests.test_managed_tree_sources -v`
then observed **18 methods: 17 pass, one expected failure, no errors; exit 1**.
The failure identifies `requiredKinds` declared before the instruction loop.
A separate source analysis supports per-step proposed roles with unchanged
cross-step contents. The coordinator confirmed production unchanged against
`4f2307d` before releasing that minimal edit. This is structural red, not an
executed C# regression.

The released production change relocates only the `requiredKinds` allocation
inside the instruction loop and adds an explanatory comment. The implementer
observed targeted **18/18** and full **78/78** source checks passing. The
coordinator independently ran full discovery with `-q`: **78 methods, OK**,
then the installed baseline parser/XML protocol: **147 C# files, no additional
diagnostic files over the six baseline files; six XML parses**. Whitespace and
exact staged-path checks passed.

Correction commit `8830dff` contains only ProcessInstructions.cs and its two
test files; coordinator plan/evidence and ignored reports were excluded. The
scoped `4f2307d..8830dff` review now marks B1 **ADDRESSED**, source-spec **APPROVED**
and source-quality **APPROVED**, with **0 Critical / 0 Important / 0 Minor** new
findings and no new out-of-scope observations. Source approval comes from that
review, not the Python markers. A fresh scoped diff confirms the three reviewed
files unchanged since `8830dff`.

All task gates, the broad review and its authorized scoped corrections are now
closed at source level. C# compilation/source generation, behavioral fixtures and
the later integration/security gates below remain unverified or unimplemented.
The source batch stops here; no push, merge, remote execution or later batch is
authorized by this approval. The existing branch/worktree and ignored evidence
are retained.

## Independent coordinator integration audit

While Task 5 was active, a separate source/XML audit resolved five project files,
three project references and four solution entries with no cycle or production
reference to the fixture project. A scoped comparison to `a06783b` confirmed the
project/dependency/solution declarations unchanged. No MSBuild evaluation occurred.

The audit cross-checked the two JSON writers and local JSON reader against the
same declared list context, CLI comparison-only DTO defaults and the server's
separate DTO/project declarations. It found no additional old string-instruction
consumer in those inspected references; this is not compiled API compatibility.
Canonical Microsoft converter/source-generation docs were consulted; a returned
.NET 11 constructor excerpt was explicitly not applied to this net10.0 project.

Private source references and later-integration observations are retained in the
ignored `docs/security/2026-10-18-foundation-integration-audit.md`; do not stage
that report. No subagent-owned implementation file was touched by this audit.

## Intentional input compatibility contracts (source batch)

- Blank instruction fields remain distinct from explicit no-op `0`/JSON `[]`
  for separator selection.
- Instruction values compare blankness and canonical ordered executable paths;
  redundant empty wire markers alone no longer constitute modifications.
- A parsed literal `0` serializes as `["0", ""]`; raw `["0"]` remains a no-op.
- Blank JSON patch fields remain blank instead of becoming the decoration `"- "`.
  Required patch validation therefore applies to meaningful supplied content.
- Missing optional fields and comparison-only DTO construction stay valid for
  their applicable role. Unrelated diff identity/hash/author rules are preserved.
- Live manifest compatibility has not been checked; no manifest was fetched.

## Runtime and integration gates still open

No C# compilation, source-generator execution or fixture execution is established.
The separately authorized future runner command is:

```text
dotnet run --project tests/LinuxRegression/LinuxRegression.csproj -c Release
```

Do not execute it under this source-only authorization. It must eventually cover
both Linux x64/arm64; the non-AOT runner does not establish NativeAOT CLI behavior
or the companion server's separate publish configuration.

This intermediate foundation batch is not the complete 4A boundary. Strict generic
helpers may reject existing special-repository alias paths until the next batch
migrates them to an explicit cache owner. Exact alias/raw-target/layout checks,
download/extraction entry guards, metadata lifecycle, cancellation/resume/recovery,
installer/profile/cache consumer wiring and their behavioral fixtures remain
later integration. The approved GAMMA/cache separation rule is not claimed
enforced by this foundation work.

Helper-local unsafe/cancellation stops have not yet been propagated through all
callers, recovery and finally paths. Some existing callers still normalize/delete
after a helper failure or invoke cleanup without the available token. Their
separately scoped orchestration work must precede any whole-installer fail-closed
or no-follow-up claim.

Inspection and path-based I/O remain non-atomic. Extractor/native-process writes,
concurrent replacement, hard links, mounts, special POSIX file classification,
artifact authenticity, atomic downloads, restrictive permissions, subprocess and
server policy, and pinned/authenticated build inputs remain separate gates.
Unchanged sentinel bytes/modes do not prove that no read occurred. None of these
source checks or planned synthetic tests authorizes personal installation/use.
