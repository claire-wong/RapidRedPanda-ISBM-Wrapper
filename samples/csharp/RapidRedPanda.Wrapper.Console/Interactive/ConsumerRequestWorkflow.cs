using RapidRedPanda.Wrapper.Console.Configuration;
using RapidRedPanda.Wrapper.Request;

namespace RapidRedPanda.Wrapper.Console.Interactive;

internal sealed class ConsumerRequestWorkflow
{
    private readonly ConsumerRequestWrapper _wrapper = new();
    private readonly IsbmConsoleSettings _settings;
    private string? _currentSessionId;
    private string? _currentRequestMessageId;
    private string? _currentRequestExpiry;
    private string? _lastReadResponseId;

    public ConsumerRequestWorkflow()
        : this(IsbmConsoleSettings.Empty)
    {
    }

    public ConsumerRequestWorkflow(IsbmConsoleSettings settings)
    {
        _settings = settings;
    }

    public void RunMenu()
    {
        while (true)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Consumer Request Console");
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
            System.Console.WriteLine("State: No Request Session");
            System.Console.WriteLine("1. Open Request Session");
            System.Console.WriteLine("0. Back to Role Selection");
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            System.Console.WriteLine("State: Request Session Open, No Request Posted");
            System.Console.WriteLine($"Current request sessionId: {_currentSessionId}");
            System.Console.WriteLine("1. Post Request");
            System.Console.WriteLine("2. Close Request Session");
            System.Console.WriteLine("3. Show Current Request Session");
            System.Console.WriteLine("0. Back to Role Selection");
            return;
        }

        if (string.IsNullOrWhiteSpace(_lastReadResponseId))
        {
            System.Console.WriteLine("State: Request Posted, Awaiting Responses");
            System.Console.WriteLine($"Current request sessionId: {_currentSessionId}");
            System.Console.WriteLine($"Current request message id: {_currentRequestMessageId}");
            WriteCurrentRequestExpiry();
            System.Console.WriteLine("1. Read Response");
            System.Console.WriteLine("2. Expire Request");
            System.Console.WriteLine("3. Close Request Session");
            System.Console.WriteLine("4. Show Current Request Session");
            System.Console.WriteLine("0. Back to Role Selection");
            return;
        }

