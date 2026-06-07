using System.Text.Json;
using System.Text.Json.Serialization;
using RapidRedPanda.Wrapper.Publication;
using RapidRedPanda.Wrapper.Request;
using RapidRedPanda.Wrapper.Responses;

const int SuccessExitCode = 0;
const int CliExecutionFailureExitCode = 1;
const int InvalidArgumentsExitCode = 2;
const int WrapperOperationFailureExitCode = 3;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    WriteIndented = true
};

try
{
    var parseResult = CliArguments.Parse(args);
    if (!parseResult.Success)
    {
        WriteJson(WrapperResponse.ValidationFailure(parseResult.Command, parseResult.Error!), jsonOptions);
        return InvalidArgumentsExitCode;
    }

    var consumerPublicationWrapper = new ConsumerPublicationWrapper();
    var providerPublicationWrapper = new ProviderPublicationWrapper();
    var consumerRequestWrapper = new ConsumerRequestWrapper();
    var providerRequestWrapper = new ProviderRequestWrapper();
    var response = parseResult.Command switch
    {
        "open-subscription" => consumerPublicationWrapper.OpenSubscription(
            parseResult.Get("--host"),
            parseResult.Get("--channel"),
            parseResult.Get("--topic"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "read-publication" => consumerPublicationWrapper.ReadPublication(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "remove-publication" => consumerPublicationWrapper.RemovePublication(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "close-subscription" => consumerPublicationWrapper.CloseSubscription(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "open-publication-session" => WithCommand(
            providerPublicationWrapper.OpenProviderSession(
                parseResult.Get("--host"),
                parseResult.Get("--channel"),
                parseResult.Get("--user"),
                parseResult.Get("--password"),
                parseResult.Has("--raw")),
            parseResult.Command),
        "post-publication" => providerPublicationWrapper.PostPublication(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--topic"),
            parseResult.Get("--content"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw"),
            parseResult.GetOptional("--expiry")),
        "expire-publication" => providerPublicationWrapper.ExpirePublication(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--message-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "close-publication-session" => WithCommand(
            providerPublicationWrapper.CloseProviderSession(
                parseResult.Get("--host"),
                parseResult.Get("--session-id"),
                parseResult.Get("--user"),
                parseResult.Get("--password"),
                parseResult.Has("--raw")),
            parseResult.Command),
        "open-request-session" => consumerRequestWrapper.OpenRequestSession(
            parseResult.Get("--host"),
            parseResult.Get("--channel"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "post-request" => consumerRequestWrapper.PostRequest(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--topic"),
            parseResult.Get("--content"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw"),
            parseResult.GetOptional("--expiry")),
        "read-response" => consumerRequestWrapper.ReadResponse(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--request-message-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "remove-response" => consumerRequestWrapper.RemoveResponse(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--request-message-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "expire-request" => WithCommand(
            consumerRequestWrapper.ExpireRequest(
                parseResult.Get("--host"),
                parseResult.Get("--session-id"),
                parseResult.Get("--message-id"),
                parseResult.Get("--user"),
                parseResult.Get("--password"),
                parseResult.Has("--raw")),
            parseResult.Command),
        "close-request-session" => consumerRequestWrapper.CloseRequestSession(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "open-provider-request-session" => providerRequestWrapper.OpenProviderRequestSession(
            parseResult.Get("--host"),
            parseResult.Get("--channel"),
            parseResult.Get("--topic"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "read-request" => providerRequestWrapper.ReadRequest(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "post-response" => providerRequestWrapper.PostResponse(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--request-message-id"),
            parseResult.Get("--content"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "remove-request" => providerRequestWrapper.RemoveRequest(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        "close-provider-request-session" => providerRequestWrapper.CloseProviderRequestSession(
            parseResult.Get("--host"),
            parseResult.Get("--session-id"),
            parseResult.Get("--user"),
            parseResult.Get("--password"),
            parseResult.Has("--raw")),
        _ => WrapperResponse.ValidationFailure(parseResult.Command, $"Unknown command: {parseResult.Command}")
    };

    WriteJson(response, jsonOptions);
    return response.Success ? SuccessExitCode : WrapperOperationFailureExitCode;
}
catch (Exception exception)
{
    var response = new WrapperResponse
    {
        Success = false,
        Command = args.FirstOrDefault() ?? "",
        Data = null,
        Raw = null,
        Fault = null,
        TransportFault = new WrapperTransportFault(
            "CliExecutionError",
            exception.Message,
            new { exceptionType = exception.GetType().Name })
    };

    WriteJson(response, jsonOptions);
    return CliExecutionFailureExitCode;
}

static void WriteJson(WrapperResponse response, JsonSerializerOptions options)
{
    Console.Out.WriteLine(JsonSerializer.Serialize(response, options));
}

static WrapperResponse WithCommand(WrapperResponse response, string command)
{
    return new WrapperResponse
    {
        Success = response.Success,
        Command = command,
        TimestampUtc = response.TimestampUtc,
        Data = response.Data,
        Raw = response.Raw,
        Fault = response.Fault,
        TransportFault = response.TransportFault
    };
}

internal sealed class CliArguments
{
    private static readonly HashSet<string> SupportedCommands =
    [
        "open-subscription",
        "read-publication",
        "remove-publication",
        "close-subscription",
        "open-publication-session",
        "post-publication",
        "expire-publication",
        "close-publication-session",
        "open-request-session",
        "post-request",
        "read-response",
        "remove-response",
        "expire-request",
        "close-request-session",
        "open-provider-request-session",
        "read-request",
        "post-response",
        "remove-request",
        "close-provider-request-session"
    ];

    private static readonly HashSet<string> SupportedOptions =
    [
        "--host",
        "--channel",
        "--topic",
        "--session-id",
        "--message-id",
        "--request-message-id",
        "--media-type",
        "--content",
        "--expiry",
        "--user",
        "--password",
        "--raw"
    ];

    private static readonly Dictionary<string, string[]> RequiredOptionsByCommand = new(StringComparer.Ordinal)
    {
        ["open-subscription"] =
        [
            "--host",
            "--channel",
            "--topic",
            "--user",
            "--password"
        ],
        ["read-publication"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password"
        ],
        ["remove-publication"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password"
        ],
        ["close-subscription"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password"
        ],
        ["open-publication-session"] =
        [
            "--host",
            "--channel",
            "--topic",
            "--user",
            "--password"
        ],
        ["post-publication"] =
        [
            "--host",
            "--session-id",
            "--topic",
            "--media-type",
            "--content",
            "--user",
            "--password"
        ],
        ["expire-publication"] =
        [
            "--host",
            "--session-id",
            "--message-id",
            "--user",
            "--password"
        ],
        ["close-publication-session"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password"
        ],
        ["open-request-session"] =
        [
            "--host",
            "--channel",
            "--topic",
            "--user",
            "--password"
        ],
        ["post-request"] =
        [
            "--host",
            "--session-id",
            "--topic",
            "--media-type",
            "--content",
            "--user",
            "--password"
        ],
        ["read-response"] =
        [
            "--host",
            "--session-id",
            "--request-message-id",
            "--user",
            "--password"
        ],
        ["remove-response"] =
        [
            "--host",
            "--session-id",
            "--request-message-id",
            "--user",
            "--password"
        ],
        ["expire-request"] =
        [
            "--host",
            "--session-id",
            "--message-id",
            "--user",
            "--password"
        ],
        ["close-request-session"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password"
        ],
        ["open-provider-request-session"] =
        [
            "--host",
            "--channel",
            "--topic",
            "--user",
            "--password"
        ],
        ["read-request"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password"
        ],
        ["post-response"] =
        [
            "--host",
            "--session-id",
            "--request-message-id",
            "--media-type",
            "--content",
            "--user",
            "--password"
        ],
        ["remove-request"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password"
        ],
        ["close-provider-request-session"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password"
        ]
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedOptionsByCommand = new(StringComparer.Ordinal)
    {
        ["open-subscription"] =
        [
            "--host",
            "--channel",
            "--topic",
            "--user",
            "--password",
            "--raw"
        ],
        ["read-publication"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["remove-publication"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["close-subscription"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["open-publication-session"] =
        [
            "--host",
            "--channel",
            "--topic",
            "--user",
            "--password",
            "--raw"
        ],
        ["post-publication"] =
        [
            "--host",
            "--session-id",
            "--topic",
            "--media-type",
            "--content",
            "--expiry",
            "--user",
            "--password",
            "--raw"
        ],
        ["expire-publication"] =
        [
            "--host",
            "--session-id",
            "--message-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["close-publication-session"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["open-request-session"] =
        [
            "--host",
            "--channel",
            "--topic",
            "--user",
            "--password",
            "--raw"
        ],
        ["post-request"] =
        [
            "--host",
            "--session-id",
            "--topic",
            "--media-type",
            "--content",
            "--expiry",
            "--user",
            "--password",
            "--raw"
        ],
        ["read-response"] =
        [
            "--host",
            "--session-id",
            "--request-message-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["remove-response"] =
        [
            "--host",
            "--session-id",
            "--request-message-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["expire-request"] =
        [
            "--host",
            "--session-id",
            "--message-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["close-request-session"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["open-provider-request-session"] =
        [
            "--host",
            "--channel",
            "--topic",
            "--user",
            "--password",
            "--raw"
        ],
        ["read-request"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["post-response"] =
        [
            "--host",
            "--session-id",
            "--request-message-id",
            "--media-type",
            "--content",
            "--expiry",
            "--topic",
            "--user",
            "--password",
            "--raw"
        ],
        ["remove-request"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password",
            "--raw"
        ],
        ["close-provider-request-session"] =
        [
            "--host",
            "--session-id",
            "--user",
            "--password",
            "--raw"
        ]
    };

    private static readonly string SupportedCommandsText = string.Join(", ", SupportedCommands);

    private static readonly StringComparer OptionComparer = StringComparer.Ordinal;

    private static readonly string[] EmptyRequiredOptions =
    [
    ];

    private readonly Dictionary<string, string?> _options;

    private CliArguments(string command, Dictionary<string, string?> options, string? error)
    {
        Command = command;
        _options = options;
        Error = error;
    }

    public bool Success => Error is null;

    public string Command { get; }

    public string? Error { get; }

    public static CliArguments Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return Failure("", $"Missing command. Supported commands: {SupportedCommandsText}.");
        }

        var command = args[0];
        if (!SupportedCommands.Contains(command))
        {
            return Failure(command, $"Unknown command: {command}. Supported commands: {SupportedCommandsText}.");
        }

        var options = new Dictionary<string, string?>(OptionComparer);
        var allowedOptions = AllowedOptionsByCommand[command];
        for (var index = 1; index < args.Length; index++)
        {
            var option = args[index];
            if (!SupportedOptions.Contains(option) || !allowedOptions.Contains(option))
            {
                return Failure(command, $"Unknown argument: {option}");
            }

            if (option == "--raw")
            {
                options[option] = "true";
                continue;
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return Failure(command, $"Missing value for argument: {option}");
            }

            options[option] = args[++index];
        }

        foreach (var requiredOption in RequiredOptionsByCommand.GetValueOrDefault(command, EmptyRequiredOptions))
        {
            if (!options.TryGetValue(requiredOption, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return Failure(command, BuildMissingRequiredArgumentMessage(command, requiredOption));
            }
        }

        return new CliArguments(command, options, null);
    }

    public string Get(string name)
    {
        return _options.TryGetValue(name, out var value) ? value ?? "" : "";
    }

    public string? GetOptional(string name)
    {
        return _options.TryGetValue(name, out var value) ? value : null;
    }

    public bool Has(string name)
    {
        return _options.ContainsKey(name);
    }

    private static CliArguments Failure(string command, string error)
    {
        return new CliArguments(command, new Dictionary<string, string?>(OptionComparer), error);
    }

    private static string BuildMissingRequiredArgumentMessage(string command, string option)
    {
        if (command == "expire-request" && option == "--message-id")
        {
            return "Message ID is required for expire-request.";
        }

        if (command == "expire-request" && option == "--session-id")
        {
            return "Session ID is required for expire-request.";
        }

        return $"Missing required argument: {option}";
    }
}
