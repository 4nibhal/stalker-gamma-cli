"""Local source contracts and Make dry runs; no C# compilation or execution."""

from pathlib import Path
import re
import subprocess
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIRS = (
    "LibCurlImpersonate",
    "Stalker.Gamma",
    "stalker-gamma-cli",
    "stalker-gamma-cli-server",
)


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
        attr = props.find(
            ".//AssemblyAttribute["
            "@Include='System.Runtime.Versioning.SupportedOSPlatformAttribute']"
        )
        self.assertIsNotNone(attr)
        self.assertEqual(attr.findtext("_Parameter1"), "linux")

    def test_entry_points_reject_other_hosts_before_side_effects(self):
        for path, marker in (
            ("stalker-gamma-cli/Program.cs", "var stalkerGammaLogsPath"),
            ("stalker-gamma-cli-server/Program.cs", "var builder"),
        ):
            with self.subTest(path=path):
                text = source(path)
                guard_text = "if (!OperatingSystem.IsLinux())"
                self.assertTrue(guard_text in text, f"{path}: Linux guard missing")
                guard = text.index(guard_text)
                self.assertLess(guard, text.index(marker))
                prefix = text[guard:text.index(marker)]
                self.assertIn("Environment.ExitCode = 1;", prefix)
                self.assertIn("return;", prefix)

    def test_server_keeps_its_non_aot_deployment(self):
        project = ET.fromstring(
            source("stalker-gamma-cli-server/stalker-gamma-cli-server.csproj")
        )
        for name, expected in (
            ("PublishAot", "false"),
            ("SelfContained", "true"),
            ("PublishSingleFile", "true"),
        ):
            with self.subTest(property=name):
                self.assertEqual(project.findtext(f".//{name}"), expected)


class LinuxInstallerTests(unittest.TestCase):
    def test_no_elevated_windows_setup_remains(self):
        self.assertFalse((ROOT / "Stalker.Gamma/Services/PowerShellCmd.cs").exists())
        for path in application_sources():
            text = path.read_text(encoding="utf-8")
            for forbidden in (
                "PowerShellCmd",
                "addFoldersToWinDefenderExclusion",
                "enableLongPaths",
                "ConfigurePowerShellSettings",
            ):
                with self.subTest(path=str(path.relative_to(ROOT)), symbol=forbidden):
                    self.assertFalse(forbidden in text, f"Stale Windows setup: {forbidden}")

    def test_both_installers_use_the_two_argument_link_helper(self):
        for name in ("GammaInstaller", "OfflineGammaInstaller"):
            with self.subTest(installer=name):
                text = source(
                    "Stalker.Gamma/GammaInstallerServices/GammaInstaller/"
                    f"{name}.cs"
                )
                expected = (
                    "CreateSymbolicLinkUtility.Create(gammaDownloadsPath, args.Cache);"
                )
                self.assertTrue(expected in text, f"{name}: two-argument link call missing")
        helper = source("Stalker.Gamma/Utilities/CreateSymbolicLinkUtility.cs")
        self.assertIn("Directory.CreateSymbolicLink(path, pathToTarget);", helper)
        self.assertNotIn("OperatingSystem.IsWindows", helper)


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
                self.assertIsNone(
                    re.search(
                        r"OperatingSystem\.IsWindows|OSPlatform\.(Windows|OSX)"
                        r"|7zz\.exe|unzip\.exe|tar\.exe",
                        source(path),
                    ),
                    f"{path}: non-Linux adapter branch remains",
                )

    def test_zip_uses_unzip_and_wine_payload_is_preserved(self):
        archive = source("Stalker.Gamma/Services/ArchiveService.cs")
        self.assertFalse("tarService" in archive, "ZIP still has the macOS tar adapter")
        self.assertIn("unzipService.ExtractAsync(", archive)
        self.assertIn("DirUtils.NormalizePermissions(destinationDir);", archive)
        ini = source("Stalker.Gamma/ModOrganizer/WriteModOrganizerIni.cs")
        self.assertIn('drivePrefix = "Z:"', ini)
        for payload in (
            "AnomalyLauncher.exe",
            "AnomalyDX11AVX.exe",
            "AnomalyDX9.exe",
            "Explorer++.exe",
        ):
            self.assertIn(payload, ini)
        self.assertIn(
            "ModOrganizer.exe",
            source(
                "Stalker.Gamma/ModOrganizer/DownloadModOrganizer/"
                "DownloadModOrganizerService.cs"
            ),
        )

    def test_desktop_opener_passes_one_linux_path_argument(self):
        text = source("stalker-gamma-cli/Utilities/FileExplorerHelper.cs")
        self.assertTrue('FileName = "xdg-open"' in text, "Linux opener missing")
        self.assertIn("UseShellExecute = false", text)
        self.assertIn("ArgumentList.Add(Path.GetFullPath(path))", text)


