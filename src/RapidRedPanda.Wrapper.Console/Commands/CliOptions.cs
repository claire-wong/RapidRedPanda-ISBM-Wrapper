namespace RapidRedPanda.Wrapper.Console.Commands;

internal sealed class CliOptions
{
    private readonly Dictionary<string, string?> _values;

    private CliOptions(string command, Dictionary<string, string?> values)
    {
        Command = command;
        _values = values;
    }

    public string Command { get; }

    public bool Raw => _values.ContainsKey("--raw");

    public static CliOptions Parse(string[] args)
    {
        var command = args.Length > 0 ? args[0] : "";
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);

        for (var index = 1; index < args.Length; index++)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (token == "--raw")
            {
                values[token] = "true";
                continue;
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[token] = null;
                continue;
            }

            values[token] = args[index + 1];
            index++;
        }

        return new CliOptions(command, values);
    }

    public string? Get(string name)
    {
        return _values.TryGetValue(name, out var value) ? value : null;
    }

    public bool Has(string name)
    {
        return _values.ContainsKey(name);
    }
}
