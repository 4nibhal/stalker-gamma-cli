using System.Text;
using Stalker.Gamma.Filesystem;

namespace Stalker.Gamma.GammaInstallerServices;

public static class WriteAddonMetaIni
{
    public static void Write(string extractPath, string archiveName, string niceUrl)
    {
        archiveName = InputPolicy.RequireComponent(archiveName, "archive name");
        var boundary = new FileSystemBoundary(extractPath, Directory.GetCurrentDirectory());
        boundary.CheckPath(boundary.Root, ExpectedEntry.Directory);
        var meta = boundary.CheckPath(Path.Join(boundary.Root, "meta.ini"), ExpectedEntry.NonDirectory,
            MissingPolicy.Leaf);
        File.WriteAllText(
            meta.FullPath,
            $"""
            [General]
            gameName=stalkeranomaly
            modid=0
            ignoredversion={archiveName}
            version={archiveName}
            newestversion={archiveName}
            category="-1,"
            nexusFileStatus=1
            installationFile={archiveName}
            repository=
            comments=
            notes=
            nexusDescription=
            url={niceUrl}
            hasCustomURL=true
            lastNexusQuery=
            lastNexusUpdate=
            nexusLastModified=2021-11-09T18:10:18Z
            converted=false
            validated=false
            color=@Variant(\0\0\0\x43\0\xff\xff\0\0\0\0\0\0\0\0)
            tracked=0

            [installedFiles]
            1\modid=0
            1\fileid=0
            size=1

            """.ReplaceLineEndings("\r\n"),
            encoding: Encoding.UTF8
        );
    }
}
