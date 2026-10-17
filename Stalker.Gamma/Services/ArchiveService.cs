using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.Services;

public class ArchiveService(SevenZipService sevenZipService, UnzipService unzipService)
{
    public async Task ExtractAsync(
        string archivePath,
        string destinationDir,
        Action<double> pct,
        CancellationToken ct
    )
    {
        await using var fs = File.OpenRead(archivePath);
        fs.Seek(0, SeekOrigin.Begin);
        if (_archiveMappings.TryGetValue(fs.ReadByte(), out var extractFunc))
        {
            try
            {
                await extractFunc.Invoke(
                    new ArchiveMappingArgs(archivePath, destinationDir, pct, ct)
                );
            }
            finally
            {
                // Permissions are a pain in my ass
                DirUtils.NormalizePermissions(destinationDir);
            }
        }
        else
        {
            throw new ArchiveUtilityException(
                $"""
                Unsupported archive type
                Archive: {archivePath}
                """
            );
        }
    }

    private record ArchiveMappingArgs(
        string ArchivePath,
        string DestinationDir,
        Action<double> Pct,
        CancellationToken Ct = default
    );

    private readonly Dictionary<int, Func<ArchiveMappingArgs, Task>> _archiveMappings = new()
    {
        {
            // zstd
            // linux -> 7z
            0x28,
            async args =>
                await sevenZipService.ExtractAsync(
                    args.ArchivePath,
                    args.DestinationDir,
                    args.Pct,
                    cancellationToken: args.Ct
                )
        },
        {
            // 7z
            // linux -> 7z
            0x37,
            async args =>
                await sevenZipService.ExtractAsync(
                    args.ArchivePath,
                    args.DestinationDir,
                    args.Pct,
                    cancellationToken: args.Ct
                )
        },
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
        {
            // rar
            // linux -> 7z
            0x52,
            async args =>
                await sevenZipService.ExtractAsync(
                    args.ArchivePath,
                    args.DestinationDir,
                    args.Pct,
                    cancellationToken: args.Ct
                )
        },
    };
}

public class ArchiveUtilityException(string message) : Exception(message);
