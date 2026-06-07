using RapidRedPanda.Wrapper.Console.Configuration;
using RapidRedPanda.Wrapper.Publication;
using RapidRedPanda.Wrapper.Request;

namespace RapidRedPanda.Wrapper.Console.Interactive;

internal sealed class ProviderRequestWorkflow
{
    private readonly ProviderRequestWrapper _wrapper = new();
    private readonly IsbmConsoleSettings _settings;
    private string? _currentSessionId;
    private string? _currentRequestMessageId;

    public ProviderRequestWorkflow()
        : this(IsbmConsoleSettings.Empty)
    {
    }

    public ProviderRequestWorkflow(IsbmConsoleSettings settings)
    {
        _settings = settings;
    }

    public void RunMenu()
    {
        while (true)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Provider Request Console");
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
            System.Console.WriteLine("State: No Provider Request Session");
            System.Console.WriteLine("1. Open Provider Request Session");
            System.Console.WriteLine("0. Back to Role Selection");
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            System.Console.WriteLine("State: Provider Request Session Open, No Request Loaded");
            System.Console.WriteLine($"Current provider request sessionId: {_currentSessionId}");
            System.Console.WriteLine("1. Read Request");
            System.Console.WriteLine("2. Close Provider Request Session");
            System.Console.WriteLine("3. Show Current Provider Request Session");
            System.Console.WriteLine("0. Back to Role Selection");
            return;
        }

        System.Console.WriteLine("State: Request Loaded, Awaiting Response");
        System.Console.WriteLine($"Current provider request sessionId: {_currentSessionId}");
        System.Console.WriteLine($"Current request message id: {_currentRequestMessageId}");
        System.Console.WriteLine("1. Post Response");
        System.Console.WriteLine("2. Remove Last Read Request");
        System.Console.WriteLine("3. Close Provider Request Session");
        System.Console.WriteLine("4. Show Current Provider Request Session");
        System.Console.WriteLine("0. Back to Role Selection");
    }

    private bool HandleStateChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            return choice switch
            {
                "1" => RunAndStay(OpenProviderRequestSession),
                "0" => true,
                _ => InvalidAndStay()
            };
        }

        if (string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            return choice switch
            {
                "1" => RunAndStay(ReadRequest),
                "2" => RunAndStay(CloseProviderRequestSession),
                "3" => RunAndStay(ShowCurrentSession),
                "0" => BackWithOpenSessionWarning(),
                _ => InvalidAndStay()
            };
        }

        return choice switch
        {
            "1" => RunAndStay(PostResponse),
            "2" => RunAndStay(RemoveLastReadRequest),
            "3" => RunAndStay(CloseProviderRequestSession),
            "4" => RunAndStay(ShowCurrentSession),
            "0" => BackWithOpenSessionWarning(),
            _ => InvalidAndStay()
        };
    }

    private void OpenProviderRequestSession()
    {
        if (!string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("A provider request session is already active. Close it before opening another.");
            return;
        }

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
        var filterExpressions = PromptFilterExpressions();
        if (filterExpressions.Cancelled)
        {
            return;
        }

        var response = _wrapper.OpenProviderRequestSession(host, channel, topic, user, password, raw, filterExpressions.Filters);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _currentSessionId = WorkflowConsole.GetStringDataValue(response, "sessionId");
            _currentRequestMessageId = null;
        }
    }

    private void ReadRequest()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current provider request session is open.");
            return;
        }

        var prompt = PromptConnection();
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.ReadRequest(prompt.Host, _currentSessionId, prompt.User, prompt.Password, prompt.Raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _currentRequestMessageId = WorkflowConsole.GetStringDataValue(response, "requestMessageId");
        }
    }

    private void PostResponse()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current provider request session is open.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            System.Console.WriteLine("No request has been read. Read a request before posting a response.");
            return;
        }

        var responseContent = ReadMultilineResponse();
        if (responseContent is null)
        {
            return;
        }

        var prompt = PromptConnection();
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.PostResponse(
            prompt.Host,
            _currentSessionId,
            _currentRequestMessageId,
            responseContent,
            prompt.User,
            prompt.Password,
            prompt.Raw);
        WorkflowConsole.WriteJson(response);
    }

    private void RemoveLastReadRequest()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current provider request session is open.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            System.Console.WriteLine("No last-read request is available to remove.");
            return;
        }

        var prompt = PromptConnection();
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.RemoveRequest(prompt.Host, _currentSessionId, prompt.User, prompt.Password, prompt.Raw);
        WorkflowConsole.WriteJson(response);
        if (response.Success)
        {
            _currentRequestMessageId = null;
        }
    }

    private void CloseProviderRequestSession()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            System.Console.WriteLine("No current provider request session is open.");
            return;
        }

        var prompt = PromptConnection();
        if (prompt is null)
        {
            return;
        }

        var response = _wrapper.CloseProviderRequestSession(prompt.Host, _currentSessionId, prompt.User, prompt.Password, prompt.Raw);
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
            ? "Current provider request sessionId: none"
            : $"Current provider request sessionId: {_currentSessionId}");
        System.Console.WriteLine(string.IsNullOrWhiteSpace(_currentRequestMessageId)
            ? "Current request message id: none"
            : $"Current request message id: {_currentRequestMessageId}");
    }

    private bool BackWithOpenSessionWarning()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            return true;
        }

        System.Console.WriteLine("A provider request session is still open.");
        if (!string.IsNullOrWhiteSpace(_currentRequestMessageId))
        {
            System.Console.WriteLine("A request is loaded and should be responded to or removed before leaving.");
        }

        if (!WorkflowConsole.AskYesNo("Close the provider request session before leaving? [Y/n]", defaultYes: true))
        {
            return true;
        }

        CloseProviderRequestSession();
        return string.IsNullOrWhiteSpace(_currentSessionId);
    }

    private ProviderRequestPrompt? PromptConnection()
    {
        var host = WorkflowConsole.PromptHost(_settings);
        if (host is null)
        {
            return null;
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

        return new ProviderRequestPrompt(host, user, password, WorkflowConsole.PromptRaw(_settings));
    }

    private string GetStateName()
    {
        if (string.IsNullOrWhiteSpace(_currentSessionId))
        {
            return "No Provider Request Session";
        }

        return string.IsNullOrWhiteSpace(_currentRequestMessageId)
            ? "Provider Request Session Open, No Request Loaded"
            : "Request Loaded, Awaiting Response";
    }

    private void ClearSessionState()
    {
        _currentSessionId = null;
        _currentRequestMessageId = null;
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
                System.Console.WriteLine("Filter setup cancelled. No provider request session was opened.");
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

    private static string? ReadMultilineResponse()
    {
        System.Console.WriteLine("Enter response message content.");
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
                System.Console.WriteLine("Response message content is required. Enter message content or type cancel.");
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

    private sealed record ProviderRequestPrompt(string Host, string User, string Password, bool Raw);

    private sealed record FilterPromptResult(IReadOnlyCollection<WrapperFilterExpression>? Filters, bool Cancelled);
}
