# Existing Linux Changes Remote Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Obtain bounded compilation, six-suite execution and publication evidence for the already implemented Linux changes, without implementing the next hardening batch.

**Architecture:** Replace the draft packaging workflow with a manual, read-only, native x64/arm64 verification matrix. Use installed Git to retrieve the exact approved source revision and an exact preinstalled .NET SDK; keep restores, builds, fixtures and publication outputs on ephemeral runners. The workflow revision and tested application revision are distinct and must both appear in evidence.

**Tech Stack:** GitHub Actions, installed Git/Python/Make/Clang/.NET SDK 10.0.401, the five existing C# projects and NuGet.org.

## Global Constraints

- Tested application revision: `71e9c8839064cb56e4761f04c1cfb9e8ba8ef642`; preserve its production sources and C# fixtures unchanged.
- Retain the existing isolated `linux-hardening` worktree and ignored private evidence. Only the coordinator stages, commits, pushes and dispatches.
- Manual `workflow_dispatch` only; `permissions: contents: read`; no external actions, secrets, releases, uploads, packaging or workflow-side Git push.
- Native `ubuntu-22.04` / `linux-x64` / `x86_64` / `amd64` and `ubuntu-22.04-arm` / `linux-arm64` / `aarch64` / `arm64`; `fail-fast: false` and a bounded job timeout.
- Remote payload writes stay under `/tmp/stalker-gamma-ci/`: `source`, `nuget`, `dotnet-home`, `scratch`, `out`. Runner infrastructure writes are outside application control.
- Use only preinstalled SDK `10.0.401` with generated `global.json` setting `rollForward: disable`, `allowPrerelease: false`. Missing tools/development libraries/SDK stop verification; never install a fallback.
- Consent explicitly permits the public `linux-hardening` push, public CI logs, mutable hosted images and first NuGet.org restore without a verified complete graph/size/hash inventory. It does not establish supply-chain verification.
- No local downloads, .NET/compiler/build/restore/runtime commands, or local credential/config changes. Local Python source/dry-run checks and shell syntax checks are permitted.
- No real installer, CLI/server, browser/container, Git application integration, extractor, game or Wine execution. Only the existing synthetic C# regression runner may execute remotely.
- No new assertions disabled, warning suppressions or forced `PublishAot` values. Preserve I1/B1, identity, sequential-instruction, Wine and filesystem expectations.
- Cache/root-consumer and stop/resume implementation remain paused. Failures are reported, not repaired by modifying production or fixtures in this plan.

## Approved scope and file responsibilities

The four-point disclosure named the source snapshot (193 blobs, 1,346,094 raw blob bytes, not transfer size), NuGet packages/transitives/implicit packs, their purpose, exact remote root and public/ephemeral scope. The user selected **“Autorizo alcance y excepción”**, explicitly accepting all four points and the limited input exception. No further permission prompt is needed for these same operations.

- `.github/workflows/build.yml`: sole CI wiring; no checked-out helper script is needed because the tested source predates this wiring.
- `tests/test_linux_only.py`: update only CI structural contracts for intentional removal of draft packaging jobs; retain unrelated checks.
- `README.md`: distinguish this bounded verification from the still-unfinished strict build-input/install-readiness gates.
- This plan: tracked scope and command contract, without private audit material.
- `.superpowers/sdd/2026-10-18-remote-verification/`: ignored consent ledger, task/review reports and summarized remote results; retain it after completion.

### Task 1: Prepare the minimum manual verification workflow

**Files:** Modify `.github/workflows/build.yml`, the CI tests in `tests/test_linux_only.py`, and `README.md:19-27` only. Write an ignored implementation report; no production/project/C# fixture edits.

**Interfaces:** Consumes the exact tested revision and constraints above. Produces one `verify` matrix job with separately named acquisition, prerequisites, source-check, restore/build, six-suite and CLI/server-publication steps. No workflow input can select arbitrary code or repositories.

- [ ] **Step 1 — CI-contract RED.** Update the old CI-only structural expectation from `lint/version/build` to `verify`, remove obsolete artifact/hash/checkout-action requirements, and add assertions for fixed source identity, both native architecture mappings, exact preinstalled SDK policy, restricted NuGet source/cache paths, six-suite invocation, separate restore/build/run/publish boundaries and forbidden installation/publishing tools. Keep manual-only, read-only, Linux-only and all unrelated tests. A workflow regression such as adding a push trigger, setup installer, artifact upload or omitting a suite/build boundary must fail these checks.

Run before workflow changes:

```sh
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests.test_linux_only -v
```

Record the expected assertion failure against the old draft, not a Python/import error.

- [ ] **Step 2 — minimal GREEN wiring.** Put inline Bash/Python commands in the workflow, using `shell: bash` and `set -euo pipefail`. Bootstrap from `/tmp` before the source exists; later steps use `/tmp/stalker-gamma-ci/source`. Assert `GITHUB_REPOSITORY=4nibhal/stalker-gamma-cli`, Linux, the matrix host architecture and an initially absent source directory. Acquire only the public fixed revision:

