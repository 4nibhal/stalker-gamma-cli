# Batch 4A — architecture review before parallel implementation

## Authorization and unchanged limits

The user requested: **“implementa de forma paralela con subagentes”**, and
explicitly asked to analyze the problem and reason about the architecture
before implementation. This supersedes the earlier no-subagent execution
preference. It does not authorize local .NET compilation, downloads, new
dependencies, workflow execution, installers, containers, push or publication.

Starting evidence: clean `linux-hardening` worktree at `a06783b`, whose changes
are the filesystem implementation plan and design/discovery status documents.
At that starting snapshot, the plan's inline/no-agent wording predated this new
instruction, and production/regression-source implementation had not started.
The investigative source findings below are that pre-implementation snapshot,
not a claim about later commits. Current source progress and verification limits
are recorded in `tests/FILESYSTEM-REGRESSIONS.md` and the revised foundations plan.

The approved behavioral scope remains the strict-link, no-automatic-repair,
checked-state-only Batch 4A design. Do not silently claim concurrency resistance,
extractor isolation or completed runtime verification. Review the complete 4A
architecture before changing the first batch's shared contracts.

## Review checklist

- [x] Read current specification, plan, branch/status and recent commits.
- [x] Review independent input/domain, filesystem-operation and verification boundaries in parallel.
- [x] Verify the selected cross-boundary findings below against current source; distinguish facts/proposals/unknowns.
- [x] Compare architectural approaches and recommend a minimal, coherent dependency structure.
- [x] Present concrete contracts, failure ordering, tests and safe parallel ownership.
- [x] Resolve the GAMMA/cache overlap decision explicitly before implementation.
- [x] Update the design/implementation plan and execution authorization to match the new direction.
- [ ] Execute only approved implementation batches with isolated file ownership and integration review.

No browser/visual companion is needed for this source/contracts review.

### Design workflow

- [x] Explore the existing project/specification/planning context.
- [x] Assess whether a visual question needs a browser companion: none identified.
- [x] Surface the GAMMA/cache compatibility restriction as one explicit question; user answered “adelante”.
- [x] Compare two or three approaches and recommend one with explicit trade-offs.
- [x] Present architecture, data flow, errors, tests and parallel ownership for approval.
- [x] Reconcile the written specification after the user approves the design.
- [x] Self-review the written specification for ambiguity, scope and contradictions.
- [x] Present this written architecture review and receive the user's “adelante”; no execution-permission expansion beyond the requested source batch.
- [x] Update the first-batch implementation plan using `writing-plans` after the design gate.

## Independent read-only reviews

1. **Input/domain contract:** manifest roles, raw TSV/JSON transport, instruction
   identity, optional metadata, equality and public/direct construction paths.
2. **Filesystem/operation architecture:** root authority, no-follow inspection,
   complete mutation sets, alias scoping, recursive traversal and failure cleanup.
3. **Verification/dependency closure:** testability without local .NET, fixture
   teardown, error classification, integration sequencing and parallel ownership.

Dispatched read-only review sessions:

| Responsibility | Session |
|---|---|
| Input/domain | `ses_eb4051564ffeWp5y52m5kIsMhh` |
| Filesystem/operations | `ses_eb405154bffenON3qv3WOPnoJt` |
| Verification/error flow | `ses_eb4051547ffeFhWi4xLe1R8hWN` |

Agents must not edit files, run .NET, download artifacts, invoke installers,
dispatch CI, commit, or start further agents. Their outputs require source
locations, explicit uncertainty and architecture proposals rather than claims of
implemented protection. The parent owns reconciliation and any documentation.

## API evidence researched by the parent

This is documentation/upstream-source evidence, not a result from the target
application. No .NET executable, compiler or behavioral fixture was run.
Context7 resolved `/dotnet/docs` but returned no match for the scoped filesystem
query; the following canonical Microsoft/.NET pages were read into the tool
response without downloading artifacts into the project.

