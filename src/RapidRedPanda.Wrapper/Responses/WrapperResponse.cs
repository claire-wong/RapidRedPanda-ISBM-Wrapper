using System.Text.Json.Nodes;

namespace RapidRedPanda.Wrapper.Responses;

public sealed class WrapperResponse
{
    public bool Success { get; init; }

    public string Command { get; init; } = "";

    public string TimestampUtc { get; init; } = DateTime.UtcNow.ToString("O");

    public object? Data { get; init; }

    public string? Raw { get; init; }

    public WrapperFault? Fault { get; init; }

    public WrapperTransportFault? TransportFault { get; init; }

    internal static WrapperResponse SuccessResponse(string command, object data, string? raw)
    {
        return new WrapperResponse
        {
            Success = true,
            Command = command,
            Data = data,
            Raw = string.IsNullOrEmpty(raw) ? null : raw,
            Fault = null,
            TransportFault = null
        };
    }

    internal static WrapperResponse FaultResponse(string command, int statusCode, string? raw, bool includeRaw)
    {
        if (IsLikelySdkTransportFault(statusCode, raw))
        {
            return new WrapperResponse
            {
                Success = false,
                Command = command,
                Data = null,
                Raw = null,
                Fault = null,
                TransportFault = new WrapperTransportFault(
                    "SdkTransportError",
                    string.IsNullOrWhiteSpace(raw) ? "The SDK reported a transport error." : raw,
                    new { statusCode })
            };
        }

        return new WrapperResponse
        {
            Success = false,
            Command = command,
            Data = null,
            Raw = includeRaw && !string.IsNullOrEmpty(raw) ? raw : null,
            Fault = WrapperFault.FromSdkResponse(statusCode, raw),
            TransportFault = null
        };
    }

    public static WrapperResponse ValidationFailure(string command, string message)
    {
        return new WrapperResponse
        {
            Success = false,
            Command = command,
            Data = null,
            Raw = null,
            Fault = null,
            TransportFault = new WrapperTransportFault("ValidationError", message, null)
        };
    }

    internal static WrapperResponse ExceptionFailure(string command, Exception exception)
    {
        return new WrapperResponse
        {
            Success = false,
            Command = command,
            Data = null,
            Raw = null,
            Fault = null,
            TransportFault = new WrapperTransportFault(exception.GetType().Name, exception.Message, null)
        };
    }

    private static bool IsLikelySdkTransportFault(int statusCode, string? raw)
    {
        if (statusCode != 400 || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.ToLowerInvariant();
        return text.Contains("one or more errors occurred", StringComparison.Ordinal)
            || text.Contains("no connection could be made", StringComparison.Ordinal)
            || text.Contains("connection refused", StringComparison.Ordinal)
            || text.Contains("actively refused", StringComparison.Ordinal)
            || text.Contains("no such host", StringComparison.Ordinal)
            || text.Contains("remote name could not be resolved", StringComparison.Ordinal)
            || text.Contains("name or service not known", StringComparison.Ordinal)
            || text.Contains("ssl connection could not be established", StringComparison.Ordinal)
            || text.Contains("certificate", StringComparison.Ordinal)
            || text.Contains("timed out", StringComparison.Ordinal)
            || text.Contains("timeout", StringComparison.Ordinal);
    }
}

public sealed record WrapperFault(string Code, string Message, object? Details)
{
    internal static WrapperFault FromSdkResponse(int statusCode, string? raw)
    {
        var message = string.IsNullOrWhiteSpace(raw)
            ? $"ISBM operation failed with HTTP status {statusCode}."
            : raw;

        object? details = TryParseJson(raw);
        if (details is null && !string.IsNullOrWhiteSpace(raw))
        {
            details = new { body = raw };
        }

        return new WrapperFault(statusCode.ToString(), message, details);
    }

    private static JsonNode? TryParseJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(raw);
        }
        catch
        {
            return null;
        }
    }
}

public sealed record WrapperTransportFault(string Type, string Message, object? Details);
