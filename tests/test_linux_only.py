"""Source contracts only; these checks do not compile or execute C#."""

from pathlib import Path
import re
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
