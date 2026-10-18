# Retained roots, exact downloads alias and cache consumers

## Status and scope

The user selected **two ordered batches**: this roots/cache batch first, then a
separately reconciled stop/resume batch. The scope/order is approved; this written
specification awaits user review before implementation planning. No production or
fixture implementation is authorized merely by this document's existence.

This is the next source-only slice after the foundations handoff at `145f310`.
Its proposed scope is retained configured-root identity, both bootstrap callers, the exact
downloads boundary, real prune check/apply and the existing local JSON read. It
does not close archive/MO2/Git integration or whole-installer stop propagation.

The normative rules remain
`2026-10-17-filesystem-confinement-design.md:145–240` and
`../plans/2026-10-18-filesystem-architecture-review.md:246–336`. This document
reconciles this slice; it does not revive the superseded historical task APIs.

## Verified starting points

| Current source | Source observation, not runtime evidence |
|---|---|
| `Stalker.Gamma/Utilities/CreateSymbolicLinkUtility.cs:5–10` | Uses Directory.Exists and conditionally creates a link; does not compare the configured target or validate layout |
| `Stalker.Gamma/GammaInstallerServices/GammaInstaller/GammaInstaller.cs:166–181`; `OfflineGammaInstaller.cs:45–60` in the same directory | Normalize mutable root strings and create directories before calling that utility |
| `GammaInstallerArgs.cs:7–9,31–32`; `GammaInstallerArgsBuilder.cs:106–125` in that directory | Carry mutable root strings without an explicit resolution base or retained root context |
| `stalker-gamma-cli/Commands/Cache.cs:148–178,259–275` | Builds the keep set from factory DownloadPath values, enumerates GAMMA/downloads, and deletes FileInfo entries directly |
| `Stalker.Gamma/Filesystem/RecordPathPolicy.cs:23–89` | Owns actual role selection and GitHub archive composition; permits unresolved metadata-only archives |
| `Stalker.Gamma/GammaInstallerServices/GetStalkerModsFromLocal.cs:13–31` | Reads GAMMA/profiles/profile/modpack_maker_list.json from strings without retained-root checks or a token |
| `stalker-gamma-cli/Commands/Update.cs:140–160` | Its separate repository check uses GAMMA/downloads; protecting the local JSON reader does not migrate that Git branch |

The existing strict FileSystemBoundary and immutable checked entries are reusable
inspection evidence, not atomic capabilities. Generic operations stay strict.

## 1. Retained identities and ownership

Introduce one immutable **InstallationRoots** context for each prepared install
or update operation. It retains an explicit absolute base, canonical Anomaly and
GAMMA identities, and exactly one **DownloadCache** owner for its GAMMA/cache pair.
Construction performs normalization and layout validation without filesystem
mutation. Physical inspection belongs to each affected operation.

CLI full-install and both update entry points capture their base once and
construct the context before their first awaited preparation. Pass its canonical
GAMMA view into the still-unmigrated resume utility too; that stabilizes spelling,
not checkpoint safety. Args retain this same context before any record
construction. Their Anomaly/Gamma/Cache string views derive from it and
cannot be changed independently; remove both installers' late reassignment.
If a legacy convenience factory remains, it captures its base once at creation
and immediately uses the same context contract, never re-resolving at install.

Legacy record constructors may receive those canonical string views in this
slice. That stabilizes their identities; it does not authorize or migrate their
archive/native I/O. Do not add unused archive or repository path APIs.

Prune creates one DownloadCache from its actual configured GAMMA, cache and
captured base. It does not require an Anomaly root or infer cache authority from
GAMMA/downloads. Contexts are operation-scoped, not a mutable settings singleton.
The cache boundary remains private: expose the narrow operations actually used,
not a generic boundary or independently supplied root/path authorization pair.
The retained strict GAMMA boundary supports the local JSON reader and future
checkpoint integration; it grants no downloads-link exemption.

## 2. Layout and exact downloads state

