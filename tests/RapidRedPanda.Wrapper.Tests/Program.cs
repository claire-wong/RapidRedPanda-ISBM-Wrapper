using System.Text.Json;
using RapidRedPanda.Wrapper.Publication;
using RapidRedPanda.Wrapper.Responses;

var tests = new (string Name, Action Run)[]
{
    ("consumer open-subscription requires host before SDK call", ConsumerOpenSubscriptionRequiresHost),
    ("provider post-publication requires message content", ProviderPostPublicationRequiresMessageContent),
    ("provider post-publication expiry overload validates before SDK call", ProviderPostPublicationExpiryOverloadValidatesBeforeSdkCall),
    ("validation failures are transport faults", ValidationFailuresAreTransportFaults),
    ("HTTP faults parse JSON details and include raw when requested", HttpFaultParsesJsonDetailsAndRaw),
    ("HTTP faults omit raw when not requested", HttpFaultOmitsRawWhenNotRequested),
    ("SDK transport failures are separated from HTTP faults", SdkTransportFailuresAreSeparated)
};

var failed = 0;

foreach (var (name, run) in tests)
{
    try
    {
        run();
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
