namespace Stalker.Gamma.Filesystem;

public static class InputPolicy
{
    public static string RequireComponent(string? value, string field)
    {
        var component = PrepareValue(value, field);
        if (
            component.Length == 0
            || component is "." or ".."
            || component.Contains('/')
            || component.Contains('\\')
        )
        {
            throw Invalid(field);
        }
        return component;
    }

    public static string? OptionalComponent(string? value, string field)
    {
        if (value is null)
        {
            return null;
        }
        var component = PrepareValue(value, field);
        return component.Length == 0 ? null : RequireComponent(value, field);
    }

    public static string RequireRelativePath(string value, string field)
    {
        var relative = PrepareValue(value, field);
        if (relative.Length == 0)
        {
            throw Invalid(field);
        }

        var components = relative.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var component in components)
        {
            // Validate each segment without trimming spaces inside the overall path.
            RequireComponent(component, field);
        }
        return string.Join('/', components);
    }

    // Wire ingress needs the same raw checks even for blank or no-op markers.
    internal static string PrepareValue(string? value, string field)
    {
        if (value is null)
        {
            throw Invalid(field);
        }
        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                throw Invalid(field);
            }
        }

        var first = 0;
        while (first < value.Length && char.IsWhiteSpace(value[first]))
        {
            first++;
        }
        if (first < value.Length)
        {
            var initial = value[first];
            var drivePrefix =
                first + 1 < value.Length
                && (initial is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                && value[first + 1] == ':';
            if ((initial is '/' or '\\') || drivePrefix)
            {
                throw Invalid(field);
            }
        }

        return value.Trim();
    }

    private static UnsafePathException Invalid(string field) => new($"Invalid {field}.");
}