GAMMA/cache must be disjoint, except that the real cache may be exactly
GAMMA/downloads. Reject equal roots, every other nesting direction, and cache
below downloads before any bootstrap mutation. Use case-sensitive component
relationships, not textual-prefix matching. No content migration or repair.

Do not add a blanket Anomaly/GAMMA/cache separation rule. The bootstrap still
rejects conflicts in its actual proposed creation set: for example, a disjoint-
cache alias location cannot simultaneously be required as Anomaly's real parent.
Compatible same-directory requirements are not an invented overlap prohibition.

| Existing downloads state | Required behavior |
|---|---|
| Real directory exactly equal to configured cache | Reuse; do not create a link |
| Direct link to the existing checked configured cache, with valid raw prerequisites | Reuse unchanged, including valid relative targets |
| Absent entry | Only bootstrap may create authorized real directories and then the intended direct link, or the exact-cache directory |
| Wrong target, dangling/cyclic/chained link, file-valued target or entry, unavailable inspection, mismatched real directory | Stop without removing, replacing, repairing or moving content |

Observe the downloads entry and existing target before creating a missing cache.
An existing dangling link must not become acceptable because bootstrap first
creates its target. Prune/read preparation does not create directories or aliases:
its cache and the exact downloads crossing must already exist and pass checks.

### Raw direct target

Only the cache owner may inspect/accept this exact alias. Check its existing
parents strictly. Obtain fresh managed entry metadata and the direct target;
unavailable or ambiguous target evidence is a stop, not proven absence. Do not
reuse a mutable FileSystemInfo as lasting checked evidence.

Walk the original target from the link parent for relative targets or the
filesystem root for absolute targets. Inspect each required directory before a
later `..` can erase it, rejecting missing, non-directory or linked prerequisites.
Only then canonicalize and compare with the configured real cache. Do not chase
a second link, use a final-target resolver as the proof, trim native names, or
feed native target text through instruction slash/backslash normalization.

For `../walk/../cache`, equality with cache is insufficient: walk must exist as
a real unlinked directory. Include missing, file-valued and linked walk variants
in fixtures. Checking these prerequisites authorizes no external content read,
recursive enumeration or mutation.

Official implementation evidence at dotnet/runtime `v10.0.0` traces LinkTarget
through FileSystem.GetLinkTarget and Interop.Sys.ReadLink to native readlink:

- `src/libraries/System.Private.CoreLib/src/System/IO/FileSystemInfo.cs:112–124`
- `src/libraries/System.Private.CoreLib/src/System/IO/FileSystem.Unix.cs:662`
- `src/libraries/Common/src/Interop/Unix/System.Native/Interop.ReadLink.cs:23–58`
- `src/native/libs/System.Native/pal_io.c:1183–1195`

These paths are under `https://github.com/dotnet/runtime/tree/v10.0.0/`.
The managed wrapper returns decoded target text without lexical normalization,
but also returns null on error. This is source evidence, not a runtime guarantee
for the future build artifact. Arbitrary non-text filename byte identity and
atomic check/use behavior are not established by that observation.

## 3. One narrow bootstrap operation

Both installers use the same preparation/execution boundary for the known set:
Anomaly, GAMMA, configured cache, GAMMA/mods and the exact downloads entry. Before
the first creation, inspect all existing relevant roots/parents/leaves, validate
layout, observe downloads and reject existing/proposed shape conflicts.

The immutable prepared result records only this operation's checked state and
creation decisions. Execution rechecks relevant paths and cancellation before
each mutation, creates only authorized real directories, and finally establishes
or reuses/checks downloads. Never overwrite an entry appearing at that location
or add a delete-and-retry fallback. In the exact-cache case create/reuse the real
directory instead. There is no recursive GAMMA inventory, transaction engine,
rollback promise or alias exemption in generic traversal.

The legacy utility must have no production callers after migration. It can be
removed after verifying the scoped call-site inventory, not retained as a hidden
alternate bootstrap path. Other installer setup/tail operations remain outside
this operation's guarantees.

## 4. Real prune preparation and apply

