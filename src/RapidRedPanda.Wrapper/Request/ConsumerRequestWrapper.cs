using RapidRedPanda.ISBM.ClientAdapter;
using RapidRedPanda.ISBM.ClientAdapter.EndpointOptions;
using RapidRedPanda.ISBM.ClientAdapter.ResponseType;
using RapidRedPanda.Wrapper.Responses;

namespace RapidRedPanda.Wrapper.Request;

public sealed class ConsumerRequestWrapper
{
    private const string OpenRequestSessionCommand = "open-request-session";
    private const string PostRequestCommand = "post-request";
    private const string ExpireRequestCommand = "expire-request";
    private const string ReadResponseCommand = "read-response";
    private const string RemoveResponseCommand = "remove-response";
    private const string CloseRequestSessionCommand = "close-request-session";

    private readonly Func<string, string, IConsumerRequestService> _createService;

    public ConsumerRequestWrapper()
        : this(CreateService)
    {
    }

    internal ConsumerRequestWrapper(Func<string, string, IConsumerRequestService> createService)
    {
        _createService = createService;
    }

    public WrapperResponse OpenRequestSession(
        string host,
        string channel,
        string user,
        string password,
        bool includeRaw = false)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("channel", channel),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(OpenRequestSessionCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = _createService(user, password);
            var response = service.OpenConsumerRequestSession(host, channel);

            if (response.StatusCode != 201)
            {
                return WrapperResponse.FaultResponse(OpenRequestSessionCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                OpenRequestSessionCommand,
                new
                {
                    statusCode = response.StatusCode,
                    sessionId = response.SessionID
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(OpenRequestSessionCommand, exception);
        }
    }

    public WrapperResponse PostRequest(
        string host,
        string sessionId,
        string topic,
        string messageContent,
        string user,
        string password,
        bool includeRaw = false)
    {
        return PostRequest(host, sessionId, topic, messageContent, user, password, includeRaw, expiry: null, mediaType: null);
    }

    public WrapperResponse PostRequest(
        string host,
        string sessionId,
        string topic,
        string messageContent,
        string user,
        string password,
        bool includeRaw,
        string? expiry)
    {
        return PostRequest(host, sessionId, topic, messageContent, user, password, includeRaw, expiry, mediaType: null);
    }

    public WrapperResponse PostRequest(
        string host,
        string sessionId,
        string topic,
        string messageContent,
        string user,
        string password,
        bool includeRaw,
        string? expiry,
        string? mediaType)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("session-id", sessionId),
            ("topic", topic),
            ("message", messageContent),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(PostRequestCommand, $"Missing required parameter: --{missing}");
        }

        var normalizedExpiry = NormalizeOptionalExpiry(expiry);
        if (expiry is not null && normalizedExpiry is null)
        {
            return WrapperResponse.ValidationFailure(PostRequestCommand, "Invalid expiry: value cannot be blank.");
        }

        try
        {
            var service = _createService(user, password);
            var normalizedMediaType = NormalizeOptionalMediaType(mediaType);
            var response = normalizedExpiry is null
                && normalizedMediaType is null
                ? service.PostRequest(host, sessionId, topic, messageContent)
                : service.PostRequest(
                    host,
                    sessionId,
                    topic,
                    messageContent,
                    new PostRequestOptions
                    {
                        Expiry = normalizedExpiry ?? "",
                        MediaType = normalizedMediaType ?? ""
                    });

            if (response.StatusCode != 201)
            {
                return WrapperResponse.FaultResponse(PostRequestCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            object data = normalizedExpiry is null
                ? new
                {
                    statusCode = response.StatusCode,
                    messageId = response.MessageID
                }
                : new
                {
                    statusCode = response.StatusCode,
                    messageId = response.MessageID,
                    expiry = normalizedExpiry
                };

            return WrapperResponse.SuccessResponse(
                PostRequestCommand,
                data,
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(PostRequestCommand, exception);
        }
    }

    public Task<WrapperResponse> PostRequestAsync(
        string host,
        string sessionId,
        string topic,
        string messageContent,
        string user,
        string password,
        bool includeRaw = false,
        CancellationToken cancellationToken = default)
    {
        return PostRequestAsync(
            host,
            sessionId,
            topic,
            messageContent,
            user,
            password,
            includeRaw,
            expiry: null,
            mediaType: null,
            cancellationToken);
    }

    public async Task<WrapperResponse> PostRequestAsync(
        string host,
        string sessionId,
        string topic,
        string messageContent,
        string user,
        string password,
        bool includeRaw,
        string? expiry,
        string? mediaType,
        CancellationToken cancellationToken = default)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("session-id", sessionId),
            ("topic", topic),
            ("message", messageContent),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(PostRequestCommand, $"Missing required parameter: --{missing}");
        }

        var normalizedExpiry = NormalizeOptionalExpiry(expiry);
        if (expiry is not null && normalizedExpiry is null)
        {
            return WrapperResponse.ValidationFailure(PostRequestCommand, "Invalid expiry: value cannot be blank.");
        }

        try
        {
            var service = _createService(user, password);
            var normalizedMediaType = NormalizeOptionalMediaType(mediaType);
            var response = normalizedExpiry is null && normalizedMediaType is null
                ? await service.PostRequestAsync(host, sessionId, topic, messageContent, cancellationToken)
                : await service.PostRequestAsync(
                    host,
                    sessionId,
                    topic,
                    messageContent,
                    new PostRequestOptions
                    {
                        Expiry = normalizedExpiry ?? "",
                        MediaType = normalizedMediaType ?? ""
                    },
                    cancellationToken);

            if (response.StatusCode != 201)
            {
                return WrapperResponse.FaultResponse(PostRequestCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            object data = normalizedExpiry is null
                ? new
                {
                    statusCode = response.StatusCode,
                    messageId = response.MessageID
                }
                : new
                {
                    statusCode = response.StatusCode,
                    messageId = response.MessageID,
                    expiry = normalizedExpiry
                };

            return WrapperResponse.SuccessResponse(
                PostRequestCommand,
                data,
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(PostRequestCommand, exception);
        }
    }

    public WrapperResponse ReadResponse(
        string host,
        string sessionId,
        string requestMessageId,
        string user,
        string password,
        bool includeRaw = false)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("session-id", sessionId),
            ("request-message-id", requestMessageId),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(ReadResponseCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = _createService(user, password);
            var response = service.ReadResponse(host, sessionId, requestMessageId);

            if (response.StatusCode != 200)
            {
                return WrapperResponse.FaultResponse(ReadResponseCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                ReadResponseCommand,
                new
                {
                    statusCode = response.StatusCode,
                    requestMessageId,
                    messageId = response.MessageID,
                    messageContent = response.MessageContent
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(ReadResponseCommand, exception);
        }
    }

    public WrapperResponse ExpireRequest(
        string host,
        string sessionId,
        string messageId,
        string user,
        string password,
        bool includeRaw = false)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("session-id", sessionId),
            ("message-id", messageId),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(ExpireRequestCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = _createService(user, password);
            var response = service.ExpireRequest(host, sessionId, messageId);

            if (response.StatusCode != 204)
            {
                return WrapperResponse.FaultResponse(ExpireRequestCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                ExpireRequestCommand,
                new
                {
                    statusCode = response.StatusCode,
                    expired = true
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(ExpireRequestCommand, exception);
        }
    }

    public WrapperResponse RemoveResponse(
        string host,
        string sessionId,
        string requestMessageId,
        string user,
        string password,
        bool includeRaw = false)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("session-id", sessionId),
            ("request-message-id", requestMessageId),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(RemoveResponseCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = _createService(user, password);
            var response = service.RemoveResponse(host, sessionId, requestMessageId);

            if (response.StatusCode != 204)
            {
                return WrapperResponse.FaultResponse(RemoveResponseCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                RemoveResponseCommand,
                new
                {
                    statusCode = response.StatusCode,
                    removed = true
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(RemoveResponseCommand, exception);
        }
    }

    public WrapperResponse CloseRequestSession(
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
            return WrapperResponse.ValidationFailure(CloseRequestSessionCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = _createService(user, password);
            var response = service.CloseConsumerRequestSession(host, sessionId);

            if (response.StatusCode != 204)
            {
                return WrapperResponse.FaultResponse(CloseRequestSessionCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                CloseRequestSessionCommand,
                new
                {
                    statusCode = response.StatusCode,
                    closed = true
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(CloseRequestSessionCommand, exception);
        }
    }

    private static IConsumerRequestService CreateService(string user, string password)
    {
        var service = new ConsumerRequestService();
        service.Credential.Username = user;
        service.Credential.Password = password;
        return new ConsumerRequestServiceAdapter(service);
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

    private static string? NormalizeOptionalExpiry(string? expiry)
    {
        return string.IsNullOrWhiteSpace(expiry) ? null : expiry.Trim();
    }

    private static string? NormalizeOptionalMediaType(string? mediaType)
    {
        return string.IsNullOrWhiteSpace(mediaType) ? null : mediaType;
    }
}

internal interface IConsumerRequestService
{
    OpenConsumerRequestSessionResponse OpenConsumerRequestSession(string hostAddress, string channelId);

    PostRequestResponse PostRequest(string hostAddress, string sessionId, string topic, string bodMessage);

    PostRequestResponse PostRequest(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        PostRequestOptions postRequestOptions);

    Task<PostRequestResponse> PostRequestAsync(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        CancellationToken cancellationToken = default);

    Task<PostRequestResponse> PostRequestAsync(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        PostRequestOptions postRequestOptions,
        CancellationToken cancellationToken = default);

    ReadResponseResponse ReadResponse(string hostAddress, string sessionId, string requestMessageId);

    ExpireRequestResponse ExpireRequest(string hostAddress, string sessionId, string messageId);

    RemoveResponseResponse RemoveResponse(string hostAddress, string sessionId, string requestMessageId);

    CloseConsumerRequestSessionResponse CloseConsumerRequestSession(string hostAddress, string sessionId);
}

internal sealed class ConsumerRequestServiceAdapter(ConsumerRequestService service) : IConsumerRequestService
{
    public OpenConsumerRequestSessionResponse OpenConsumerRequestSession(string hostAddress, string channelId)
    {
        return service.OpenConsumerRequestSession(hostAddress, channelId);
    }

    public PostRequestResponse PostRequest(string hostAddress, string sessionId, string topic, string bodMessage)
    {
        return service.PostRequest(hostAddress, sessionId, topic, bodMessage);
    }

    public PostRequestResponse PostRequest(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        PostRequestOptions postRequestOptions)
    {
        return service.PostRequest(hostAddress, sessionId, topic, bodMessage, postRequestOptions);
    }

    public Task<PostRequestResponse> PostRequestAsync(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        CancellationToken cancellationToken = default)
    {
        return service.PostRequestAsync(hostAddress, sessionId, topic, bodMessage, cancellationToken);
    }

    public Task<PostRequestResponse> PostRequestAsync(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        PostRequestOptions postRequestOptions,
        CancellationToken cancellationToken = default)
    {
        return service.PostRequestAsync(hostAddress, sessionId, topic, bodMessage, postRequestOptions, cancellationToken);
    }

    public ReadResponseResponse ReadResponse(string hostAddress, string sessionId, string requestMessageId)
    {
        return service.ReadResponse(hostAddress, sessionId, requestMessageId);
    }

    public ExpireRequestResponse ExpireRequest(string hostAddress, string sessionId, string messageId)
    {
        return service.ExpireRequest(hostAddress, sessionId, messageId);
    }

    public RemoveResponseResponse RemoveResponse(string hostAddress, string sessionId, string requestMessageId)
    {
        return service.RemoveResponse(hostAddress, sessionId, requestMessageId);
    }

    public CloseConsumerRequestSessionResponse CloseConsumerRequestSession(string hostAddress, string sessionId)
    {
        return service.CloseConsumerRequestSession(hostAddress, sessionId);
    }
}
