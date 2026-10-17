using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Stalker.Gamma.Filesystem;

public sealed class InstructionSet : IEquatable<InstructionSet>
{
    private const string InstructionField = "instruction";
    private readonly ReadOnlyCollection<string> _paths;

    private InstructionSet(bool isBlank, IEnumerable<string> paths)
    {
        IsBlank = isBlank;
        _paths = Array.AsReadOnly(paths.ToArray());
    }

    public bool IsBlank { get; }
    public IReadOnlyList<string> Paths => _paths;
    public static InstructionSet Blank { get; } = new(true, Array.Empty<string>());

    public static InstructionSet FromTsv(string? value)
    {
        if (value is null)
        {
            return Blank;
        }
        var text = InputPolicy.PrepareValue(value, InstructionField);
        if (text.Length == 0)
        {
            return Blank;
        }

        // Reject ambiguous drive-like entries before ':' can become a list delimiter.
        if (Regex.IsMatch(text, @"(?:^|:)\s*[A-Za-z]:", RegexOptions.CultureInvariant))
        {
            throw new UnsafePathException("Invalid instruction.");
        }
        return FromJsonTokens(text.Split(':'));
    }

    public static InstructionSet FromJsonTokens(IEnumerable<string?>? tokens)
    {
        if (tokens is null)
        {
            return Blank;
        }

        // Retain raw marker spelling: a path such as "0/" is not the wire sentinel "0".
        var wire = tokens.Select(token => InputPolicy.PrepareValue(token, InstructionField)).ToArray();
        // Validate every member before interpreting singleton markers.
        var canonical = wire
            .Where(token => token.Length != 0)
            .Select(token => InputPolicy.RequireRelativePath(token, InstructionField))
            .ToArray();
        if (wire.Length == 1)
        {
            if (wire[0].Length == 0)
            {
                return Blank;
            }
            if (wire[0] == "0")
            {
                return new InstructionSet(false, Array.Empty<string>());
            }
        }
        return new InstructionSet(false, canonical);
    }

    public static InstructionSet FromParsed(IEnumerable<string?>? paths)
    {
        if (paths is null)
        {
            return new InstructionSet(false, Array.Empty<string>());
        }

        // Parsed entries are executable paths, not blank/zero wire markers.
        return new InstructionSet(
            false,
            paths.Select(path =>
                InputPolicy.RequireRelativePath(
                    path ?? throw new UnsafePathException("Invalid instruction."),
                    InstructionField
                )
            )
        );
    }

    public IReadOnlyList<string> ToJsonTokens()
    {
        if (IsBlank)
        {
            return Array.AsReadOnly(new[] { "" });
        }
        if (_paths.Count == 1 && _paths[0] == "0")
        {
            return Array.AsReadOnly(new[] { "0", "" });
        }
        return _paths;
    }

    public bool Equals(InstructionSet? other) =>
        other is not null
        && IsBlank == other.IsBlank
        && _paths.SequenceEqual(other._paths, StringComparer.Ordinal);

    public override bool Equals(object? obj) => obj is InstructionSet other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IsBlank);
        foreach (var path in _paths)
        {
            hash.Add(path, StringComparer.Ordinal);
        }
        return hash.ToHashCode();
    }
}
