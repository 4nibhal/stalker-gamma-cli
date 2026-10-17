# Linux-only Personal Fork Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Do not dispatch agents until an execution approach has been selected.

**Goal:** Remove Windows/macOS host support from the personal fork while preserving Linux x64/arm64 installation behavior and Wine game payloads, without using a local C# toolchain.

**Architecture:** Keep the existing C# projects and service boundaries. The CLI remains NativeAOT; the optional server remains self-contained without AOT. Local Python source-contract tests precede each conversion, while GitHub Actions is the only planned location for compilation and isolated C# regression execution.

**Tech Stack:** C#/.NET 10, ConsoleAppFramework, existing .NET services, Python standard-library unittest, GNU Make, Linux AppImage, manually dispatched GitHub Actions.

## Global Constraints

- Personal fork: `https://github.com/4nibhal/stalker-gamma-cli`.
- Host platform: Linux only, with the existing x64 and arm64 targets.
- Work on branch `linux-hardening` in `.worktrees/linux-hardening`.
- Retain C# and NativeAOT. Compile the native Linux executable in GitHub Actions; do not install or run a C# compiler, .NET SDK, or local .NET build toolchain on the user's machine. This restriction does not require rewriting the project.
- Preserve Anomaly/GAMMA/MO2 `.exe` and `.dll` payloads and Wine-compatible paths.
- CI is manually dispatched so committing or pushing a branch does not silently start SDK/package/tool downloads. Native compilation belongs on the remote Linux runners, not on the user's machine.
- No SDK, NuGet package, native library, browser image or other artifact may be downloaded, locally or by a triggered CI run, without separate explicit scoped consent.
- No public disclosure or upstream PR is implied by this specification.
- Preserve commands, installer/update/resume flows and existing Linux layout except for removed non-Linux options. Do not add unrelated refactors.
- Do not execute the installer, game, build-download targets or companion server as an improvised test. No actual personal installation is part of this stage.
- Local source/parse checks do not establish C# compilation, native ABI correctness, safe extraction, or successful installation.
- Preserve the server's explicit `PublishAot=false`, `PublishSingleFile=true`, and `SelfContained=true` settings. Do not change its browser fingerprint or start its container.
- Filesystem confinement, restrictive permissions, checksum enforcement, atomic downloads, server controls, and immutable build inputs belong to subsequent security specifications. Do not label them fixed by this conversion.

## Verified Starting Point and Documentation

