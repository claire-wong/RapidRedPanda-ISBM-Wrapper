using System.Text.Json;
using System.Text.Json.Serialization;
using RapidRedPanda.Wrapper.Console.Commands;
using RapidRedPanda.Wrapper.Console.Configuration;
using RapidRedPanda.Wrapper.Console.Interactive;
using RapidRedPanda.Wrapper.Publication;
using RapidRedPanda.Wrapper.Responses;

if (args.Length == 0)
{
    RunRoleSelectionMenu(IsbmConsoleSettings.Load());
    return 0;
}

var options = CliOptions.Parse(args);
var consumerWrapper = new ConsumerPublicationWrapper();
var providerWrapper = new ProviderPublicationWrapper();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    WriteIndented = false
};

if (options.Command == "consumer-workflow")
{
    return new ConsumerPublicationWorkflow().RunWorkflowSequence(
        options.Get("--host") ?? "",
        options.Get("--channel") ?? "",
        options.Get("--topic") ?? "",
        options.Get("--user") ?? "",
        options.Get("--password") ?? "",
        options.Raw);
}

if (options.Command is "help" or "--help" or "-h")
{
    PrintUsage();
    return 0;
}

WrapperResponse response = options.Command == "remove-publication" && options.Has("--publication-id")
    ? WrapperResponse.ValidationFailure("remove-publication", "Unsupported parameter: --publication-id")
    : options.Command switch
    {
        "open-subscription" => consumerWrapper.OpenSubscription(
            options.Get("--host") ?? "",
            options.Get("--channel") ?? "",
            options.Get("--topic") ?? "",
            options.Get("--user") ?? "",
            options.Get("--password") ?? "",
            options.Raw),
        "read-publication" => consumerWrapper.ReadPublication(
            options.Get("--host") ?? "",
            options.Get("--session-id") ?? "",
            options.Get("--user") ?? "",
            options.Get("--password") ?? "",
            options.Raw),
        "remove-publication" => consumerWrapper.RemovePublication(
            options.Get("--host") ?? "",
            options.Get("--session-id") ?? "",
            options.Get("--user") ?? "",
            options.Get("--password") ?? "",
            options.Raw),
        "close-subscription" or "close-subscription-session" => consumerWrapper.CloseSubscription(
            options.Get("--host") ?? "",
            options.Get("--session-id") ?? "",
            options.Get("--user") ?? "",
            options.Get("--password") ?? "",
            options.Raw),
        "open-provider-session" => providerWrapper.OpenProviderSession(
            options.Get("--host") ?? "",
            options.Get("--channel") ?? "",
            options.Get("--user") ?? "",
            options.Get("--password") ?? "",
            options.Raw),
        "post-publication" => PostPublication(providerWrapper, options),
        "close-provider-session" => providerWrapper.CloseProviderSession(
            options.Get("--host") ?? "",
            options.Get("--session-id") ?? "",
            options.Get("--user") ?? "",
            options.Get("--password") ?? "",
            options.Raw),
        "" => WrapperResponse.ValidationFailure("", "Missing command."),
        _ => WrapperResponse.ValidationFailure(options.Command, $"Unknown command: {options.Command}")
    };

WriteJson(response, jsonOptions);
return response.Success ? 0 : 1;

static void RunRoleSelectionMenu(IsbmConsoleSettings settings)
{
    while (true)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("RapidRedPanda ISBM Test Console");
        System.Console.WriteLine("1. Consumer Publication Console");
        System.Console.WriteLine("2. Provider Publication Console");
        System.Console.WriteLine("3. End-to-End Publication Demo");
        System.Console.WriteLine("0. Exit");
        System.Console.Write("Select a role/workflow: ");

        var choice = System.Console.ReadLine();
        if (choice is null)
        {
            return;
        }

        switch (choice)
        {
            case "1":
                new ConsumerPublicationWorkflow(settings).RunMenu();
                break;
            case "2":
                new ProviderPublicationWorkflow(settings).RunMenu();
                break;
            case "3":
                new EndToEndPublicationWorkflow(settings).Run();
                break;
            case "0":
                return;
            default:
                System.Console.WriteLine("Invalid menu choice.");
                break;
        }
    }
}

static WrapperResponse PostPublication(ProviderPublicationWrapper wrapper, CliOptions options)
{
    var messageResult = ResolveMessageContent(options);
    if (!messageResult.Success)
    {
        return messageResult.Response;
    }

    return wrapper.PostPublication(
        options.Get("--host") ?? "",
        options.Get("--session-id") ?? "",
        options.Get("--topic") ?? "",
        messageResult.MessageContent ?? "",
        options.Get("--user") ?? "",
        options.Get("--password") ?? "",
        options.Raw);
}

static (bool Success, string? MessageContent, WrapperResponse Response) ResolveMessageContent(CliOptions options)
{
    var message = options.Get("--message");
    if (!string.IsNullOrWhiteSpace(message))
    {
        return (true, message, null!);
    }

    var messageFile = options.Get("--message-file");
    if (string.IsNullOrWhiteSpace(messageFile))
    {
        return (false, null, WrapperResponse.ValidationFailure("post-publication", "Missing required parameter: --message or --message-file"));
    }

    try
    {
        return (true, File.ReadAllText(messageFile), null!);
    }
    catch (Exception exception)
    {
        return (false, null, WrapperResponse.ValidationFailure("post-publication", $"Unable to read --message-file: {exception.Message}"));
    }
}

static void WriteJson(WrapperResponse response, JsonSerializerOptions jsonOptions)
{
    System.Console.Out.WriteLine(JsonSerializer.Serialize(response, jsonOptions));
}

static void PrintUsage()
{
    System.Console.WriteLine("""
RapidRedPanda.Wrapper.Console usage:

Consumer publication commands:
  open-subscription --host <url> --channel <channel> --topic <topic> --user <user> --password <password> [--raw]
  read-publication --host <url> --session-id <id> --user <user> --password <password> [--raw]
  remove-publication --host <url> --session-id <id> --user <user> --password <password> [--raw]
  close-subscription --host <url> --session-id <id> --user <user> --password <password> [--raw]
  close-subscription-session --host <url> --session-id <id> --user <user> --password <password> [--raw]

Provider publication commands:
  open-provider-session --host <url> --channel <channel> --user <user> --password <password> [--raw]
  post-publication --host <url> --session-id <id> --topic <topic> (--message <json> | --message-file <path>) --user <user> --password <password> [--raw]
  close-provider-session --host <url> --session-id <id> --user <user> --password <password> [--raw]

Interactive mode:
  Run with no arguments to open the role selection menu.
  consumer-workflow --host <url> --channel <channel> --topic <topic> --user <user> --password <password> [--raw]

Individual commands write JSON only to stdout. Passwords are never included in JSON responses.
""");
}
