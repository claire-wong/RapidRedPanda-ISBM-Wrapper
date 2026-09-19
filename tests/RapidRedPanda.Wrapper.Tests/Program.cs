using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RapidRedPanda.ISBM.ClientAdapter.EndpointOptions;
using RapidRedPanda.ISBM.ClientAdapter.ResponseType;
using RapidRedPanda.Wrapper.Publication;
using RapidRedPanda.Wrapper.Request;
using RapidRedPanda.Wrapper.Responses;

var tests = new (string Name, Func<Task> Run)[]
{
    ("consumer open-subscription requires host before SDK call", () => RunSync(ConsumerOpenSubscriptionRequiresHost)),
    ("provider post-publication requires message content", () => RunSync(ProviderPostPublicationRequiresMessageContent)),
    ("provider post-publication expiry overload validates before SDK call", () => RunSync(ProviderPostPublicationExpiryOverloadValidatesBeforeSdkCall)),
    ("validation failures are transport faults", () => RunSync(ValidationFailuresAreTransportFaults)),
    ("HTTP faults parse JSON details and include raw when requested", () => RunSync(HttpFaultParsesJsonDetailsAndRaw)),
    ("HTTP faults omit raw when not requested", () => RunSync(HttpFaultOmitsRawWhenNotRequested)),
    ("SDK transport failures are separated from HTTP faults", () => RunSync(SdkTransportFailuresAreSeparated)),
    ("open-subscription filter options use ClientAdapter 2.2.0 shape", () => RunSync(OpenSubscriptionFilterOptionsUseClientAdapter22Shape)),
    ("open-provider-request-session filter options use ClientAdapter 2.2.0 shape", () => RunSync(OpenProviderRequestFilterOptionsUseClientAdapter22Shape)),
    ("post-publication omits options when media type and expiry are omitted", () => RunSync(PostPublicationOmittedMediaTypeUsesNoOptions)),
    ("post-publication blank media type behaves as omitted", () => RunSync(PostPublicationBlankMediaTypeBehavesAsOmitted)),
    ("post-publication preserves expiry and media type together", () => RunSync(PostPublicationPreservesExpiryAndMediaType)),
    ("post-publication async uses SDK async options overload", PostPublicationAsyncPreservesMediaType),
    ("post-request omits options when media type and expiry are omitted", () => RunSync(PostRequestOmittedMediaTypeUsesNoOptions)),
    ("post-request preserves blank expiry validation", () => RunSync(PostRequestPreservesBlankExpiryValidation)),
    ("post-request preserves expiry and media type together", () => RunSync(PostRequestPreservesExpiryAndMediaType)),
    ("post-request async uses SDK async options overload", PostRequestAsyncPreservesMediaType),
    ("post-response blank media type uses no-options overload", () => RunSync(PostResponseBlankMediaTypeUsesNoOptions)),
    ("post-response supplied media type uses options", () => RunSync(PostResponseSuppliedMediaTypeUsesOptions)),
    ("post-response async uses SDK async options overload", PostResponseAsyncPreservesMediaType),
    ("CLI forwards post-publication media type", () => CliPostCommandForwardsMediaType("post-publication", "application/xml", "<root><value>42</value></root>")),
    ("CLI forwards post-request media type", () => CliPostCommandForwardsMediaType("post-request", "text/plain", "hello from cli")),
    ("CLI forwards post-response media type", () => CliPostCommandForwardsMediaType("post-response", "application/json", "{\"value\":42}")),
    ("CLI omitted media type remains native JSON", CliPostPublicationOmittedMediaTypeUsesNativeJson)
};

var failed = 0;

foreach (var (name, run) in tests)
{
    try
    {
        await run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {name}");
        Console.Error.WriteLine(exception.Message);
    }
}

return failed == 0 ? 0 : 1;

static Task RunSync(Action action)
{
    action();
    return Task.CompletedTask;
}

static void ConsumerOpenSubscriptionRequiresHost()
{
    var response = new ConsumerPublicationWrapper().OpenSubscription(
        "",
        "channel-a",
        "topic-a",
        "user-a",
        "password-a");

    AssertFalse(response.Success, "Validation failure should not succeed.");
    AssertEqual("open-subscription", response.Command);
    var transportFault = AssertNotNull(response.TransportFault, "Validation failure should return a transport fault.");
    AssertEqual("ValidationError", transportFault.Type);
    AssertEqual("Missing required parameter: --host", transportFault.Message);
    AssertNull(response.Fault, "Validation failure should not return an HTTP fault.");
}