Move prune selection/deletion policy into the core cache owner; wire both CLI
commands to it. Retain the existing manifest/API fetch in production, but prepare
the cache context before that await and check cancellation afterward. Tests feed
manifest records in memory and do not invoke HTTP or construct downloadable,
native, extractor or installer services to calculate a keep set.

### Desired set

Use actual RecordPathPolicy preparation, not a duplicate role selector or naive
ZipName projection. Standard ModDB and GitHub roles contribute their prepared
archive names; separators/unsupported roles contribute none, with supplied input
validation preserved. Validate the entire known list before any prune mutation.

A metadata-only desired archive without a resolved identity is valid domain
state but makes this prune keep set incomplete. Both commands report a clear
CachePruneUnavailableException; no actionable deletion plan or misleading
"nothing to prune" success is produced. Do not fetch metadata just to enable
pruning, silently drop that row, or invent a directory-shaped archive identity.
Live-manifest incidence/compatibility is unverified; no manifest was fetched.

### Existing set and immutable display

Revalidate the exact crossing and access the configured real cache, not the
alias-spelled path. Inspect every encountered direct child before selection;
linked children fail even when their names would be ignored. Do not recurse into
real child directories or select them for pruning. Preserve native names and the
existing top-level non-directory and ignore-fragment selection. The existing
fragments are Anomaly-1.5.3-Full.2.7z and ModOrganizer.v2.5.2.7z; preserve their
current substring matching, not a new extension or case-folding policy.

Only after complete entry preflight, collect checked candidate metadata and the
complete stale-file selection. Compare archive leaf identities ordinally, not
independently spelled relative/absolute alias paths. Return an immutable
owner-bound CachePrunePlan with read-only name/path/length/time descriptors for
display. Descriptor paths are not caller-supplied deletion authority. Metadata
reads are checked near access; no mutable FileInfo is a safety token. The existing
NonDirectory classification is not a claim of POSIX regular-file detection.

### Apply and cancellation

Check displays descriptors and performs no mutation. Apply prepares a fresh plan
for its own invocation, displays only its immutable metadata, then calls the same
owner's narrow apply operation; CLI code cannot call FileInfo.Delete directly.

Apply rejects a foreign-owner plan, revalidates the crossing, re-enumerates and
inspects all current direct children, and checks the complete selected set before
its first deletion. Check each target again near its delete. Newly encountered
children are not silently added to the selection. A missing, linked or shape-
incompatible selected target is a stop, not permission to pick another path.
Controlled link/type replacement fixtures are required. Do not claim detection
of every same-kind replacement, persisted-plan safety or concurrent-race resistance.

Check cancellation on entry, after awaits/callbacks, before metadata/display
follow-up, before deletes and before terminal success. If rendering requests
cancellation, no subsequent deletion starts. Unsafe/cancellation remains
classifiable through the prune command boundary; it cannot become a successful
return or trigger repair/retry. Existing ordinary failure diagnostics may remain.
Disposal/logging scopes are not destructive recovery or confinement evidence.

## 5. Existing GAMMA/profile JSON reader

Change the actual IGetStalkerModsFromLocal reader/caller contract to receive the
retained root context and operation token. Validate the profile component, then
strictly inspect GAMMA/profiles/profile/modpack_maker_list.json, including its
existing parents and leaf, before text read. Preserve the existing useful missing-
file diagnostic for genuine absence; linked/inaccessible paths must not be
misreported as ordinary missing files. No downloads crossing belongs here.

Use a narrow internal memory-only text-read seam after the same production
checks if needed by fixtures. Production uses the actual tokenized file read.
Check after the await and before returning/deserializing follow-up; preserve the
existing DTO/deserialization and role/comparison semantics. Do not require a
downloadable archive identity merely to read comparison records. Wire the actual
diff caller; securing this reader does not secure update's separate Git branch.

## 6. Ownership and acceptance evidence

