using Stalker.Gamma.Filesystem;
using Stalker.Gamma.Models;

namespace Stalker.Gamma.Factories;

public interface IModListRecordFactory
{
    List<ModPackMakerRecord> Create(string modpackMakerTxt);
}

public class ModPackMakerRecordFactory : IModListRecordFactory
{
    public List<ModPackMakerRecord> Create(string modpackMakerTxt)
    {
        var lines = modpackMakerTxt.Split('\n');
        List<ModPackMakerRecord> records = [];
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (index < lines.Length - 1 && line.EndsWith('\r'))
            {
                line = line[..^1];
            }
            if (
                string.IsNullOrWhiteSpace(line)
                && !line.Any(character => char.IsControl(character) && character != '\t')
            )
            {
                continue;
            }

            var columns = line.Split('\t');
            records.Add(new ModPackMakerRecord
            {
                Counter = records.Count + 1,
                DlLink = columns[0],
                Instructions = InstructionSet.FromTsv(columns.ElementAtOrDefault(1)),
                Patch = columns.ElementAtOrDefault(2),
                AddonName = columns.ElementAtOrDefault(3),
                ModDbUrl = columns.ElementAtOrDefault(4),
                ZipName = columns.ElementAtOrDefault(5),
                Md5ModDb = columns.ElementAtOrDefault(6),
            });
        }

        // Inspect raw contributing fields for the complete list before existing display trims.
        RecordPathPolicy.ValidateAll(records);
        foreach (var record in records)
        {
            record.DlLink = record.DlLink.Trim();
            record.Patch = record.Patch?.Trim();
            record.AddonName = record.AddonName?.Trim();
            record.ModDbUrl = record.ModDbUrl?.Trim();
            record.ZipName = record.ZipName?.Trim();
            record.Md5ModDb = record.Md5ModDb?.Trim();
        }
        return records;
    }
}
