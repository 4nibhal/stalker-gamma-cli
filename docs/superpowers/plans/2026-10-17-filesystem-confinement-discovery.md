# Filesystem confinement — design preparation

## Scope and authorization

The user approved preparation of the next filesystem/name/link design and its
test strategy after the Linux-only source conversion. This is a discovery
checklist, not an implementation plan or approval to edit production code.
Continue inline without subagents, downloads, a local .NET toolchain, workflow
execution, live installation, push, merge or publication. The existing worktree
and branch are retained.

## Brainstorming checklist

- [x] Explore current source, private audit, existing tests and recent commits.
- [x] Assess whether any design question needs a visual companion; offer only if useful.
- [x] Clarify the remaining scope/compatibility decision, one question at a time.
- [x] Present 2–3 approaches with trade-offs and a recommendation.
- [x] Present the proposed design and obtain the user's approval.
- [x] Write and locally commit the approved specification.
- [x] Self-review the specification for gaps, contradictions and scope.
- [x] Ask the user to review the written specification; await approval.
- [x] Load `writing-plans` only after written-spec approval.

## Planning authorization and checklist

After requesting a design summary, the user replied **“adelante”** to continue
with implementation planning. This clears the written-design/planning gate only;
it does not authorize implementation or execution. The next document is
`docs/superpowers/plans/2026-10-18-filesystem-confinement.md`.

- [x] Verify the current branch, clean starting worktree, specification and source interfaces.
- [x] Map shared policy, alias consumers, managed I/O, error/recovery paths and regression access.
- [x] Write concrete interfaces and independently reviewable test-first tasks.
- [x] Self-review specification coverage, placeholders and signature consistency.
- [x] Verify documentation-only changes and record available local checks.
- [ ] Obtain implementation-plan approval before beginning the first inline batch.

Planning verification: the 10-task plan has four inline review batches and no
completed implementation steps. `python3 -m unittest discover -s tests -v`
passes the 16 existing source/dry-run tests; document fence/placeholder/Python
snippet checks and `git diff --check` pass. The change set contains only this
tracker, the specification's approval status and the new plan. No production or
test source, downloaded artifact or CI execution is part of this checkpoint.

## Evidence boundaries

- Starting revision: `54e6c6c`; `git status --short --branch` reported a clean
  `linux-hardening` worktree before this checklist was added.
- Roadmap: `docs/superpowers/plans/2026-10-17-linux-only-fork.md:1063-1069`.
- Scope split: `docs/superpowers/specs/2026-10-17-linux-only-fork-design.md:16-29`.
- The graph in the original checkout was queried separately with the existing
  vocabulary tokens `instruction`, `cache` and `profile`. It is auxiliary
  navigation; current worktree source is authoritative. No graph rebuild ran.
- Existing private audit remains in the original checkout's ignored
  `docs/security/`; do not copy vulnerability details into tracked/public docs.
- No C# compilation or runtime security reproduction has been performed during
  this design preparation. A static finding is not a demonstrated exploit.

## Current design decision

The two presented link-policy alternatives were:

1. **Recommended: reject links in managed source/destination trees**, allowing
   only the explicitly checked downloads-to-configured-cache alias. Unexpected,
   broken or cyclic links cause an error; do not silently delete/replace them.
   This policy would reject existing installations that rely on other links.
2. **Alternative: allow contained internal links**, rejecting escapes and cycles.
   The design would additionally need target-resolution and alias-handling rules
   for each traversal/operation, including destructive operations.

This is a textual compatibility decision; no visual companion is useful here.
The user selected **strict rejection** through the link-policy question. That
choice approves the compatibility policy, not an implementation or a claim of
working protection. Do not silently remove or repair unexpected links.

The user also selected **staged work**: the first design covers validation and
strict link checks in managed filesystem operations (Batch 4A); concurrent
replacement and isolation of external extractors need a separate later design.
These latter guarantees remain explicitly pending. Path prechecks alone must
not be presented as a complete sandbox or a complete fix of filesystem risks.

## Approved functional Batch 4A design

The user selected **Aprobar diseño** after the design summary and its alias,
metadata, separator and compatibility details were presented. The subsequent
**“adelante”** authorized preparation of the implementation plan, not its execution.

The normative design and planned test matrix are in
`docs/superpowers/specs/2026-10-17-filesystem-confinement-design.md`.
Keep policy definitions there rather than maintaining a second specification in
this discovery checklist. Production code and tests remain unchanged until the
written-spec and implementation-plan gates are satisfied.

## Documentation lookup

Context7 was queried for current .NET filesystem documentation. It returned
archive/link guidance but no match for the specific link-resolution API query.
The canonical Microsoft .NET 10 `FileSystemInfo.ResolveLinkTarget` and
`Path.GetFullPath` references were then read into the conversation, without
downloading a tool/artifact or running their examples. Source locations and
the static data-flow inventory are recorded separately in ignored
`docs/security/FILESYSTEM-DISCOVERY-2026-10-17.md`.

## Design checkpoint and verification

- Specification committed locally as `2448186`:
  `docs/superpowers/specs/2026-10-17-filesystem-confinement-design.md`.
- Inline self-review checked placeholders, internal consistency, scope and
  ambiguity. It clarified the parsed JSON/TSV instruction distinction, list
  preflight versus sequential flattening, and rejection of a self-dependent
  downloads/cache alias. These are proposed contracts, not implemented fixes.
- The 16 existing Python source/dry-run contracts pass via
  `python3 -m unittest discover -s tests -v`. No new security regression source
  was written, compiled or executed. Document whitespace/structure checks pass.
- Before the specification commit, the staged list was verified to contain
  only that document. Production/test files were unchanged. Private evidence
  is ignored by `.gitignore:4` and was not staged.
- At this design checkpoint, written-spec user review and `writing-plans` were
  pending. The subsequent planning authorization is recorded above; implementation,
  compilation and CI remain unauthorized.
