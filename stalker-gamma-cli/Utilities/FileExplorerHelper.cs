using System.Diagnostics;

namespace stalker_gamma_cli.Utilities;

public static class FileExplorerHelper
{
    public static void OpenFolderInExplorer(string path)
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Directory not found: {path}");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "xdg-open",
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(Path.GetFullPath(path));
        Process.Start(startInfo);
    }
}
