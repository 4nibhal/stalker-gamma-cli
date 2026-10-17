namespace Stalker.Gamma.Utilities;

public static class CreateSymbolicLinkUtility
{
    public static void Create(string path, string pathToTarget)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateSymbolicLink(path, pathToTarget);
        }
    }
}