class LinuxCurlTests(unittest.TestCase):
    def test_resolver_and_wrappers_are_linux_only(self):
        text = source("LibCurlImpersonate/LibCurl.cs")
        self.assertTrue(
            'NativeLibrary.Load("libcurl-impersonate.so", assembly, searchPath)' in text,
            "Native resolver is not Linux-only",
        )
        for forbidden in (
            "OSPlatform.Windows",
            "OSPlatform.OSX",
            "_isMacOsArm64",
            "macos_arm64_impl",
            ".dylib",
            "libcurl-impersonate.dll",
        ):
            with self.subTest(symbol=forbidden):
                self.assertFalse(forbidden in text, f"Non-Linux curl binding: {forbidden}")
        for name in (
            "getinfo_long",
            "setopt_str",
            "setopt_long",
            "setopt_ptr",
            "setopt_cb",
            "setopt_xcb",
        ):
            with self.subTest(wrapper=name):
                self.assertIsNotNone(
                    re.search(rf"=>\s*{name}_impl\(", text),
                    f"{name}: direct Linux wrapper missing",
                )
        self.assertIn("typeof(CallConvCdecl)", text)
        self.assertIn("out long value", text)

    def test_application_has_no_non_linux_host_selection(self):
        for path in application_sources():
            with self.subTest(path=str(path.relative_to(ROOT))):
                self.assertIsNone(
                    re.search(
                        r"OperatingSystem\.IsWindows|OSPlatform\.(Windows|OSX)",
                        path.read_text(encoding="utf-8"),
                    ),
                    f"{path.relative_to(ROOT)}: non-Linux host selection remains",
                )