```sh
git init /tmp/stalker-gamma-ci/source
git -C /tmp/stalker-gamma-ci/source remote add origin https://github.com/4nibhal/stalker-gamma-cli.git
git -C /tmp/stalker-gamma-ci/source -c core.hooksPath=/dev/null fetch --depth=1 origin "$SOURCE_SHA"
git -C /tmp/stalker-gamma-ci/source -c core.hooksPath=/dev/null checkout --detach FETCH_HEAD
test "$(git -C /tmp/stalker-gamma-ci/source rev-parse HEAD)" = "$SOURCE_SHA"
test -z "$(git -C /tmp/stalker-gamma-ci/source status --porcelain)"
```

Set `SOURCE_SHA` to the full tested revision above; log `GITHUB_SHA` as the workflow revision and actual checked-out HEAD separately. Disable Git prompting and system/global Git config for this retrieval; do not persist tokens.

Create the disclosed directories, a generated source `global.json`, and `/tmp/stalker-gamma-ci/nuget/NuGet.Config` with package sources cleared and only `https://api.nuget.org/v3/index.json` enabled. Clear fallback folders and audit sources if configuring them. Set `NUGET_PACKAGES`, `NUGET_HTTP_CACHE_PATH`, `NUGET_PLUGINS_CACHE_PATH`, `DOTNET_CLI_HOME` and `TMPDIR` to children of the disclosed root; disable telemetry, first-run experience and workload-update notification. No SDK download, tool restore or package-manager fallback.

Prerequisites must assert the selected SDK, .NET 10 runtime and installed native development packages, not only print diagnostics:

```sh
test "$(dotnet --version)" = 10.0.401
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes | grep -E '^Microsoft.NETCore.App 10\.'
test "$(dpkg --print-architecture)" = "$EXPECTED_DPKG_ARCH"
for package in zlib1g-dev libc6-dev libssl-dev libicu-dev; do
  test "$(dpkg-query -W -f='${Status}' "$package")" = 'install ok installed'
  dpkg-query -W -f='${Package} ${Version} ${Architecture}\n' "$package"
done
test -r /usr/include/zlib.h
test -r "$(clang -print-file-name=libz.so)"
clang --version
ld --version
```

Check installed Git, Python, Make, file and readelf. Record image/runner version from GitHub's setup log; never infer actual image bytes from labels.

- [ ] **Step 3 — test and publication commands.** Run the existing source checks against the fixed snapshot (whose workflow remains the old draft), then shell syntax checks. Local new structural tests validate the newly prepared workflow separately:

```sh
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -v
git diff --check
bash -n build/update-pkgbuild.sh
sh -n build/linux/stalker-gamma.AppDir/AppRun
sh -n build/linux/stalker-gamma-server.AppDir/AppRun
dotnet restore tests/LinuxRegression/LinuxRegression.csproj --configfile /tmp/stalker-gamma-ci/nuget/NuGet.Config --use-lock-file -p:Configuration=Release
dotnet build tests/LinuxRegression/LinuxRegression.csproj -c Release --no-restore --no-incremental -bl:/tmp/stalker-gamma-ci/out/runner.binlog
dotnet run --project tests/LinuxRegression/LinuxRegression.csproj -c Release --no-build --no-restore --no-launch-profile
dotnet restore stalker-gamma-cli/stalker-gamma-cli.csproj --configfile /tmp/stalker-gamma-ci/nuget/NuGet.Config --use-lock-file -r "$RID" -p:Configuration=Release -p:SelfContained=true
dotnet publish stalker-gamma-cli/stalker-gamma-cli.csproj -c Release -r "$RID" --self-contained true --no-restore -o /tmp/stalker-gamma-ci/out/cli -bl:/tmp/stalker-gamma-ci/out/cli.binlog
dotnet restore stalker-gamma-cli-server/stalker-gamma-cli-server.csproj --configfile /tmp/stalker-gamma-ci/nuget/NuGet.Config --use-lock-file -r "$RID" -p:Configuration=Release
dotnet publish stalker-gamma-cli-server/stalker-gamma-cli-server.csproj -c Release -r "$RID" --no-restore -o /tmp/stalker-gamma-ci/out/server -bl:/tmp/stalker-gamma-ci/out/server.binlog
```

Capture runner stdout using `tee` with pipefail and assert exactly these six success lines in registration order: `Linux regression suite '<name>' passed.` for `inputs`, `scratch`, `wine-ini`, `records`, `filesystem`, `managed-tree`. Preserve every conditional unexercised-denial message. Do not call `dotnet test` as a substitute.