Freeze retained-root/cache contracts before dependent edits. Cache-core/raw
inspection and fixture infrastructure work can use disjoint agents; prune and
local-reader work can separate after their dependencies stabilize. A single
integration owner controls args/builder, both installers, CLI commands, DI/runner
wiring, shared evidence and git staging/commits. Never divide shared-file hunks
among parallel writers or replay completed foundations tasks.

| Production-C# fixture family | Required observation when eventually executed |
|---|---|
| Stable roots | Absolute/relative roots, one captured base despite controlled later CWD change, spaces/apostrophes/Unicode; preparation creates nothing |
| Layout and proposed shapes | Disjoint/exact real cache accepted; all other GAMMA/cache nesting and conflicting bootstrap proposals rejected before any creation; prefix siblings/case-distinct names preserved |
| Downloads states | Correct absolute/relative direct aliases reused unchanged; exact-cache real directory works; wrong/file/dangling/cyclic/chained/mismatched entries reject without repair |
| Raw prerequisites | Canonically equal dot-segment target requires real walk components; missing/file/link variants and linked cache ancestors reject unchanged |
| Strictness and prune | Generic tree inspection still rejects downloads alias; checked crossing succeeds; ignored linked children cannot be hidden; real directories are not recursively pruned |
| Desired identities | Actual standard/GitHub/unsupported/separator roles; incomplete metadata-only set yields no plan or mutation, not a fabricated archive path |
| Apply ordering | Full selection preflight precedes the first delete; foreign plans and controlled missing/link/type replacements reject; cancellation from display causes no deletion |
| Local JSON | Valid checked read dispatch; invalid profile and linked parent/leaf reject before dispatch; post-read normal-return cancellation stops follow-up |
| Actual integration | Both bootstrap callers, both prune commands and real diff-reader call use the retained operations; old utility/raw deletion paths are absent from those boundaries |

Use the existing ScratchTree, immutable captures and Check helpers. External
sentinels belong to a second independently owned tree. Observe inventories,
expected-absent paths, bytes and file/directory modes; unchanged state does not
prove absence of reads. Preserve body failures if cleanup also fails.

Write real production-C# fixtures before production edits. Observe available
Python structural red/green separately; do not reimplement policy in Python or
call that behavioral TDD. Installed parser/XML/whitespace checks may run locally.
C# fixtures remain written, uncompiled and unexecuted until separate permission.
The current runner references core, not CLI: CLI wiring is source evidence, not
an executed command-level test. Neither parser checks nor a future non-AOT runner
establishes CLI NativeAOT or both Linux architectures by itself.

## 7. Explicit residual gates and handoff

This slice does not migrate addon archive/existence/deletion, MO2 cache access,
Git/update repository reads, special-repository bare/temp/source operations or
standalone Anomaly cache usage. Existing alias-spelled special-repository sources
can still be rejected by strict generic helpers; never mask that with an exemption.
Canonical args and successful bootstrap do not make those paths safe.

The subsequent stop/resume batch needs a separate reconciled spec/plan and user
gate: lazy identity/key forwarding, completed offline resume before path I/O,
checked checkpoint persistence, retry/sibling cancellation, success/cleanup
continuations and caller publication. Do not introduce those shared model or
orchestration changes opportunistically in this first batch.

No whole-installer fail-closed/no-follow-up claim, complete hardening, malware-free
guarantee or personal-installation approval follows. Native-process/extractor
isolation, concurrent replacement, hardlinks/mounts/special POSIX classification,
integrity/atomic downloads, restrictive modes and authenticated/pinned build
inputs remain separate.

Keep C#, CLI NativeAOT, the server's distinct publishing settings, Linux x64/arm64
and Wine payload/INI names. Preserve the named branch/worktree and ignored private
evidence. No downloads/packages, local .NET/compiler/build/restore/runtime tests,
native initialization, real HTTP, extractors/installers/containers, CI dispatch,
push, merge, publication or upstream disclosure is authorized by this spec.

After user review, write a focused implementation plan with test-first gates and
disjoint ownership. After implementation and scoped source review, stop for the
user. Neither writing this spec nor completing its first source slice starts the
second batch or authorizes remote execution automatically.
