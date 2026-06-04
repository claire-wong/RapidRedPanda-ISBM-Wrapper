# RapidRedPanda.ISBM.ClientAdapter SDK Study

This document captures SDK usage patterns discovered from:

- Authoritative package reference: `RapidRedPanda.ISBM.ClientAdapter` on NuGet, currently version `2.0.2.4` (`https://www.nuget.org/packages/RapidRedPanda.ISBM.ClientAdapter`)
- `ISBM-2.0-Client-SDK`: downloaded under `.research/ISBM-2.0-Client-SDK-main`
- `ISBM-Publication-Provider`: downloaded under `.research/ISBM-Publication-Provider-master`
- `ISBM-Publication-Consumer`: downloaded under `.research/ISBM-Publication-Consumer-master`
- The local NuGet assembly `src/RapidRedPanda.Wrapper/bin/Debug/net8.0/RapidRedPanda.ISBM.ClientAdapter.dll`, inspected by reflection and decompiled into `.research/decompiled-sdk`

No CLI wrapper implementation was made during this study.

## Authoritative NuGet Package

The package being wrapped is `RapidRedPanda.ISBM.ClientAdapter` from NuGet. The NuGet page is the authoritative package reference for supported targets, dependencies, implemented service operations, and published code samples. The GitHub repositories are treated as example consumers of the package.

Current package facts from NuGet:

- Current version observed: `2.0.2.4`
- Install form used by this project: `<PackageReference Include="RapidRedPanda.ISBM.ClientAdapter" Version="2.0.2.4" />`
- Included target frameworks: `.NETCoreApp 3.1`, `.NETStandard 2.0`, `net6.0`, and `net8.0`
- Dependency: `Newtonsoft.Json >= 13.0.1`
- Package purpose: hide ISBM 2.0 REST interface details behind user-friendly service objects for OIIE/ISBM applications.

NuGet lists these implemented ISA-95 ISBM 2.0 RESTful interfaces:

- Provider Publication Service: Open Publication Session, Post Publication, Expire Publication, Close Publication Session.
- Consumer Publication Service: Open Subscription Session, Read Publication, Remove Publication, Close Subscription Session.
- Provider Request Service: Open Provider Request Session, Read Request, Remove Request, Post Response, Close Provider Request Session.
- Consumer Request Service: Open Consumer Request Session, Post Request, Expire Request, Read Response, Remove Response, Close Consumer Request Session.
- Authentication: Basic and Custom.

## Programming Model

The SDK exposes one service object per ISBM role/model:

- `ConsumerPublicationService`: subscription-session consumer for publication/subscription flows.
- `ProviderPublicationService`: publication-session provider for publishing BOD payloads.
- `ConsumerRequestService`: request-session consumer for asynchronous request/response flows.
- `ProviderRequestService`: provider-session responder for asynchronous request/response flows.

The application creates a service instance, sets credentials or authentication options on public fields, then calls synchronous methods that return typed response objects.

Example consumer setup from `.research/ISBM-Publication-Consumer-master/ISBMTempGauge/FormMain.cs`:

```csharp
ConsumerPublicationService myConsumerPublicationService = new ConsumerPublicationService();
myConsumerPublicationService.Credential.Username = textBoxUserName.Text;
myConsumerPublicationService.Credential.Password = textBoxPassword.Text;
OpenSubscriptionSessionResponse response =
    myConsumerPublicationService.OpenSubscriptionSession(host, channelId, topic);
```

Example provider setup from `.research/ISBM-Publication-Provider-master/ISBMTempSensor/Program.cs`:

```csharp
ProviderPublicationService service = new ProviderPublicationService();
service.Credential.Username = _username;
service.Credential.Password = _password;
OpenPublicationSessionResponse opened = service.OpenPublicationSession(_hostName, _channelId);
```

All public SDK calls are synchronous. Internally, `ISBMHandler.ISBMApi` uses `HttpClient` with `.Result` on `GetAsync`, `PostAsync`, `DeleteAsync`, and `ReadAsStringAsync` (`.research/decompiled-sdk/RapidRedPanda.ISBM.ClientAdapter/ISBMHandler.cs`).

## Major Classes

### Services

`ConsumerPublicationService` has public fields:

- `ISBMServerType`
- `Credential`
- `Authentication`

Important methods:

- `OpenSubscriptionSession(string hostAddress, string channelId, string topic)`
- `OpenSubscriptionSession(string hostAddress, string channelId, string[] topics)`
- overloads with `OpenSubscriptionSessionOptions`
- `CloseSubscriptionSession(string hostAddress, string sessionId)`
- `ReadPublication(string hostAddress, string sessionId)`
- `RemovePublication(string hostAddress, string sessionId)`

`ProviderPublicationService` has:

- `OpenPublicationSession(hostAddress, channelId)`
- `PostPublication(hostAddress, sessionId, topic/topics, bodMessage)`
- overloads with `PostPublicationOptions`
- `ExpirePublication(hostAddress, sessionId, messageId)`
- `ClosePublicationSession(hostAddress, sessionId)`

`ConsumerRequestService` and `ProviderRequestService` mirror this style for request/response operations.

