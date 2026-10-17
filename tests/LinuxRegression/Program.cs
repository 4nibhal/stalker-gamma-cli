using LinuxRegression;

if (!OperatingSystem.IsLinux())
{
    throw new PlatformNotSupportedException("Linux regression runner only.");
}

var suites = new (string Name, Func<Task> Run)[]
{
    ("inputs", InputPolicyCases.RunAsync),
    ("scratch", ScratchTreeCases.RunAsync),
    ("wine-ini", WineIniCases.RunAsync),
    ("records", PathPolicyCases.RunAsync),
    ("filesystem", FileSystemBoundaryCases.RunAsync),
    ("managed-tree", ManagedTreeCases.RunAsync),
};

var selected = new List<(string Name, Func<Task> Run)>();
if (args.Length == 0)
{
    selected.AddRange(suites);
}
else
{
    foreach (var name in args)
    {
        var index = Array.FindIndex(
            suites,
            suite => string.Equals(suite.Name, name, StringComparison.Ordinal)
        );
        if (index < 0)
        {
            throw new ArgumentException("Unknown Linux regression suite.", nameof(args));
        }
        selected.Add(suites[index]);
    }
}

foreach (var suite in selected)
{
    await suite.Run();
    Console.WriteLine($"Linux regression suite '{suite.Name}' passed.");
}
