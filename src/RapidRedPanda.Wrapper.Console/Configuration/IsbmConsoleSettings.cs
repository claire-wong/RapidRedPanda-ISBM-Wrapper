using System.Text.Json;

namespace RapidRedPanda.Wrapper.Console.Configuration;

internal sealed class IsbmConsoleSettings
{
    public string Host { get; private init; } = "";
    public string Channel { get; private init; } = "";
    public string Topic { get; private init; } = "";
    public string User { get; private init; } = "";
    public string Password { get; private init; } = "";
    public bool IncludeRawDefault { get; private init; }

    public static IsbmConsoleSettings Empty { get; } = new();

    public static IsbmConsoleSettings Load()
    {
        var settings = Empty;
        foreach (var path in GetCandidatePaths("appsettings.json"))
        {
            settings = settings.Merge(Read(path));
        }

        foreach (var path in GetCandidatePaths("appsettings.Development.json"))
        {
            settings = settings.Merge(Read(path));
        }

        return settings.ApplyEnvironment();
    }

    private IsbmConsoleSettings Merge(IsbmConsoleSettings other)
    {
        return new IsbmConsoleSettings
        {
            Host = Pick(other.Host, Host),
            Channel = Pick(other.Channel, Channel),
            Topic = Pick(other.Topic, Topic),
            User = Pick(other.User, User),
            Password = Pick(other.Password, Password),
            IncludeRawDefault = other.IncludeRawDefault || IncludeRawDefault
        };
    }

    private IsbmConsoleSettings ApplyEnvironment()
    {
        return new IsbmConsoleSettings
        {
            Host = Pick(Environment.GetEnvironmentVariable("ISBM_HOST"), Host),
            Channel = Pick(Environment.GetEnvironmentVariable("ISBM_CHANNEL"), Channel),
            Topic = Pick(Environment.GetEnvironmentVariable("ISBM_TOPIC"), Topic),
            User = Pick(Environment.GetEnvironmentVariable("ISBM_USER"), User),
            Password = Pick(Environment.GetEnvironmentVariable("ISBM_PASSWORD"), Password),
            IncludeRawDefault = ReadBoolEnvironment("ISBM_INCLUDE_RAW_DEFAULT") ?? IncludeRawDefault
        };
    }

    private static IsbmConsoleSettings Read(string path)
    {
        if (!File.Exists(path))
        {
            return Empty;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("Isbm", out var isbm))
        {
            return Empty;
        }

        return new IsbmConsoleSettings
        {
            Host = ReadString(isbm, "Host"),
            Channel = ReadString(isbm, "Channel"),
            Topic = ReadString(isbm, "Topic"),
            User = ReadString(isbm, "User"),
            Password = ReadString(isbm, "Password"),
            IncludeRawDefault = ReadBool(isbm, "IncludeRawDefault")
        };
    }

    private static IEnumerable<string> GetCandidatePaths(string fileName)
    {
        var paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(Directory.GetCurrentDirectory(), fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "RapidRedPanda.Wrapper.Console", fileName)
        };

        return paths.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? ""
            : "";
    }

    private static bool ReadBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.True
            || element.TryGetProperty(propertyName, out property) && property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out var value) && value;
    }

    private static bool? ReadBoolEnvironment(string name)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return bool.TryParse(raw, out var value) ? value : null;
    }

    private static string Pick(string? preferred, string fallback)
    {
        return string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
    }
}
