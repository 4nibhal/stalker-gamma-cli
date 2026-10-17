# Batch 4A: managed filesystem boundaries

## Decisions and status

The user approved the functional design after selecting:

- **Strict links:** reject other symbolic links, even links remaining inside a
  managed tree; preserve only the verified GAMMA downloads/cache alias.
- **Staged work:** implement managed-input/path/tree checks first. Resistance to
  concurrent entry replacement and isolation of external extractors require a
  later, separate design. This stage is not a complete filesystem sandbox.
- **No automatic repair:** an unexpected link or mismatched downloads directory
  stops the operation; do not replace, delete or migrate it automatically.

This document specifies intended behavior, not a claim that the complete design
is implemented or runtime-verified. Source progress and verification limits are
recorded separately in `tests/FILESYSTEM-REGRESSIONS.md`. After the original
planning checkpoint, the user requested parallel
implementation with subagents, preceded by architecture analysis. The subsequent
**“adelante”** approves the presented architecture and the explicit GAMMA/cache
layout restriction below. Continue with the revised first implementation batch;
stop at its review gate. This does not authorize .NET or application execution.

## Scope and execution constraints

Keep C#, the CLI's NativeAOT configuration, the non-AOT companion server, Linux
x64/arm64, Wine payload names/INI paths, and the online/offline/update/resume
flows. Continue on `linux-hardening` in its existing worktree. Parallel agents
must have disjoint file ownership and stable dependency contracts; shared-file
integration and git commits belong to the coordinator.

The current implementation request permits scoped source/test/documentation
edits and local commits in the revised first batch. It does not authorize
downloads, new dependencies, a local .NET SDK/compiler/build/restore/test command,
remote workflow execution, installer or container execution, push, merge,
publication or upstream disclosure. Any later
remote build/download still needs the previously required scoped consent and
build-input verification. Private audit evidence stays ignored.

## Intended guarantees and explicit limits

| Subject | Batch 4A contract |
|---|---|
| Values used as filesystem components | Validate by role before related mutations |
| Instruction paths | Relative, nested and confined to their extraction source |
| Existing source/destination/intermediate symlinks | Reject before the managed operation under the checked tree state |
| Recursive copy, move, counting, cleanup and chmod walks | Do not descend into or operate through disallowed links |
| Downloads/cache alias | One explicit checked boundary, never a generic link exemption |
| Invalid input or disallowed links | Fail closed without automatic destructive recovery |
| Concurrent replacement after checking | Not guaranteed; separate later stage |
| Writes made by external extractors | Not isolated by this stage; separate later stage |
| Hard-link aliases, mount substitution and local hostile processes | Not covered by these path/link prechecks |
| Download integrity/atomicity and permission-mode policy | Separate stages; do not claim fixed here |

The checked tree state is a limitation, not an assumption that concurrent
changes cannot happen. Rechecking near an operation does not turn separate
checks and path-based I/O into an atomic security boundary. Similarly, detecting
an unsafe extracted tree before managed follow-up work cannot undo writes made
by the extractor itself. The application remains not ready for personal use
solely on completion of this stage.

## Architecture

Use three small shared responsibilities rather than unrelated checks at every
call site:

1. **Input policy:** pure validation/parsing of components and instruction paths.
   It does not create directories, resolve network metadata or repair inputs.
2. **Filesystem boundary policy:** validate an explicit root and the paths/trees
   an operation will read or mutate, applying strict link rules. Keep lexical
   containment distinct from inspection of existing filesystem components.
3. **Downloads/cache boundary:** establish or validate the one authorized alias
   and expose its checked target to explicitly scoped consumers. It does not
   enable arbitrary links in the general recursive utilities.

Keep policy in the shared application library so installer and CLI paths use
the same rules. Validate at public mutation boundaries as well as factories;
constructing records directly or receiving metadata later must not bypass the
policy. Use owned instruction values, immutable entry descriptions and narrow
operation-specific preparation results rather than mutable arrays or checked
`FileSystemInfo` objects as trust tokens. Preserve intended root ownership through
execution and validate proposed output conflicts, not just existing trees. This
design does not prescribe a native ABI or a general transaction framework.

## Input contracts

### Names used as a single component

