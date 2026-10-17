using Stalker.Gamma.Filesystem;
using Stalker.Gamma.GammaInstallerServices;
using Stalker.Gamma.GammaInstallerServices.SpecialRepos;
using Stalker.Gamma.ModDb.Models;
using Stalker.Gamma.Models;
using Stalker.Gamma.Services;
using ModDbGetAddonMetadataService = Stalker.Gamma.ModDb.Services.ModDbGetAddonMetadataService;
using ModDbService = Stalker.Gamma.ModDb.Services.ModDbService;

namespace Stalker.Gamma.Factories;

public interface IDownloadableRecordFactory
{
    IDownloadableRecord CreateAnomalyRecord(string downloadDirectory, string anomalyDir);
    IDownloadableRecord CreateGammaSetupRecord(
        string gammaDir,
        string gammaSetupRepo,
        string gammaSetupBranch
    );
    IDownloadableRecord CreateGammaLargeFilesRecord(
        string gammaDir,
        string gammaLargeFilesRepo,
        string gammaLargeFilesBranch
    );
    IDownloadableRecord CreateStalkerGammaRecord(
        string gammaDir,
        string anomalyDir,
        string stalkerGammaRepo,
        string stalkerGammaBranch
    );
    IDownloadableRecord CreateTeivazAnomalyGunslingerRecord(
        string gammaDir,
        string teivazAnomalyGunslingerRepo,
        string teivazAnomalyGunslingerBranch
    );

    bool TryCreate(
        string gammaDir,
        ModPackMakerRecord record,
        out IDownloadableRecord? downloadableRecord
    );

    List<IDownloadableRecord> CreateGroupedDownloadableRecords(IList<IDownloadableRecord> records);

    IDownloadableRecord CreateSkippedRecord(IDownloadableRecord record);

    IDownloadableRecord CreateSkipExtractWhenNotDownloadedRecord(IDownloadableRecord record);
}

public class DownloadableRecordFactory(
    IHttpClientFactory httpClientFactory,
    GammaProgress gammaProgress,
    ArchiveService archiveService,
    GitService gitService,
    GetCanonicalLinkFromModDbStartLink getCanonicalLinkFromModDbStartLink,
    ModDbGetAddonMetadataService modDbGetAddonMetadataService,
    ModDbService modDbService
) : IDownloadableRecordFactory
{
    public IDownloadableRecord CreateSkippedRecord(IDownloadableRecord record) =>
        new SkippedRecord(gammaProgress, record);

    public IDownloadableRecord CreateSkipExtractWhenNotDownloadedRecord(
        IDownloadableRecord record
    ) => new SkipExtractWhenNotDownloadedRecord(gammaProgress, record);

    public IDownloadableRecord CreateAnomalyRecord(string downloadDirectory, string anomalyDir) =>
        new AnomalyInstaller(
            gammaProgress,
            downloadDirectory,
            anomalyDir,
            modDbService,
            archiveService
        );

    public IDownloadableRecord CreateGammaSetupRecord(
        string gammaDir,
        string gammaSetupRepo,
        string gammaSetupBranch
    ) => new GammaSetupRepo(gammaProgress, gammaDir, gammaSetupRepo, gammaSetupBranch, gitService);

    public IDownloadableRecord CreateGammaLargeFilesRecord(
        string gammaDir,
        string gammaLargeFilesRepo,
        string gammaLargeFilesBranch
    ) =>
        new GammaLargeFilesRepo(
            gammaProgress,
            gammaDir,
            gammaLargeFilesRepo,
            gammaLargeFilesBranch,
            gitService
        );

    public IDownloadableRecord CreateStalkerGammaRecord(
        string gammaDir,
        string anomalyDir,
        string stalkerGammaRepo,
        string stalkerGammaBranch
    ) =>
        new StalkerGammaRepo(
            gammaProgress,
            gammaDir,
            anomalyDir,
            stalkerGammaRepo,
            stalkerGammaBranch,
            gitService
        );

    public IDownloadableRecord CreateTeivazAnomalyGunslingerRecord(
        string gammaDir,
        string teivazAnomalyGunslingerRepo,
        string teivazAnomalyGunslingerBranch
    ) =>
        new TeivazAnomalyGunslingerRepo(
            gammaProgress,
            gammaDir,
            teivazAnomalyGunslingerRepo,
            teivazAnomalyGunslingerBranch,
            gitService
        );

    public List<IDownloadableRecord> CreateGroupedDownloadableRecords(
        IList<IDownloadableRecord> records
    ) =>
        [
            .. records
                .OfType<ModDbRecord>()
                .GroupBy(r => r.ArchiveName)
                .Select(r => new ModDbRecordGroup(gammaProgress, r.ToList())),
            .. records
                .OfType<ModDbRecordGetMetadata>()
                .GroupBy(r => r.StartLink)
                .Select(r => new ModDbRecordGetMetadataGroup(gammaProgress, r.ToList())),
            .. records
                .OfType<GithubRecord>()
                .GroupBy(r => r.ArchiveName)
                .Select(r => new GithubRecordGroup(gammaProgress, r.ToList())),
        ];

    public bool TryCreate(
        string gammaDir,
        ModPackMakerRecord record,
        out IDownloadableRecord? downloadableRecord
    )
    {
        downloadableRecord = null;
        var prepared = RecordPathPolicy.Prepare(record);
        switch (prepared.Role)
        {
            case RecordRole.StandardModDb:
                downloadableRecord = new ModDbRecord(
                    gammaProgress,
                    record.AddonName!,
                    record.DlLink,
                    record.ModDbUrl ?? record.DlLink,
                    prepared.ArchiveName!,
                    record.Md5ModDb,
                    gammaDir,
                    prepared.OutputDirectoryName!,
                    prepared.Instructions.Paths.ToList(),
                    archiveService,
                    modDbService
                );
                return true;
            case RecordRole.MetadataOnlyModDb:
                downloadableRecord = new ModDbRecordGetMetadata(
                    record.AddonName!,
                    record.DlLink,
                    prepared.Instructions.Paths.ToList(),
                    prepared.OutputDirectoryName!,
                    gammaDir,
                    archiveService,
                    gammaProgress,
                    modDbService,
                    getCanonicalLinkFromModDbStartLink,
                    modDbGetAddonMetadataService
                );
                return true;
            case RecordRole.Github:
                downloadableRecord = new GithubRecord(
                    gammaProgress,
                    record.AddonName!,
                    record.DlLink,
                    record.ModDbUrl ?? record.DlLink,
                    prepared.ArchiveName!,
                    record.Md5ModDb,
                    gammaDir,
                    prepared.OutputDirectoryName!,
                    prepared.Instructions.Paths.ToList(),
                    httpClientFactory,
                    archiveService
                );
                return true;
            default:
                return false;
        }
    }
}

public class DownloadableRecordFactoryException(string msg) : Exception(msg);
