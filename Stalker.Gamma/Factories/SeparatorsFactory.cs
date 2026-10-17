using Stalker.Gamma.Filesystem;
using Stalker.Gamma.Models;

namespace Stalker.Gamma.Factories;

public interface ISeparatorsFactory
{
    List<ISeparator> Create(IList<ModPackMakerRecord> records);
}

public class SeparatorsFactory : ISeparatorsFactory
{
    public List<ISeparator> Create(IList<ModPackMakerRecord> records)
    {
        var snapshot = records.ToArray();
        var prepared = RecordPathPolicy.ValidateAll(snapshot);
        return snapshot
            .Select((record, index) => (record, index))
            .Where(item => prepared[item.index].Role == RecordRole.Separator)
            .Select(item =>
            {
                var (record, index) = item;
                return (ISeparator)new Separator
                {
                    Name = $"{record.DlLink} Separator",
                    FolderName = prepared[index].OutputDirectoryName!,
                };
            })
            .ToList();
    }
}
