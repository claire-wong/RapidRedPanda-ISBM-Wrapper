using RapidRedPanda.Wrapper.Console.Configuration;
using RapidRedPanda.Wrapper.Publication;
using RapidRedPanda.Wrapper.Responses;

namespace RapidRedPanda.Wrapper.Console.Interactive;

internal sealed class ConsumerPublicationWorkflow
{
    private readonly ConsumerPublicationWrapper _wrapper = new();
    private readonly IsbmConsoleSettings _settings;
    private string? _currentSessionId;
    private string? _lastReadMessageId;

    public ConsumerPublicationWorkflow()
        : this(IsbmConsoleSettings.Empty)
    {
    }

    public ConsumerPublicationWorkflow(IsbmConsoleSettings settings)
    {
        _settings = settings;
    }

    public void RunMenu()
    {
        while (true)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Consumer Publication Console");
            WriteStateMenu();
            System.Console.Write("Select an option: ");

            var choice = System.Console.ReadLine();
            if (choice is null)
            {
                return;
            }

            if (HandleStateChoice(choice))
            {
                return;
            }
        }
    }

    public int RunWorkflowSequence(
        string host,
        string channel,
        string topic,
        string user,
        string password,
        bool raw)
    {
        var open = _wrapper.OpenSubscription(host, channel, topic, user, password, raw);
        WorkflowConsole.WriteJson(open);
        if (!open.Success)
        {
            return 1;
        }

        var sessionId = WorkflowConsole.GetStringDataValue(open, "sessionId");
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            WorkflowConsole.WriteJson(WrapperResponse.ValidationFailure("consumer-workflow", "OpenSubscription succeeded but did not return a sessionId."));
            return 1;
        }

        _currentSessionId = sessionId;
        var shouldRead = true;
        var sawFailure = false;

        while (shouldRead)
        {
            WorkflowConsole.WaitForEnter("Press Enter to call ReadPublication...");
            var read = _wrapper.ReadPublication(host, sessionId, user, password, raw);
            WorkflowConsole.WriteJson(read);
            if (!read.Success)
            {
                sawFailure = true;
                shouldRead = WorkflowConsole.AskYesNo("Read another publication? [Y/n]", defaultYes: true);
                continue;
            }

            _lastReadMessageId = WorkflowConsole.GetStringDataValue(read, "messageId");

            WorkflowConsole.WaitForEnter("Press Enter to call RemovePublication...");
            var remove = _wrapper.RemovePublication(host, sessionId, user, password, raw);
            WorkflowConsole.WriteJson(remove);
            if (!remove.Success)
            {
                sawFailure = true;
                shouldRead = WorkflowConsole.AskYesNo("Read another publication? [Y/n]", defaultYes: true);
                continue;
            }

            _lastReadMessageId = null;
            shouldRead = WorkflowConsole.AskYesNo("Read another publication? [Y/n]", defaultYes: true);
        }

        WorkflowConsole.WaitForEnter("Press Enter to close subscription session...");
        var close = _wrapper.CloseSubscription(host, sessionId, user, password, raw);
        WorkflowConsole.WriteJson(close);
        if (close.Success)
        {
            ClearSessionState();
        }

        return close.Success && !sawFailure ? 0 : 1;
    }

    private void WriteStateMenu()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("State: No Subscription Session");
            System.Console.WriteLine("1. Open Subscription Session");
            System.Console.WriteLine("0. Back to Role Selection");
            return;
        }

        if (string.IsNullOrWhiteSpace(_lastReadMessageId))
        {
            System.Console.WriteLine("State: Subscription Session Open, No Publication Read");
            System.Console.WriteLine($"Current subscription sessionId: {_currentSessionId}");
            System.Console.WriteLine("1. Read Publication");
            System.Console.WriteLine("2. Close Subscription Session");
            System.Console.WriteLine("3. Show Current Subscription Session");
            System.Console.WriteLine("0. Back to Role Selection");
            return;
        }

        System.Console.WriteLine("State: Publication Loaded and Awaiting Removal");
        System.Console.WriteLine($"Current subscription sessionId: {_currentSessionId}");
        System.Console.WriteLine($"Last-read publication/message id: {_lastReadMessageId}");
        System.Console.WriteLine("1. Remove Last Read Publication");
        System.Console.WriteLine("2. Show Current Subscription Session");
        System.Console.WriteLine("3. Close Subscription Session");
        System.Console.WriteLine("0. Back to Role Selection");
    }

    private bool HandleStateChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            return choice switch
            {
                "1" => RunAndStay(OpenSubscription),
                "0" => true,
                _ => InvalidAndStay()
            };
        }

        if (string.IsNullOrWhiteSpace(_lastReadMessageId))
        {
            return choice switch
            {
                "1" => RunAndStay(ReadPublication),
                "2" => RunAndStay(() => CloseSubscription()),
                "3" => RunAndStay(ShowCurrentSession),
                "0" => BackWithOpenSessionWarning(),
                _ => InvalidAndStay()
            };
        }

        return choice switch
        {
            "1" => RunAndStay(RemoveLastReadPublication),
            "2" => RunAndStay(ShowCurrentSession),
            "3" => RunAndStay(() => CloseSubscription()),
            "0" => BackWithOpenSessionWarning(),
            _ => InvalidAndStay()
        };
    }

    private void OpenSubscription()
    {
        if (!string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("A subscription session is already active. Close it before opening another.");
            return;
        }

        var prompt = PromptConnection(includeChannelAndTopic: true);
        if (prompt is null)
        {
            return;
        }

        var filterExpressions = PromptFilterExpressions();
        if (filterExpressions.Cancelled)
        {
            return;
        }

        var response = _wrapper.OpenSubscription(prompt.Host, prompt.Channel, prompt.Topic, prompt.User, prompt.Password, prompt.Raw, filterExpressions.Filters);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _currentSessionId = WorkflowConsole.GetStringDataValue(response, "sessionId");
            _lastReadMessageId = null;
        }
    }

    private void ReadPublication()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No active subscription session.");
            return;
        }

        var prompt = PromptConnection(includeChannelAndTopic: false);
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.ReadPublication(prompt.Host, _currentSessionId, prompt.User, prompt.Password, prompt.Raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _lastReadMessageId = WorkflowConsole.GetStringDataValue(response, "messageId");
        }
    }

    private void RemoveLastReadPublication()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No active subscription session.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_lastReadMessageId))
        {
            System.Console.WriteLine("No last-read publication/message id is available to remove.");
            return;
        }

        var prompt = PromptConnection(includeChannelAndTopic: false);
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.RemovePublication(prompt.Host, _currentSessionId, prompt.User, prompt.Password, prompt.Raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _lastReadMessageId = null;
        }
    }

    private bool CloseSubscription()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No active subscription session.");
            return true;
        }

        var prompt = PromptConnection(includeChannelAndTopic: false);
        if (prompt is null)
        {
            return false;
        }

        var response = _wrapper.CloseSubscription(prompt.Host, _currentSessionId, prompt.User, prompt.Password, prompt.Raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            ClearSessionState();
            return true;
        }

        return false;
    }

    private void ShowCurrentSession()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current subscription session.");
            return;
        }

        System.Console.WriteLine($"Current subscription sessionId: {_currentSessionId}");
        System.Console.WriteLine(string.IsNullOrWhiteSpace(_lastReadMessageId)
            ? "Last-read publication/message id: none"
            : $"Last-read publication/message id: {_lastReadMessageId}");
    }

    private bool BackWithOpenSessionWarning()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            return true;
        }

        System.Console.WriteLine("A subscription session is still open.");
        if (WorkflowConsole.AskYesNo("Close it before leaving? [Y/n]", defaultYes: true))
        {
            if (CloseSubscription())
            {
                return true;
            }

            return !WorkflowConsole.AskYesNo("Close failed. Remain in the workflow? [Y/n]", defaultYes: true);
        }

        return true;
    }

    private ConsumerPrompt? PromptConnection(bool includeChannelAndTopic)
    {
        var host = WorkflowConsole.PromptHost(_settings);
        if (host is null)
        {
            return null;
        }

        var channel = "";
        var topic = "";
        if (includeChannelAndTopic)
        {
            channel = WorkflowConsole.PromptPublicationChannel(_settings) ?? "";
            if (string.IsNullOrWhiteSpace(channel))
            {
                return null;
            }

            topic = WorkflowConsole.PromptPublicationTopic(_settings) ?? "";
            if (string.IsNullOrWhiteSpace(topic))
            {
                return null;
            }
        }

        var user = WorkflowConsole.PromptUser(_settings);
        if (user is null)
        {
            return null;
        }

        var password = WorkflowConsole.PromptPassword(_settings);
        if (password is null)
        {
            return null;
        }

        return new ConsumerPrompt(host, channel, topic, user, password, WorkflowConsole.PromptRaw(_settings));
    }

    private static FilterPromptResult PromptFilterExpressions()
    {
        if (!WorkflowConsole.AskYesNo("Use filter expressions? [y/N]", defaultYes: false))
        {
            return new FilterPromptResult(null, Cancelled: false);
        }

        var filters = new List<WrapperFilterExpression>();
        var filterNumber = 1;

        while (true)
        {
            System.Console.WriteLine($"Filter Expression #{filterNumber}");

            var mediaTypes = WorkflowConsole.Prompt("Applicable media types, comma-separated", "application/json")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(mediaType => !string.IsNullOrWhiteSpace(mediaType))
                .ToList();

            if (mediaTypes.Count == 0)
            {
                mediaTypes.Add("application/json");
            }

            var language = WorkflowConsole.Prompt("Filter language", "JSONPath");
            var languageVersion = WorkflowConsole.Prompt("Filter language version", "com.jayway.jsonpath:json-path:2.4.0");
            var expression = WorkflowConsole.PromptRequired("Filter expression");
            if (expression is null)
            {
                System.Console.WriteLine(filters.Count == 0
                    ? "Filter setup cancelled. No subscription session was opened."
                    : "Filter setup cancelled. No subscription session was opened.");
                return new FilterPromptResult(null, Cancelled: true);
            }

            filters.Add(new WrapperFilterExpression
            {
                ApplicableMediaTypes = mediaTypes,
                Expression = expression,
                Language = language,
                LanguageVersion = languageVersion,
                Namespaces = []
            });

            if (!WorkflowConsole.AskYesNo("Add another filter expression? [y/N]", defaultYes: false))
            {
                return new FilterPromptResult(filters, Cancelled: false);
            }

            filterNumber++;
        }
    }

    private void ClearSessionState()
    {
        _currentSessionId = null;
        _lastReadMessageId = null;
    }

    private static bool RunAndStay(Action action)
    {
        action();
        return false;
    }

    private static bool InvalidAndStay()
    {
        System.Console.WriteLine("Invalid menu choice.");
        return false;
    }

    private sealed record ConsumerPrompt(string Host, string Channel, string Topic, string User, string Password, bool Raw);

    private sealed record FilterPromptResult(IReadOnlyCollection<WrapperFilterExpression>? Filters, bool Cancelled);
}
