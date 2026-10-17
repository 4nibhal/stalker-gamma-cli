"""Source contracts only; these checks do not compile or execute C#."""

from pathlib import Path
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
