using RapidRedPanda.Wrapper.Console.Configuration;
using RapidRedPanda.Wrapper.Publication;

namespace RapidRedPanda.Wrapper.Console.Interactive;

internal sealed class ProviderPublicationWorkflow
{
    private readonly ProviderPublicationWrapper _wrapper = new();
    private readonly IsbmConsoleSettings _settings;
    private string? _currentSessionId;
    private string? _lastPostedMessageId;

    public ProviderPublicationWorkflow()
        : this(IsbmConsoleSettings.Empty)
    {
    }

    public ProviderPublicationWorkflow(IsbmConsoleSettings settings)
    {
        _settings = settings;
    }

    public void RunMenu()
    {
        while (true)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Provider Publication Console");
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

    private void WriteStateMenu()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("State: No Provider Session");
            System.Console.WriteLine("1. Open Provider Session");
            System.Console.WriteLine("0. Back to Role Selection");
            return;
        }

        System.Console.WriteLine("State: Provider Session Open");
        System.Console.WriteLine($"Current provider sessionId: {_currentSessionId}");
        if (!string.IsNullOrWhiteSpace(_lastPostedMessageId))
        {
            System.Console.WriteLine($"Last posted publication/message id: {_lastPostedMessageId}");
        }

        System.Console.WriteLine("1. Post Publication");
        if (string.IsNullOrWhiteSpace(_lastPostedMessageId))
        {
            System.Console.WriteLine("2. Close Provider Session");
            System.Console.WriteLine("3. Show Current Provider Session");
        }
        else
        {
            System.Console.WriteLine("2. Expire Publication");
            System.Console.WriteLine("3. Close Provider Session");
            System.Console.WriteLine("4. Show Current Provider Session");
        }

        System.Console.WriteLine("0. Back to Role Selection");
    }

    private bool HandleStateChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            return choice switch
            {
                "1" => RunAndStay(OpenProviderSession),
                "0" => true,
                _ => InvalidAndStay()
            };
        }

        if (string.IsNullOrWhiteSpace(_lastPostedMessageId))
        {
            return choice switch
            {
                "1" => RunAndStay(PostPublication),
                "2" => RunAndStay(CloseProviderSession),
                "3" => RunAndStay(ShowCurrentSession),
                "0" => true,
                _ => InvalidAndStay()
            };
        }

        return choice switch
        {
            "1" => RunAndStay(PostPublication),
            "2" => RunAndStay(ExpirePublication),
            "3" => RunAndStay(CloseProviderSession),
            "4" => RunAndStay(ShowCurrentSession),
            "0" => true,
            _ => InvalidAndStay()
        };
    }

    private void OpenProviderSession()
    {
        if (!string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("A provider session is already active. Close it before opening another.");
            return;
        }

        var host = WorkflowConsole.PromptHost(_settings);
        var channel = WorkflowConsole.PromptPublicationChannel(_settings);
        var user = WorkflowConsole.PromptUser(_settings);
        var password = WorkflowConsole.PromptPassword(_settings);
        if (host is null || channel is null || user is null || password is null)
        {
            return;
        }

        var raw = WorkflowConsole.PromptRaw(_settings);

        var response = _wrapper.OpenProviderSession(host, channel, user, password, raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _currentSessionId = WorkflowConsole.GetStringDataValue(response, "sessionId");
            _lastPostedMessageId = null;
        }
    }

    private void PostPublication()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current provider session is open.");
            return;
        }

        var messageContent = ReadMultilineMessage();
        if (messageContent is null)
        {
            return;
        }

        var host = WorkflowConsole.PromptHost(_settings);
        var sessionId = WorkflowConsole.Prompt("Session ID", _currentSessionId);
        var topic = WorkflowConsole.PromptPublicationTopic(_settings);
        var expiry = WorkflowConsole.Prompt("Expiry (optional)");
        var user = WorkflowConsole.PromptUser(_settings);
        var password = WorkflowConsole.PromptPassword(_settings);
        if (host is null || string.IsNullOrWhiteSpace(sessionId) || topic is null || user is null || password is null)
        {
            return;
        }

        var raw = WorkflowConsole.PromptRaw(_settings);

        var response = _wrapper.PostPublication(host, sessionId, topic, messageContent, user, password, raw, expiry);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _lastPostedMessageId = WorkflowConsole.GetStringDataValue(response, "messageId");
        }
    }

    private void ExpirePublication()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current provider session is open.");
            return;
        }

        var messageId = _lastPostedMessageId;
        if (string.IsNullOrWhiteSpace(messageId))
        {
            messageId = WorkflowConsole.PromptRequired("Publication/message ID");
            if (messageId is null)
            {
                return;
            }
        }

        var host = WorkflowConsole.PromptHost(_settings);
        var sessionId = WorkflowConsole.Prompt("Session ID", _currentSessionId);
        var user = WorkflowConsole.PromptUser(_settings);
        var password = WorkflowConsole.PromptPassword(_settings);
        if (host is null || string.IsNullOrWhiteSpace(sessionId) || user is null || password is null)
        {
            return;
        }

        var raw = WorkflowConsole.PromptRaw(_settings);

        var response = _wrapper.ExpirePublication(host, sessionId, messageId, user, password, raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success && messageId == _lastPostedMessageId)
        {
            _lastPostedMessageId = null;
        }
    }

    private void CloseProviderSession()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current provider session is open.");
            return;
        }

        var host = WorkflowConsole.PromptHost(_settings);
        var sessionId = WorkflowConsole.Prompt("Session ID", _currentSessionId);
        var user = WorkflowConsole.PromptUser(_settings);
        var password = WorkflowConsole.PromptPassword(_settings);
        if (host is null || string.IsNullOrWhiteSpace(sessionId) || user is null || password is null)
        {
            return;
        }

        var raw = WorkflowConsole.PromptRaw(_settings);

        var response = _wrapper.CloseProviderSession(host, sessionId, user, password, raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success && sessionId == _currentSessionId)
        {
            _currentSessionId = null;
            _lastPostedMessageId = null;
        }
    }

    private void ShowCurrentSession()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current provider session.");
            return;
        }

        System.Console.WriteLine($"Current provider sessionId: {_currentSessionId}");
        System.Console.WriteLine(string.IsNullOrWhiteSpace(_lastPostedMessageId)
            ? "Last posted publication/message id: none"
            : $"Last posted publication/message id: {_lastPostedMessageId}");
    }

    private static string? ReadMultilineMessage()
    {
        System.Console.WriteLine("Enter publication message content.");
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
                System.Console.WriteLine("Message content is required. Enter message content or type cancel.");
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
}
