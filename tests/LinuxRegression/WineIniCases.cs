using Stalker.Gamma.ModOrganizer;

namespace LinuxRegression;

internal static class WineIniCases
{
    internal static Task RunAsync()
    {
        var scratch = new ScratchTree();
        return Check.WithCleanupAsync(async () =>
        {
            var gamma = scratch.Dir("GAMMA ñ space");
            var anomaly = scratch.Dir("Anomaly d'Artagnan");
            await WriteModOrganizerIni.WriteAsync(gamma, anomaly, "v2.5.2", []);
            var ini = await System.IO.File.ReadAllTextAsync(Path.Join(gamma, "ModOrganizer.ini"));
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
                Check.True(ini.Contains(expected, StringComparison.Ordinal),
                    $"Missing expected INI value: {expected}");
            }
        }, scratch.Dispose);
    }
}
