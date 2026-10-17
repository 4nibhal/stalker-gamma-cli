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
