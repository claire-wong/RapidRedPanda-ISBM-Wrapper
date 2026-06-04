using RapidRedPanda.ISBM.ClientAdapter;
using RapidRedPanda.ISBM.ClientAdapter.EndpointOptions;
using RapidRedPanda.Wrapper.Responses;

namespace RapidRedPanda.Wrapper.Publication;

public sealed class ProviderPublicationWrapper
{
    private const string OpenProviderSessionCommand = "open-provider-session";
    private const string PostPublicationCommand = "post-publication";
    private const string ExpirePublicationCommand = "expire-publication";
    private const string CloseProviderSessionCommand = "close-provider-session";

    public WrapperResponse OpenProviderSession(
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
            return WrapperResponse.ValidationFailure(OpenProviderSessionCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.OpenPublicationSession(host, channel);

            if (response.StatusCode != 201)
            {
                return WrapperResponse.FaultResponse(OpenProviderSessionCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                OpenProviderSessionCommand,
                new
                {
                    statusCode = response.StatusCode,
                    sessionId = response.SessionID
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(OpenProviderSessionCommand, exception);
        }
    }

    public WrapperResponse PostPublication(
        string host,
        string sessionId,
        string topic,
        string messageContent,
        string user,
        string password,
        bool includeRaw = false)
    {
        return PostPublication(host, sessionId, topic, messageContent, user, password, includeRaw, expiry: null);
    }

    public WrapperResponse PostPublication(
        string host,
        string sessionId,
        string topic,
        string messageContent,
        string user,
        string password,
        bool includeRaw,
        string? expiry)
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
            return WrapperResponse.ValidationFailure(PostPublicationCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = string.IsNullOrWhiteSpace(expiry)
                ? service.PostPublication(host, sessionId, topic, messageContent)
                : service.PostPublication(
                    host,
                    sessionId,
                    topic,
                    messageContent,
                    new PostPublicationOptions { Expiry = expiry });

            if (response.StatusCode != 201)
            {
                return WrapperResponse.FaultResponse(PostPublicationCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                PostPublicationCommand,
                new
                {
                    statusCode = response.StatusCode,
                    messageId = response.MessageID
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(PostPublicationCommand, exception);
        }
    }

    public WrapperResponse CloseProviderSession(
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
            return WrapperResponse.ValidationFailure(CloseProviderSessionCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.ClosePublicationSession(host, sessionId);

            if (response.StatusCode != 204)
            {
                return WrapperResponse.FaultResponse(CloseProviderSessionCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                CloseProviderSessionCommand,
                new
                {
                    statusCode = response.StatusCode,
                    closed = true
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(CloseProviderSessionCommand, exception);
        }
    }

    public WrapperResponse ExpirePublication(
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
            return WrapperResponse.ValidationFailure(ExpirePublicationCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.ExpirePublication(host, sessionId, messageId);

            if (response.StatusCode != 204)
            {
                return WrapperResponse.FaultResponse(ExpirePublicationCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                ExpirePublicationCommand,
                new
                {
                    statusCode = response.StatusCode,
                    expired = true,
                    messageId
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(ExpirePublicationCommand, exception);
        }
    }

    private static ProviderPublicationService CreateService(string user, string password)
    {
        var service = new ProviderPublicationService();
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
