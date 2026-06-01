using RapidRedPanda.ISBM.ClientAdapter;
using RapidRedPanda.Wrapper.Responses;

namespace RapidRedPanda.Wrapper.Publication;

public sealed class ConsumerPublicationWrapper
{
    private const string OpenSubscriptionCommand = "open-subscription";
    private const string ReadPublicationCommand = "read-publication";
    private const string RemovePublicationCommand = "remove-publication";
    private const string CloseSubscriptionCommand = "close-subscription";

    public WrapperResponse OpenSubscription(
        string host,
        string channel,
        string topic,
        string user,
        string password,
        bool includeRaw = false)
    {
        var missing = ValidateRequired(
            ("host", host),
            ("channel", channel),
            ("topic", topic),
            ("user", user),
            ("password", password));

        if (missing is not null)
        {
            return WrapperResponse.ValidationFailure(OpenSubscriptionCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.OpenSubscriptionSession(host, channel, topic);

            if (response.StatusCode != 201)
            {
                return WrapperResponse.FaultResponse(OpenSubscriptionCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                OpenSubscriptionCommand,
                new
                {
                    statusCode = response.StatusCode,
                    sessionId = response.SessionID
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(OpenSubscriptionCommand, exception);
        }
    }

    public WrapperResponse ReadPublication(
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
            return WrapperResponse.ValidationFailure(ReadPublicationCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.ReadPublication(host, sessionId);

            if (response.StatusCode != 200)
            {
                return WrapperResponse.FaultResponse(ReadPublicationCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                ReadPublicationCommand,
                new
                {
                    statusCode = response.StatusCode,
                    messageId = response.MessageID,
                    messageContent = response.MessageContent,
                    topics = response.Topics
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(ReadPublicationCommand, exception);
        }
    }

    public WrapperResponse RemovePublication(
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
            return WrapperResponse.ValidationFailure(RemovePublicationCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.RemovePublication(host, sessionId);

            if (response.StatusCode != 204)
            {
                return WrapperResponse.FaultResponse(RemovePublicationCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                RemovePublicationCommand,
                new
                {
                    statusCode = response.StatusCode,
                    removed = true
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(RemovePublicationCommand, exception);
        }
    }

    public WrapperResponse CloseSubscription(
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
            return WrapperResponse.ValidationFailure(CloseSubscriptionCommand, $"Missing required parameter: --{missing}");
        }

        try
        {
            var service = CreateService(user, password);
            var response = service.CloseSubscriptionSession(host, sessionId);

            if (response.StatusCode != 204)
            {
                return WrapperResponse.FaultResponse(CloseSubscriptionCommand, response.StatusCode, response.ISBMHTTPResponse, includeRaw);
            }

            return WrapperResponse.SuccessResponse(
                CloseSubscriptionCommand,
                new
                {
                    statusCode = response.StatusCode,
                    closed = true
                },
                includeRaw ? response.ISBMHTTPResponse : null);
        }
        catch (Exception exception)
        {
            return WrapperResponse.ExceptionFailure(CloseSubscriptionCommand, exception);
        }
    }

    private static ConsumerPublicationService CreateService(string user, string password)
    {
        var service = new ConsumerPublicationService();
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
