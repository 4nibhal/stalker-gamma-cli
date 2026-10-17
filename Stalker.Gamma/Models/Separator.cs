using Stalker.Gamma.Filesystem;

namespace Stalker.Gamma.Models;

public interface ISeparator
{
    string Name { get; init; }
    string FolderName { get; init; }
    Task WriteAsync(string gammaPath);
}

public class Separator : ISeparator
{
    public required string Name { get; init; }
    public required string FolderName { get; init; }

    public async Task WriteAsync(string gammaPath)
    {
        // FolderName is a filesystem component; Name remains a display label.
        var folder = InputPolicy.RequireComponent(FolderName, "separator folder");
        var boundary = new FileSystemBoundary(gammaPath, Directory.GetCurrentDirectory());
        boundary.CheckPath(boundary.Root, ExpectedEntry.Directory, MissingPolicy.Suffix);
        var mods = boundary.CheckPath(boundary.Resolve("mods"), ExpectedEntry.Directory, MissingPolicy.Suffix);
        var selected = boundary.CheckPath(Path.Join(mods.FullPath, folder), ExpectedEntry.Directory, MissingPolicy.Suffix);
        var meta = boundary.CheckPath(Path.Join(selected.FullPath, "meta.ini"), ExpectedEntry.NonDirectory,
            MissingPolicy.Suffix);
        // All known targets have been checked before the first creation. These
        // near-I/O checks still do not make concurrent replacement race safe.
        selected = boundary.CheckPath(selected.FullPath, ExpectedEntry.Directory, MissingPolicy.Suffix);
        Directory.CreateDirectory(selected.FullPath);
        meta = boundary.CheckPath(meta.FullPath, ExpectedEntry.NonDirectory, MissingPolicy.Leaf);
        await File.WriteAllTextAsync(
            meta.FullPath,
            SeparatorMetaIni.ReplaceLineEndings("\r\n")
        );
    }

    private const string SeparatorMetaIni = """
        [General]
        modid=0
        version=
        newestVersion=
        category=0
        installationFile=

        [installedFiles]
        size=0

        """;
}