        System.Console.WriteLine("State: Response Read, Awaiting Removal");
        System.Console.WriteLine($"Current request sessionId: {_currentSessionId}");
        System.Console.WriteLine($"Current request message id: {_currentRequestMessageId}");
        WriteCurrentRequestExpiry();
        System.Console.WriteLine($"Last-read response id: {_lastReadResponseId}");
        System.Console.WriteLine("1. Remove Last Read Response");
        System.Console.WriteLine("2. Close Request Session");
        System.Console.WriteLine("3. Show Current Request Session");
        System.Console.WriteLine("0. Back to Role Selection");
    }

    private bool HandleStateChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            return choice switch
            {
                "1" => RunAndStay(OpenRequestSession),
                "0" => true,
                _ => InvalidAndStay()
            };
        }

        if (string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            return choice switch
            {
                "1" => RunAndStay(PostRequest),
                "2" => RunAndStay(CloseRequestSession),
                "3" => RunAndStay(ShowCurrentSession),
                "0" => BackWithOpenSessionWarning(),
                _ => InvalidAndStay()
            };
        }

        if (string.IsNullOrWhiteSpace(_lastReadResponseId))
        {
            return choice switch
            {
                "1" => RunAndStay(ReadResponse),
                "2" => RunAndStay(ExpireRequest),
                "3" => RunAndStay(CloseRequestSession),
                "4" => RunAndStay(ShowCurrentSession),
                "0" => BackWithOpenSessionWarning(),
                _ => InvalidAndStay()
            };
        }

        return choice switch
        {
            "1" => RunAndStay(RemoveLastReadResponse),
            "2" => RunAndStay(CloseRequestSession),
            "3" => RunAndStay(ShowCurrentSession),
            "0" => BackWithOpenSessionWarning(),
            _ => InvalidAndStay()
        };
    }

    private void OpenRequestSession()
    {
        if (!string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("A request session is already active. Close it before opening another.");
            return;
        }

        var host = WorkflowConsole.PromptHost(_settings);
        var channel = WorkflowConsole.PromptRequestChannel(_settings);
        var user = WorkflowConsole.PromptUser(_settings);
        var password = WorkflowConsole.PromptPassword(_settings);
        if (host is null || channel is null || user is null || password is null)
        {
            return;
        }

        var raw = WorkflowConsole.PromptRaw(_settings);

        var response = _wrapper.OpenRequestSession(host, channel, user, password, raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _currentSessionId = WorkflowConsole.GetStringDataValue(response, "sessionId");
            _currentRequestMessageId = null;
            _currentRequestExpiry = null;
            _lastReadResponseId = null;
        }
    }

    private void PostRequest()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current request session is open.");
            return;
        }

        var messageContent = ReadMultilineMessage();
        if (messageContent is null)
        {
            return;
        }

        var expiryPrompt = PromptRequestExpiry();
        if (!expiryPrompt.ShouldPost)
        {
            return;
        }

        var prompt = PromptConnection(includeTopic: true);
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.PostRequest(
            prompt.Host,
            _currentSessionId,
            prompt.Topic,
            messageContent,
            prompt.User,
            prompt.Password,
            prompt.Raw,
            expiryPrompt.Expiry);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _currentRequestMessageId = WorkflowConsole.GetStringDataValue(response, "messageId");
            _currentRequestExpiry = expiryPrompt.Expiry;
            _lastReadResponseId = null;
        }
    }

    private void ReadResponse()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current request session is open.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            System.Console.WriteLine("No posted request message id is available.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_lastReadResponseId))
        {
            System.Console.WriteLine("Remove the last read response before reading another response.");
            return;
        }

        var prompt = PromptConnection(includeTopic: false);
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.ReadResponse(
            prompt.Host,
            _currentSessionId,
            _currentRequestMessageId,
            prompt.User,
            prompt.Password,
            prompt.Raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _lastReadResponseId = WorkflowConsole.GetStringDataValue(response, "messageId");
        }
    }

    private void ExpireRequest()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current request session is open.");
            return;
        }

        var requestMessageId = _currentRequestMessageId;
        if (string.IsNullOrWhiteSpace(requestMessageId))
        {
            requestMessageId = WorkflowConsole.PromptRequired("Request ID");
            if (string.IsNullOrWhiteSpace(requestMessageId))
            {
                return;
            }
        }

        var prompt = PromptConnection(includeTopic: false);
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.ExpireRequest(
            prompt.Host,
            _currentSessionId,
            requestMessageId,
            prompt.User,
            prompt.Password,
            prompt.Raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _currentRequestMessageId = null;
            _currentRequestExpiry = null;
            _lastReadResponseId = null;
        }
    }

    private void RemoveLastReadResponse()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current request session is open.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentRequestMessageId) || string.IsNullOrWhiteSpace(_lastReadResponseId))
        {
            System.Console.WriteLine("No last-read response is available to remove.");
            return;
        }

        var prompt = PromptConnection(includeTopic: false);
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.RemoveResponse(
            prompt.Host,
            _currentSessionId,
            _currentRequestMessageId,
            prompt.User,
            prompt.Password,
            prompt.Raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _lastReadResponseId = null;
        }
    }

    private void CloseRequestSession()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current request session is open.");
            return;
        }

        var prompt = PromptConnection(includeTopic: false);
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.CloseRequestSession(prompt.Host, _currentSessionId, prompt.User, prompt.Password, prompt.Raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            ClearSessionState();
        }
    }

    private void ShowCurrentSession()
    {
        System.Console.WriteLine($"Current state: {GetStateName()}");
        System.Console.WriteLine(string.IsNullOrWhiteSpace(_currentSessionId)
            ? "Current request sessionId: none"
            : $"Current request sessionId: {_currentSessionId}");
        System.Console.WriteLine(string.IsNullOrWhiteSpace(_currentRequestMessageId)
            ? "Current request message id: none"
            : $"Current request message id: {_currentRequestMessageId}");
        System.Console.WriteLine(string.IsNullOrWhiteSpace(_currentRequestExpiry)
            ? "Current request expiry: none"
            : $"Current request expiry: {_currentRequestExpiry}");
        System.Console.WriteLine(string.IsNullOrWhiteSpace(_lastReadResponseId)
            ? "Last-read response id: none"
            : $"Last-read response id: {_lastReadResponseId}");
    }

    private bool BackWithOpenSessionWarning()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            return true;
        }

        System.Console.WriteLine("A request session is still open.");
        if (!string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            System.Console.WriteLine("A request remains active until it is expired or the request session is closed.");
        }

        if (!WorkflowConsole.AskYesNo("Close the request session before leaving? [Y/n]", defaultYes: true))
        {
            return true;
        }

        CloseRequestSession();
        return string.IsNullOrWhiteSpace(_currentSessionId);
    }

    private ConsumerRequestPrompt? PromptConnection(bool includeTopic)
    {
        var host = WorkflowConsole.PromptHost(_settings);
        if (host is null)
        {
            return null;
        }

        var topic = "";
        if (includeTopic)
        {
            topic = WorkflowConsole.PromptRequestTopic(_settings) ?? "";
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

        return new ConsumerRequestPrompt(host, topic, user, password, WorkflowConsole.PromptRaw(_settings));
    }

    private string GetStateName()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            return "No Request Session";
        }

        if (string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            return "Request Session Open, No Request Posted";
        }

        return string.IsNullOrWhiteSpace(_lastReadResponseId)
            ? "Request Posted, Awaiting Responses"
            : "Response Read, Awaiting Removal";
    }

    private void ClearSessionState()
    {
        _currentSessionId = null;
        _currentRequestMessageId = null;
        _currentRequestExpiry = null;
        _lastReadResponseId = null;
    }

    private void WriteCurrentRequestExpiry()
    {
        if (!string.IsNullOrWhiteSpace(_currentRequestExpiry))
        {
            System.Console.WriteLine($"Current request expiry: {_currentRequestExpiry}");
        }
    }

    private static string? ReadMultilineMessage()
    {
        System.Console.WriteLine("Enter request message content.");
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
                System.Console.WriteLine("Request message content is required. Enter message content or type cancel.");
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

    private static (bool ShouldPost, string? Expiry) PromptRequestExpiry()
    {
        while (true)
        {
            System.Console.Write("Request expiry [blank = no expiry, type cancel to cancel]: ");
            var value = System.Console.ReadLine();
            if (value is null)
            {
                return (false, null);
            }

            if (string.Equals(value, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return (true, null);
            }

            return (true, value.Trim());
        }
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

    private sealed record ConsumerRequestPrompt(string Host, string Topic, string User, string Password, bool Raw);
}