- Application baseline: `04d828e174dcef4ee53b2dc45e9ab31530043674`.
- Specification: `docs/superpowers/specs/2026-10-17-linux-only-fork-design.md`, approved including the no-local-compiler clarification.
- `Directory.Build.props:3-6` targets .NET 10 and enables AOT; the server overrides AOT at `stalker-gamma-cli-server/stalker-gamma-cli-server.csproj:7-9`.
- `build/linux/Makefile:70` omits the CLI RID; the server explicitly selects `linux-$(ARCH)` at lines 39 and 85.
- `build/linux/README.md:36-38` requires Wine for `ModOrganizer.exe`; do not remove its payload names.
- Installed local tools checked during planning: Python, Make, Bash, ShellCheck, and Graphify's existing Tree-sitter C# grammar. `actionlint` is not installed. No tool installation is part of execution.
- Parser baseline: 129 application C# files, six with diagnostics. Four are generated request builders with conditional method declarations; two contain `(ref entry) => entry.ToFileSystemInfo()` at `stalker-gamma-cli/Commands/Anomaly.cs:254` and `stalker-gamma-cli/Utilities/HashUtility.cs:80`. Selecting the existing modern preprocessor branch and making the lambda type explicit **in memory only** removed these diagnostics. This is a parser investigation, not compiler evidence; do not make those edits to application sources.
- Microsoft documentation consulted: [assembly attributes from MSBuild](https://learn.microsoft.com/en-us/dotnet/standard/assembly/set-attributes-project-file), [CA1416](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1416), and [.NET 10 ArgumentList](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.argumentlist?view=net-10.0). Context7 did not return a matching ArgumentList page; its API contract was checked on Microsoft Learn instead.
- GitHub documentation consulted: [manual workflows](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow). `workflow_dispatch` requires the workflow on the default branch; do not merge or push it there without authorization.

## File Boundaries

| Unit | Files and responsibility |
|---|---|
| Linux host boundary | `Directory.Build.props`, both `Program.cs` entry points: assembly declaration and early unsupported-host failure |
| Installer wiring | `FullInstall.cs`, `GammaInstaller.cs`, `OfflineGammaInstaller.cs`, `ServiceCollectionExtensions.cs`, `CreateSymbolicLinkUtility.cs`: remove elevated Windows setup |
| Linux runtime adapters | archive/readiness services, tool settings/setup, `DirUtils.cs`, `WriteModOrganizerIni.cs`, `FileExplorerHelper.cs`: select existing Linux behavior |
| Native curl adapter | `LibCurlImpersonate/LibCurl.cs`: Linux resolver and existing Linux call signatures only |
| Local contracts | `tests/__init__.py`, `tests/test_linux_only.py`: file/XML/text contracts using only the Python standard library |
| Remote regression | `tests/LinuxRegression/LinuxRegression.csproj`, `tests/LinuxRegression/Program.cs`: isolated MO2 configuration checks; never run locally |
| Delivery | `build/linux/Makefile`, `.github/workflows/build.yml`, removed non-Linux packaging files: remote Linux builds with no publishing |
| Documentation | `README.md`, `build/linux/README.md`, `build/update-pkgbuild.sh`: fork identity, prerequisites, no-local-SDK workflow |

Before staging each task, inspect `git status --short` and the diff. Stage only the listed files; preserve any unrelated user work. Tasks are sequential because the final source contracts cover earlier changes.

---

### Task 1: Establish the Linux Host Boundary

**Files:**
- Create: `tests/__init__.py` (empty).
- Create: `tests/test_linux_only.py`.
- Modify: `Directory.Build.props:1-8`.
- Modify: `stalker-gamma-cli/Program.cs:17-25`.
- Modify: `stalker-gamma-cli-server/Program.cs:9-13`.

**Interfaces:**
- Consumes: existing `public static async Task Main(string[] args)` and the server's top-level entry point.
- Produces: unchanged entry-point signatures, early exit status 1 outside Linux, and generated assembly-level `SupportedOSPlatform("linux")` attributes. The attribute informs analysis; the entry-point checks enforce runtime rejection.

- [ ] **Step 1: Create the failing local source contracts.**

Use this initial content for `tests/test_linux_only.py`:

```python
from pathlib import Path
import re
import subprocess
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIRS = ("LibCurlImpersonate", "Stalker.Gamma", "stalker-gamma-cli", "stalker-gamma-cli-server")


def source(path):
    return (ROOT / path).read_text(encoding="utf-8")


def application_sources():
    return [
        path
        for directory in SOURCE_DIRS
        for path in (ROOT / directory).rglob("*.cs")
        if not {"bin", "obj"}.intersection(path.relative_to(ROOT).parts)
    ]


class LinuxHostTests(unittest.TestCase):
    def test_assembly_declares_linux(self):
        props = ET.fromstring(source("Directory.Build.props"))
        attr = props.find(".//AssemblyAttribute[@Include='System.Runtime.Versioning.SupportedOSPlatformAttribute']")
        self.assertIsNotNone(attr)
        self.assertEqual(attr.findtext("_Parameter1"), "linux")

    def test_entry_points_reject_other_hosts_before_side_effects(self):
        for path, marker in (
            ("stalker-gamma-cli/Program.cs", "var stalkerGammaLogsPath"),
            ("stalker-gamma-cli-server/Program.cs", "var builder"),
        ):
            with self.subTest(path=path):
                text = source(path)
                guard = text.index("if (!OperatingSystem.IsLinux())")
                self.assertLess(guard, text.index(marker))
                prefix = text[guard:text.index(marker)]
                self.assertIn("Environment.ExitCode = 1;", prefix)
                self.assertIn("return;", prefix)

    def test_server_keeps_its_non_aot_deployment(self):
        project = ET.fromstring(source("stalker-gamma-cli-server/stalker-gamma-cli-server.csproj"))
        for name, expected in (("PublishAot", "false"), ("SelfContained", "true"), ("PublishSingleFile", "true")):
            self.assertEqual(project.findtext(f".//{name}"), expected)
```

- [ ] **Step 2: Observe the baseline failure.**

Run locally: `python3 -m unittest tests.test_linux_only.LinuxHostTests -v`.
Expected: missing assembly attribute and missing early guards fail; the server deployment characterization already passes. Record actual output, not an invented failure count.

- [ ] **Step 3: Add the attribute and both early guards.**

Append inside `Directory.Build.props`, after its property group:

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Runtime.Versioning.SupportedOSPlatformAttribute">
    <_Parameter1>linux</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

Insert this as the first executable code in CLI `Main`, and before `var builder` in the server:

```csharp
if (!OperatingSystem.IsLinux())
{
    Console.Error.WriteLine("This fork supports Linux only.");
    Environment.ExitCode = 1;
    return;
}
```

Replace the server's Homebrew-specific launch comment with an explanation of resolving the content root next to the executable. Leave the three deployment properties unchanged.

- [ ] **Step 4: Re-run the task's contracts and whitespace check.**

```bash
python3 -m unittest tests.test_linux_only.LinuxHostTests -v
git diff --check
```

Expected: local contracts pass. This does not demonstrate execution on another OS or successful MSBuild attribute generation.

- [ ] **Step 5: Commit the reviewed task files.**

```bash
git add -- tests/__init__.py tests/test_linux_only.py Directory.Build.props stalker-gamma-cli/Program.cs stalker-gamma-cli-server/Program.cs
git commit -m "refactor: declare and enforce Linux-only host support"
```

### Task 2: Remove Windows Elevation and Installer Wiring

**Files:**
- Delete: `Stalker.Gamma/Services/PowerShellCmd.cs`.
- Modify: `Stalker.Gamma/Utilities/CreateSymbolicLinkUtility.cs:1-25`.
- Modify: `Stalker.Gamma/Extensions/ServiceCollectionExtensions.cs:65-68`.
- Modify: `Stalker.Gamma/GammaInstallerServices/GammaInstaller/GammaInstaller.cs:16-41,179-187`.
- Modify: `Stalker.Gamma/GammaInstallerServices/GammaInstaller/OfflineGammaInstaller.cs:14-43,58-66`.
- Modify: `stalker-gamma-cli/Commands/FullInstall.cs:16-97,221-244,312-319`.
- Test: `tests/test_linux_only.py`.

**Interfaces:**
- Consumes: `CreateSymbolicLinkUtility.Create` at the two installer call sites and constructor-based DI.
- Produces: `public static void Create(string path, string pathToTarget)`; no PowerShell types, flags, elevated execution, or registration. Existing cache-link trust behavior is unchanged and is not certified secure here.

- [ ] **Step 1: Append failing installer contracts.**

```python
class LinuxInstallerTests(unittest.TestCase):
    def test_no_elevated_windows_setup_remains(self):
        self.assertFalse((ROOT / "Stalker.Gamma/Services/PowerShellCmd.cs").exists())
        for path in application_sources():
            with self.subTest(path=str(path.relative_to(ROOT))):
                text = path.read_text(encoding="utf-8")
                for forbidden in ("PowerShellCmd", "addFoldersToWinDefenderExclusion", "enableLongPaths", "ConfigurePowerShellSettings"):
                    self.assertNotIn(forbidden, text)

    def test_both_installers_use_the_two_argument_link_helper(self):
        for name in ("GammaInstaller", "OfflineGammaInstaller"):
            text = source(f"Stalker.Gamma/GammaInstallerServices/GammaInstaller/{name}.cs")
            self.assertIn("CreateSymbolicLinkUtility.Create(gammaDownloadsPath, args.Cache);", text)
        helper = source("Stalker.Gamma/Utilities/CreateSymbolicLinkUtility.cs")
        self.assertIn("Directory.CreateSymbolicLink(path, pathToTarget);", helper)
        self.assertNotIn("OperatingSystem.IsWindows", helper)
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_linux_only.LinuxInstallerTests -v`.**

Expected: failure on the existing file/references and three-argument call sites.

- [ ] **Step 3: Remove the complete dependency chain, not just its implementation.**

Replace the link helper with:

```csharp
namespace Stalker.Gamma.Utilities;

public static class CreateSymbolicLinkUtility
{
    public static void Create(string path, string pathToTarget)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateSymbolicLink(path, pathToTarget);
        }
    }
}
```

At both call sites use:

```csharp
CreateSymbolicLinkUtility.Create(gammaDownloadsPath, args.Cache);
```

Delete the immediately following Windows-only `Build().ExecuteAsync` block. Remove `PowerShellCmdBuilder powerShellCmdBuilder` from both installer constructors, the protected property from the base class, and `powerShellCmdBuilder` from the derived base-constructor argument list. End the DI chain with `.AddScoped<IAnomalyInstaller, AnomalyInstaller>();`.

In `FullInstall.cs`, remove the constructor dependency, `_powerShellCmdBuilder` field, `ConfigurePowerShellSettings` method and its entire invocation. Remove these parameter declarations and their XML documentation:

```csharp
bool addFoldersToWinDefenderExclusion = false,
bool enableLongPaths = false,
```

Delete `PowerShellCmd.cs` only after all consumers have been removed. Keep every unrelated full-install option and the offline/resume logic intact.

- [ ] **Step 4: Re-run all current contracts and inspect constructor consistency.**

```bash
python3 -m unittest tests.test_linux_only -v
git diff --check
```

Check matching base/derived argument order and DI registration in the actual diff. Compilation remains remote-only and not run.

- [ ] **Step 5: Commit only this task's files.**

```bash
git add -- tests/test_linux_only.py Stalker.Gamma/Services/PowerShellCmd.cs Stalker.Gamma/Utilities/CreateSymbolicLinkUtility.cs Stalker.Gamma/Extensions/ServiceCollectionExtensions.cs Stalker.Gamma/GammaInstallerServices/GammaInstaller/GammaInstaller.cs Stalker.Gamma/GammaInstallerServices/GammaInstaller/OfflineGammaInstaller.cs stalker-gamma-cli/Commands/FullInstall.cs
git commit -m "refactor: remove Windows elevation and installer options"
```

### Task 3: Select Linux Runtime Adapters and Preserve Wine Configuration

**Files:**
- Modify: `Stalker.Gamma/Services/ArchiveService.cs:5-132`.
- Modify: `Stalker.Gamma/Services/{SevenZipService,TarService,UnzipService}.cs` (`Ready` properties).
- Modify: `Stalker.Gamma/Models/StalkerGammaSettings.cs:23`.
- Modify: `Stalker.Gamma/Utilities/DirUtils.cs:17-30`.
- Modify: `Stalker.Gamma/ModOrganizer/WriteModOrganizerIni.cs:13-26`.
- Modify: `Stalker.Gamma/GammaInstallerServices/AnomalyInstaller.cs:79` (obsolete Windows comment only).
- Modify: `Stalker.Gamma/Services/NetworkService.cs:59-62` (obsolete executable-readiness comment only).
- Modify: `stalker-gamma-cli/Services/SetupUtilitiesService.cs:9-12`.
- Modify: `stalker-gamma-cli/Commands/Anomaly.cs:63-67`.
- Modify: `stalker-gamma-cli/Utilities/{UtilitiesReady,FileExplorerHelper}.cs`.
- Create: `tests/LinuxRegression/{LinuxRegression.csproj,Program.cs}`.
- Test: `tests/test_linux_only.py`.

**Interfaces:**
- Consumes: existing Linux extraction mappings, `WriteModOrganizerIni.WriteAsync(string, string, string, IList<string>, string)` with its default selected profile, and tool settings.
- Produces: `ArchiveService(SevenZipService sevenZipService, UnzipService unzipService)` with the same `ExtractAsync` signature; Linux tool names; unchanged Wine INI payload; isolated remote regression executable.

- [ ] **Step 1: Append failing Linux adapter contracts.**

```python
class LinuxRuntimeTests(unittest.TestCase):
    def test_runtime_adapters_have_no_non_linux_host_branches(self):
        paths = (
            "Stalker.Gamma/Services/ArchiveService.cs",
            "Stalker.Gamma/Services/SevenZipService.cs",
            "Stalker.Gamma/Services/TarService.cs",
            "Stalker.Gamma/Services/UnzipService.cs",
            "Stalker.Gamma/Models/StalkerGammaSettings.cs",
            "Stalker.Gamma/Utilities/DirUtils.cs",
            "Stalker.Gamma/ModOrganizer/WriteModOrganizerIni.cs",
            "stalker-gamma-cli/Services/SetupUtilitiesService.cs",
            "stalker-gamma-cli/Commands/Anomaly.cs",
            "stalker-gamma-cli/Utilities/UtilitiesReady.cs",
            "stalker-gamma-cli/Utilities/FileExplorerHelper.cs",
        )
        for path in paths:
            with self.subTest(path=path):
                self.assertNotRegex(source(path), r"OperatingSystem\.IsWindows|OSPlatform\.(Windows|OSX)|7zz\.exe|unzip\.exe|tar\.exe")

    def test_zip_uses_unzip_and_wine_payload_is_preserved(self):
        archive = source("Stalker.Gamma/Services/ArchiveService.cs")
        self.assertNotIn("tarService", archive)
        self.assertIn("unzipService.ExtractAsync(", archive)
        self.assertIn("DirUtils.NormalizePermissions(destinationDir);", archive)
        ini = source("Stalker.Gamma/ModOrganizer/WriteModOrganizerIni.cs")
        self.assertIn('drivePrefix = "Z:"', ini)
        for payload in ("AnomalyLauncher.exe", "AnomalyDX11AVX.exe", "AnomalyDX9.exe", "Explorer++.exe"):
            self.assertIn(payload, ini)
        self.assertIn("ModOrganizer.exe", source("Stalker.Gamma/ModOrganizer/DownloadModOrganizer/DownloadModOrganizerService.cs"))

    def test_desktop_opener_passes_one_linux_path_argument(self):
        text = source("stalker-gamma-cli/Utilities/FileExplorerHelper.cs")
        self.assertIn('FileName = "xdg-open"', text)
        self.assertIn("UseShellExecute = false", text)
        self.assertIn("ArgumentList.Add(Path.GetFullPath(path))", text)
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_linux_only.LinuxRuntimeTests -v`.**

Expected: failure on the existing host branches, archive constructor, and quoted desktop argument string. Wine payload assertions characterize existing behavior.

- [ ] **Step 3: Simplify archive selection without changing extraction policy.**

Remove `TarService` from `ArchiveService`'s constructor. Remove the outer Windows branch, retaining the existing file-header lookup and `try/finally` permissions call. Replace the ZIP mapping with:

```csharp
{
    // zip -> unzip on Linux
    0x50,
    async args =>
        await unzipService.ExtractAsync(
            args.ArchivePath,
            args.DestinationDir,
            args.Pct,
            args.Ct
        )
},
```

Keep the zstd/7z/rar mappings to SevenZip and remove their obsolete macOS comments. Keep the general `TarService`, its DI registration, and current Linux readiness requirement; this avoids an unrelated prerequisite/API change.

Replace tool selections exactly as follows:

| File | Replacement |
|---|---|
| `SevenZipService.cs` | `EnvChecker.IsInPath("7zz")` |
| `TarService.cs` | `EnvChecker.IsInPath("tar")` |
| `UnzipService.cs` | `EnvChecker.IsInPath("unzip")` |
| `StalkerGammaSettings.cs` | `public string PathTo7Z = "7zz";` |
| `SetupUtilitiesService.cs` | `settings.PathTo7Z = Path.Join(ResourcesPath, "7zz");` |
| `Commands/Anomaly.cs` | `stalkerGammaSettings.PathTo7Z = Path.Join(resourcesPath, "7zz");` |
| `UtilitiesReady.cs` | replace the last two readiness clauses with `&& tarService.Ready && unzipService.Ready` |

In `DirUtils.NormalizePermissions`, remove only the Windows guard and unindent its Unix mode assignment. Do not change the mode mask in this conversion: restrictive permissions require their own regression tests in the next security stage. Remove the obsolete Windows comment in `AnomalyInstaller`. Replace `NetworkService.Ready`'s stale comment with: `Readiness placeholder; this does not verify native library availability.` Do not turn its constant `true` into a claimed real readiness probe.

- [ ] **Step 4: Preserve the current Wine path transformation explicitly.**

Replace only the initial path calculation in `WriteModOrganizerIni.WriteAsync`:

```csharp
const string drivePrefix = "Z:";
var gammaPathWithDrivePrefix = Path.Join(
    drivePrefix,
    Path.GetFullPath(gammaPath)
).Replace(@"\", "/");
var anomalyPathWithDrivePrefix = Path.Join(
    drivePrefix,
    Path.GetFullPath(anomalyPath)
).Replace(@"\", "/");
var escapedWinAnomalyPath = anomalyPathWithDrivePrefix.TrimEnd('/').Replace("/", @"\\");
```

Leave the INI template and its game/MO2 executables unchanged. In `FileExplorerHelper`, preserve the missing-directory check and replace the remaining host dispatch with:

```csharp
var startInfo = new ProcessStartInfo
{
    FileName = "xdg-open",
    UseShellExecute = false,
};
startInfo.ArgumentList.Add(Path.GetFullPath(path));
Process.Start(startInfo);
```

Remove `using System.Runtime.InteropServices;` from that helper. This one argument change accompanies removal of its host dispatch; the general `RunProcessUtility` hardening remains separate.

- [ ] **Step 5: Add the isolated remote characterization test, without executing it locally.**

`tests/LinuxRegression/LinuxRegression.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <PublishAot>false</PublishAot>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../Stalker.Gamma/Stalker.Gamma.csproj" />
  </ItemGroup>
</Project>
```

`tests/LinuxRegression/Program.cs`:

```csharp
using Stalker.Gamma.ModOrganizer;

if (!OperatingSystem.IsLinux())
{
    throw new PlatformNotSupportedException("Linux regression runner only.");
}

var scratch = Directory.CreateTempSubdirectory("stalker-linux-regression-");
try
{
    var gamma = Path.Join(scratch.FullName, "GAMMA ñ space");
    var anomaly = Path.Join(scratch.FullName, "Anomaly d'Artagnan");
    Directory.CreateDirectory(gamma);
    Directory.CreateDirectory(anomaly);
    await WriteModOrganizerIni.WriteAsync(gamma, anomaly, "v2.5.2", []);
    var ini = await File.ReadAllTextAsync(Path.Join(gamma, "ModOrganizer.ini"));
    var wineAnomaly = "Z:" + Path.GetFullPath(anomaly);
    var escapedAnomaly = wineAnomaly.TrimEnd('/').Replace("/", @"\\");
    foreach (
        var expected in new[]
        {
            $"gamePath=@ByteArray({escapedAnomaly})",
            $"1\\binary={wineAnomaly}/AnomalyLauncher.exe",
            $"2\\binary={wineAnomaly}/bin/AnomalyDX11AVX.exe",
            "selected_profile=@ByteArray(G.A.M.M.A)",
            "version=2.5.2",
        }
    )
    {
        if (!ini.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Missing expected INI value: {expected}");
        }
    }
    Console.WriteLine("Isolated Wine INI regression passed.");
}
finally
{
    Directory.Delete(scratch.FullName, recursive: true);
}
```

This executes only the INI writer in a fresh runner-owned directory. It does not start the CLI, installer, game, native curl, browser, or container. It is characterization of preserved behavior, not a demonstrated red/green security fix.

- [ ] **Step 6: Re-run local contracts and inspect the diff.**

```bash
python3 -m unittest tests.test_linux_only -v
git diff --check
```

Remote-only command for a separately authorized CI run: `dotnet run --project tests/LinuxRegression/LinuxRegression.csproj -c Release`. Do not run it locally.

- [ ] **Step 7: Commit only the listed runtime/test files.**

```bash
git add -- tests/test_linux_only.py tests/LinuxRegression Stalker.Gamma/Services/ArchiveService.cs Stalker.Gamma/Services/SevenZipService.cs Stalker.Gamma/Services/TarService.cs Stalker.Gamma/Services/UnzipService.cs Stalker.Gamma/Models/StalkerGammaSettings.cs Stalker.Gamma/Utilities/DirUtils.cs Stalker.Gamma/ModOrganizer/WriteModOrganizerIni.cs Stalker.Gamma/GammaInstallerServices/AnomalyInstaller.cs Stalker.Gamma/Services/NetworkService.cs stalker-gamma-cli/Services/SetupUtilitiesService.cs stalker-gamma-cli/Commands/Anomaly.cs stalker-gamma-cli/Utilities/UtilitiesReady.cs stalker-gamma-cli/Utilities/FileExplorerHelper.cs
git commit -m "refactor: select Linux tools and preserve Wine configuration"
```

### Task 4: Remove Non-Linux Native Curl Bindings

**Files:**
- Modify: `LibCurlImpersonate/LibCurl.cs:18-22,59-61,101-219`.
- Test: `tests/test_linux_only.py`.

**Interfaces:**
- Consumes: existing Linux P/Invoke declarations ending in `_impl` and their callback types.
- Produces: the same six public-to-assembly wrapper signatures, now directly calling the Linux declarations; only `libcurl-impersonate.so` is resolved. No ABI redesign or native-library download.

- [ ] **Step 1: Append the failing curl contracts.**

```python
class LinuxCurlTests(unittest.TestCase):
    def test_resolver_and_wrappers_are_linux_only(self):
        text = source("LibCurlImpersonate/LibCurl.cs")
        self.assertIn('NativeLibrary.Load("libcurl-impersonate.so", assembly, searchPath)', text)
        for forbidden in ("OSPlatform.Windows", "OSPlatform.OSX", "_isMacOsArm64", "macos_arm64_impl", ".dylib", "libcurl-impersonate.dll"):
            self.assertNotIn(forbidden, text)
        for name in ("getinfo_long", "setopt_str", "setopt_long", "setopt_ptr", "setopt_cb", "setopt_xcb"):
            self.assertRegex(text, rf"=>\s*{name}_impl\(")
        self.assertIn("typeof(CallConvCdecl)", text)
        self.assertIn("out long value", text)

    def test_application_has_no_non_linux_host_selection(self):
        for path in application_sources():
            with self.subTest(path=str(path.relative_to(ROOT))):
                self.assertNotRegex(path.read_text(encoding="utf-8"), r"OperatingSystem\.IsWindows|OSPlatform\.(Windows|OSX)")
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_linux_only.LinuxCurlTests -v`.**

Expected: resolver and wrapper checks fail on the existing bindings.

- [ ] **Step 3: Make the existing Linux implementation unconditional.**

Keep the resolver's unrelated-library check, followed by:

```csharp
return NativeLibrary.Load("libcurl-impersonate.so", assembly, searchPath);
```

Remove `_isMacOsArm64` and all six `_macos_arm64_impl` declarations. Keep the Linux declarations at lines 65-99 unchanged and replace the wrappers with:

```csharp
internal static int curl_easy_getinfo_long(IntPtr h, int info, out long value) =>
    getinfo_long_impl(h, info, out value);

internal static int curl_easy_setopt_str(IntPtr h, int opt, string v) =>
    setopt_str_impl(h, opt, v);

internal static int curl_easy_setopt_long(IntPtr h, int opt, long v) =>
    setopt_long_impl(h, opt, v);

internal static int curl_easy_setopt_ptr(IntPtr h, int opt, IntPtr v) =>
    setopt_ptr_impl(h, opt, v);

internal static int curl_easy_setopt_cb(IntPtr h, int opt, WriteCallback v) =>
    setopt_cb_impl(h, opt, v);

internal static int curl_easy_setopt_xcb(IntPtr h, int opt, XferInfoCallback v) =>
    setopt_xcb_impl(h, opt, v);
```

Preserve all constants, callback signatures, string marshalling, and `long`/pointer widths. Do not claim Linux arm64 ABI correctness from these source tests.

- [ ] **Step 4: Run all local contracts and whitespace checks.**

```bash
python3 -m unittest tests.test_linux_only -v
git diff --check
```

- [ ] **Step 5: Commit the curl change.**

```bash
git add -- tests/test_linux_only.py LibCurlImpersonate/LibCurl.cs
git commit -m "refactor: retain only Linux curl native bindings"
```

### Task 5: Retain Linux Packaging and Replace Inherited Publishing CI

**Files:**
- Modify: `build/linux/Makefile:10-16,35-39,70,85`.
- Replace: `.github/workflows/build.yml`.
- Delete exactly: `build/mac/Makefile`, `build/mac/README.md`, `build/win/BUILD.md`, `build/win/build.bat`, `build/win/build.ps1`, `build/win/chocolatey.bat`, `build/win/chocolatey.ps1`, `build/update-cask.sh`.
- Preserve: both Linux AppDir trees and `build/update-pkgbuild.sh`.
- Test: `tests/test_linux_only.py`.

**Interfaces:**
- Consumes: `make build appimage -C build/linux VERSION=<assembly-version> ARCH=x64|arm64`, plus the remote regression project from Task 3.
- Produces: explicit CLI/server Linux RIDs, two manually built Linux artifacts per architecture and their checksum file, no release/package publishing jobs.

- [ ] **Step 1: Append failing delivery contracts.**

```python
class LinuxDeliveryTests(unittest.TestCase):
    def test_non_linux_packaging_files_are_removed(self):
        for directory in ("build/win", "build/mac"):
            self.assertEqual([p for p in (ROOT / directory).rglob("*") if p.is_file()], [])
        self.assertFalse((ROOT / "build/update-cask.sh").exists())
        for path in ("build/linux/stalker-gamma.AppDir/AppRun", "build/linux/stalker-gamma-server.AppDir/AppRun"):
            self.assertTrue((ROOT / path).is_file())

    def test_make_dry_run_has_both_explicit_linux_rids(self):
        for arch in ("x64", "arm64"):
            result = subprocess.run(
                ["make", "--dry-run", "-C", str(ROOT / "build/linux"), "build", "appimage", f"ARCH={arch}", "VERSION=1.0.0"],
                capture_output=True, text=True, check=False,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            publish = [line for line in result.stdout.splitlines() if line.lstrip().startswith("dotnet publish ")]
            self.assertEqual(len(publish), 2)
            for line in publish:
                self.assertIn(f"-r linux-{arch}", line)

    def test_ci_is_manual_linux_only_and_non_publishing(self):
        text = source(".github/workflows/build.yml")
        event_section = re.search(r"(?ms)^on:\n(.*?)(?=^[^\s#])", text)
        self.assertIsNotNone(event_section)
        self.assertEqual(re.findall(r"^  ([\w-]+):", event_section.group(1), re.M), ["workflow_dispatch"])
        self.assertRegex(text, r"(?m)^permissions:\n  contents: read$")
        for forbidden in ("windows-", "macos-", "secrets.", "update-aur:", "update-chocolatey:", "update-homebrew:", "publish-release:", "action-gh-release", "git push", "inputs.dry_run"):
            self.assertNotIn(forbidden, text)
        self.assertIn("ubuntu-22.04-arm", text)
        self.assertIn("ubuntu-22.04", text)
        self.assertIn("tests/LinuxRegression/LinuxRegression.csproj", text)
        self.assertIn("persist-credentials: false", text)
```

These are source contracts and a Make dry run, not a complete YAML/schema validator or a build.

- [ ] **Step 2: Run `python3 -m unittest tests.test_linux_only.LinuxDeliveryTests -v`.**

Expected: the existing packaging, omitted CLI RID, push trigger and publishing jobs fail the contracts. The dry run must not execute a compiler, downloader, or packaging tool.

- [ ] **Step 3: Correct the RID and remove the listed packaging files.**

Add beside the CLI project variable:

```makefile
STALKER_GAMMA_CLI_RID = linux-$(ARCH)
```

Change the two publish recipes to:

```makefile
	dotnet publish -c Release "$(STALKER_GAMMA_CLI_PROJECT)" -r $(STALKER_GAMMA_CLI_RID) -o "$(STALKER_GAMMA_BUILD_DIR)" -p:AssemblyVersion=$(VERSION)
	dotnet publish -c Release "$(STALKER_GAMMA_SERVER_PROJECT)" -r $(STALKER_GAMMA_SERVER_RID) -o "$(STALKER_GAMMA_SERVER_BUILD_DIR)" -p:AssemblyVersion=$(VERSION)
```

Keep the existing separate recipe locations. Replace `CURL_ARCH := unknown` in the first architecture selection with:

```makefile
    $(error Unsupported ARCH '$(ARCH)' - use x64 or arm64)
```

Do not execute `make build`, `make appimage` or `make clean`. Retain the known unverified tool downloads as explicitly pending supply-chain hardening; this task does not authorize fetching or running them. Delete only the eight listed non-Linux packaging files using file-specific patches.

- [ ] **Step 4: Replace the workflow with this manual, non-publishing draft.**

```yaml
name: Linux builds

on:
  workflow_dispatch:

permissions:
  contents: read

jobs:
  lint:
    runs-on: ubuntu-22.04
    steps:
      - uses: actions/checkout@v7
        with:
          persist-credentials: false
      - name: Check Linux source contracts without .NET
        run: python3 -m unittest tests.test_linux_only -v
      - uses: actions/setup-dotnet@v6
        with:
          dotnet-version: 10.0.x
      - name: Check CSharpier formatting
        run: |
          dotnet tool restore
          dotnet csharpier check .

  version:
    runs-on: ubuntu-22.04
    outputs:
      file_version: ${{ steps.gitversion.outputs.AssemblySemFileVer }}
    steps:
      - uses: actions/checkout@v7
        with:
          fetch-depth: 0
          persist-credentials: false
      - uses: gittools/actions/gitversion/setup@v4
        with:
          versionSpec: "6.x"
      - id: gitversion
        uses: gittools/actions/gitversion/execute@v4

  build:
    needs: [lint, version]
    runs-on: ${{ matrix.runner }}
    strategy:
      fail-fast: false
      matrix:
        include:
          - arch: x64
            runner: ubuntu-22.04
          - arch: arm64
            runner: ubuntu-22.04-arm
    env:
      TARGET_ARCH: ${{ matrix.arch }}
      FILE_VERSION: ${{ needs.version.outputs.file_version }}
    defaults:
      run:
        shell: bash
    steps:
      - uses: actions/checkout@v7
        with:
          persist-credentials: false
      - uses: actions/setup-dotnet@v6
        with:
          dotnet-version: 10.0.x
      - name: Run isolated Wine configuration regression
        run: dotnet run --project tests/LinuxRegression/LinuxRegression.csproj -c Release
      - name: Build Linux artifacts
        run: make build appimage -C build/linux VERSION="$FILE_VERSION" ARCH="$TARGET_ARCH"
      - name: Hash artifacts
        working-directory: build/linux
        run: sha256sum "stalker-gamma+linux.${TARGET_ARCH}.AppImage" "stalker-gamma-server+linux.${TARGET_ARCH}.AppImage" > "SHA256SUMS-linux.${TARGET_ARCH}"
      - uses: actions/upload-artifact@v7
        with:
          name: linux-${{ matrix.arch }}
          path: |
            build/linux/stalker-gamma+linux.${{ matrix.arch }}.AppImage
            build/linux/stalker-gamma-server+linux.${{ matrix.arch }}.AppImage
            build/linux/SHA256SUMS-linux.${{ matrix.arch }}
          if-no-files-found: error
```

This draft retains existing Action/tool version tags rather than inventing immutable pins. Before any authorized execution, the supply-chain stage must resolve trusted Action SHAs, SDK/tool versions and download checksums, including the mutable AppImage tool. Artifact hashes describe outputs, not trustworthy verification of build inputs. This plan does not enable Actions, push, merge, dispatch, publish a release or download artifacts.

- [ ] **Step 5: Run all local contracts and static checks.**

```bash
python3 -m unittest tests.test_linux_only -v
git diff --check
bash -n build/update-pkgbuild.sh
sh -n build/linux/stalker-gamma.AppDir/AppRun
sh -n build/linux/stalker-gamma-server.AppDir/AppRun
```

Record that workflow execution and full YAML/action validation are not run. Do not install `actionlint` to remove that limitation.

- [ ] **Step 6: Commit only the build/workflow/test changes and named deletions.**

```bash
git add -- tests/test_linux_only.py .github/workflows/build.yml build/linux/Makefile build/mac/Makefile build/mac/README.md build/win/BUILD.md build/win/build.bat build/win/build.ps1 build/win/chocolatey.bat build/win/chocolatey.ps1 build/update-cask.sh
git commit -m "build: retain Linux packaging and disable inherited publishing"
```

### Task 6: Document the Fork and Record Verification Limits

**Files:**
- Modify: `README.md:1-84`.
- Modify: `build/linux/README.md:1-38`.
- Modify: `build/update-pkgbuild.sh:41,45-46`.
- Modify: the specification's progress checklist and this plan's completed checkboxes.
- Test: `tests/test_linux_only.py`.

**Interfaces:**
- Consumes: the Linux-only behavior, remote-only compilation policy and non-publishing CI from Tasks 1-5.
- Produces: accurate fork documentation and Linux package metadata; no promise of a validated artifact, secure installation or completed security fixes.

- [ ] **Step 1: Append failing documentation contracts.**

```python
class LinuxDocumentationTests(unittest.TestCase):
    def test_readme_identifies_the_fork_and_remote_build_policy(self):
        text = source("README.md")
        self.assertIn("https://github.com/4nibhal/stalker-gamma-cli", text)
        self.assertIn("Linux-only", text)
        self.assertIn("No local .NET SDK or C# compiler", text)
        self.assertIn("workflow_dispatch", text)
        self.assertNotRegex(text, r"Windows-Install|MacOS-Install|build/win|build/mac")

    def test_linux_usage_preserves_wine_and_fork_package_urls(self):
        text = source("build/linux/README.md")
        self.assertIn("ModOrganizer.exe", text)
        self.assertIn("Wine", text)
        self.assertIn("tar", text)
        self.assertIn("unzip", text)
        package = source("build/update-pkgbuild.sh")
        self.assertIn("https://github.com/4nibhal/stalker-gamma-cli", package)
        self.assertNotIn("https://github.com/FaithBeam/stalker-gamma-cli", package)
```

- [ ] **Step 2: Run `python3 -m unittest tests.test_linux_only.LinuxDocumentationTests -v`.**

Expected: current fork/build-policy/prerequisite/package-URL contracts fail.

- [ ] **Step 3: Update documentation without inventing a release or runtime result.**

Use these exact concepts in `README.md`, retaining the existing command/features/update reference, upstream attribution, and unchanged `LICENSE`:

```markdown
# stalker-gamma-cli — Linux-only personal fork

Personal fork: https://github.com/4nibhal/stalker-gamma-cli
Derived from https://github.com/FaithBeam/stalker-gamma-cli; the existing GPLv3 license is retained.

This fork targets Linux x64 and arm64. Game and Mod Organizer executables remain Windows payloads run with Wine; they are not Windows host support for the installer.

## Build and verification policy

No local .NET SDK or C# compiler is required by this fork's intended end-user workflow. The CLI is configured for NativeAOT; the optional companion server bundles its .NET runtime without AOT. Native/system library requirements still apply.

Compilation is planned on GitHub-hosted Linux runners using manual `workflow_dispatch` only. Preparing the workflow is not an executed or verified build. Builds need separate authorization and completed build-input verification; there is no automatic package or release publication.

Local source checks use `python3 -m unittest tests.test_linux_only -v`. These checks do not replace compilation, runtime regression tests, or downloaded-content verification. No validated fork release is claimed here.

See [Linux usage](build/linux/README.md) for the installation layout and Wine configuration. Do not treat the platform conversion as completion of the staged security work.
```

Replace the Windows/macOS install and local build tables; do not direct this user to install an SDK. In `build/linux/README.md`, retain the config/full-install examples and Wine dependency guidance, label them usage documentation rather than an action to execute during this work, and describe the existing `tar`, `unzip`, bundled 7zz/native curl, and Wine requirements without giving unapproved install/download commands. Do not claim a separate `git` executable is required: the library's readiness property is currently constant `true` and Git operations use LibGit2Sharp.

In `build/update-pkgbuild.sh`, replace the three project/release URL occurrences of `https://github.com/FaithBeam/stalker-gamma-cli` with `https://github.com/4nibhal/stalker-gamma-cli`. Keep it a dormant manual generator; no AUR publication or verified release availability is implied.

- [ ] **Step 4: Run the complete local checks and compare C# parser diagnostics with the baseline.**

```bash
python3 -m unittest tests.test_linux_only -v
git diff --check
bash -n build/update-pkgbuild.sh
sh -n build/linux/stalker-gamma.AppDir/AppRun
sh -n build/linux/stalker-gamma-server.AppDir/AppRun
```

Run the following parse-only script through the already-installed Graphify interpreter, not a newly created environment. It compares error text/kinds, ignoring line shifts, and rejects additional diagnostics without suppressing the existing six-file baseline:

```bash
/home/4nibhal/.local/share/uv/tools/graphifyy/bin/python3 - <<'PY'
from collections import Counter
from pathlib import Path
import subprocess
from tree_sitter import Language, Parser
import tree_sitter_c_sharp

parser = Parser(Language(tree_sitter_c_sharp.language()))
baseline = "04d828e174dcef4ee53b2dc45e9ab31530043674"


def diagnostics(data):
    pending = [parser.parse(data).root_node]
    found = Counter()
    while pending:
        node = pending.pop()
        if node.is_error or node.is_missing:
            found[(node.type, data[node.start_byte:node.end_byte])] += 1
        pending.extend(node.children)
    return found


files = [
    p
    for directory in ("LibCurlImpersonate", "Stalker.Gamma", "stalker-gamma-cli", "stalker-gamma-cli-server", "tests/LinuxRegression")
    for p in Path(directory).rglob("*.cs")
    if not {"bin", "obj"}.intersection(p.parts)
]
failed = []
for path in files:
    old = subprocess.run(["git", "show", f"{baseline}:{path.as_posix()}"], capture_output=True, check=False)
    previous = diagnostics(old.stdout) if old.returncode == 0 else Counter()
    additional = diagnostics(path.read_bytes()) - previous
    if additional:
        failed.append(str(path))
print(f"Parse-only comparison: {len(files)} files; {len(failed)} with additional diagnostics")
for path in failed:
    print(path)
raise SystemExit(bool(failed))
PY
```

Inspect the complete final diff against the application baseline, accounting separately for the existing ignore/design commits. Confirm no game payload filenames, browser OS fingerprint, server network policy, external hash reference, or unrelated generated clients were rewritten. Run installed ShellCheck on the retained shell wrappers and generator; report pre-existing warnings rather than refactoring unrelated scripts to silence them.

- [ ] **Step 5: Record an evidence-based handoff and commit documentation.**

Mark only observed local checks as passing. Leave these explicitly **not run**: .NET compilation, remote C# regression, native curl/ABI tests, workflow execution, actual installer/game/server operation, and verification of downloaded tools/mods. Do not call the application ready for personal installation. List the next security stage without publishing the private audit or exploit details.

```bash
git add -- README.md build/linux/README.md build/update-pkgbuild.sh tests/test_linux_only.py docs/superpowers/specs/2026-10-17-linux-only-fork-design.md docs/superpowers/plans/2026-10-17-linux-only-fork.md
git commit -m "docs: explain Linux fork usage and remote-only build limits"
```

## Execution and Publication Gate

Executing this plan permits local source edits, local standard-library/static checks and task-sized local commits. It does **not** authorize a push, merge, public disclosure, upstream PR, enabling Actions, CI dispatch, artifact download, dependency installation, installer execution, or container launch.

Before any later download/run proposal, list the actual artifact/tool, source and verified or explicitly unknown size, purpose, exact destination (including remote runner paths where applicable), and scope. Obtain explicit consent covering all four points. Do not substitute remote execution for consent, and do not execute unverified downloaded build tools merely because they run in CI.

## Plan Self-Review

- [x] Scope remains platform conversion plus delivery/documentation; later security subsystems are excluded explicitly.
- [x] Host boundary, elevation removal, Linux adapters, Wine preservation, native bindings, Linux packaging, safe CI triggers, fork identity and no-local-toolchain restriction each map to a task.
- [x] Every implementation task has concrete source-contract tests and an observed-red/observed-green step; preservation-only assertions are identified as characterization.
- [x] Exact new helper signatures match their named consumers; server deployment remains non-AOT.
- [x] Local parse comparison accounts for existing grammar diagnostics and is not called compilation.
- [x] No task requires local .NET commands, new dependencies, live installation, network downloads or publishing.
- [ ] User selects inline or subagent-driven execution; implementation has not started.
