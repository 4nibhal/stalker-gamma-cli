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
            ["lint", "version", "build"],
        )
        for forbidden in (
            "windows-",
            "macos-",
            "secrets.",
            "update-aur:",
            "update-chocolatey:",
            "update-homebrew:",
            "publish-release:",
            "action-gh-release",
            "git push",
            "inputs.dry_run",
        ):
            with self.subTest(symbol=forbidden):
                self.assertFalse(forbidden in text, f"Inherited CI behavior: {forbidden}")
        for required in (
            "ubuntu-22.04-arm",
            "ubuntu-22.04",
            "tests/LinuxRegression/LinuxRegression.csproj",
            "persist-credentials: false",
            "sha256sum",
            "SHA256SUMS-linux.",
        ):
            with self.subTest(symbol=required):
                self.assertTrue(required in text, f"Linux CI contract missing: {required}")
        self.assertEqual(
            len(re.findall(r"uses: actions/checkout@", text)),
            text.count("persist-credentials: false"),
        )


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
