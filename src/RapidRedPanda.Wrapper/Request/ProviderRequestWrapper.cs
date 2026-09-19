using RapidRedPanda.ISBM.ClientAdapter;
using RapidRedPanda.ISBM.ClientAdapter.EndpointOptions;
using RapidRedPanda.ISBM.ClientAdapter.ResponseType;
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

    private readonly Func<string, string, IProviderRequestService> _createService;

    public ProviderRequestWrapper()
        : this(CreateService)
    {
    }

    internal ProviderRequestWrapper(Func<string, string, IProviderRequestService> createService)
    {
        _createService = createService;
    }

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
            var service = _createService(user, password);
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
                ApplicableMediaTypes = NormalizeApplicableMediaTypes(filterExpression.ApplicableMediaTypes),
                ExpressionString = new ExpressionString
                {
                    Expression = filterExpression.Expression?.Trim() ?? "",
                    Language = filterExpression.Language?.Trim() ?? "",
                    LanguageVersion = filterExpression.LanguageVersion?.Trim() ?? ""
                },
                Namespaces = filterExpression.Namespaces.Select(filterNamespace => new FilterExpressionNamespace
                {
                    Prefix = filterNamespace.Prefix ?? "",
                    Name = filterNamespace.Name
                }).ToList()
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
            var service = _createService(user, password);
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
        return PostResponse(host, sessionId, requestMessageId, responseContent, user, password, includeRaw, mediaType: null);
    }

    public WrapperResponse PostResponse(
        string host,
        string sessionId,
        string requestMessageId,
        string responseContent,
        string user,
        string password,
        bool includeRaw,
        string? mediaType)
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
            var service = _createService(user, password);
            var normalizedMediaType = NormalizeOptionalMediaType(mediaType);
            var response = normalizedMediaType is null
                ? service.PostResponse(host, sessionId, requestMessageId, responseContent)
                : service.PostResponse(
                    host,
                    sessionId,
                    requestMessageId,
                    responseContent,
                    new PostResponseOptions { MediaType = normalizedMediaType });

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

    public Task<WrapperResponse> PostResponseAsync(
        string host,
        string sessionId,
        string requestMessageId,
        string responseContent,
        string user,
        string password,
        bool includeRaw = false,
        CancellationToken cancellationToken = default)
    {
        return PostResponseAsync(
            host,
            sessionId,
            requestMessageId,
            responseContent,
            user,
            password,
            includeRaw,
            mediaType: null,
            cancellationToken);
    }

    public async Task<WrapperResponse> PostResponseAsync(
        string host,
        string sessionId,
        string requestMessageId,
        string responseContent,
        string user,
        string password,
        bool includeRaw,
        string? mediaType,
        CancellationToken cancellationToken = default)
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
            var service = _createService(user, password);
            var normalizedMediaType = NormalizeOptionalMediaType(mediaType);
            var response = normalizedMediaType is null
                ? await service.PostResponseAsync(host, sessionId, requestMessageId, responseContent, cancellationToken)
                : await service.PostResponseAsync(
                    host,
                    sessionId,
                    requestMessageId,
                    responseContent,
                    new PostResponseOptions { MediaType = normalizedMediaType },
                    cancellationToken);

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
            var service = _createService(user, password);
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
            var service = _createService(user, password);
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

    private static IProviderRequestService CreateService(string user, string password)
    {
        var service = new ProviderRequestService();
        service.Credential.Username = user;
        service.Credential.Password = password;
        return new ProviderRequestServiceAdapter(service);
    }

    private static string? NormalizeOptionalMediaType(string? mediaType)
    {
        return string.IsNullOrWhiteSpace(mediaType) ? null : mediaType;
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

internal interface IProviderRequestService
{
    OpenProviderRequestSessionResponse OpenProviderRequestSession(string hostAddress, string channelId, string topic);

    OpenProviderRequestSessionResponse OpenProviderRequestSession(
        string hostAddress,
        string channelId,
        string topic,
        OpenProviderRequestSessionOptions openProviderRequestSessionOptions);

    ReadRequestResponse ReadRequest(string hostAddress, string sessionId);

    PostResponseResponse PostResponse(string hostAddress, string sessionId, string requestMessageId, string bodMessage);

    PostResponseResponse PostResponse(
        string hostAddress,
        string sessionId,
        string requestMessageId,
        string bodMessage,
        PostResponseOptions postResponseOptions);

    Task<PostResponseResponse> PostResponseAsync(
        string hostAddress,
        string sessionId,
        string requestMessageId,
        string bodMessage,
        CancellationToken cancellationToken = default);

    Task<PostResponseResponse> PostResponseAsync(
        string hostAddress,
        string sessionId,
        string requestMessageId,
        string bodMessage,
        PostResponseOptions postResponseOptions,
        CancellationToken cancellationToken = default);

    RemoveRequestResponse RemoveRequest(string hostAddress, string sessionId);

    CloseProviderRequestSessionResponse CloseProviderRequestSession(string hostAddress, string sessionId);
}

internal sealed class ProviderRequestServiceAdapter(ProviderRequestService service) : IProviderRequestService
{
    public OpenProviderRequestSessionResponse OpenProviderRequestSession(string hostAddress, string channelId, string topic)
    {
        return service.OpenProviderRequestSession(hostAddress, channelId, topic);
    }

    public OpenProviderRequestSessionResponse OpenProviderRequestSession(
        string hostAddress,
        string channelId,
        string topic,
        OpenProviderRequestSessionOptions openProviderRequestSessionOptions)
    {
        return service.OpenProviderRequestSession(hostAddress, channelId, topic, openProviderRequestSessionOptions);
    }

    public ReadRequestResponse ReadRequest(string hostAddress, string sessionId)
    {
        return service.ReadRequest(hostAddress, sessionId);
    }

    public PostResponseResponse PostResponse(string hostAddress, string sessionId, string requestMessageId, string bodMessage)
    {
        return service.PostResponse(hostAddress, sessionId, requestMessageId, bodMessage);
    }

    public PostResponseResponse PostResponse(
        string hostAddress,
        string sessionId,
        string requestMessageId,
        string bodMessage,
        PostResponseOptions postResponseOptions)
    {
        return service.PostResponse(hostAddress, sessionId, requestMessageId, bodMessage, postResponseOptions);
    }

    public Task<PostResponseResponse> PostResponseAsync(
        string hostAddress,
        string sessionId,
        string requestMessageId,
        string bodMessage,
        CancellationToken cancellationToken = default)
    {
        return service.PostResponseAsync(hostAddress, sessionId, requestMessageId, bodMessage, cancellationToken);
    }

    public Task<PostResponseResponse> PostResponseAsync(
        string hostAddress,
        string sessionId,
        string requestMessageId,
        string bodMessage,
        PostResponseOptions postResponseOptions,
        CancellationToken cancellationToken = default)
    {
        return service.PostResponseAsync(hostAddress, sessionId, requestMessageId, bodMessage, postResponseOptions, cancellationToken);
    }

    public RemoveRequestResponse RemoveRequest(string hostAddress, string sessionId)
    {
        return service.RemoveRequest(hostAddress, sessionId);
    }

    public CloseProviderRequestSessionResponse CloseProviderRequestSession(string hostAddress, string sessionId)
    {
        return service.CloseProviderRequestSession(hostAddress, sessionId);
    }
}