Before each publish use `dotnet msbuild -getProperty:...` with the same RID/configuration context, parse its JSON output and assert CLI `TargetFramework=net10.0`, expected RID, inherited `PublishAot=true`, explicit self-contained publication; server `TargetFramework=net10.0`, expected RID, `PublishAot=false`, `SelfContained=true`, `PublishSingleFile=true`. Never force `PublishAot`. Require nonempty expected executable outputs, inspect `file` and `readelf -h`, and assert the native machine without launching either output. Print final generated lockfile identities/resolved versions/content hashes from the NuGet locks into logs, explicitly labeling them observed, not verified trusted inputs. Keep binaries/binlogs only on the ephemeral runner; no uploads.

- [ ] **Step 4 — local verification and report.** Run the full Python discovery command and `git diff --check`; extract each inline run block for `bash -n` without executing it or accessing the network. Use an already-installed YAML parser if available; never install one. Update README with bounded scope, exact source/SDK and unchanged input-exception/install-readiness limits. Report RED/GREEN commands/results and all unverified remote gates. Coordinator commits only the allowlisted workflow/test/README/plan paths, generates the task diff and obtains independent spec-compliance AND code-quality approval before publication.

### Task 2: Execute once and record the actual boundary reached

**Files:** Coordinator updates the ignored ledger and `results.md`; no workflow/production/C# fixture repair is part of this task.

**Interfaces:** Consumes independently approved Task 1 workflow commit. Produces the exact public run URL, workflow/source SHAs, both native job outcomes, source/build/suite/publication outcomes and explicit untested claims. Remote auth/dispatch belong only to the coordinator.

- [ ] **Step 1 — pre-push evidence.** Confirm clean tracked state, `linux-hardening`, exact origin and unchanged production/C# fixtures from the tested revision. Inventory staged paths and the publish range for private evidence. Do not stage `.superpowers`, `docs/security` or credentials; use explicit paths, never `git add .`. Observe remote refs again and do not overwrite an unexpected branch or change `master`.
- [ ] **Step 2 — authorized publication/dispatch.** Push only `HEAD:refs/heads/linux-hardening` to origin, without force, after final CI-scoped review. Dispatch the existing `build.yml` path on that branch only:

```sh
git push origin HEAD:refs/heads/linux-hardening
gh api --method POST -H 'X-GitHub-Api-Version: 2026-03-10' repos/4nibhal/stalker-gamma-cli/actions/workflows/build.yml/dispatches -f ref=linux-hardening
```

The versioned REST endpoint accepts the existing workflow filename and returns
the run ID/URLs directly with HTTP200 (official Actions-workflows reference and
REST breaking changes for2026-03-10, queried via Context7). This is the same
single manual dispatch as the CLI command, not an enable/settings workaround.

If GitHub does not register/permit that workflow, stop and report the API response; do not enable settings, alter default branch, merge, add another trigger or run master's original publishing workflow.

- [ ] **Step 3 — wait and inspect.** Observe the exact run by its workflow SHA/ref; wait via an installed CLI/API without downloading artifacts or using log-cache commands. Read metadata and job-log text into tool output only; never `gh run download`, `gh run view --log` (which may cache logs), ZIP download or SDK/compiler locally. Record actual image/runner/SDK versions, fixed source SHA, step outcomes, complete compiler/test error messages and conditional-denial messages. A queued/missing arm64 leg, skipped suite or failed prerequisite is not a complete pass.
- [ ] **Step 4 — bounded outcome.** Have independent read-only agents interpret separate architecture/job results when independent evidence exists. Update the ledger with facts and sources and provide the run URL. If compilation/runtime/publication fails, report the exact blocker without silently fixing sources/assertions or dispatching repeated runs. Do not continue cache/stop work. Even full success proves only the source checks, five-project compilation, six existing synthetic suites and configured publication contracts, not installation readiness, real integrations or complete security.

## Self-review / execution decision

The consent and acceptance map are covered by the two tasks; no new feature, dependency declaration or local build is required. Source revision, workflow revision and input trust are deliberately separate. The existing user's multi-subagent request selects subagent-driven execution; preparation proceeds directly under the recorded consent. Private evidence is retained rather than deleting the SDD workspace.

## Documentation used for command contracts

- .NET SDK selection: https://learn.microsoft.com/en-us/dotnet/core/tools/global-json
- Restore and explicit configuration: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore
- NativeAOT publication: https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/
- Evaluated property output: https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference
- Manual event/ref selection: https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow?tool=cli
- Versioned workflow dispatch: https://docs.github.com/en/rest/actions/workflows and https://docs.github.com/en/rest/about-the-rest-api/breaking-changes
- Plain-text job logs: https://docs.github.com/en/rest/actions/workflow-jobs
- CLI response/cache behavior: https://cli.github.com/manual/gh_api

These are command/documentation sources, not evidence that this source compiles
or that a remote runner has any particular installed tool. Actual outputs decide.