- [`File.GetAttributes`, .NET 10](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.getattributes?view=net-10.0)
  documents missing-file/directory and unauthorized-access exceptions. Do not
  replace inspection failures with a broad `Exists == false` inference.
- [`FileSystemInfo.LinkTarget`, .NET 10](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesysteminfo.linktarget?view=net-10.0)
  describes the target string or `null` for an instance not representing a link;
  this page alone does not establish the required inaccessible/dangling-entry
  classification. A null value alone is not the proposed safety check.
- [`FileAttributes`, .NET 10](https://learn.microsoft.com/en-us/dotnet/api/system.io.fileattributes?view=net-10.0)
  documents cross-platform `ReparsePoint`; `Device` is reserved for future use.
  This does not prove POSIX regular-file classification for every non-directory.
- [`FileStatus.Unix.cs`, upstream `release/10.0`](https://github.com/dotnet/runtime/blob/release/10.0/src/libraries/System.Private.CoreLib/src/System/IO/FileStatus.Unix.cs)
  lines 493–525 inspect through `LStat` and distinguish missing/error states;
  lines 193–215 derive attributes from cached flags, with a `Normal` fallback.
- [`FileSystem.Unix.cs`, upstream `release/10.0`](https://github.com/dotnet/runtime/blob/release/10.0/src/libraries/System.Private.CoreLib/src/System/IO/FileSystem.Unix.cs)
  lines 582–589 implement path-based attributes through `FileInfo`; line 662
  implements `GetLinkTarget` using `ReadLink`. These branch URLs are research
  references, not pinned or authenticated build inputs.

**Design consequence to reconcile:** distinguish lexical validation, existing
entry inspection and operation-specific expected file/directory shape. Reject
inspection errors rather than treating them as absence. Do not introduce an
unverified native ABI or imply that path checks authorize later I/O atomically.
Runtime behavior on both target architectures remains unverified.

## Architectural options for reconciliation — proposals, not approved changes

| Approach | Proposed benefit | Trade-off / scope decision |
|---|---|---|
| Local guards in each existing caller | Small changes at individual sites | Leaves each caller responsible for parsing, alias exceptions and complete preflight ordering; not the proposed direction |
| Shared input policy + strict boundary + explicit cache boundary + operation-owned preflight | Separates syntax, inspected state, the one alias and mutation ordering; matches the approved specification's responsibilities | Requires contracts to be frozen before parallel consumers; recommended basis for reconciliation |
| New general sandbox/native-handle/extractor framework | Targets a stronger isolation objective | Changes the agreed stage; concurrency and extractor isolation need their own later design, not an implicit addition to 4A |

The recommended option is not permission for a new generic framework, opaque
security tokens or a second path policy in tests. An inspected snapshot remains
evidence only; the operation still owns rechecks, mutation ordering and failure
propagation. Whether the existing plan's concrete signatures express this cleanly
is pending the independent reviews.

### Proposed closure and parallelism rules

- Freeze shared contracts and common test-runner ownership before agent edits.
- Separate input/domain and filesystem primitives by explicit file ownership;
  do not pretend managed operations are independent of both foundations.
- The parent integrates shared entry points/tests and owns git staging/commits;
  agents must not commit each other's incomplete work in the shared worktree.
- Review each integrated batch before authorizing dependent work. A task may be
  marked source-implemented/reviewed without being marked runtime-verified.
- Keep the runtime gate and personal-installation gate separate: neither a
  passing Python structural check nor an agent review closes either gate.

These are execution proposals to present with the reconciled findings, not a
claim that the first implementation batch has started or passed.

## Returned review: verification and error flow

Read-only session `ses_eb4051547ffeFhWi4xLe1R8hWN` returned its report. The parent
checked the following findings against the current files; no runtime reproduction
was attempted because .NET execution remains unauthorized.

| Verified source/planning fact | Architectural consequence proposed for review |
|---|---|
| `Stalker.Gamma/Models/ResumableRecord.cs:23–41,55–65` skips completed operations but reads `ArchiveName` for its event; the plan at `1154–1166` proposes a throwing unresolved getter | Separate optional presentation metadata from the archive identity required for actual I/O; test the real completed-resume decorator chain |
| `stalker-gamma-cli/Commands/Update.cs:144–160` resolves Git repositories through the downloads alias and probes `Directory.Exists` first | Include `update check` in the explicit real-cache migration, not only recovery filtering |
| `Stalker.Gamma/ModDb/Services/ModDbService.cs:34–39,111–116` accepts every non-Cloudflare exception in its retry predicates; canonical-link, mirror and manifest wrappers retain cancellation as inner exceptions | Use recursive cancellation recognition separately from unsafe-path classification; do not retry or convert either stop into broken-addon success |
| `Stalker.Gamma/ModDb/Services/ModDbService.cs:56–87` places semaphore acquisition inside the `try` whose `finally` releases it; `Stalker.Gamma/ModDb/Services/ModDbMirrorService.cs:20–47` acquires before entering its release-owning `try` | Resource ownership must survive cancellation; this is not a reason to prohibit all `finally` blocks |
| `Stalker.Gamma/GammaInstallerServices/GammaInstaller/GammaInstaller.cs:125–153,221–240,475–504` publishes statuses after awaited calls and links batch cancellation only for the shown Cloudflare catch | Specify per-worker stop propagation and token checks before status publication or follow-up operations |
| `Stalker.Gamma/GammaInstallerServices/GammaInstaller/OfflineGammaInstaller.cs:258–269` omits the token from `ParallelOptions` and discards the worker token | Include offline orchestration in cancellation fixtures; a fake that cancels and returns normally is different from a throwing fake |
| `Stalker.Gamma/Factories/NetworkServiceFactory.cs:13–19` chooses native curl when no server URL exists; `Stalker.Gamma/Factories/StalkerGammaServerClientFactory.cs:8–16` creates its adapter without the supplied HTTP factory | The proposed memory handler is not yet an end-to-end metadata-receipt seam; either wire existing injection narrowly or limit the coverage claim |
| `tests/LinuxRegression/Program.cs:8–40` has one Wine INI fixture and recursive scratch deletion | Establish independent non-following teardown and its own external-tree preservation fixture before adding unsafe-tree cases |

The cancellation wrappers checked are
`Stalker.Gamma/GammaInstallerServices/GetCanonicalLinkFromModDbStartLink.cs:29–44`,
`Stalker.Gamma/ModDb/Services/ModDbMirrorService.cs:33–42` and
`Stalker.Gamma/GammaInstallerServices/GetStalkerModsFromApi.cs:26–35`.
Plan line references are to the unchanged
`2026-10-18-filesystem-confinement.md` at `a06783b`.

The report additionally requests checked extractor test seams, public extractor
entry-point coverage, directory-mode/absent-output observations, MO2 complete-set
deletion checks and a dependency-ordered implementation DAG. These await the
filesystem review and parent cross-check; they are not accepted as verified
runtime behavior or implemented fixes.

## Returned review: input/domain contracts

Read-only session `ses_eb4051564ffeWp5y52m5kIsMhh` returned its report. The parent
cross-checked the following against the named files and the unchanged plan.

| Verified source/planning fact | Architectural consequence proposed for review |
|---|---|
| `Stalker.Gamma/Models/ModPackMakerRecord.cs:78–116` joins/splits JSON entries using `:`; `Stalker.Gamma/Factories/DownloadableRecordFactory.cs:283–289` trims and removes leading backslashes | Distinguish raw TSV, raw JSON and already-parsed inputs; never restore the old join/reparse pipeline |
| `Stalker.Gamma/Factories/SeparatorsFactory.cs:15–26` tests field blankness and uses list position; plan `362–375,448–449` substitutes an empty array for blank and explicit-zero values | Retain blank-versus-explicit-no-op meaning independently of executable path count; preserve separator numbering |
| Plan `371–389` keeps a parsed literal `0`, but its JSON reader treats singleton `["0"]` as a sentinel and its writer emits that singleton | Specify a stable unambiguous representation for literal `0`, such as `["0", ""]`, and test parse/write/parse |
| `Stalker.Gamma/Factories/DownloadableRecordFactory.cs:148–166,175–181,210–223,250–259` selects ordinary ModDB, metadata-only ModDB then GitHub, with different incomplete-field outcomes | Use one role-aware preparation function, not an optional-patch naming helper as the entire role validator; keep comparison-only DTOs possible |
| `Stalker.Gamma/Models/ModPackMakerRecord.cs:121–135` decorates JSON patch values and strips a set of prefix characters when writing; TSV only trims its patch field at `Stalker.Gamma/Factories/ModPackMakerRecordFactory.cs:24` | Decide the raw-patch/role compatibility rule explicitly; the plan's blank-preserving proposal is not equivalent to the current JSON conversion |
| `Stalker.Gamma/ModDb/Services/ModDbGetAddonMetadataService.cs:32–34,59` trims the HTML filename before assignment | Specify which transport formatting may be trimmed and validate filename data before lossy normalization; a setter alone does not preserve raw evidence |
| `Stalker.Gamma/Models/ModPackMakerRecord.cs:22–58` compares all non-counter fields; `Stalker.Gamma/Extensions/DiffModPackMakerLists.cs:19–49,76–98` has narrower identity/modification rules | Define instruction-value equality/hash together; replace only instruction comparison in diffs, not the entire diff contract |
| `stalker-gamma-cli/Commands/Update.cs:161–167,180–186` creates comparison records without patch/instructions | Do not require a complete installable row whenever a DTO is constructed |

**Proposed direction:** one small owned instruction value with defensive copying,
no mutable backing collection exposure, explicit blank/no-op meaning and distinct
raw-ingress adapters. This is not a public hierarchy of record roles. Equality,
non-executable marker handling, edge whitespace and literal-zero serialization
must be fixed contracts before parallel consumers are assigned.

Both returned reviews independently identify the unresolved-filename/resume
conflict. Preserve an observable unresolved metadata state for display/resume,
but require a validated filename before producing or using an archive path.
No setter, constructor, equality implementation or behavioral fixture has been
changed or executed.

## Returned review: filesystem and operation ownership

Read-only session `ses_eb405154bffenON3qv3WOPnoJt` returned its report. The parent
cross-checked the following selected source/planning findings.

| Verified source/planning fact | Architectural consequence proposed for review |
|---|---|
| `Stalker.Gamma/Utilities/DirUtils.cs:17–51` mutates the starting directory before recursion and suppresses permission errors; `175–195` creates output and hashes before applying the file filter | Inspect the complete affected state and derive selection before managed mutations or content reads; observe directory modes and absent outputs in fixtures |
| `Stalker.Gamma/Utilities/DirUtils.cs:88–108,130–135,234–254` performs separate counting/descendant probes; recursive copying omits the token at `244–254` | Reuse one operation-owned traversal for selection/counting/mapping and carry cancellation through execution; no unchecked progress walk |
| `Stalker.Gamma/GammaInstallerServices/ProcessInstructions.cs:13–40` processes instructions sequentially; plan `112–118,754–782` requires list-wide preflight as well | Validate the whole list/tree first, then prepare each step against current state; retain sources created by earlier steps |
| `Stalker.Gamma/GammaInstallerServices/CleanExtractPath.cs:9–16` composes chmod and deletion; `Stalker.Gamma/GammaInstallerServices/SpecialRepos/StalkerGammaRepo.cs:103–126` composes two moves, a version write and finally cleanup | The enclosing operation owns the complete known set, not just each helper in isolation; destructive cleanup must not run as failure recovery |
| `Stalker.Gamma/Services/GitService.cs:186–220` discovers and writes blobs in the same traversal; plan `1354–1393` proposes collecting writes | Before opening streams, also validate proposed-output duplicates, file/parent conflicts and mapped existing-entry types; inspecting existing trees alone is insufficient |
| `Stalker.Gamma/Services/ArchiveService.cs:14–38` opens a header before dispatch and chmods in dispatch `finally`; unsupported headers do not enter that block | An unsupported-header fixture does not test failed-extractor follow-up; use a narrow internal checked dispatch seam and fresh successful-output inspection |
| `Stalker.Gamma/Services/SevenZipService.cs:11–29`, `Stalker.Gamma/Services/UnzipService.cs:10–20` and `Stalker.Gamma/Services/TarService.cs:10–22` expose public extractor entry points | Guard or narrow those entry points too; a private checked `ArchiveService` dispatch does not by itself cover the other public APIs |
| `Stalker.Gamma/ModOrganizer/DownloadModOrganizer/DownloadModOrganizerService.cs:257–266` deletes two archive versions incrementally | Preflight both considered deletion targets before removing the first, and make download/deletion cache-boundary ownership explicit |
| `Stalker.Gamma/GammaInstallerServices/GammaInstaller/GammaInstallerArgs.cs:7–28` exposes mutable roots/record collections; plan `827–832,1436–1452` recreates a boundary and returns void from installer preflight | Capture one base/root context and retain the actual selected records/targets in an immutable preparation result; execution must use that result, not independently reconstructed args |
| Plan `924–948` normalizes the raw alias target before comparing it with the cache root | Equality alone leaves discarded raw components unexamined; inspect the target walk before accepting normalized equality |
| Plan `967–990` omits exactly `GAMMA/downloads`, but the specification `155–177` does not define every GAMMA/cache overlap | Decide the supported root relationships explicitly; skipping the alias does not specify exclusion of a real `GAMMA/cache` subtree |

The raw-target examples `../missing/../cache`,
`../ordinary-file/../cache` and `../intermediate-link/../cache` are test/design
counterexamples to the proposed inspection proof. They were not executed against
.NET and are not asserted as demonstrated application exploits.

### Inspector evidence gate

The proposed inspector must distinguish absence, required directory shape,
non-directory leaf shape, disallowed links and inspection/enumeration errors.
Catch enumeration creation/advancement errors as stops too, preserving cancellation
separately. Do not classify from the enumerator object's CLR type after another
API has inspected a potentially different entry.

The current documentation research does **not** establish reliable POSIX
regular-file classification for all non-directory entries. A typed result must
not call a candidate leaf `RegularFile` solely because its directory/reparse bits
are absent. Deciding or implementing special-entry rejection needs evidence of
the chosen supported primitive; no native `stat` layout/ABI is authorized or
invented here. The approved specification's symlink/path scope must not silently
be presented as full special-file or sandbox protection.

## Reconciled architectural direction — approved for revised first batch

### 1. Owned inputs, not public mutable path arrays

Use one small instruction value that owns its entries and distinguishes a blank
field from explicit no-op encodings. Raw TSV, raw JSON and already-parsed paths
are distinct ingress operations. Sentinels are interpreted only at raw ingress;
JSON remains an array and a literal folder `0` must serialize unambiguously.

One internal record-preparation function selects the existing role and validates
its contributing/composed names. Keep installability validation out of generic
DTO construction. Preserve unrelated diff identity rules. Before implementation,
write explicit tables for patch blankness, instruction equality/markers and
edge whitespace; do not advertise current behavior as preserved where a table
intentionally changes it.

### 2. Retained authority and typed inspected state

Retain the configured roots/base through record construction and installer
preparation. A generic full-path guard offers existing-path inspection only;
constructing a new boundary around an arbitrary supplied path does not prove
that the intended GAMMA/Anomaly/cache root authorized it.

The strict inspector returns immutable entry descriptions with explicit missing
and expected-shape semantics, not mutable `FileSystemInfo` objects as security
tokens. Checks and I/O remain separate, non-atomic operations. Normalization errors
must enter the same stop classification, including failures before construction.

### 3. Operation preparation, not a general transaction framework

Each affected operation owns a narrow internal preparation result: inspected
entries, the selected source-to-target mappings, target shapes/creation decisions,
overlap decisions and execution order. Validate the whole known write/delete set
before its first mutation. Check proposed conflicts as well as existing paths.

Copy/move counts and selection come from that materialization; ancestor flattening
must read original shallower sources before deeper mappings can overwrite them.
Instructions combine list-wide initial preflight with per-step current-state
preparation. Successful extractor output requires a fresh inspection. No prepared
object becomes permanent authorization, and no general operation DSL is proposed.

### 4. One cache owner and explicit crossing

`DownloadCache` owns exact alias checking/establishment and cache-scoped archive
and repository access. Do not split archive authority into independently supplied
root/path values or bypass alias validation through an exposed generic cache
boundary. Migrate update checks, MO2 download/delete, records/decorators, cached
reads and pruning explicitly.

Inspect raw alias-target walk prerequisites before comparing canonical paths;
allow checked relative targets without following intermediate links or treating
missing/incorrect entries as migration opportunities.

**Approved configuration decision:** the previous specification did not define
all overlapping GAMMA/cache roots. The approved minimal rule is disjoint GAMMA
and cache trees, with the already-approved exception where the real cache is
exactly `GAMMA/downloads`. Reject other equal/nested layouts before mutation,
without repair or migration. The user's subsequent **“adelante”** approves this
explicit compatibility restriction and continuation with the presented
architecture. It is not currently enforced in production.

The alternative is to design an explicit overlapping-tree partition for every
consumer and mutation, not just skip another directory during chmod. That is
additional design/integration scope, not an inferred permission.

### 5. Stop propagation and metadata lifecycle

Unsafe-input/inspection stops and cancellation are different classifications,
both recognized through wrappers and all aggregate members. A worker's stop must
reach sibling cancellation and must not become a broken-addon retry, minimal
deletion, metadata write, resume completion or success event. Check tokens before
no-op returns, after awaited/callback work and before final publication.

An unresolved filename is valid presentation/resume state, but invalid archive-I/O
identity. Require it at path-producing/mutating boundaries. Keep resource-disposal
`finally` blocks and acquisition ownership; remove failure-triggered destructive
follow-up. Cancellation of managed continuations is not proof that an external
process stopped writing.

### 6. Evidence that can actually close a claim

Use production C# components with owned synthetic trees, memory-only receipt
seams and a narrow internal checked extractor-dispatch seam. Test direct/wrapped
stops and cancellation followed by normal return. Observe directory inventories,
expected-absent targets, file bytes and file/directory modes; do not derive a
no-read claim from unchanged bytes. Test scratch teardown against a second,
independently owned tree and preserve the original assertion if cleanup fails.

Python source checks establish wiring only. New C# fixtures will remain **written,
not compiled/executed** until separately authorized remote verification. A
non-AOT runner result would not by itself establish NativeAOT CLI behavior.

### Proposed dependency-ordered parallel delivery

| Stage | Ownership / gate |
|---|---|
| Foundations in parallel | One owner: pure input/error contracts. Another: independent fixture infrastructure and teardown contract |
| Consumers in parallel, after foundations stabilize | Input/domain owner: record transport/roles/pure cases. Filesystem owner: typed inspection/boundary cases |
| Dependent managed operations | Begin only after inspector contracts stabilize; copy/move/instructions/cleanup fixtures precede implementation |
| Coordinator-only integration | Shared runner, friend assembly, Python wiring checks, shared factories/installer entry points and git staging/commits |
| First reviewed batch | Source-reviewed foundations and managed operations, not a runnable or fully integrated installation claim |
| Later reviewed batches | Cache ownership before archive/MO2 and Git/repository integration; stop/resume foundations before installer orchestration; no overlapping file ownership |

Do not dispatch whole Tasks 1–3 concurrently or let agents edit different hunks
of shared factories/installers. Preserve the current worktree and review gates.
Reconcile the existing task plan after the configuration/design decision; do not
execute the stale fixed interfaces merely because they are written down.