- Required names must not be null, empty or whitespace-only. Reject `.` and
  `..`, `/` and `\` separators, control characters and rooted/drive/UNC forms.
- Preserve ordinary spaces, Unicode, apostrophes, extensions and ordinary dots
  inside valid names. Do not use an ASCII-only name allowlist.
- Apply the contract to supplied archive names, late metadata filenames,
  addon/patch fields used to form directory names, the composed directory
  names, separator output names, and MO2 profile names used in filesystem paths.
- Missing optional fields retain their current meaning for the selected record
  role. Metadata-only addon rows may omit an archive name until metadata arrives;
  separator rows are not invalid merely because addon-only fields are absent.
  Validate the separator label only when that row is used to produce a path.
- A CLI settings profile's display label is not automatically a path component.
  Do not restrict it merely because the model calls it `ProfileName`.
- Reject an unsafe value rather than stripping path prefixes or substituting a
  different name. Error messages identify the record/field without requiring
  full remote URLs or unrelated settings to be printed.

### Instruction paths

- Keep blank-field versus explicit-no-op meaning independently of executable path
  count; separator selection must not turn an explicit `0` or JSON `[]` into a
  blank field. Own a defensive copy of parsed paths and expose no mutable backing
  array. Empty wire markers are encoding, not executable paths. Instruction-value
  equality compares blankness and ordered canonical paths; unrelated diff identity
  and author/hash comparisons remain unchanged.
- A parsed literal singleton folder `0` must survive JSON write/read; encode it
  unambiguously, for example as `["0", ""]`. Raw singleton `["0"]` remains a no-op.
- Preserve the current no-op meanings of absent/blank instructions and `0`.
- Support relative nested components separated by `/` or ordinary `\`; retain
  the existing TSV list delimiter `:` and JSON array wire shape.
- Validate raw entries before lossy joining, prefix trimming or separator
  conversion. JSON array entries are checked individually; subsequent consumers
  use parsed entries rather than joining and reparsing them. For TSV, an ASCII
  letter followed by `:` at the start of the field or a delimiter-separated
  item is rejected as an ambiguous drive-like prefix, even when it could mean
  a list containing a one-letter folder. For example, TSV `C:folder` is rejected;
  the unambiguous JSON array `["C", "folder"]` remains a list of relative paths.
- Reject absolute/rooted paths, drive/UNC forms, dot/parent segments and control
  characters. A leading backslash is not silently converted from a rooted-looking
  input into a relative instruction. Colon-containing JSON path entries are not
  converted into additional instructions.
- Validate every instruction's syntax before processing the list. For each
  selected source, enforce the extraction-root boundary and check its tree and
  relevant destination before any associated managed move/copy. Preflight the
  existing extraction/destination tree for the instruction list before its
  first mutation, so a disallowed link encountered later in that tree does not
  first allow earlier instructions to move files. Preserve sequential semantics
  for sources produced by an earlier instruction, with checks before their use.
- Preserve a valid source's existing flattening behavior: a nested source can
  move into its extraction-root parent or `gamedata`. Do not introduce a copy
  whose destination is inside the selected source or recurse into new output.

These stricter parsing choices are intentional. Compatibility with the current
online manifest is not verified: no live manifest was retrieved for this design.
Do not silently loosen the contract to make an unverified fixture pass.

### Configured roots

Anomaly, GAMMA and cache are explicitly configured roots, not manifest-derived
child names. Preserve support for absolute roots and relative configured roots
resolved against a stable explicit base. Spaces, Unicode and apostrophes remain
valid. Normalize paths without treating normalization as physical link safety.

Check existing components on the way to a root as well as the root itself and
affected descendants. Under the strict policy, an unapproved root/ancestor
symlink is an error; direct users to an explicit real path rather than silently
accepting the alias. Missing components may be created only after the relevant
input and existing-parent checks. Do not confuse a sibling sharing a textual
prefix with a child of an authorized root; Linux comparisons are case-sensitive.

## Filesystem and alias rules

GAMMA and cache trees must be disjoint, except that the real cache may be exactly
`GAMMA/downloads`. Reject all other equal/nested GAMMA/cache layouts before any
creation or mutation. Do not repair or migrate their contents. This additional
configuration restriction was presented explicitly and approved by the user's
latest **“adelante”**; it is not an already-implemented check.

For each managed operation, perform input validation and a preflight of the
affected source/destination paths and selected traversal trees before its first
mutation. Check existing final file entries as well as directories and parents,
including dangling symlinks and cycles. Failure on a later sibling in that
preflight must not first chmod, copy or remove an earlier valid sibling.

Recursive counting/progress/logging reads require the same policy as copying:
do not perform an unchecked all-descendants enumeration or open a file before
its boundary checks. Preserve existing selection/filtering semantics, but do
not use them to authorize following an encountered link. Cancellation must not
start follow-up processing after an operation was rejected or cancelled.

The one exception is the exact `<GAMMA>/downloads` entry:

- Validate the explicitly configured cache root and the downloads entry's
  existing parents before creating/reusing the entry.
- Reuse an existing direct symlink only if its target, resolved relative to the
  link's parent when necessary, equals that configured cache path. The cache
  root itself must pass the strict checks; a link chain is not accepted merely
  because its eventual destination would match.
- Inspect the raw target's path-walk prerequisites before lexical equality;
  normalization must not hide a missing, file-valued or linked component removed
  by `..`. Alias-target syntax is distinct from untrusted instruction syntax.
- A dangling, cyclic, file-valued or wrong-target entry fails without removal or
  replacement. Existing mismatches are not migration opportunities.
- If the entry is absent, create missing authorized real directories first and
  then create/check the intended link. Never overwrite an entry appearing at
  that location instead. This still does not claim atomic race resistance.
- A real downloads directory is accepted only when its path is the configured
  cache itself. In that case no symbolic link is needed. Otherwise stop and
  explain the mismatch without moving existing content.
- When an alias is required, reject a cache path nested under that downloads
  entry before creating directories: the target must not depend on traversing
  the alias being established. Equality is only the real-directory case above.
- Scope the exception to consumers explicitly accessing this boundary, including
  cache reads/pruning and special-repository sources. After crossing to the
  checked cache root, apply strict rules to descendants. A generic recursive
  walk must not gain permission to follow arbitrary encountered links.

## Integration and error behavior

Cover these functional boundaries without rewriting unrelated services:

- Manifest/record creation and separator creation, including composed names.
- Filename receipt from metadata, before using it as a download destination.
- Download/extraction destinations and managed post-extraction instructions,
  metadata writes and cleanup. Guard paths before the corresponding existing
  I/O; checksum/atomic-download changes remain outside this stage.
- Shared recursive copy/move/count/cleanup/permission operations and their
  special-repository callers, including sources under the cache alias.
- Both installer bootstraps and profile setup/write paths, plus CLI MO2 profile
  activation, reading, editing and deletion. Check fixed profile files before
  opening them too; a validated directory name does not authorize a linked file.
- Existing cached profile/manifest reads and cache-prune mutations that traverse
  the registered downloads boundary.

Validate the currently known manifest/profile data before its related installer
mutations; late metadata is validated when received, before its associated I/O.
This is not a transaction or a promise to roll back previously completed work
when a later operation fails. Logging/in-memory status updates are not filesystem
confinement proofs, and prior unrelated successful work is not silently erased.

An unsafe input/tree stops the affected operation with a clear error. Do not
silently skip a disallowed link, continue after a validation failure, mark a
failed record complete, or invoke destructive cleanup as recovery. Check cleanup
and `finally` paths too. Existing permission-mode choices are not broadened or
declared secure by this stage; only their traversal boundary is addressed.

Recognize unsafe stops and cancellation separately through ordinary wrappers and
all aggregate members. Check cancellation after callbacks/awaited work and before
follow-up/status publication. Resource-disposal `finally` blocks remain valid;
destructive failure recovery does not. Unresolved archive metadata is valid for
display/completed resume, but must fail before archive-path I/O. Keep one retained
cache owner for alias-aware consumers, including update checks and MO2 deletion.

## Planned regression strategy

Prepare tests against production C# policy and operations using synthetic trees
in a new scratch directory. Avoid a separate Python reimplementation of the
security policy. Reuse the regression runner and existing dependencies where
possible; do not add a test package simply for assertions. Keep test-only access
to internal code narrow if needed, rather than widening the public API for tests.

| Fixture family | Required observation when executed remotely |
|---|---|
| Valid names | Spaces, apostrophes, Unicode and ordinary dotted names preserved |
| Invalid components/rows | Traversal, separators, rooted/drive/UNC/control inputs rejected before related changes; optional roles preserved |
| Instructions | Valid nested slash/backslash sources retain flattening behavior; invalid raw/list entries rejected |
| Source paths | File, directory and intermediate links rejected; external files are not read/moved by the tested operation |
| Destination paths | Linked leaf/parent/root rejected; external content and modes unchanged |
| Recursive traversal | Cycles and late unsafe siblings rejected before managed mutations; counting/logging does not enter links |
| Cache boundary | Correct direct alias works; wrong/dangling/chained aliases and mismatched real directories fail without repair; same-path real directory works |
| MO2 profiles | Invalid names and linked profile/file/ancestor targets rejected for the relevant read/write/delete operation |
| Side-effect ordering | External sentinel existence, bytes and Unix modes unchanged after rejection; selected valid siblings untouched on preflight failure |
| Compatibility | Both installer paths, cache consumers, nested instructions and existing Wine INI/payload behavior remain represented |

Use in-memory responses or local fake handlers only where receipt of metadata
must be exercised; no real HTTP request, download, container, installer or external
extractor belongs in these tests. Test cleanup must not follow the deliberately
created unsafe links or delete the wrong tree after a failing assertion.

Local checks may inspect source contracts, XML, whitespace and parser diagnostics
without compiling C#. Record new source-contract red/green cycles separately
from unexecuted C# behavioral fixtures. No claim that the security regression
passes is allowed before separately authorized remote compilation/execution.

## Handoff and review gates

1. Review this written specification and resolve requested changes.
2. After user approval, prepare a focused Batch 4A implementation plan and its
   test-first checkpoints. Do not execute it merely because this design exists.
3. Keep the later concurrency/extractor design, download integrity/atomicity,
   restrictive permissions, subprocess/server and supply-chain stages explicit.
4. Obtain separate authorization and verified build inputs before any remote
   execution; report runtime results only from actual evidence.

The architecture/configuration decision is approved. Execute only the revised
first batch in `docs/superpowers/plans/2026-10-18-filesystem-foundations.md`, then
stop for review; reconcile later tasks before executing them. No stage of this
handoff authorizes installing or running the application for personal use.
