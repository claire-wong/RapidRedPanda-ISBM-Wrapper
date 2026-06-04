using RapidRedPanda.ISBM.ClientAdapter;
using RapidRedPanda.ISBM.ClientAdapter.EndpointOptions;
using RapidRedPanda.Wrapper.Publication;
using RapidRedPanda.Wrapper.Responses;

namespace RapidRedPanda.Wrapper.Request;

public sealed class ProviderRequestWrapper
{
    private const string OpenProviderRequestSessionCommand = "open-provider-request-session";
    private const string ReadRequestCommand = "read-request";
    private const string PostResponseCommand = "post-response";
    private const string RemoveRequestCommand = "remove-request";
    private const string CloseProviderRequestSessionCommand = "close-provider-request-session";

    public WrapperResponse OpenProviderRequestSession(
        string host,
        string channel,
        string topic,
        string user,
        string password,
        bool includeRaw = false)
    {
        return OpenProviderRequestSession(host, channel, topic, user, password, includeRaw, filterExpressions: null);
    }

    public WrapperResponse OpenProviderRequestSession(
        string host,
        string channel,
        string topic,
        string user,
        string password,
        bool includeRaw,
        IReadOnlyCollection<WrapperFilterExpression>? filterExpressions)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("channel", channel),
            ("topic", topic),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(OpenProviderRequestSessionCommand, $"Missing required parameter: --{missing}");
        }

        var activeFilterExpressions = filterExpressions?
            .Where(filterExpression => filterExpression is not null && !filterExpression.IsEmpty)
            .ToList();

        var filterValidationMessage = ValidateFilterExpressions(activeFilterExpressions);
        if (filterValidationMessage is not null)
        {
            return WrapperResponse.ValidationFailure(OpenProviderRequestSessionCommand, filterValidationMessage);
        }

        try
        {
            var service = CreateService(user, password);
            var response = activeFilterExpressions is null || activeFilterExpressions.Count == 0
                ? service.OpenProviderRequestSession(host, channel, topic)
                : service.OpenProviderRequestSession(host, channel, topic, CreateOpenProviderRequestSessionOptions(activeFilterExpressions));

            if (response.StatusCode != 201)
            {
                return WrapperResponse.FaultResponse(OpenProviderRequestSessionCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                OpenProviderRequestSessionCommand,
                new
                {
                    statusCode = response.StatusCode,
                    sessionId = response.SessionID
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(OpenProviderRequestSessionCommand, exception);
        }
    }

    private static OpenProviderRequestSessionOptions CreateOpenProviderRequestSessionOptions(
        IReadOnlyCollection<WrapperFilterExpression> filterExpressions)
    {
        return new OpenProviderRequestSessionOptions
        {
            FilterExpressions = filterExpressions.Select(filterExpression => new FilterExpression
            {
                ApplicableMediaTypes = NormalizeApplicableMediaTypes(filterExpression.ApplicableMediaTypes)
                    .Select(mediaType => new ApplicableMediaType { MediaType = mediaType })
                    .ToList(),
                ExpressionString = new ExpressionString
                {
                    Expression = filterExpression.Expression?.Trim() ?? "",
                    Language = filterExpression.Language?.Trim() ?? "",
                    LanguageVersion = filterExpression.LanguageVersion?.Trim() ?? ""
                }
            }).ToList()
        };
    }

    private static List<string> NormalizeApplicableMediaTypes(IEnumerable<string> applicableMediaTypes)
    {
        var normalizedMediaTypes = applicableMediaTypes
            .Where(mediaType => !string.IsNullOrWhiteSpace(mediaType))
            .Select(mediaType => mediaType.Trim())
            .ToList();

        if (normalizedMediaTypes.Count == 0)
        {
            normalizedMediaTypes.Add("application/json");
        }

        return normalizedMediaTypes;
    }

    private static string? ValidateFilterExpressions(IReadOnlyCollection<WrapperFilterExpression>? filterExpressions)
    {
        if (filterExpressions is null || filterExpressions.Count == 0)
        {
            return null;
        }

        foreach (var filterExpression in filterExpressions)
        {
            if (string.IsNullOrWhiteSpace(filterExpression.Expression))
            {
                return "Filter expression is required when filter options are provided.";
            }

            if (string.IsNullOrWhiteSpace(filterExpression.Language))
            {
                return "Filter language is required when filter expression is provided.";
            }

            foreach (var filterNamespace in filterExpression.Namespaces)
            {
                if (string.IsNullOrWhiteSpace(filterNamespace.Name))
                {
                    return "Filter namespace name must not be empty.";
                }
            }

            if (filterExpression.Namespaces.Count > 0)
            {
                return "Filter namespaces are not supported by RapidRedPanda.ISBM.ClientAdapter 2.0.2.4 OpenProviderRequestSessionOptions.";
            }
        }

        return null;
    }

    public WrapperResponse ReadRequest(
        string host,
        string sessionId,
        string user,
        string password,
        bool includeRaw = false)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("session-id", sessionId),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(ReadRequestCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.ReadRequest(host, sessionId);

            if (response.StatusCode != 200)
            {
                return WrapperResponse.FaultResponse(ReadRequestCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                ReadRequestCommand,
                new
                {
                    statusCode = response.StatusCode,
                    requestMessageId = response.MessageID,
                    messageContent = response.MessageContent
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(ReadRequestCommand, exception);
        }
    }

    public WrapperResponse PostResponse(
        string host,
        string sessionId,
        string requestMessageId,
        string responseContent,
        string user,
        string password,
        bool includeRaw = false)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("session-id", sessionId),
            ("request-message-id", requestMessageId),
            ("response", responseContent),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(PostResponseCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.PostResponse(host, sessionId, requestMessageId, responseContent);

            if (response.StatusCode != 201)
            {
                return WrapperResponse.FaultResponse(PostResponseCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                PostResponseCommand,
                new
                {
                    statusCode = response.StatusCode,
                    requestMessageId,
                    responseMessageId = response.MessageID
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(PostResponseCommand, exception);
        }
    }

    public WrapperResponse RemoveRequest(
        string host,
        string sessionId,
        string user,
        string password,
        bool includeRaw = false)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("session-id", sessionId),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(RemoveRequestCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.RemoveRequest(host, sessionId);

            if (response.StatusCode != 204)
            {
                return WrapperResponse.FaultResponse(RemoveRequestCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                RemoveRequestCommand,
                new
                {
                    statusCode = response.StatusCode,
                    removed = true
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(RemoveRequestCommand, exception);
        }
    }

    public WrapperResponse CloseProviderRequestSession(
        string host,
        string sessionId,
        string user,
        string password,
        bool includeRaw = false)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("session-id", sessionId),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(CloseProviderRequestSessionCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.CloseProviderRequestSession(host, sessionId);

            if (response.StatusCode != 204)
            {
                return WrapperResponse.FaultResponse(CloseProviderRequestSessionCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                CloseProviderRequestSessionCommand,
                new
                {
                    statusCode = response.StatusCode,
                    closed = true
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(CloseProviderRequestSessionCommand, exception);
        }
    }

    private static ProviderRequestService CreateService(string user, string password)
    {
        var service = new ProviderRequestService();
        service.Credential.Username = user;
        service.Credential.Password = password;
        return service;
    }

    private static string? ValidateRequired(params (string Name, string? Value)[] values)
    {
        foreach (var (name, value) in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return name;
            }
        }

        return null;
    }
}
