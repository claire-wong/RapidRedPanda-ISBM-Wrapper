using System.Text.Json;
using System.Text.Json.Serialization;
using RapidRedPanda.Wrapper.Console.Configuration;
using RapidRedPanda.Wrapper.Responses;

namespace RapidRedPanda.Wrapper.Console.Interactive;

internal static class WorkflowConsole
{
    public static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = true
    };

    public static void WriteJson(WrapperResponse response)
    {
        System.Console.WriteLine(JsonSerializer.Serialize(response, PrettyJsonOptions));
    }

    public static string Prompt(string label, string? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(defaultValue))
        {
            System.Console.Write(label + ": ");
        }
        else
        {
            System.Console.Write($"{label} [{defaultValue}]: ");
        }

        var value = System.Console.ReadLine();
        return string.IsNullOrWhiteSpace(value) ? defaultValue ?? "" : value;
    }

    public static string? PromptRequired(string label, string? defaultValue = null, bool isPassword = false)
    {
        while (true)
        {
            WritePrompt(label, defaultValue, isPassword);
            var value = System.Console.ReadLine();
            if (value is null)
            {
                return null;
            }

            var resolved = string.IsNullOrWhiteSpace(value) ? defaultValue ?? "" : value;
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }

            System.Console.Write("Required value is missing. Press Enter to try again or type 0 to cancel: ");
            var retry = System.Console.ReadLine();
            if (retry is null || retry == "0")
            {
                return null;
            }
        }
    }

    public static string? PromptHost(IsbmConsoleSettings settings)
    {
        return PromptRequired("Host", settings.Host);
    }

    public static string? PromptChannel(IsbmConsoleSettings settings)
    {
        return PromptRequired("Channel", settings.Channel);
    }

    public static string? PromptTopic(IsbmConsoleSettings settings)
    {
        return PromptRequired("Topic", settings.Topic);
    }

    public static string? PromptUser(IsbmConsoleSettings settings)
    {
        return PromptRequired("User", settings.User);
    }

    public static string? PromptPassword(IsbmConsoleSettings settings)
    {
        return PromptRequired("Password", settings.Password, isPassword: true);
    }

    public static bool PromptRaw()
    {
        return AskYesNo("Include raw response? [y/N]", defaultYes: false);
    }

    public static bool PromptRaw(IsbmConsoleSettings settings)
    {
        return AskYesNo(settings.IncludeRawDefault ? "Include raw response? [Y/n]" : "Include raw response? [y/N]", settings.IncludeRawDefault);
    }

    public static bool AskYesNo(string message, bool defaultYes)
    {
        System.Console.Write(message + " ");
        var answer = System.Console.ReadLine();
        if (string.IsNullOrWhiteSpace(answer))
        {
            return defaultYes;
        }

        return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public static void WaitForEnter(string message)
    {
        System.Console.WriteLine(message);
        System.Console.ReadLine();
    }

    public static string? GetStringDataValue(WrapperResponse response, string propertyName)
    {
        if (response.Data is null)
        {
            return null;
        }

        var property = response.Data.GetType().GetProperty(propertyName);
        return property?.GetValue(response.Data)?.ToString();
    }

    private static void WritePrompt(string label, string? defaultValue, bool isPassword)
    {
        if (isPassword)
        {
            System.Console.Write(string.IsNullOrWhiteSpace(defaultValue)
                ? $"{label} [empty]: "
                : $"{label} [configured]: ");
            return;
        }

        if (string.IsNullOrWhiteSpace(defaultValue))
        {
            System.Console.Write(label + ": ");
        }
        else
        {
            System.Console.Write($"{label} [{defaultValue}]: ");
        }
    }
}
