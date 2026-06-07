using System.Text.Json;

namespace RapidRedPanda.Wrapper.Console.Configuration;

internal sealed class IsbmConsoleSettings
{
    public string Host { get; private init; } = "";
    public string User { get; private init; } = "";
    public string Password { get; private init; } = "";
    public bool IncludeRawDefault { get; private init; }
    public IsbmServiceSettings Publication { get; private init; } = IsbmServiceSettings.Empty;
    public IsbmServiceSettings Request { get; private init; } = IsbmServiceSettings.Empty;

    public string Channel => Publication.Channel;
    public string Topic => Publication.Topic;

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
            User = Pick(other.User, User),
            Password = Pick(other.Password, Password),
            IncludeRawDefault = other.IncludeRawDefault || IncludeRawDefault,
            Publication = Publication.Merge(other.Publication),
            Request = Request.Merge(other.Request)
        };
    }

    private IsbmConsoleSettings ApplyEnvironment()
    {
        return new IsbmConsoleSettings
        {
            Host = Pick(Environment.GetEnvironmentVariable("ISBM_HOST"), Host),
            User = Pick(Environment.GetEnvironmentVariable("ISBM_USER"), User),
            Password = Pick(Environment.GetEnvironmentVariable("ISBM_PASSWORD"), Password),
            IncludeRawDefault = ReadBoolEnvironment("ISBM_INCLUDE_RAW_DEFAULT") ?? IncludeRawDefault,
            Publication = new IsbmServiceSettings
            {
                Channel = Pick(
                    Environment.GetEnvironmentVariable("ISBM_PUBLICATION_CHANNEL"),
                    Pick(Environment.GetEnvironmentVariable("ISBM_CHANNEL"), Publication.Channel)),
                Topic = Pick(
                    Environment.GetEnvironmentVariable("ISBM_PUBLICATION_TOPIC"),
                    Pick(Environment.GetEnvironmentVariable("ISBM_TOPIC"), Publication.Topic))
            },
            Request = new IsbmServiceSettings
            {
                Channel = Pick(Environment.GetEnvironmentVariable("ISBM_REQUEST_CHANNEL"), Request.Channel),
                Topic = Pick(Environment.GetEnvironmentVariable("ISBM_REQUEST_TOPIC"), Request.Topic)
            }
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

        var legacyPublication = new IsbmServiceSettings
        {
            Channel = ReadString(isbm, "Channel"),
            Topic = ReadString(isbm, "Topic")
        };

        var publication = legacyPublication.Merge(ReadServiceSettings(isbm, "Publication"));
        var request = ReadServiceSettings(isbm, "Request");

        return new IsbmConsoleSettings
        {
            Host = ReadString(isbm, "Host"),
            User = ReadString(isbm, "User"),
            Password = ReadString(isbm, "Password"),
            IncludeRawDefault = ReadBool(isbm, "IncludeRawDefault"),
            Publication = publication,
            Request = request
        };
    }

    private static IsbmServiceSettings ReadServiceSettings(JsonElement isbm, string propertyName)
    {
        if (!isbm.TryGetProperty(propertyName, out var section) || section.ValueKind != JsonValueKind.Object)
        {
            return IsbmServiceSettings.Empty;
        }

        return new IsbmServiceSettings
        {
            Channel = ReadString(section, "Channel"),
            Topic = ReadString(section, "Topic")
        };
    }

    private static IEnumerable<string> GetCandidatePaths(string fileName)
    {
        var paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(Directory.GetCurrentDirectory(), fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "samples", "csharp", "RapidRedPanda.Wrapper.Console", fileName),
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

internal sealed class IsbmServiceSettings
{
    public static IsbmServiceSettings Empty { get; } = new();

    public string Channel { get; init; } = "";
    public string Topic { get; init; } = "";

    public IsbmServiceSettings Merge(IsbmServiceSettings other)
    {
        return new IsbmServiceSettings
        {
            Channel = string.IsNullOrWhiteSpace(other.Channel) ? Channel : other.Channel,
            Topic = string.IsNullOrWhiteSpace(other.Topic) ? Topic : other.Topic
        };
    }
}
