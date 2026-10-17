using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices;

internal static class CleanExtractPath
{
    internal static void Clean(string extractPath, CancellationToken cancellationToken = default)
    {
        // One complete operation-owned inspection covers chmod AND selection,
        // including retained content, before either operation can mutate it.
        var tree = DirUtils.PrepareDirectoryTree(extractPath, cancellationToken);
        var selected = DirUtils.PrepareCleanup(tree, DoNotMatch, cancellationToken);
        DirUtils.NormalizePermissions(tree, cancellationToken);
        DirUtils.ExecuteCleanup(tree, selected, cancellationToken);
    }

    private static readonly IReadOnlyList<string> DoNotMatch =
    [
        "gamedata",
        "appdata",
        "db",
        "fomod",
    ];
}
