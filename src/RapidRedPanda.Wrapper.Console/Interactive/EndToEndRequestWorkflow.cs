using RapidRedPanda.Wrapper.Console.Configuration;
using RapidRedPanda.Wrapper.Request;
using RapidRedPanda.Wrapper.Responses;

namespace RapidRedPanda.Wrapper.Console.Interactive;

internal sealed class EndToEndRequestWorkflow
{
    private const string DemoCommand = "end-to-end-request-demo";

    private readonly ConsumerRequestWrapper _consumerWrapper = new();
    private readonly ProviderRequestWrapper _providerWrapper = new();
    private readonly IsbmConsoleSettings _settings;

    public EndToEndRequestWorkflow()
        : this(IsbmConsoleSettings.Empty)
    {
    }

    public EndToEndRequestWorkflow(IsbmConsoleSettings settings)
    {
        _settings = settings;
    }

    public void Run()
    {
        var host = WorkflowConsole.PromptHost(_settings);
        var channel = WorkflowConsole.PromptRequestChannel(_settings);
        var topic = WorkflowConsole.PromptRequestTopic(_settings);
        var user = WorkflowConsole.PromptUser(_settings);
        var password = WorkflowConsole.PromptPassword(_settings);
        if (host is null || channel is null || topic is null || user is null || password is null)
        {
            return;
        }

        var raw = WorkflowConsole.PromptRaw(_settings);

        string? consumerRequestSessionId = null;
        string? providerRequestSessionId = null;
        string? requestMessageId = null;
        string? providerReadRequestMessageId = null;
        string? responseMessageId = null;
        var requestExpired = false;

        var openConsumer = _consumerWrapper.OpenRequestSession(host, channel, user, password, raw);
        WorkflowConsole.WriteJson(openConsumer);
        if (!openConsumer.Success)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        consumerRequestSessionId = WorkflowConsole.GetStringDataValue(openConsumer, "sessionId");
        if (string.IsNullOrWhiteSpace(consumerRequestSessionId))
        {
            WorkflowConsole.WriteJson(WrapperResponse.ValidationFailure(DemoCommand, "OpenRequestSession succeeded but did not return a sessionId."));
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        var openProvider = _providerWrapper.OpenProviderRequestSession(host, channel, topic, user, password, raw);
        WorkflowConsole.WriteJson(openProvider);
        if (!openProvider.Success)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        providerRequestSessionId = WorkflowConsole.GetStringDataValue(openProvider, "sessionId");
        if (string.IsNullOrWhiteSpace(providerRequestSessionId))
        {
            WorkflowConsole.WriteJson(WrapperResponse.ValidationFailure(DemoCommand, "OpenProviderRequestSession succeeded but did not return a sessionId."));
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        var requestContent = ReadMultilineContent("request message");
        if (requestContent is null)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        var postRequest = _consumerWrapper.PostRequest(host, consumerRequestSessionId, topic, requestContent, user, password, raw);
        WorkflowConsole.WriteJson(postRequest);
        if (!postRequest.Success)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        requestMessageId = WorkflowConsole.GetStringDataValue(postRequest, "messageId");
        if (string.IsNullOrWhiteSpace(requestMessageId))
        {
            WorkflowConsole.WriteJson(WrapperResponse.ValidationFailure(DemoCommand, "PostRequest succeeded but did not return a messageId."));
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        var readRequest = _providerWrapper.ReadRequest(host, providerRequestSessionId, user, password, raw);
        WorkflowConsole.WriteJson(readRequest);
        if (!readRequest.Success)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        providerReadRequestMessageId = WorkflowConsole.GetStringDataValue(readRequest, "requestMessageId");
        if (string.IsNullOrWhiteSpace(providerReadRequestMessageId))
        {
            WorkflowConsole.WriteJson(WrapperResponse.ValidationFailure(DemoCommand, "ReadRequest succeeded but did not return a requestMessageId."));
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        var responseContent = ReadMultilineContent("response message");
        if (responseContent is null)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        var postResponse = _providerWrapper.PostResponse(host, providerRequestSessionId, providerReadRequestMessageId, responseContent, user, password, raw);
        WorkflowConsole.WriteJson(postResponse);
        if (!postResponse.Success)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        responseMessageId = WorkflowConsole.GetStringDataValue(postResponse, "responseMessageId");

        var readResponse = _consumerWrapper.ReadResponse(host, consumerRequestSessionId, requestMessageId, user, password, raw);
        WorkflowConsole.WriteJson(readResponse);
        if (!readResponse.Success)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        responseMessageId = WorkflowConsole.GetStringDataValue(readResponse, "messageId") ?? responseMessageId;
        if (string.IsNullOrWhiteSpace(responseMessageId))
        {
            WorkflowConsole.WriteJson(WrapperResponse.ValidationFailure(DemoCommand, "ReadResponse succeeded but did not return a messageId."));
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        var removeResponse = _consumerWrapper.RemoveResponse(host, consumerRequestSessionId, requestMessageId, user, password, raw);
        WorkflowConsole.WriteJson(removeResponse);
        if (!removeResponse.Success)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        var removeRequest = _providerWrapper.RemoveRequest(host, providerRequestSessionId, user, password, raw);
        WorkflowConsole.WriteJson(removeRequest);
        if (!removeRequest.Success)
        {
            HandleFailureAndCleanup(host, user, password, raw, consumerRequestSessionId, providerRequestSessionId, requestMessageId, requestExpired);
            return;
        }

        requestExpired = OfferExpireRequest(host, consumerRequestSessionId, requestMessageId, user, password, raw);

        CloseConsumer(host, consumerRequestSessionId, user, password, raw);
        CloseProvider(host, providerRequestSessionId, user, password, raw);
    }

    private void HandleFailureAndCleanup(
        string host,
        string user,
        string password,
        bool raw,
        string? consumerRequestSessionId,
        string? providerRequestSessionId,
        string? requestMessageId,
        bool requestExpired)
    {
        System.Console.WriteLine("A request demo step failed or was cancelled.");
        System.Console.WriteLine("1. Continue to cleanup");
        System.Console.WriteLine("2. Return to role selection");
        System.Console.Write("Select an option: ");
        _ = System.Console.ReadLine();

        if (!requestExpired && !string.IsNullOrWhiteSpace(consumerRequestSessionId) && !string.IsNullOrWhiteSpace(requestMessageId))
        {
            _ = OfferExpireRequest(host, consumerRequestSessionId, requestMessageId, user, password, raw);
        }

        if (!string.IsNullOrWhiteSpace(consumerRequestSessionId))
        {
            CloseConsumer(host, consumerRequestSessionId, user, password, raw);
        }

        if (!string.IsNullOrWhiteSpace(providerRequestSessionId))
        {
            CloseProvider(host, providerRequestSessionId, user, password, raw);
        }
    }

    private bool OfferExpireRequest(string host, string consumerRequestSessionId, string requestMessageId, string user, string password, bool raw)
    {
        if (!WorkflowConsole.AskYesNo("Expire the original request before cleanup? [y/N]", defaultYes: false))
        {
            return false;
        }

        var expire = _consumerWrapper.ExpireRequest(host, consumerRequestSessionId, requestMessageId, user, password, raw);
        WorkflowConsole.WriteJson(expire);
        return expire.Success;
    }

    private void CloseConsumer(string host, string consumerRequestSessionId, string user, string password, bool raw)
    {
        var closeConsumer = _consumerWrapper.CloseRequestSession(host, consumerRequestSessionId, user, password, raw);
        WorkflowConsole.WriteJson(closeConsumer);
    }

    private void CloseProvider(string host, string providerRequestSessionId, string user, string password, bool raw)
    {
        var closeProvider = _providerWrapper.CloseProviderRequestSession(host, providerRequestSessionId, user, password, raw);
        WorkflowConsole.WriteJson(closeProvider);
    }

    private static string? ReadMultilineContent(string description)
    {
        System.Console.WriteLine($"Enter {description} content.");
        System.Console.WriteLine("Type cancel to cancel.");
        System.Console.WriteLine("Submit an empty line after content to finish.");

        var lines = new List<string>();
        while (true)
        {
            var line = System.Console.ReadLine();
            if (line is null)
            {
                return null;
            }

            if (lines.Count == 0 && string.Equals(line, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.IsNullOrEmpty(line) && lines.Count == 0)
            {
                System.Console.WriteLine($"{description} content is required. Enter content or type cancel.");
                continue;
            }

            if (string.IsNullOrEmpty(line))
            {
                break;
            }

            lines.Add(line);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