static void ProviderPostPublicationRequiresMessageContent()
{
    var response = new ProviderPublicationWrapper().PostPublication(
        "https://isbm.example",
        "session-a",
        "topic-a",
        "",
        "user-a",
        "password-a");

    AssertFalse(response.Success, "Validation failure should not succeed.");
    AssertEqual("post-publication", response.Command);
    var transportFault = AssertNotNull(response.TransportFault, "Validation failure should return a transport fault.");
    AssertEqual("Missing required parameter: --message", transportFault.Message);
}

static void ProviderPostPublicationExpiryOverloadValidatesBeforeSdkCall()
{
    var response = new ProviderPublicationWrapper().PostPublication(
        "",
        "session-a",
        "topic-a",
        "{}",
        "user-a",
        "password-a",
        includeRaw: false,
        expiry: "2026-06-01T12:00:00Z");

    AssertFalse(response.Success, "Validation failure should not succeed.");
    AssertEqual("post-publication", response.Command);
    var transportFault = AssertNotNull(response.TransportFault, "Validation failure should return a transport fault.");
    AssertEqual("Missing required parameter: --host", transportFault.Message);
}

static void ValidationFailuresAreTransportFaults()
{
    var response = WrapperResponse.ValidationFailure("command-a", "message-a");

    AssertFalse(response.Success, "Validation failure should not succeed.");
    AssertEqual("command-a", response.Command);
    AssertNull(response.Data, "Validation failure should not return data.");
    AssertNull(response.Raw, "Validation failure should not return raw content.");
    AssertNull(response.Fault, "Validation failure should not return an HTTP fault.");
    var transportFault = AssertNotNull(response.TransportFault, "Validation failure should return a transport fault.");
    AssertEqual("ValidationError", transportFault.Type);
    AssertEqual("message-a", transportFault.Message);
}

static void HttpFaultParsesJsonDetailsAndRaw()
{
    const string raw = "{\"code\":\"BadTopic\",\"message\":\"Topic was rejected.\"}";

    var response = WrapperResponse.FaultResponse("read-publication", 404, raw, includeRaw: true);

    AssertFalse(response.Success, "HTTP fault should not succeed.");
    AssertEqual("read-publication", response.Command);
    AssertEqual(raw, response.Raw);
    AssertNull(response.TransportFault, "HTTP fault should not be classified as transport.");
    var fault = AssertNotNull(response.Fault, "HTTP fault should return fault details.");
    AssertEqual("404", fault.Code);
    AssertEqual(raw, fault.Message);
    AssertEqual(raw, JsonSerializer.Serialize(fault.Details));
}

static void HttpFaultOmitsRawWhenNotRequested()
{
    const string raw = "Plain fault body";

    var response = WrapperResponse.FaultResponse("read-publication", 500, raw, includeRaw: false);

    AssertNull(response.Raw, "Raw content should be omitted unless requested.");
    var fault = AssertNotNull(response.Fault, "HTTP fault should return fault details.");
    AssertEqual("Plain fault body", fault.Message);
}

static void SdkTransportFailuresAreSeparated()
{
    var response = WrapperResponse.FaultResponse(
        "open-subscription",
        400,
        "One or more errors occurred. (No such host is known.)",
        includeRaw: true);

    AssertFalse(response.Success, "Transport fault should not succeed.");
    AssertNull(response.Raw, "Transport fault should not expose raw as an HTTP body.");
    AssertNull(response.Fault, "Transport fault should not return an HTTP fault.");
    var transportFault = AssertNotNull(response.TransportFault, "Transport fault should be set.");
    AssertEqual("SdkTransportError", transportFault.Type);
}