class LinuxDeliveryTests(unittest.TestCase):
    def test_non_linux_packaging_files_are_removed(self):
        for directory in ("build/win", "build/mac"):
            with self.subTest(directory=directory):
                self.assertEqual(
                    [p for p in (ROOT / directory).rglob("*") if p.is_file()], []
                )
        self.assertFalse((ROOT / "build/update-cask.sh").exists())
        for path in (
            "build/linux/stalker-gamma.AppDir/AppRun",
            "build/linux/stalker-gamma-server.AppDir/AppRun",
        ):
            with self.subTest(path=path):
                self.assertTrue((ROOT / path).is_file())

    def test_make_dry_run_has_both_explicit_linux_rids(self):
        for arch in ("x64", "arm64"):
            with self.subTest(arch=arch):
                result = subprocess.run(
                    [
                        "make",
                        "--dry-run",
                        "-C",
                        str(ROOT / "build/linux"),
                        "build",
                        "appimage",
                        f"ARCH={arch}",
                        "VERSION=1.0.0",
                    ],
                    capture_output=True,
                    text=True,
                    check=False,
                    timeout=10,
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                publish = [
                    line
                    for line in result.stdout.splitlines()
                    if line.lstrip().startswith("dotnet publish ")
                ]
                self.assertEqual(len(publish), 2)
                for line in publish:
                    self.assertIn(f"-r linux-{arch}", line)

    def test_make_rejects_unsupported_architecture(self):
        result = subprocess.run(
            [
                "make",
                "--dry-run",
                "-C",
                str(ROOT / "build/linux"),
                "build",
                "ARCH=riscv64",
            ],
            capture_output=True,
            text=True,
            check=False,
            timeout=10,
        )
        self.assertNotEqual(result.returncode, 0, "Unsupported ARCH was accepted")
        self.assertIn("Unsupported ARCH", result.stderr)


class LinuxCiTests(unittest.TestCase):
    def step(self, name):
        match = re.search(
            rf"(?ms)^      - name: {re.escape(name)}\n(.*?)(?=^      - name:|\Z)",
            source(".github/workflows/build.yml"),
        )
        self.assertIsNotNone(match, f"Missing verification step: {name}")
        return match.group(1)

    def test_ci_is_manual_linux_only_and_non_publishing(self):
        text = source(".github/workflows/build.yml")
        event_section = re.search(r"(?ms)^on:\n(.*?)(?=^[^\s#])", text)
        self.assertIsNotNone(event_section)
        self.assertEqual(
            re.findall(r"^  ([\w-]+):", event_section.group(1), re.M),
            ["workflow_dispatch"],
        )
        self.assertIsNotNone(
            re.search(r"(?m)^permissions:\n  contents: read$", text),
            "Read-only workflow token permission missing",
        )
        jobs_section = text.split("\njobs:\n", 1)[1]
        self.assertEqual(
            re.findall(r"^  ([\w-]+):", jobs_section, re.M),
            ["verify"],
        )
        self.assertEqual(event_section.group(1).strip(), "workflow_dispatch:")
        self.assertRegex(text, r"(?m)^    timeout-minutes: 45$")
        self.assertIn("fail-fast: false", text)
        self.assertIn("shell: bash", text)
        self.assertIn("working-directory: /tmp/stalker-gamma-ci/source", text)
        for forbidden in (
            "windows-",
            "macos-",
            "uses:",
            "secrets.",
            "update-aur:",
            "update-chocolatey:",
            "update-homebrew:",
            "publish-release:",
            "action-gh-release",
            "git push",
            "inputs.",
            "inputs:",
            "setup-dotnet",
            "upload-artifact",
            "dotnet tool",
            "dotnet test",
            "apt-get ",
            "apt install",
            "pip install",
            "pip3 install",
            "npm install",
            "wget ",
            "curl ",
            "docker ",
            "podman ",
            "qemu",
            "make build",
            "appimage",
            "PublishAot=",
            "continue-on-error",
            "|| true",
        ):
            with self.subTest(symbol=forbidden):
                self.assertFalse(forbidden in text, f"Inherited CI behavior: {forbidden}")
        for runner, rid, host, dpkg, machine in (
            ("ubuntu-22.04", "linux-x64", "x86_64", "amd64", "Advanced Micro Devices X86-64"),
            ("ubuntu-22.04-arm", "linux-arm64", "aarch64", "arm64", "AArch64"),
        ):
            with self.subTest(rid=rid):
                self.assertIn(
                    f"- runner: {runner}\n"
                    f"            rid: {rid}\n"
                    f"            host_arch: {host}\n"
                    f"            dpkg_arch: {dpkg}\n"
                    f"            elf_machine: {machine}",
                    text,
                )
        for name, field in (
            ("RID", "rid"), ("EXPECTED_HOST_ARCH", "host_arch"),
            ("EXPECTED_DPKG_ARCH", "dpkg_arch"), ("EXPECTED_ELF_MACHINE", "elf_machine"),
        ):
            self.assertIn(f"{name}: ${{{{ matrix.{field} }}}}", text)
        names = re.findall(r"(?m)^      - name: (.+)$", text)
        self.assertEqual(
            names,
            [
                "Acquire fixed source and isolate paths", "Assert preinstalled prerequisites",
                "Check fixed snapshot source", "Restore regression runner",
                "Build regression runner", "Run six regression suites", "Restore CLI",
                "Evaluate and publish CLI", "Restore server", "Evaluate and publish server",
                "Log observed NuGet locks",
            ],
        )
        for name in names:
            self.assertIn("run: |\n          set -euo pipefail\n", self.step(name))

    def test_ci_acquires_only_the_fixed_public_source(self):
        text = source(".github/workflows/build.yml")
        self.assertIn("SOURCE_SHA: 71e9c8839064cb56e4761f04c1cfb9e8ba8ef642", text)
        for setting in (
            'GIT_TERMINAL_PROMPT: "0"', 'GIT_CONFIG_NOSYSTEM: "1"',
            "GIT_CONFIG_SYSTEM: /dev/null", "GIT_CONFIG_GLOBAL: /dev/null",
            "GIT_ASKPASS: /bin/false",
        ):
            self.assertIn(setting, text)
        block = self.step("Acquire fixed source and isolate paths")
        for line in (
            "working-directory: /tmp",
            'test "$GITHUB_REPOSITORY" = 4nibhal/stalker-gamma-cli',
            'test "$(uname -s)" = Linux',
            'test "$(uname -m)" = "$EXPECTED_HOST_ARCH"',
            "test ! -e /tmp/stalker-gamma-ci/source",
            "test ! -L /tmp/stalker-gamma-ci/source",
            "git init /tmp/stalker-gamma-ci/source",
            "git -C /tmp/stalker-gamma-ci/source remote add origin https://github.com/4nibhal/stalker-gamma-cli.git",
            'git -C /tmp/stalker-gamma-ci/source -c core.hooksPath=/dev/null fetch --depth=1 origin "$SOURCE_SHA"',
            "git -C /tmp/stalker-gamma-ci/source -c core.hooksPath=/dev/null checkout --detach FETCH_HEAD",
            'test "$(git -C /tmp/stalker-gamma-ci/source rev-parse HEAD)" = "$SOURCE_SHA"',
            'test -z "$(git -C /tmp/stalker-gamma-ci/source status --porcelain)"',
            '"$GITHUB_SHA"', "Source revision:", "Workflow revision:",
        ):
            self.assertIn(line, block)
        self.assertEqual(text.count(" fetch "), 1)

    def test_ci_uses_exact_preinstalled_sdk_and_isolated_nuget(self):
        text = source(".github/workflows/build.yml")
        for setting in (
            "NUGET_PACKAGES: /tmp/stalker-gamma-ci/nuget/packages",
            "NUGET_HTTP_CACHE_PATH: /tmp/stalker-gamma-ci/nuget/http-cache",
            "NUGET_PLUGINS_CACHE_PATH: /tmp/stalker-gamma-ci/nuget/plugins-cache",
            "DOTNET_CLI_HOME: /tmp/stalker-gamma-ci/dotnet-home",
            "TMPDIR: /tmp/stalker-gamma-ci/scratch",
            'DOTNET_CLI_TELEMETRY_OPTOUT: "1"',
            'DOTNET_SKIP_FIRST_TIME_EXPERIENCE: "1"',
            'DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE: "true"',
        ):
            self.assertIn(setting, text)
        acquisition = self.step("Acquire fixed source and isolate paths")
        for line in (
            "cat > /tmp/stalker-gamma-ci/source/global.json <<'JSON'",
            '"version": "10.0.401"', '"rollForward": "disable"',
            '"allowPrerelease": false',
            "cat > /tmp/stalker-gamma-ci/nuget/NuGet.Config <<'XML'",
            "<packageSources>", "<clear />",
            '<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />',
            "<fallbackPackageFolders><clear /></fallbackPackageFolders>",
            "<auditSources>",
        ):
            self.assertIn(line, acquisition)
        self.assertEqual(
            re.findall(r'<add key="[^"]+" value="([^"]+)"', acquisition),
            ["https://api.nuget.org/v3/index.json"] * 2,
        )
        prerequisites = self.step("Assert preinstalled prerequisites")
        for line in (
            "for tool in git python3 make file readelf dotnet clang ld dpkg dpkg-query; do",
            'command -v "$tool"', 'test "$(dotnet --version)" = 10.0.401',
            "dotnet --info", "dotnet --list-sdks",
            r"dotnet --list-runtimes | grep -E '^Microsoft.NETCore.App 10\.'",
            'test "$(dpkg --print-architecture)" = "$EXPECTED_DPKG_ARCH"',
            "for package in zlib1g-dev libc6-dev libssl-dev libicu-dev; do",
            "test \"$(dpkg-query -W -f='${Status}' \"$package\")\" = 'install ok installed'",
            "dpkg-query -W -f='${Package} ${Version} ${Architecture}\\n' \"$package\"",
            "test -r /usr/include/zlib.h", 'test -r "$(clang -print-file-name=libz.so)"',
            "clang --version", "ld --version", "Set up job",
        ):
            self.assertIn(line, prerequisites)

    def test_ci_runs_snapshot_checks_and_all_six_suites_without_implicit_builds(self):
        checks = self.step("Check fixed snapshot source")
        for line in (
            "PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -v",
            "git diff --check", "bash -n build/update-pkgbuild.sh",
            "sh -n build/linux/stalker-gamma.AppDir/AppRun",
            "sh -n build/linux/stalker-gamma-server.AppDir/AppRun",
        ):
            self.assertIn(line, checks)
        project = "tests/LinuxRegression/LinuxRegression.csproj"
        self.assertIn(
            f"dotnet restore {project} --configfile /tmp/stalker-gamma-ci/nuget/NuGet.Config --use-lock-file -p:Configuration=Release",
            self.step("Restore regression runner"),
        )
        self.assertIn(
            f"dotnet build {project} -c Release --no-restore --no-incremental -bl:/tmp/stalker-gamma-ci/out/runner.binlog",
            self.step("Build regression runner"),
        )
        run = self.step("Run six regression suites")
        self.assertIn(
            f"dotnet run --project {project} -c Release --no-build --no-restore --no-launch-profile 2>&1 | tee /tmp/stalker-gamma-ci/out/runner.log",
            run,
        )
        expected = [
            f'"Linux regression suite \'{name}\' passed."'
            for name in ("inputs", "scratch", "wine-ini", "records", "filesystem", "managed-tree")
        ]
        self.assertEqual(re.findall(r'"Linux regression suite \'[^\']+\' passed\."', run), expected)
        self.assertIn("if observed != expected:", run)
        self.assertIn('line.startswith("Linux regression suite \'")', run)

    def test_ci_evaluates_inherited_properties_before_native_publication(self):
        for name, directory, executable, properties in (
            ("CLI", "stalker-gamma-cli", "stalker-gamma", ("TargetFramework", "RuntimeIdentifier", "PublishAot", "SelfContained")),
            ("server", "stalker-gamma-cli-server", "stalker-gamma-server", ("TargetFramework", "RuntimeIdentifier", "PublishAot", "SelfContained", "PublishSingleFile")),
        ):
            with self.subTest(project=name):
                project = f"{directory}/{directory}.csproj"
                output = "cli" if name == "CLI" else "server"
                restore_flags = " -p:SelfContained=true" if name == "CLI" else ""
                self.assertIn(
                    f'dotnet restore {project} --configfile /tmp/stalker-gamma-ci/nuget/NuGet.Config --use-lock-file -r "$RID" -p:Configuration=Release{restore_flags}',
                    self.step(f"Restore {name}"),
                )
                block = self.step(f"Evaluate and publish {name}")
                evaluation = (
                    f'dotnet msbuild {project} -p:Configuration=Release -p:RuntimeIdentifier="$RID"{restore_flags}'
                    f' -getProperty:{",".join(properties)} > /tmp/stalker-gamma-ci/out/{output}-properties.json'
                )
                self.assertIn(evaluation, block)
                publish_flags = " --self-contained true" if name == "CLI" else ""
                publish = (
                    f'dotnet publish {project} -c Release -r "$RID"{publish_flags} --no-restore'
                    f' -o /tmp/stalker-gamma-ci/out/{output} -bl:/tmp/stalker-gamma-ci/out/{output}.binlog'
                )
                self.assertIn(publish, block)
                for line in (
                    '["Properties"]', '"TargetFramework": "net10.0"',
                    '"RuntimeIdentifier": os.environ["RID"]', '"SelfContained": "true"',
                    f'"PublishAot": "{"true" if name == "CLI" else "false"}"',
                    "if observed != expected:",
                    f"test -s /tmp/stalker-gamma-ci/out/{output}/{executable}",
                    f"test -x /tmp/stalker-gamma-ci/out/{output}/{executable}",
                    f"file /tmp/stalker-gamma-ci/out/{output}/{executable}",
                    f"readelf -h /tmp/stalker-gamma-ci/out/{output}/{executable} | tee /tmp/stalker-gamma-ci/out/{output}-elf.txt",
                    f'grep -Eq "^[[:space:]]*Machine:[[:space:]]+$EXPECTED_ELF_MACHINE[[:space:]]*$" /tmp/stalker-gamma-ci/out/{output}-elf.txt',
                ):
                    self.assertIn(line, block)
                self.assertLess(block.index("if observed != expected:"), block.index(publish))
                if name == "server":
                    self.assertIn('"PublishSingleFile": "true"', block)
                self.assertNotRegex(block, rf"(?m)^\s*(?:\./)?/tmp/stalker-gamma-ci/out/{output}/{executable}(?:\s|$)")
        locks = self.step("Log observed NuGet locks")
        for line in (
            "Observed NuGet locks (not verified trusted inputs)",
            'rglob("packages.lock.json")', "hashlib.sha256", "resolved", "contentHash",
        ):
            self.assertIn(line, locks)


class LinuxDocumentationTests(unittest.TestCase):
    def test_readme_identifies_the_fork_and_remote_build_policy(self):
        text = source("README.md")
        for required in (
            "https://github.com/4nibhal/stalker-gamma-cli",
            "Linux-only",
            "No local .NET SDK or C# compiler",
            "workflow_dispatch",
        ):
            with self.subTest(symbol=required):
                self.assertTrue(required in text, f"Fork/build policy missing: {required}")
        self.assertIsNone(
            re.search(r"Windows-Install|MacOS-Install|build/win|build/mac", text),
            "README still directs users to non-Linux host support",
        )

    def test_linux_usage_preserves_wine_and_fork_package_urls(self):
        text = source("build/linux/README.md")
        for required in ("ModOrganizer.exe", "Wine", "tar", "unzip"):
            with self.subTest(symbol=required):
                self.assertTrue(required in text, f"Linux usage requirement missing: {required}")
        package = source("build/update-pkgbuild.sh")
        self.assertTrue(
            "https://github.com/4nibhal/stalker-gamma-cli" in package,
            "Package generator does not identify the fork",
        )
        self.assertFalse(
            "https://github.com/FaithBeam/stalker-gamma-cli" in package,
            "Package generator still targets upstream releases",
        )
