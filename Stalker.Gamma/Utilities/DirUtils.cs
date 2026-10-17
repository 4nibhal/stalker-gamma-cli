using System.Security.Cryptography;
using Stalker.Gamma.Filesystem;

namespace Stalker.Gamma.Utilities;

public static class DirUtils
{
    public static void NormalizePermissions(string dir)
    {
        NormalizePermissions(dir, default);
    }

    internal static void NormalizePermissions(string dir, CancellationToken ct)
    {
        var tree = PrepareDirectoryTree(dir, ct);
        NormalizePermissions(tree, ct);
    }

    internal static void NormalizePermissions(CheckedTree tree, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var skipped = new List<string>();
        var filesByParent = tree.Entries.Where(entry => entry.Kind == EntryKind.NonDirectory)
            .ToLookup(entry => Path.GetDirectoryName(entry.FullPath)!, StringComparer.Ordinal);
        foreach (var planned in tree.Entries.Where(entry => entry.Kind == EntryKind.Directory)
            .OrderBy(entry => ManagedCopyPlan.Depth(entry.FullPath))
            .ThenBy(entry => entry.FullPath, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            if (skipped.Any(parent => ManagedCopyPlan.IsAtOrBelow(planned.FullPath, parent)))
                continue;
            // Throwing inspection is outside the mutation-only permission
            // catches. A reparse/inspection failure must never become a skip.
            var attributes = ReadCheckedAttributes(tree, planned, ct);
            var directory = tree.Boundary.CheckPath(planned.FullPath, ExpectedEntry.Directory,
                MissingPolicy.Reject, ct);
            try
            {
                File.SetAttributes(directory.FullPath, attributes & ~FileAttributes.ReadOnly);
            }
            catch (UnauthorizedAccessException)
            {
                skipped.Add(directory.FullPath);
                continue;
            }
            directory = tree.Boundary.CheckPath(directory.FullPath, ExpectedEntry.Directory,
                MissingPolicy.Reject, ct);
            try
            {
                // Deliberately retain the existing directory mode policy.
                File.SetUnixFileMode(directory.FullPath,
                    UnixFileMode.UserRead
                    | UnixFileMode.UserWrite
                    | UnixFileMode.UserExecute
                    | UnixFileMode.GroupRead
                    | UnixFileMode.GroupExecute
                    | UnixFileMode.GroupWrite
                    | UnixFileMode.OtherRead
                    | UnixFileMode.OtherExecute);
            }
            catch (UnauthorizedAccessException)
            {
                skipped.Add(directory.FullPath);
                continue;
            }
            foreach (var file in filesByParent[directory.FullPath])
            {
                ct.ThrowIfCancellationRequested();
                var fileAttributes = ReadCheckedAttributes(tree, file, ct);
                var current = tree.Boundary.CheckPath(file.FullPath, ExpectedEntry.NonDirectory,
                    MissingPolicy.Reject, ct);
                try
                {
                    File.SetAttributes(current.FullPath, fileAttributes & ~FileAttributes.ReadOnly);
                }
                catch (UnauthorizedAccessException)
                {
                    // Preserve skipping an individual unwritable file only.
                }
            }
        }
        ct.ThrowIfCancellationRequested();
    }

    public static void RecursivelyDeleteDirectory(string dir, IReadOnlyList<string> doNotMatch) =>
        RecursivelyDeleteDirectory(dir, doNotMatch, default);

    internal static void RecursivelyDeleteDirectory(
        string dir, IReadOnlyList<string> doNotMatch, CancellationToken ct)
    {
        var tree = PrepareDirectoryTree(dir, ct, MissingPolicy.Reject);
        var selected = PrepareCleanup(tree, doNotMatch, ct);
        ExecuteCleanup(tree, selected, ct);
    }

    internal static IReadOnlyList<CheckedEntry> PrepareCleanup(
        CheckedTree tree, IReadOnlyList<string> doNotMatch, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(doNotMatch);
        var retainedNames = doNotMatch.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedDirectories = tree.Entries.Where(entry => entry.Kind == EntryKind.Directory
            && string.Equals(Path.GetDirectoryName(entry.FullPath), tree.StartPath, StringComparison.Ordinal)
            && !retainedNames.Contains(Path.GetFileName(entry.FullPath)))
            .Select(entry => entry.FullPath).ToHashSet(StringComparer.Ordinal);
        var selected = new List<CheckedEntry>();
        foreach (var entry in tree.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.FullPath == tree.StartPath || entry.Kind == EntryKind.Missing)
                continue;
            var relative = Path.GetRelativePath(tree.StartPath, entry.FullPath);
            var firstSeparator = relative.IndexOf(Path.DirectorySeparatorChar);
            var topName = firstSeparator < 0 ? relative : relative[..firstSeparator];
            if (removedDirectories.Contains(Path.Join(tree.StartPath, topName))
                || (entry.Kind == EntryKind.NonDirectory && firstSeparator < 0
                    && Path.GetFileName(entry.FullPath).StartsWith(".", StringComparison.Ordinal)))
                selected.Add(entry);
        }
        var result = Array.AsReadOnly(selected.OrderByDescending(entry => ManagedCopyPlan.Depth(entry.FullPath))
            .ThenBy(entry => entry.FullPath, StringComparer.Ordinal).ToArray());
        ct.ThrowIfCancellationRequested();
        return result;
    }

