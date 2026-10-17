using System.Text.Json;
using System.Text.Json.Serialization;
using Stalker.Gamma.Filesystem;

namespace Stalker.Gamma.Models;

public class ModPackMakerRecord : IEquatable<ModPackMakerRecord>
{
    [JsonIgnore]
    public int Counter { get; set; }
    public required string DlLink { get; set; }

    [JsonConverter(typeof(ModPackMakerInstructionConverter))]
    public InstructionSet Instructions { get; set; } = InstructionSet.Blank;

    [JsonConverter(typeof(ModPackMakerPatchConverter))]
    public string? Patch { get; set; }
    public string? AddonName { get; set; }
    public string? ModDbUrl { get; set; }
    public string? ZipName { get; set; }
    public string? Md5ModDb { get; set; }

    public bool Equals(ModPackMakerRecord? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return DlLink == other.DlLink
            && Instructions.Equals(other.Instructions)
            && Patch == other.Patch
            && AddonName == other.AddonName
            && ModDbUrl == other.ModDbUrl
            && ZipName == other.ZipName
            && Md5ModDb == other.Md5ModDb;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((ModPackMakerRecord)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            DlLink,
            Instructions,
            Patch,
            AddonName,
            ModDbUrl,
            ZipName,
            Md5ModDb
        );
    }
}

[JsonSerializable(typeof(ModPackMakerRecord))]
[JsonSerializable(typeof(List<ModPackMakerRecord>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
public partial class ModPackMakerCtx : JsonSerializerContext;

public class ModPackMakerInstructionConverter : JsonConverter<InstructionSet>
{
    public override bool HandleNull => true;

    public override InstructionSet Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        try
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return InstructionSet.FromJsonTokens(null);
            }
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new UnsafePathException("Invalid instruction token.");
            }

            List<string?> tokens = [];
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return InstructionSet.FromJsonTokens(tokens);
                }
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new UnsafePathException("Invalid instruction token.");
                }
                tokens.Add(reader.GetString());
            }
            throw new UnsafePathException("Invalid instruction token.");
        }
        catch (UnsafePathException error)
        {
            throw new JsonException("Invalid instruction.", error);
        }
        catch (JsonException)
        {
            throw new JsonException("Invalid instruction.", new UnsafePathException("Invalid instruction token."));
        }
    }

    public override void Write(Utf8JsonWriter writer, InstructionSet value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var token in (value ?? InstructionSet.Blank).ToJsonTokens())
        {
            writer.WriteStringValue(token);
        }
        writer.WriteEndArray();
    }
}

public class ModPackMakerPatchConverter : JsonConverter<string>
{
    public override bool HandleNull => true;

    public override string? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Invalid patch.", new UnsafePathException("Invalid patch token."));
        }
        var value = reader.GetString();
        try
        {
            // Validate the raw author before adding the display prefix; preserve its spelling.
            InputPolicy.OptionalComponent(value, "patch");
            return string.IsNullOrWhiteSpace(value) ? value : $"- {value}";
        }
        catch (UnsafePathException error)
        {
            throw new JsonException("Invalid patch.", error);
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStringValue(value.StartsWith("- ", StringComparison.Ordinal) ? value[2..] : value);
    }
}