### Authentication and Server Options

From `.research/decompiled-sdk/RapidRedPanda.ISBM.ClientAdapter.ServerOptions/Authentication.cs`:

- `AuthenticationSchemeType` defaults to `Basic`.
- `CustomAuthorizationHeader` defaults to empty string.
- `WebRequestHeaders` is a list of additional headers.
- `AcceptUnsignedHttps` can disable TLS certificate validation globally through `ServicePointManager.ServerCertificateValidationCallback`.

From `.research/decompiled-sdk/RapidRedPanda.ISBM.ClientAdapter.ServerOptions/Credential.cs`:

- `Username`
- `Password`

From `.research/decompiled-sdk/RapidRedPanda.ISBM.ClientAdapter.Enums/AuthenticationSchemeType.cs`:

- `Custom`
- `Basic`

From `.research/decompiled-sdk/RapidRedPanda.ISBM.ClientAdapter.Enums/ServerType.cs`:

- `Native`
- `IIS`

The SDK always adds an `Authorization` header. For `Basic`, it base64 encodes `username:password`. For `Custom`, it uses `Authentication.CustomAuthorizationHeader` as the full header value.

The example repositories commonly set `Credential.Username` and `Credential.Password` directly. The provider example reads an `authentication` flag from `Configs.json`, but the SDK itself still defaults to Basic auth even if username/password are empty.

## Response Object Model

All response types inherit from `ISBMResponse`, which uses public fields rather than properties:

```csharp
public class ISBMResponse
{
    public int StatusCode;
    public string ReasonPhrase;
    public string ISBMHTTPResponse;
}
```

Important typed response fields:

- `OpenSubscriptionSessionResponse.SessionID`
- `OpenPublicationSessionResponse.SessionID`
- `PostPublicationResponse.MessageID`
- `ReadPublicationResponse.MessageID`
- `ReadPublicationResponse.MessageContent`
- `ReadPublicationResponse.Topics`

Close/remove/expire responses carry only the common fields: `StatusCode`, `ReasonPhrase`, and `ISBMHTTPResponse`.

This matters for a future JSON CLI wrapper: reflection or explicit mapping must read public fields, not just public properties.

## Publication Subscription Lifecycle

The consumer lifecycle in the examples is:

1. Create `ConsumerPublicationService`.
2. Set `Credential.Username` and `Credential.Password`.
3. Call `OpenSubscriptionSession(host, channelId, topic)`.
4. If `StatusCode == 201`, store `SessionID`.
5. Poll with `ReadPublication(host, sessionId)`.
6. If `StatusCode == 200`, consume `MessageContent`, `MessageID`, and `Topics`.
7. Call `RemovePublication(host, sessionId)` after processing the current publication.
8. Call `CloseSubscriptionSession(host, sessionId)` when done.
9. Treat `StatusCode == 204` as successful close.

The Windows consumer example calls these operations in `.research/ISBM-2.0-Client-SDK-main/CSharp/Windows-10/ISBM20ConsumerPublicationTestCSharp/Form1.cs`.

The temperature gauge example follows the same pattern in `.research/ISBM-Publication-Consumer-master/ISBMTempGauge/FormMain.cs`, using a timer to read every two seconds, parse the BOD JSON with `JObject.Parse`, then remove the publication.

The provider lifecycle in `.research/ISBM-Publication-Provider-master/ISBMTempSensor/Program.cs` is:

1. Read config from `Configs.json`.
2. Set credentials.
3. Call `OpenPublicationSession(host, channelId)`.
4. If `StatusCode == 201`, store `SessionID`.
5. Build a BOD payload.
6. Call `PostPublication(host, sessionId, topic, bodMessage)` every five seconds.
7. If `StatusCode == 201`, store/use `MessageID`.
8. Call `ClosePublicationSession(host, sessionId)`.
9. Treat `StatusCode == 204` as successful close.

## Operation Details

### OpenSubscriptionSession

Implemented in `.research/decompiled-sdk/RapidRedPanda.ISBM.ClientAdapter/ConsumerPublicationService.cs`.

Request flow:

- Endpoint: `POST {hostAddress}/channels/{channelId}/subscription-sessions`
- Body starts as:

```json
{"topics":["topic"]}
```

- Multiple topics are added to the `topics` JSON array.
- `OpenSubscriptionSessionOptions.ListenerURL` is emitted as `listenerUrl`.
- `OpenSubscriptionSessionOptions.FilterExpressions` is emitted as `filterExpressions`.
- Subscription filter expressions use `RapidRedPanda.ISBM.ClientAdapter.EndpointOptions.FilterExpression`.
  The installed adapter version exposes `List<ApplicableMediaType> ApplicableMediaTypes` and `ExpressionString ExpressionString`.
  `ApplicableMediaType` contains `MediaType`, and `ExpressionString` contains `Expression`, `Language`, and `LanguageVersion`.
  No `MediaTypeList` or namespace collection/type was found on the `OpenSubscriptionSessionOptions` filter surface in `RapidRedPanda.ISBM.ClientAdapter` version `2.0.2.4`.