    internal static void ExecuteCleanup(
        CheckedTree tree, IReadOnlyList<CheckedEntry> selected, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        foreach (var entry in selected)
        {
            ct.ThrowIfCancellationRequested();
            var current = tree.Boundary.CheckPath(entry.FullPath,
                entry.Kind == EntryKind.Directory ? ExpectedEntry.Directory : ExpectedEntry.NonDirectory,
                MissingPolicy.Reject, ct);
            if (current.Kind == EntryKind.Directory)
                Directory.Delete(current.FullPath);
            else
                File.Delete(current.FullPath);
        }
        ct.ThrowIfCancellationRequested();
    }

    public static void CopyDirectory(
        string sourceDir,
        string destDir,
        bool overwrite = true,
        string? fileFilter = null,
        Action<double>? onProgress = null,
        Action<string>? txtProgress = null,
        bool moveFile = false,
        CancellationToken cancellationToken = default
    )
    {
        var plan = PrepareCopy(sourceDir, destDir, overwrite, fileFilter, moveFile, cancellationToken);
        ExecuteCopy(plan, onProgress, txtProgress, cancellationToken);
    }

    internal static ManagedCopyPlan PrepareCopy(
        string sourceDir, string destDir, bool overwrite, string? fileFilter,
        bool moveFile, CancellationToken ct) =>
        ManagedCopyPlan.Prepare(sourceDir, destDir, overwrite, fileFilter, moveFile, ct);