static void OpenSubscriptionFilterOptionsUseClientAdapter22Shape()
{
    var options = InvokePrivateStatic<OpenSubscriptionSessionOptions>(
        typeof(ConsumerPublicationWrapper),
        "CreateOpenSubscriptionOptions",
        new List<WrapperFilterExpression>
        {
            new()
            {
                ApplicableMediaTypes = [" application/json ", "application/xml"],
                Expression = "$.value",
                Language = "JSONPath",
                LanguageVersion = "com.jayway.jsonpath:json-path:2.4.0",
                Namespaces =
                [
                    new WrapperFilterNamespace { Prefix = "rrp", Name = "urn:rapidredpanda:test" }
                ]
            }
        });

    var filterExpression = options.FilterExpressions.Single();
    AssertEqual(2, filterExpression.ApplicableMediaTypes.Count);
    AssertEqual("application/json", filterExpression.ApplicableMediaTypes[0]);
    AssertEqual("application/xml", filterExpression.ApplicableMediaTypes[1]);
    var filterNamespace = filterExpression.Namespaces.Single();
    AssertEqual("rrp", filterNamespace.Prefix);
    AssertEqual("urn:rapidredpanda:test", filterNamespace.Name);
}

static void OpenProviderRequestFilterOptionsUseClientAdapter22Shape()
{
    var options = InvokePrivateStatic<OpenProviderRequestSessionOptions>(
        typeof(ProviderRequestWrapper),
        "CreateOpenProviderRequestSessionOptions",
        new List<WrapperFilterExpression>
        {
            new()
            {
                ApplicableMediaTypes = [" text/plain "],
                Expression = "$.request",
                Language = "JSONPath",
                LanguageVersion = "com.jayway.jsonpath:json-path:2.4.0",
                Namespaces =
                [
                    new WrapperFilterNamespace { Prefix = "req", Name = "urn:rapidredpanda:request" }
                ]
            }
        });

    var filterExpression = options.FilterExpressions.Single();
    AssertEqual(1, filterExpression.ApplicableMediaTypes.Count);
    AssertEqual("text/plain", filterExpression.ApplicableMediaTypes[0]);
    var filterNamespace = filterExpression.Namespaces.Single();
    AssertEqual("req", filterNamespace.Prefix);
    AssertEqual("urn:rapidredpanda:request", filterNamespace.Name);
}

static void PostPublicationOmittedMediaTypeUsesNoOptions()
{
    var fake = new FakeProviderPublicationService();
    var wrapper = new ProviderPublicationWrapper((_, _) => fake);

    var response = wrapper.PostPublication("host", "session", "topic", "{\"value\":42}", "user", "password");

    AssertTrue(response.Success, "PostPublication should succeed.");
    AssertEqual(1, fake.PostPublicationNoOptionsCalls);
    AssertEqual(0, fake.PostPublicationOptionsCalls);
    AssertEqual("{\"value\":42}", fake.LastContent);
}

static void PostPublicationBlankMediaTypeBehavesAsOmitted()
{
    foreach (var mediaType in new string?[] { null, "", "   " })
    {
        var fake = new FakeProviderPublicationService();
        var wrapper = new ProviderPublicationWrapper((_, _) => fake);

        wrapper.PostPublication("host", "session", "topic", "{\"value\":42}", "user", "password", false, null, mediaType);

        AssertEqual(1, fake.PostPublicationNoOptionsCalls);
        AssertEqual(0, fake.PostPublicationOptionsCalls);
    }
}

static void PostPublicationPreservesExpiryAndMediaType()
{
    var fake = new FakeProviderPublicationService();
    var wrapper = new ProviderPublicationWrapper((_, _) => fake);
    const string xml = "<root><value>42</value></root>";

    wrapper.PostPublication(
        "host",
        "session",
        "topic",
        xml,
        "user",
        "password",
        false,
        "2026-06-01T12:00:00Z",
        "application/xml");

    AssertEqual(0, fake.PostPublicationNoOptionsCalls);
    AssertEqual(1, fake.PostPublicationOptionsCalls);
    AssertEqual(xml, fake.LastContent);
    AssertEqual("2026-06-01T12:00:00Z", fake.LastPostPublicationOptions?.Expiry);
    AssertEqual("application/xml", fake.LastPostPublicationOptions?.MediaType);
}

