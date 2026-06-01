using RapidRedPanda.Wrapper.Console.Configuration;
using RapidRedPanda.Wrapper.Publication;
using RapidRedPanda.Wrapper.Responses;

namespace RapidRedPanda.Wrapper.Console.Interactive;

internal sealed class EndToEndPublicationWorkflow
{
    private readonly ProviderPublicationWrapper _providerWrapper = new();
    private readonly ConsumerPublicationWrapper _consumerWrapper = new();
    private readonly IsbmConsoleSettings _settings;

    public EndToEndPublicationWorkflow()
        : this(IsbmConsoleSettings.Empty)
    {
    }

    public EndToEndPublicationWorkflow(IsbmConsoleSettings settings)
    {
        _settings = settings;
    }

    public void Run()
    {
        var host = WorkflowConsole.PromptHost(_settings);
        var channel = WorkflowConsole.PromptChannel(_settings);
        var topic = WorkflowConsole.PromptTopic(_settings);
        var user = WorkflowConsole.PromptUser(_settings);
        var password = WorkflowConsole.PromptPassword(_settings);
        if (host is null || channel is null || topic is null || user is null || password is null)
        {
            return;
        }

        var raw = WorkflowConsole.PromptRaw(_settings);

        System.Console.WriteLine("Enter publication message content. Submit an empty line to finish:");
        var messageContent = ReadMultilineMessage();

        string? providerSessionId = null;
        string? consumerSessionId = null;

        var openProvider = _providerWrapper.OpenProviderSession(host, channel, user, password, raw);
        WorkflowConsole.WriteJson(openProvider);
        if (!openProvider.Success)
        {
            return;
        }

        providerSessionId = WorkflowConsole.GetStringDataValue(openProvider, "sessionId");
        if (string.IsNullOrWhiteSpace(providerSessionId))
        {
            WorkflowConsole.WriteJson(WrapperResponse.ValidationFailure("end-to-end-publication-demo", "OpenProviderSession succeeded but did not return a sessionId."));
            return;
        }

        var post = _providerWrapper.PostPublication(host, providerSessionId, topic, messageContent, user, password, raw);
        WorkflowConsole.WriteJson(post);
        if (!post.Success && !WorkflowConsole.AskYesNo("Post failed. Continue to consumer subscription? [y/N]", defaultYes: false))
        {
            CloseProvider(host, providerSessionId, user, password, raw);
            return;
        }

        var openConsumer = _consumerWrapper.OpenSubscription(host, channel, topic, user, password, raw);
        WorkflowConsole.WriteJson(openConsumer);
        if (!openConsumer.Success)
        {
            CloseProvider(host, providerSessionId, user, password, raw);
            return;
        }

        consumerSessionId = WorkflowConsole.GetStringDataValue(openConsumer, "sessionId");
        if (string.IsNullOrWhiteSpace(consumerSessionId))
        {
            WorkflowConsole.WriteJson(WrapperResponse.ValidationFailure("end-to-end-publication-demo", "OpenSubscription succeeded but did not return a sessionId."));
            CloseProvider(host, providerSessionId, user, password, raw);
            return;
        }

        var read = _consumerWrapper.ReadPublication(host, consumerSessionId, user, password, raw);
        WorkflowConsole.WriteJson(read);

        var remove = _consumerWrapper.RemovePublication(host, consumerSessionId, user, password, raw);
        WorkflowConsole.WriteJson(remove);

        var closeConsumer = _consumerWrapper.CloseSubscription(host, consumerSessionId, user, password, raw);
        WorkflowConsole.WriteJson(closeConsumer);

        CloseProvider(host, providerSessionId, user, password, raw);
    }

    private void CloseProvider(string host, string providerSessionId, string user, string password, bool raw)
    {
        var closeProvider = _providerWrapper.CloseProviderSession(host, providerSessionId, user, password, raw);
        WorkflowConsole.WriteJson(closeProvider);
    }

    private static string ReadMultilineMessage()
    {
        var lines = new List<string>();
        while (true)
        {
            var line = System.Console.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                break;
            }

            lines.Add(line);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