    internal static void ExecuteCopy(
        ManagedCopyPlan plan, Action<double>? onProgress, Action<string>? txtProgress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (plan.IsNoOp)
        {
            ct.ThrowIfCancellationRequested();
            return;
        }
        var copiedFiles = 0;
        ReportText(txtProgress, $"""
            sourceDir = {plan.SourceBoundary.Root}
            destDir = {plan.DestinationBoundary.Root}
            overwrite = {plan.Overwrite}
            fileFilter = {plan.FileFilter}
            copiedFiles = {copiedFiles}
            totalFiles = {plan.TotalFiles}
            """, ct);
        ReportText(txtProgress, $"Copying {plan.SourceBoundary.Root} to {plan.DestinationBoundary.Root}", ct);
        foreach (var mapping in plan.Directories)
        {
            ct.ThrowIfCancellationRequested();
            plan.SourceBoundary.CheckPath(mapping.Source.FullPath, ExpectedEntry.Directory, MissingPolicy.Reject, ct);
            var destination = plan.DestinationBoundary.CheckPath(mapping.Destination.FullPath,
                ExpectedEntry.Directory, MissingPolicy.Suffix, ct);
            Directory.CreateDirectory(destination.FullPath);
            ReportText(txtProgress, $"Created {destination.FullPath}", ct);
        }
        foreach (var mapping in plan.Files)
        {
            ct.ThrowIfCancellationRequested();
            var source = plan.SourceBoundary.CheckPath(mapping.Source.FullPath,
                ExpectedEntry.NonDirectory, MissingPolicy.Reject, ct);
            if (txtProgress is not null)
            {
                plan.DestinationBoundary.CheckPath(mapping.Destination.FullPath,
                    ExpectedEntry.NonDirectory, MissingPolicy.Leaf, ct);
                string hash;
                using (var stream = File.OpenRead(source.FullPath))
                    hash = Convert.ToHexString(SHA256.HashData(stream));
                ct.ThrowIfCancellationRequested();
                ReportText(txtProgress, $"Copying {source.FullPath}, SHA256 = {hash}", ct);
            }
            var target = plan.DestinationBoundary.CheckPath(mapping.Destination.FullPath,
                ExpectedEntry.NonDirectory, MissingPolicy.Leaf, ct);
            if (target.Kind == EntryKind.NonDirectory)
            {
                ReportText(txtProgress, $"File {target.FullPath} already exists", ct);
                if (!plan.Overwrite)
                {
                    ct.ThrowIfCancellationRequested();
                    continue;
                }
                ReportText(txtProgress, $"Overwriting {target.FullPath}", ct);
            }
            else
                ReportText(txtProgress, $"Copying {mapping.Source.FullPath} to {target.FullPath}", ct);

            // Recheck after callbacks too. Checked entries are immutable state
            // evidence, NOT authority to use stale paths or a race-safe handle.
            source = plan.SourceBoundary.CheckPath(mapping.Source.FullPath,
                ExpectedEntry.NonDirectory, MissingPolicy.Reject, ct);
            var destination = plan.DestinationBoundary.CheckPath(mapping.Destination.FullPath,
                ExpectedEntry.NonDirectory, MissingPolicy.Leaf, ct);
            ct.ThrowIfCancellationRequested();
            if (plan.MoveFile)
                File.Move(source.FullPath, destination.FullPath, plan.Overwrite);
            else
                File.Copy(source.FullPath, destination.FullPath, plan.Overwrite);
            copiedFiles++;
            ReportProgress(onProgress, (double)copiedFiles / plan.TotalFiles, ct);
        }
        ct.ThrowIfCancellationRequested();
    }

    internal static int CountFiles(string sourceDir, string? fileFilter, CancellationToken ct = default)
    {
        var tree = PrepareDirectoryTree(sourceDir, ct, MissingPolicy.Reject);
        var directories = ManagedCopyPlan.SelectDirectories(tree, ct);
        var files = ManagedCopyPlan.SelectFiles(tree, directories, fileFilter, ct);
        ct.ThrowIfCancellationRequested();
        return files.Count;
    }

    internal static CheckedTree PrepareDirectoryTree(
        string dir, CancellationToken ct, MissingPolicy missing = MissingPolicy.Suffix)
    {
        ct.ThrowIfCancellationRequested();
        var boundary = new FileSystemBoundary(dir, Directory.GetCurrentDirectory());
        return boundary.CheckTree(boundary.Root, ExpectedEntry.Directory, missing, ct);
    }

    private static FileAttributes ReadCheckedAttributes(CheckedTree tree, CheckedEntry entry, CancellationToken ct)
    {
        var current = tree.Boundary.CheckPath(entry.FullPath,
            entry.Kind == EntryKind.Directory ? ExpectedEntry.Directory : ExpectedEntry.NonDirectory,
            MissingPolicy.Reject, ct);
        try
        {
            var attributes = File.GetAttributes(current.FullPath);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new UnsafePathException("Linked filesystem entries are not allowed.");
            ct.ThrowIfCancellationRequested();
            return attributes;
        }
        catch (Exception error) when (!OperationFailure.IsCancellation(error) && !UnsafePathException.IsUnsafe(error))
        {
            throw new UnsafePathException("Cannot inspect filesystem attributes.", error);
        }
    }

    private static void ReportText(Action<string>? callback, string text, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        callback?.Invoke(text);
        ct.ThrowIfCancellationRequested();
    }

    private static void ReportProgress(Action<double>? callback, double progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        callback?.Invoke(progress);
        ct.ThrowIfCancellationRequested();
    }
}