static async Task PostPublicationAsyncPreservesMediaType()
{
    var fake = new FakeProviderPublicationService();
    var wrapper = new ProviderPublicationWrapper((_, _) => fake);
    using var cancellationSource = new CancellationTokenSource();
    const string text = "plain async text";

    var response = await wrapper.PostPublicationAsync(
        "host",
        "session",
        "topic",
        text,
        "user",
        "password",
        false,
        null,
        "text/plain",
        cancellationSource.Token);

    AssertTrue(response.Success, "PostPublicationAsync should succeed.");
    AssertEqual(1, fake.PostPublicationAsyncOptionsCalls);
    AssertEqual(text, fake.LastContent);
    AssertEqual("text/plain", fake.LastPostPublicationOptions?.MediaType);
    AssertEqual(cancellationSource.Token, fake.LastCancellationToken);
}

static void PostRequestOmittedMediaTypeUsesNoOptions()
{
    var fake = new FakeConsumerRequestService();
    var wrapper = new ConsumerRequestWrapper((_, _) => fake);

    var response = wrapper.PostRequest("host", "session", "topic", "{\"value\":42}", "user", "password");

    AssertTrue(response.Success, "PostRequest should succeed.");
    AssertEqual(1, fake.PostRequestNoOptionsCalls);
    AssertEqual(0, fake.PostRequestOptionsCalls);
    AssertEqual("{\"value\":42}", fake.LastContent);
}

static void PostRequestPreservesBlankExpiryValidation()
{
    var fake = new FakeConsumerRequestService();
    var wrapper = new ConsumerRequestWrapper((_, _) => fake);

    var response = wrapper.PostRequest("host", "session", "topic", "{\"value\":42}", "user", "password", false, "   ", "text/plain");

    AssertFalse(response.Success, "Blank expiry should still fail validation.");
    AssertEqual(0, fake.PostRequestNoOptionsCalls);
    AssertEqual(0, fake.PostRequestOptionsCalls);
}

static void PostRequestPreservesExpiryAndMediaType()
{
    var fake = new FakeConsumerRequestService();
    var wrapper = new ConsumerRequestWrapper((_, _) => fake);
    const string jsonText = "{\"value\":42}";

    wrapper.PostRequest(
        "host",
        "session",
        "topic",
        jsonText,
        "user",
        "password",
        false,
        " 2026-06-01T12:00:00Z ",
        "application/json");

    AssertEqual(0, fake.PostRequestNoOptionsCalls);
    AssertEqual(1, fake.PostRequestOptionsCalls);
    AssertEqual(jsonText, fake.LastContent);
    AssertEqual("2026-06-01T12:00:00Z", fake.LastPostRequestOptions?.Expiry);
    AssertEqual("application/json", fake.LastPostRequestOptions?.MediaType);
}

static async Task PostRequestAsyncPreservesMediaType()
{
    var fake = new FakeConsumerRequestService();
    var wrapper = new ConsumerRequestWrapper((_, _) => fake);
    using var cancellationSource = new CancellationTokenSource();

    var response = await wrapper.PostRequestAsync(
        "host",
        "session",
        "topic",
        "plain async request",
        "user",
        "password",
        false,
        null,
        "text/plain",
        cancellationSource.Token);

    AssertTrue(response.Success, "PostRequestAsync should succeed.");
    AssertEqual(1, fake.PostRequestAsyncOptionsCalls);
    AssertEqual("text/plain", fake.LastPostRequestOptions?.MediaType);
    AssertEqual(cancellationSource.Token, fake.LastCancellationToken);
}

static void PostResponseBlankMediaTypeUsesNoOptions()
{
    foreach (var mediaType in new string?[] { null, "", "   " })
    {
        var fake = new FakeProviderRequestService();
        var wrapper = new ProviderRequestWrapper((_, _) => fake);

        wrapper.PostResponse("host", "session", "request-message", "{\"value\":42}", "user", "password", false, mediaType);

        AssertEqual(1, fake.PostResponseNoOptionsCalls);
        AssertEqual(0, fake.PostResponseOptionsCalls);
    }
}

static void PostResponseSuppliedMediaTypeUsesOptions()
{
    var fake = new FakeProviderRequestService();
    var wrapper = new ProviderRequestWrapper((_, _) => fake);
    const string xml = "<response>ok</response>";

    wrapper.PostResponse("host", "session", "request-message", xml, "user", "password", false, "application/xml");

    AssertEqual(0, fake.PostResponseNoOptionsCalls);
    AssertEqual(1, fake.PostResponseOptionsCalls);
    AssertEqual(xml, fake.LastContent);
    AssertEqual("application/xml", fake.LastPostResponseOptions?.MediaType);
}

