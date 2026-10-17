# Linux-only personal fork: design and security roadmap

## User decisions

- Personal fork: `https://github.com/4nibhal/stalker-gamma-cli`.
- Host platform: Linux only, with the existing x64 and arm64 targets.
- Remove Windows installer support rather than maintaining cross-platform
  compatibility. macOS host support is also outside this Linux-only fork.
- Work on branch `linux-hardening` in `.worktrees/linux-hardening`.
- Retain C# and NativeAOT. Compile the native Linux executable in GitHub Actions;
  do not install or run a C# compiler, .NET SDK, or local .NET build toolchain on
  the user's machine. This restriction does not require rewriting the project.
- Continue the security review and hardening; platform removal is not a claim
  that the remaining program or downloaded content is safe.

## Scope decomposition

Do not combine all security changes into one untestable rewrite:

1. **This specification:** remove non-Linux host support and make the fork's build
   and documentation describe Linux accurately.
2. Filesystem boundaries: manifest-derived names, paths, instructions, links,
   cache-link exception, profile deletion, and restrictive permissions.
3. Download integrity: exclusive temporary files, checksum validation before
   publication/extraction, missing-reference policy, and interrupted transfers.
4. Remaining subprocess/network/supply-chain hardening, including the optional
   browser server, reviewed immutable tools, and reproducible build inputs.

Each subsequent security stage needs concrete tests and its own focused design.
No public disclosure or upstream PR is implied by this specification.

## Important distinction: host support versus game payloads

Remove PowerShell, elevation, Defender exclusions, Windows registry settings,
Chocolatey packaging, Windows/macOS build scripts, and host-platform branches.

Preserve Anomaly/GAMMA/MO2 `.exe` and `.dll` payloads and Wine-compatible paths.
The existing Linux instructions explicitly run `ModOrganizer.exe` with Wine
(`build/linux/README.md:36-38`). A Windows-looking filename in downloaded game
content is not Windows host support. Likewise, Camoufox's emulated browser OS is
not the server's host platform; do not change that fingerprint as part of this
conversion.

## Application changes

- CLI and companion-server entry points refuse unsupported host platforms before
  starting installation, network services, or a browser container.
- Remove `PowerShellCmd.cs`, its DI registration, constructor dependencies,
  elevated execution calls, and Windows-only CLI flags/documentation.
- Symbolic-link setup uses the Linux directory-link implementation directly. Its
  existing trust weaknesses remain explicitly scheduled for filesystem hardening;
  this conversion does not claim to fix them.
- Native curl resolves the Linux `.so` only; remove Windows/macOS library choices
  and macOS ARM64-specific variadic calling shims. Preserve the existing Linux ABI.
- Archive/tool readiness and executable names use Linux tools, without `.exe`
  alternatives or macOS ZIP extraction branches. Retain tar where Linux archive
  operations still require it.
- File-browser integration uses `xdg-open`. Preserve the actual Windows-path
  syntax required inside the Wine/MO2 configuration, selecting Linux behavior
  directly rather than removing that configuration.
- Preserve commands, installer/update/resume flows and existing Linux layout
  except for removed non-Linux options. Do not add unrelated refactors.

## Build, CI, and documentation

- Retain Linux x64/arm64 build and AppImage support.
- Preserve the existing deployment distinction: the CLI inherits NativeAOT from
  `Directory.Build.props`; the optional server explicitly disables AOT and bundles
  its runtime (`stalker-gamma-cli-server/stalker-gamma-cli-server.csproj:7-9`).
  Neither deployment requires a compiler on the user's machine. Do not force the
  server into AOT as part of this conversion.
- Remove `build/win/`, `build/mac/`, Homebrew cask generation, and Windows package
  publication. Remove inherited automatic publication to the upstream's AUR,
  Chocolatey, Homebrew and release destinations.
- CI is Linux-only, with explicit read-only repository permissions for build
  jobs, no publisher secrets, and no release/package publication jobs. Changes to
  workflow files do not authorize triggering downloads or publishing this branch.
- CI is manually dispatched so committing or pushing a branch does not silently
  start SDK/package/tool downloads. Native compilation belongs on the remote
  Linux runners, not on the user's machine.
- Correct Linux publish RID selection and keep tool-download integrity gaps
  documented until the supply-chain stage; do not invent hashes.
- README identifies this personal Linux-only fork, retains attribution/license,
  provides Linux prerequisites, and distinguishes source review from unverified
  external game/mod/tool artifacts.

## Failure handling and tests

- Add standard-library Python source-invariant tests before implementation. They
  must fail on the baseline and cover removed files/options/DI references,
  Linux-only native/tool selection, Wine-path preservation, and Linux-only
  non-publishing CI.
- Inspect parsing of changed C# with the installed parser and run available
  shell/static checks. These are not substitutes for compiling C#.
- Compare parser diagnostics against the unchanged source baseline rather than
  declaring the entire tree syntax-clean. The installed Tree-sitter grammar
  flags existing conditional method declarations in four generated clients and
  inferred `ref` lambdas in `Commands/Anomaly.cs` and `Utilities/HashUtility.cs`.
  Do not rewrite those unrelated constructs merely to satisfy this parser.
- The user explicitly selected compilation in GitHub Actions, with no local C#
  compiler or SDK. Do not install a toolchain or try local .NET build/restore/test
  commands. Local checks use existing parsers and standard-library Python tests.
- A successful remote .NET build and runtime regression tests must be recorded
  from an actual CI run before reporting them as passing. Until then, mark them
  as not run. No SDK, NuGet package, native library, browser image or other
  artifact may be downloaded, locally or by a triggered CI run, without separate
  explicit scoped consent.
- Do not execute the installer, game, build-download targets or companion server
  as an improvised test. No actual personal installation is part of this stage.

## Acceptance and limits

The source conversion is reviewable when the Linux-only source tests and
available static checks pass, removed host-support references are accounted for,
the Wine payload behavior remains represented, and the diff contains no unrelated
changes. A successful .NET build and runtime regression tests remain required
before declaring the converted application ready to use.

Filesystem escapes, missing post-download hash checks, server authentication and
egress, native callback limits, and external artifact authenticity are **not
resolved merely by removing Windows support**. The local audit and user-supplied
security requirements remain applicable to the later stages.

## Progress checklist

- [x] Audit baseline and identify affected host-support boundaries.
- [x] Create and verify the personal GitHub fork.
- [x] Create an ignored isolated worktree and configure origin/upstream.
- [x] Record Linux-only design and explicit runtime-verification limits.
- [x] User review of the initial written specification.
- [x] User review of the remote-compilation/no-local-compiler clarification.
- [x] Write implementation plan.
- [x] Add and run failing source-invariant tests for Tasks 1-4; Tasks 5-6 contracts remain to be added.
- [x] Complete source conversion for the host boundary, installer wiring, runtime adapters and native curl (Tasks 1-4); local static checks only, remote .NET/runtime verification not run.
- [ ] Remove non-Linux host support and fork-unsafe publishing.
- [ ] Run available checks and report .NET verification blockers explicitly.
