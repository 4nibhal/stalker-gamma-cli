using Stalker.Gamma.Models;

namespace Stalker.Gamma.Filesystem;

internal enum RecordRole
{
    Unsupported,
    Separator,
    StandardModDb,
    MetadataOnlyModDb,
    Github,
}

internal sealed record PreparedRecord(
    RecordRole Role,
    InstructionSet Instructions,
    string? OutputDirectoryName,
    string? ArchiveName
);

internal static class RecordPathPolicy
{
    internal static PreparedRecord Prepare(ModPackMakerRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var instructions = record.Instructions ?? throw new UnsafePathException("Invalid instruction.");
        // A supplied archive is checked even when the selected role does not use it.
        var archive = InputPolicy.OptionalComponent(record.ZipName, "archive");
        var moddb = record.DlLink?.Contains("moddb") == true;
        RecordRole role;
        if (
            moddb
            && !string.IsNullOrWhiteSpace(record.AddonName)
            && !string.IsNullOrWhiteSpace(record.Patch)
            && archive is not null
        )
        {
            role = RecordRole.StandardModDb;
        }
        else if (moddb && !string.IsNullOrWhiteSpace(record.AddonName) && archive is null)
        {
            role = RecordRole.MetadataOnlyModDb;
        }
        else if (record.DlLink?.Contains("github") == true)
        {
            role = RecordRole.Github;
        }
        else if (
            string.IsNullOrWhiteSpace(record.AddonName)
            && record.Instructions.IsBlank
            && string.IsNullOrWhiteSpace(record.Md5ModDb)
            && archive is null
            && string.IsNullOrWhiteSpace(record.ModDbUrl)
            && string.IsNullOrWhiteSpace(record.Patch)
        )
        {
            role = RecordRole.Separator;
        }
        else
        {
            role = RecordRole.Unsupported;
        }

        if (role == RecordRole.Unsupported)
        {
            return new PreparedRecord(role, instructions, null, null);
        }
        if (role == RecordRole.Separator)
        {
            var label = InputPolicy.RequireComponent(record.DlLink, "separator label");
            // List position is added only by ValidateAll, never from record.Counter.
            var directory = InputPolicy.RequireComponent($"{label}_separator", "separator directory");
            return new PreparedRecord(role, instructions, directory, null);
        }

        // Check every raw contributor before its numeric/display decoration can mask it.
        var addon = InputPolicy.RequireComponent(record.AddonName, "addon");
        var patch = role == RecordRole.MetadataOnlyModDb
            ? InputPolicy.OptionalComponent(record.Patch, "patch")
            : InputPolicy.RequireComponent(record.Patch, "patch");
        if (role == RecordRole.Github)
        {
            // Retain the existing link/host selection and repository segment convention.
            var segments = record.DlLink!.Split('/');
            var repository = InputPolicy.RequireComponent(segments.ElementAtOrDefault(4), "repository");
            archive = InputPolicy.RequireComponent($"{repository}.zip", "archive");
        }
        var output = InputPolicy.RequireComponent($"{record.Counter}- {addon} {patch}", "output directory");
        return new PreparedRecord(role, instructions, output, archive);
    }

    internal static IReadOnlyList<PreparedRecord> ValidateAll(IReadOnlyList<ModPackMakerRecord> records)
    {
        var prepared = new PreparedRecord[records.Count];
        for (var index = 0; index < records.Count; index++)
        {
            var item = Prepare(records[index]);
            if (item.Role == RecordRole.Separator)
            {
                item = item with
                {
                    OutputDirectoryName = InputPolicy.RequireComponent(
                        $"{index + 1}- {item.OutputDirectoryName}",
                        "separator directory"
                    ),
                };
            }
            prepared[index] = item;
        }
        return Array.AsReadOnly(prepared);
    }
}