static async Task PostResponseAsyncPreservesMediaType()
{
    var fake = new FakeProviderRequestService();
    var wrapper = new ProviderRequestWrapper((_, _) => fake);
    using var cancellationSource = new CancellationTokenSource();

    var response = await wrapper.PostResponseAsync(
        "host",
        "session",
        "request-message",
        "{\"value\":42}",
        "user",
        "password",
        false,
        "application/json",
        cancellationSource.Token);

    AssertTrue(response.Success, "PostResponseAsync should succeed.");
    AssertEqual(1, fake.PostResponseAsyncOptionsCalls);
    AssertEqual("application/json", fake.LastPostResponseOptions?.MediaType);
    AssertEqual(cancellationSource.Token, fake.LastCancellationToken);
}

static async Task CliPostCommandForwardsMediaType(string command, string mediaType, string content)
{
    using var server = new RecordingHttpServer();
    var args = BuildPostCommandArgs(command, server.Url, content, mediaType);
    var result = await RunCli(args);

    AssertEqual(0, result.ExitCode);
    var requestBody = AssertNotNull(server.RequestBody, "CLI request body should be recorded.");
    var messageContent = AssertMessageContent(requestBody);
    AssertEqual(mediaType, messageContent["mediaType"]?.GetValue<string>());
    AssertEqual(content, messageContent["content"]?.GetValue<string>());
}

static async Task CliPostPublicationOmittedMediaTypeUsesNativeJson()
{
    using var server = new RecordingHttpServer();
    var args = BuildPostCommandArgs("post-publication", server.Url, "{\"value\":42}", mediaType: null);
    var result = await RunCli(args);

    AssertEqual(0, result.ExitCode);
    var requestBody = AssertNotNull(server.RequestBody, "CLI request body should be recorded.");
    var messageContent = AssertMessageContent(requestBody);
    AssertFalse(messageContent.ContainsKey("mediaType"), "Omitted media type should not be emitted.");
    AssertEqual(42, messageContent["content"]?["value"]?.GetValue<int>());
}

static string[] BuildPostCommandArgs(string command, string host, string content, string? mediaType)
{
    var args = new List<string>
    {
        command,
        "--host",
        host,
        "--session-id",
        "session-a"
    };

    if (command is "post-publication" or "post-request")
    {
        args.AddRange(["--topic", "topic-a"]);
    }

    if (command == "post-response")
    {
        args.AddRange(["--request-message-id", "request-message-a"]);
    }

    if (mediaType is not null)
    {
        args.AddRange(["--media-type", mediaType]);
    }

    args.AddRange(
    [
        "--content",
        content,
        "--user",
        "user-a",
        "--password",
        "password-a"
    ]);

    return [.. args];
}

static JsonObject AssertMessageContent(string requestBody)
{
    var root = JsonNode.Parse(requestBody)?.AsObject();
    var messageContent = root?["messageContent"]?.AsObject();
    return AssertNotNull(messageContent, "Request body should include messageContent object.");
}

static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunCli(string[] cliArgs)
{
    var repositoryRoot = FindRepositoryRoot();
    var cliProject = Path.Combine(repositoryRoot, "src", "RapidRedPanda.Wrapper.Cli", "RapidRedPanda.Wrapper.Cli.csproj");
    var argumentList = new List<string> { "run", "--project", cliProject, "--no-restore", "--" };
    argumentList.AddRange(cliArgs);

    var startInfo = new ProcessStartInfo("dotnet")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    foreach (var argument in argumentList)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start CLI process.");
    var standardOutputTask = process.StandardOutput.ReadToEndAsync();
    var standardErrorTask = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    return (process.ExitCode, await standardOutputTask, await standardErrorTask);
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "RapidRedPanda.Wrapper.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Unable to find repository root.");
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}