- On `StatusCode == 201`, the SDK parses `sessionId` from the JSON body into `OpenSubscriptionSessionResponse.SessionID`.

Channel ID encoding depends on `ISBMServerType`. The default enum value is `Native`. If opening a subscription returns `404` while `ISBMServerType == Native`, the SDK retries with `/` double-encoded as `%252F`.

### CloseSubscriptionSession

Request flow:

- Endpoint: `DELETE {hostAddress}/sessions/{sessionId}`
- Response: `CloseSubscriptionSessionResponse`
- Expected success in examples: `StatusCode == 204`

The response type has no session-specific fields. For HTTP 204, `HttpContent.ReadAsStringAsync()` returns an empty string, so `ISBMHTTPResponse` should normally be empty. A CLI wrapper must not try to parse JSON for 204 responses.

### ReadPublication

Request flow:

- Endpoint: `GET {hostAddress}/sessions/{sessionId}/publication`
- Response: `ReadPublicationResponse`
- Expected success in examples: `StatusCode == 200`

On `StatusCode == 200`, the SDK parses:

- `messageId` into `MessageID`
- `messageContent.content` into `MessageContent`
- `topics` into `Topics`

The examples then parse `MessageContent` as the business payload. In the temperature gauge example, `MessageContent` is a BOD JSON string containing CCOM measurement data.

### RemovePublication

Request flow:

- Endpoint: `DELETE {hostAddress}/sessions/{sessionId}/publication`
- Response: `RemovePublicationResponse`

This removes the current publication for the subscription session after it has been consumed. The example clears local UI message fields after calling it. Like close operations, the typed response only exposes common HTTP fields.

## HTTP Behavior and Errors

`ISBMHandler.ISBMApi` centralizes transport behavior:

- Builds `StringContent(requestBody, Encoding.UTF8, "application/json")` for all calls, including GET/DELETE where the body is unused.
- Creates a new `HttpClient` per SDK call.
- Adds `Authorization` and any configured extra headers.
- Executes GET, POST, or DELETE.
- Reads the response body as a string.
- Sets `StatusCode` from `HttpResponseMessage.StatusCode`.
- Sets `ReasonPhrase` from `HttpResponseMessage.ReasonPhrase`.
- Returns the body string as `ISBMHTTPResponse`.

Important implications:

- HTTP success and HTTP failure status codes are both returned as typed response objects.
- ISBM fault bodies are not parsed into a fault model. They remain in `ISBMHTTPResponse`.
- HTTP 204 responses are represented as `StatusCode = 204`, a reason phrase, and an empty `ISBMHTTPResponse`.
- SDK JSON parsing is conditional and narrow: open methods parse only on `201`; read methods parse only on `200`.
- SDK parse failures inside typed extraction are swallowed. The response object still contains status, reason, and raw `ISBMHTTPResponse`.
- Transport exceptions are caught broadly. The SDK returns `StatusCode = 400`, `ReasonPhrase = "Bad Request"`, and `ISBMHTTPResponse = ex.Message`. This means a real HTTP 400 and a client-side transport exception can look similar unless the wrapper adds its own exception boundary around SDK calls.

## Raw Response Exposure

Every SDK response has `ISBMHTTPResponse`, which is the safest raw-response hook available from the adapter. A future CLI can expose it without changing SDK behavior:

- Default mode: emit stable typed JSON fields such as `ok`, `operation`, `statusCode`, `reasonPhrase`, `sessionId`, `messageId`, `topics`, and parsed/encoded payload fields.
- Optional raw mode: include `rawResponse.body` from `ISBMHTTPResponse`.
- For safety, keep credentials and authorization headers out of raw output.
- Preserve empty body as `""` or `null` consistently for 204 responses; do not attempt to parse it.
- For non-2xx responses, include `ISBMHTTPResponse` in an error/fault object in raw mode and possibly a summarized `message` in default mode.
- For transport failures collapsed by the SDK into `400 Bad Request`, consider wrapping SDK invocation with CLI-level exception handling and a `source`/`category` field so future direct HTTP code can distinguish `transport`, `http`, `isbm-fault`, and `parse` failures.

## CLI Design Notes for Later

- Output must be JSON-only on stdout, so diagnostics should go to stderr or be included as JSON fields.
- Treat SDK response objects as field-based DTOs.
- Do not infer success from exceptions; infer success from expected status codes per operation.
- Expected status codes for studied operations:
  - `OpenSubscriptionSession`: `201`
  - `CloseSubscriptionSession`: `204`
  - `ReadPublication`: `200`
  - `RemovePublication`: likely `204` when successful, but examples do not explicitly check it.
- `ReadPublicationResponse.MessageContent` is already the inner BOD/content string, not the whole ISBM envelope.
- `ReadPublicationResponse.Topics` is an array. One SDK example appears to reference `Topic`, but the actual adapter model exposes `Topics`.
- Preserve `ReasonPhrase`, but do not depend on it for program logic.
- Be careful with `AcceptUnsignedHttps`; it mutates global certificate validation behavior and should be an explicit CLI option if exposed.
- If supporting `AuthenticationSchemeType.Custom`, require the full authorization header value from the user/config and avoid constructing it.