static void AssertFalse(bool value, string message)
{
    if (value)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertTrue(bool value, string message)
{
    if (!value)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertNull(object? value, string message)
{
    if (value is not null)
    {
        throw new InvalidOperationException(message);
    }
}

static T AssertNotNull<T>(T? value, string message)
    where T : class
{
    if (value is null)
    {
        throw new InvalidOperationException(message);
    }

    return value;
}

static T InvokePrivateStatic<T>(Type type, string methodName, object argument)
{
    var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Missing method {type.Name}.{methodName}.");

    return (T)AssertNotNull(method.Invoke(null, [argument]), $"{type.Name}.{methodName} should return a value.");
}

internal sealed class RecordingHttpServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly Task _serverTask;

    public RecordingHttpServer()
    {
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Url = $"http://127.0.0.1:{port}";
        _serverTask = ServeOneRequest();
    }

    public string Url { get; }

    public string? RequestBody { get; private set; }

    public void Dispose()
    {
        _listener.Stop();
        try
        {
            _serverTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
        }
    }

    private async Task ServeOneRequest()
    {
        using var client = await _listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        var contentLength = 0;
        while (await reader.ReadLineAsync() is { } line && line.Length > 0)
        {
            const string contentLengthHeader = "Content-Length:";
            if (line.StartsWith(contentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                contentLength = int.Parse(line[contentLengthHeader.Length..].Trim());
            }
        }

        var bodyBuffer = new char[contentLength];
        var totalRead = 0;
        while (totalRead < contentLength)
        {
            var read = await reader.ReadAsync(bodyBuffer, totalRead, contentLength - totalRead);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        RequestBody = new string(bodyBuffer, 0, totalRead);

        var responseBody = "{\"messageId\":\"message-from-test\"}";
        var response = Encoding.UTF8.GetBytes(
            "HTTP/1.1 201 Created\r\n" +
            "Content-Type: application/json\r\n" +
            $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
            "Connection: close\r\n" +
            "\r\n" +
            responseBody);

        await stream.WriteAsync(response);
    }
}

internal sealed class FakeProviderPublicationService : IProviderPublicationService
{
    public int PostPublicationNoOptionsCalls { get; private set; }
    public int PostPublicationOptionsCalls { get; private set; }
    public int PostPublicationAsyncOptionsCalls { get; private set; }
    public string? LastContent { get; private set; }
    public PostPublicationOptions? LastPostPublicationOptions { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public OpenPublicationSessionResponse OpenPublicationSession(string hostAddress, string channelId) => throw new NotSupportedException();

    public PostPublicationResponse PostPublication(string hostAddress, string sessionId, string topic, string bodMessage)
    {
        PostPublicationNoOptionsCalls++;
        LastContent = bodMessage;
        return SuccessfulPostPublication();
    }

    public PostPublicationResponse PostPublication(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        PostPublicationOptions postPublicationOptions)
    {
        PostPublicationOptionsCalls++;
        LastContent = bodMessage;
        LastPostPublicationOptions = postPublicationOptions;
        return SuccessfulPostPublication();
    }

    public Task<PostPublicationResponse> PostPublicationAsync(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        return Task.FromResult(PostPublication(hostAddress, sessionId, topic, bodMessage));
    }

    public Task<PostPublicationResponse> PostPublicationAsync(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        PostPublicationOptions postPublicationOptions,
        CancellationToken cancellationToken = default)
    {
        PostPublicationAsyncOptionsCalls++;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(PostPublication(hostAddress, sessionId, topic, bodMessage, postPublicationOptions));
    }

    public ExpirePublicationResponse ExpirePublication(string hostAddress, string sessionId, string messageId) => throw new NotSupportedException();

    public ClosePublicationSessionResponse ClosePublicationSession(string hostAddress, string sessionId) => throw new NotSupportedException();

    private static PostPublicationResponse SuccessfulPostPublication() => new()
    {
        StatusCode = 201,
        MessageID = "message-a",
        ISBMHTTPResponse = "{\"messageId\":\"message-a\"}"
    };
}

internal sealed class FakeConsumerRequestService : IConsumerRequestService
{
    public int PostRequestNoOptionsCalls { get; private set; }
    public int PostRequestOptionsCalls { get; private set; }
    public int PostRequestAsyncOptionsCalls { get; private set; }
    public string? LastContent { get; private set; }
    public PostRequestOptions? LastPostRequestOptions { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public OpenConsumerRequestSessionResponse OpenConsumerRequestSession(string hostAddress, string channelId) => throw new NotSupportedException();

    public PostRequestResponse PostRequest(string hostAddress, string sessionId, string topic, string bodMessage)
    {
        PostRequestNoOptionsCalls++;
        LastContent = bodMessage;
        return SuccessfulPostRequest();
    }

    public PostRequestResponse PostRequest(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        PostRequestOptions postRequestOptions)
    {
        PostRequestOptionsCalls++;
        LastContent = bodMessage;
        LastPostRequestOptions = postRequestOptions;
        return SuccessfulPostRequest();
    }

    public Task<PostRequestResponse> PostRequestAsync(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        return Task.FromResult(PostRequest(hostAddress, sessionId, topic, bodMessage));
    }

    public Task<PostRequestResponse> PostRequestAsync(
        string hostAddress,
        string sessionId,
        string topic,
        string bodMessage,
        PostRequestOptions postRequestOptions,
        CancellationToken cancellationToken = default)
    {
        PostRequestAsyncOptionsCalls++;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(PostRequest(hostAddress, sessionId, topic, bodMessage, postRequestOptions));
    }

    public ReadResponseResponse ReadResponse(string hostAddress, string sessionId, string requestMessageId) => throw new NotSupportedException();

    public ExpireRequestResponse ExpireRequest(string hostAddress, string sessionId, string messageId) => throw new NotSupportedException();

    public RemoveResponseResponse RemoveResponse(string hostAddress, string sessionId, string requestMessageId) => throw new NotSupportedException();

    public CloseConsumerRequestSessionResponse CloseConsumerRequestSession(string hostAddress, string sessionId) => throw new NotSupportedException();

    private static PostRequestResponse SuccessfulPostRequest() => new()
    {
        StatusCode = 201,
        MessageID = "message-a",
        ISBMHTTPResponse = "{\"messageId\":\"message-a\"}"
    };
}

internal sealed class FakeProviderRequestService : IProviderRequestService
{
    public int PostResponseNoOptionsCalls { get; private set; }
    public int PostResponseOptionsCalls { get; private set; }
    public int PostResponseAsyncOptionsCalls { get; private set; }
    public string? LastContent { get; private set; }
    public PostResponseOptions? LastPostResponseOptions { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public OpenProviderRequestSessionResponse OpenProviderRequestSession(string hostAddress, string channelId, string topic) => throw new NotSupportedException();

    public OpenProviderRequestSessionResponse OpenProviderRequestSession(
        string hostAddress,
        string channelId,
        string topic,
        OpenProviderRequestSessionOptions openProviderRequestSessionOptions) => throw new NotSupportedException();

    public ReadRequestResponse ReadRequest(string hostAddress, string sessionId) => throw new NotSupportedException();

    public PostResponseResponse PostResponse(string hostAddress, string sessionId, string requestMessageId, string bodMessage)
    {
        PostResponseNoOptionsCalls++;
        LastContent = bodMessage;
        return SuccessfulPostResponse();
    }

    public PostResponseResponse PostResponse(
        string hostAddress,
        string sessionId,
        string requestMessageId,
        string bodMessage,
        PostResponseOptions postResponseOptions)
    {
        PostResponseOptionsCalls++;
        LastContent = bodMessage;
        LastPostResponseOptions = postResponseOptions;
        return SuccessfulPostResponse();
    }

    public Task<PostResponseResponse> PostResponseAsync(
        string hostAddress,
        string sessionId,
        string requestMessageId,
        string bodMessage,
        CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        return Task.FromResult(PostResponse(hostAddress, sessionId, requestMessageId, bodMessage));
    }

    public Task<PostResponseResponse> PostResponseAsync(
        string hostAddress,
        string sessionId,
        string requestMessageId,
        string bodMessage,
        PostResponseOptions postResponseOptions,
        CancellationToken cancellationToken = default)
    {
        PostResponseAsyncOptionsCalls++;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(PostResponse(hostAddress, sessionId, requestMessageId, bodMessage, postResponseOptions));
    }

    public RemoveRequestResponse RemoveRequest(string hostAddress, string sessionId) => throw new NotSupportedException();

    public CloseProviderRequestSessionResponse CloseProviderRequestSession(string hostAddress, string sessionId) => throw new NotSupportedException();

    private static PostResponseResponse SuccessfulPostResponse() => new()
    {
        StatusCode = 201,
        MessageID = "message-a",
        ISBMHTTPResponse = "{\"messageId\":\"message-a\"}"
    };
}
