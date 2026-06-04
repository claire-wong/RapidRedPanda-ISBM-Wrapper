# Request Services SDK Study

This study covers the request/response portion of `RapidRedPanda.ISBM.ClientAdapter` version `2.0.2.4`, the package referenced by `src/RapidRedPanda.Wrapper/RapidRedPanda.Wrapper.csproj`.

Sources inspected:

- Local NuGet package assembly: `C:\Users\Administrator\.nuget\packages\rapidredpanda.isbm.clientadapter\2.0.2.4\lib\netstandard2.0\RapidRedPanda.ISBM.ClientAdapter.dll`
- Existing publication SDK notes: `docs/SDK_STUDY.md`
- Runtime observation against a local TCP HTTP stub to confirm request methods, paths, request bodies, typed response parsing, and expected success status codes

No request wrapper source code exists in this repository at the time of this study. No SDK source files were present locally under `.research`; the available SDK input was the NuGet package assembly.

## SDK Service Classes

| Namespace | Class Name | Purpose |
| --- | --- | --- |
| `RapidRedPanda.ISBM.ClientAdapter` | `ConsumerRequestService` | Opens consumer request sessions, posts requests, expires requests, reads responses, removes responses, and closes consumer request sessions. |
| `RapidRedPanda.ISBM.ClientAdapter` | `ProviderRequestService` | Opens provider request sessions, reads incoming requests, removes requests, posts responses, and closes provider request sessions. |
| `RapidRedPanda.ISBM.ClientAdapter.EndpointOptions` | `OpenConsumerRequestSessionOptions` | Optional settings for opening a consumer request session. Contains `ListenerURL`. |
| `RapidRedPanda.ISBM.ClientAdapter.EndpointOptions` | `OpenProviderRequestSessionOptions` | Optional settings for opening a provider request session. Contains `ListenerURL` and `FilterExpressions`. |
| `RapidRedPanda.ISBM.ClientAdapter.EndpointOptions` | `PostRequestOptions` | Optional settings for posting a request. Contains `Expiry`. |
| `RapidRedPanda.ISBM.ClientAdapter.EndpointOptions` | `FilterExpression` | Filter expression container used by provider request session options. Contains `ApplicableMediaTypes` and `ExpressionString`. |
| `RapidRedPanda.ISBM.ClientAdapter.EndpointOptions` | `ExpressionString` | Filter expression text and language metadata. Contains `Expression`, `Language`, and `LanguageVersion`. |
| `RapidRedPanda.ISBM.ClientAdapter.EndpointOptions` | `ApplicableMediaType` | Media type entry for filter applicability. Contains `MediaType`. |
| `RapidRedPanda.ISBM.ClientAdapter.ResponseType` | `ISBMResponse` | Base SDK response object. Contains `StatusCode`, `ReasonPhrase`, and `ISBMHTTPResponse`. |

No public SDK interfaces related to request/response operations were found in the inspected assembly.

Both request service classes expose the same public configuration fields used by the publication services:

```csharp
public ServerType ISBMServerType;
public Credential Credential;
public Authentication Authentication;
```

## Consumer Request Operations

`ConsumerRequestService` namespace: `RapidRedPanda.ISBM.ClientAdapter`

### Open Request Session

```csharp
OpenConsumerRequestSessionResponse OpenConsumerRequestSession(
    string hostAddress,
    string channelId)
```

```csharp
OpenConsumerRequestSessionResponse OpenConsumerRequestSession(
    string hostAddress,
    string channelId,
    OpenConsumerRequestSessionOptions myOpenRequestOptions)
```

Parameters:

- `hostAddress`: ISBM server base URL.
- `channelId`: ISBM channel id.
- `myOpenRequestOptions`: optional request-session settings.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.OpenConsumerRequestSessionResponse`

Observed HTTP behavior:

- Request: `POST {hostAddress}/channels/{channelId}/consumer-request-sessions`
- Default body: `{}`
- Options body includes `listenerUrl` when `OpenConsumerRequestSessionOptions.ListenerURL` is set.
- Expected success: `201 Created`
- On `201`, SDK parses `sessionId` into `SessionID`.

### Post Request

```csharp
PostRequestResponse PostRequest(
    string hostAddress,
    string sessionId,
    string topic,
    string bodMessage)
```

```csharp
PostRequestResponse PostRequest(
    string hostAddress,
    string sessionId,
    string topic,
    string bodMessage,
    PostRequestOptions myPostRequestOptions)
```

Parameters:

- `hostAddress`: ISBM server base URL.
- `sessionId`: consumer request session id.
- `topic`: request topic.
- `bodMessage`: business payload. The SDK embeds this as JSON content.
- `myPostRequestOptions`: optional request settings.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.PostRequestResponse`

Observed HTTP behavior:

- Request: `POST {hostAddress}/sessions/{sessionId}/requests`
- Default body shape:

```json
{
  "topics": ["topic-a"],
  "messageContent": {
    "mediaType": "application/json",
    "content": {}
  }
}
```

- Options body adds `expiry` when `PostRequestOptions.Expiry` is set.
- Expected success: `201 Created`
- On `201`, SDK parses `messageId` into `MessageID`.
- Wrapper/console note: request post-time expiry is supported through `RapidRedPanda.ISBM.ClientAdapter.EndpointOptions.PostRequestOptions.Expiry`, a `string` field. This is distinct from `ConsumerRequestService.ExpireRequest`, which manually expires an already posted request.

### Expire Request

This method is request-related even though it was not in the initial controlled workflow list.

```csharp
ExpireRequestResponse ExpireRequest(
    string hostAddress,
    string sessionId,
    string messageId)
```

Parameters:

- `hostAddress`: ISBM server base URL.
- `sessionId`: consumer request session id.
- `messageId`: request message id returned by `PostRequest`.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.ExpireRequestResponse`

Observed HTTP behavior:

- Request: `DELETE {hostAddress}/sessions/{sessionId}/requests/{messageId}`
- Expected success: `204 No Content`
- Response type carries only common fields.

### Read Response

```csharp
ReadResponseResponse ReadResponse(
    string hostAddress,
    string sessionlId,
    string requestMessageId)
```

The SDK parameter name is `sessionlId` with a lowercase `l` before `Id`.

Parameters:

- `hostAddress`: ISBM server base URL.
- `sessionlId`: consumer request session id.
- `requestMessageId`: original request message id, not the response message id.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.ReadResponseResponse`

Observed HTTP behavior:

- Request: `GET {hostAddress}/sessions/{sessionId}/requests/{requestMessageId}/response`
- Expected success: `200 OK`
- On `200`, SDK parses `messageId` into `MessageID`.
- On `200`, SDK parses `messageContent.content` into `MessageContent`.

### Remove Response

```csharp
RemoveResponseResponse RemoveResponse(
    string hostAddress,
    string sessionId,
    string requestMessageId)
```

Parameters:

- `hostAddress`: ISBM server base URL.
- `sessionId`: consumer request session id.
- `requestMessageId`: original request message id.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.RemoveResponseResponse`

Observed HTTP behavior:

- Request: `DELETE {hostAddress}/sessions/{sessionId}/requests/{requestMessageId}/response`
- Expected success: `204 No Content`
- Response type carries only common fields.

### Close Request Session

```csharp
CloseConsumerRequestSessionResponse CloseConsumerRequestSession(
    string hostAddress,
    string sessionlId)
```

The SDK parameter name is `sessionlId` with a lowercase `l` before `Id`.

Parameters:

- `hostAddress`: ISBM server base URL.
- `sessionlId`: consumer request session id.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.CloseConsumerRequestSessionResponse`

Observed HTTP behavior:

- Request: `DELETE {hostAddress}/sessions/{sessionId}`
- Expected success: `204 No Content`
- Response type carries only common fields.

## Provider Request Operations

`ProviderRequestService` namespace: `RapidRedPanda.ISBM.ClientAdapter`

### Open Provider Session

```csharp
OpenProviderRequestSessionResponse OpenProviderRequestSession(
    string hostAddress,
    string channelId,
    string topic)
```

```csharp
OpenProviderRequestSessionResponse OpenProviderRequestSession(
    string hostAddress,
    string channelId,
    string topic,
    OpenProviderRequestSessionOptions myOpenProviderRequestSessionOptions)
```

```csharp
OpenProviderRequestSessionResponse OpenProviderRequestSession(
    string hostAddress,
    string channelId,
    string[] topics)
```

```csharp
OpenProviderRequestSessionResponse OpenProviderRequestSession(
    string hostAddress,
    string channelId,
    string[] topics,
    OpenProviderRequestSessionOptions myOpenProviderRequestSessionOptions)
```

Parameters:

- `hostAddress`: ISBM server base URL.
- `channelId`: ISBM channel id.
- `topic`: single topic.
- `topics`: multiple topics.
- `myOpenProviderRequestSessionOptions`: optional provider request session settings.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.OpenProviderRequestSessionResponse`

Observed HTTP behavior:

- Request: `POST {hostAddress}/channels/{channelId}/provider-request-sessions`
- Single topic body: `{"topics":["topic-a"]}`
- Multiple topic body includes each topic in the `topics` array.
- Options body includes `listenerUrl`; in the local stub test, `FilterExpressions` was present on the options type but was not emitted in the request body.
- Expected success: `201 Created`
- On `201`, SDK parses `sessionId` into `SessionID`.

### Read Request

```csharp
ReadRequestResponse ReadRequest(
    string hostAddress,
    string sessionlId)
```

The SDK parameter name is `sessionlId` with a lowercase `l` before `Id`.

Parameters:

- `hostAddress`: ISBM server base URL.
- `sessionlId`: provider request session id.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.ReadRequestResponse`

Observed HTTP behavior:

- Request: `GET {hostAddress}/sessions/{sessionId}/request`
- Expected success: `200 OK`
- On `200`, SDK parses `messageId` into `MessageID`.
- On `200`, SDK parses `messageContent.content` into `MessageContent`.

### Remove Request

```csharp
RemoveRequestResponse RemoveRequest(
    string hostAddress,
    string sessionId)
```

Parameters:

- `hostAddress`: ISBM server base URL.
- `sessionId`: provider request session id.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.RemoveRequestResponse`

Observed HTTP behavior:

- Request: `DELETE {hostAddress}/sessions/{sessionId}/request`
- Expected success: `204 No Content`
- Response type carries only common fields.
- Wrapper note: `ProviderRequestWrapper.RemoveRequest` should not require a `requestMessageId`; the SDK method removes the current request for the provider request session by `sessionId`.

### Post Response

```csharp
PostResponseResponse PostResponse(
    string hostAddress,
    string sessionlId,
    string requestMessageId,
    string bodMessage)
```

The SDK parameter name is `sessionlId` with a lowercase `l` before `Id`.

Parameters:

- `hostAddress`: ISBM server base URL.
- `sessionlId`: provider request session id.
- `requestMessageId`: request message id returned by `ReadRequest`.
- `bodMessage`: business payload. The SDK embeds this as JSON content.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.PostResponseResponse`

Observed HTTP behavior:

- Request: `POST {hostAddress}/sessions/{sessionId}/requests/{requestMessageId}/responses`
- Body shape:

```json
{
  "messageContent": {
    "mediaType": "application/json",
    "content": {}
  }
}
```

- Expected success: `201 Created`
- On `201`, SDK parses `messageId` into `MessageID`.
- Wrapper note: expose the returned SDK `MessageID` as `responseMessageId` and preserve the input request id as `requestMessageId`.

### Close Provider Session

```csharp
CloseProviderRequestSessionResponse CloseProviderRequestSession(
    string hostAddress,
    string sessionlId)
```

The SDK parameter name is `sessionlId` with a lowercase `l` before `Id`.

Parameters:

- `hostAddress`: ISBM server base URL.
- `sessionlId`: provider request session id.

Return type: `RapidRedPanda.ISBM.ClientAdapter.ResponseType.CloseProviderRequestSessionResponse`

Observed HTTP behavior:

- Request: `DELETE {hostAddress}/sessions/{sessionId}`
- Expected success: `204 No Content`
- Response type carries only common fields.

## Request Message Types

The inspected SDK assembly does not expose separate public request-message, response-message, or fault DTO classes. Request/response message content is represented through SDK response fields and raw `ISBMHTTPResponse` bodies.

### Base Response

`RapidRedPanda.ISBM.ClientAdapter.ResponseType.ISBMResponse`

Fields:

- `int StatusCode`
- `string ReasonPhrase`
- `string ISBMHTTPResponse`

All request-service response classes inherit from `ISBMResponse`. The SDK response model uses public fields, not properties.

### Session Responses

`OpenConsumerRequestSessionResponse`

- Base fields from `ISBMResponse`
- `string SessionID`

`OpenProviderRequestSessionResponse`

- Base fields from `ISBMResponse`
- `string SessionID`

### Posted Message Responses

`PostRequestResponse`

- Base fields from `ISBMResponse`
- `string MessageID`

`PostResponseResponse`

- Base fields from `ISBMResponse`
- `string MessageID`

### Read Message Responses

`ReadRequestResponse`

- Base fields from `ISBMResponse`
- `string MessageID`
- `string MessageContent`

`ReadResponseResponse`

- Base fields from `ISBMResponse`
- `string MessageID`
- `string MessageContent`

Observed parse behavior:

- `messageId` from the raw JSON response maps to `MessageID`.
- `messageContent.content` maps to `MessageContent`.
- Other envelope fields, if present, should be preserved only through `ISBMHTTPResponse`.

### Empty-Body Operation Responses

These response classes carry only base fields:

- `ExpireRequestResponse`
- `RemoveRequestResponse`
- `RemoveResponseResponse`
- `CloseConsumerRequestSessionResponse`
- `CloseProviderRequestSessionResponse`

Expected successful operations commonly return HTTP `204 No Content`, so `ISBMHTTPResponse` should normally be an empty string.

### Options Objects

`OpenConsumerRequestSessionOptions`

- `string ListenerURL`
- Observed JSON field: `listenerUrl`

`OpenProviderRequestSessionOptions`

- `string ListenerURL`
- `List<FilterExpression> FilterExpressions`
- Observed JSON field for listener: `listenerUrl`
- `FilterExpressions` exists on the public type, but the local provider request session test did not observe it in the emitted JSON body.

`PostRequestOptions`

- `string Expiry`
- Observed JSON field: `expiry`
- The SDK does not expose a strongly typed `DateTime`, `TimeSpan`, TTL, or duration property for request expiry in version `2.0.2.4`; callers provide the expiry value as a string.

`FilterExpression`

- `List<ApplicableMediaType> ApplicableMediaTypes`
- `ExpressionString ExpressionString`

`ExpressionString`

- `string Expression`
- `string Language`
- `string LanguageVersion`

`ApplicableMediaType`

- `string MediaType`

### Fault Objects

No public SDK request fault DTOs were found. HTTP error bodies and ISBM fault bodies remain in `ISBMHTTPResponse`. Transport exceptions are collapsed by the SDK into an SDK response object, consistent with the publication study:

- `StatusCode = 400`
- `ReasonPhrase = "Bad Request"`
- `ISBMHTTPResponse = exception message`

Wrapper code should keep the existing distinction between likely SDK transport failures and real HTTP faults.

## Wrapper Mapping Recommendations

Request wrappers should follow the existing publication wrapper style:

- Create a fresh SDK service per operation.
- Set `Credential.Username` and `Credential.Password`.
- Call the synchronous SDK method inside a `try/catch`.
- Infer success from expected HTTP status code, not exception absence.
- Return `WrapperResponse.SuccessResponse`, `WrapperResponse.FaultResponse`, `WrapperResponse.ValidationFailure`, or `WrapperResponse.ExceptionFailure`.
- Preserve optional raw output through `ISBMHTTPResponse`.
- Treat SDK DTOs as field-based objects.

### Recommended ConsumerRequestWrapper Methods

```csharp
WrapperResponse OpenConsumerRequestSession(
    string host,
    string channel,
    string user,
    string password,
    string? listenerUrl = null,
    bool includeRaw = false)
```

Success payload:

```json
{
  "statusCode": 201,
  "sessionId": "..."
}
```

Validation:

- `--host`
- `--channel`
- `--user`
- `--password`

```csharp
WrapperResponse PostRequest(
    string host,
    string sessionId,
    string topic,
    string messageContent,
    string user,
    string password,
    string? expiry = null,
    bool includeRaw = false)
```

The current wrapper implementation should keep the existing no-expiry overload and add a backward-compatible overload that accepts the SDK expiry string:

```csharp
WrapperResponse PostRequest(
    string host,
    string sessionId,
    string topic,
    string messageContent,
    string user,
    string password,
    bool includeRaw,
    string? expiry)
```

The Consumer Request Console prompts with `Request expiry [blank = no expiry, type cancel to cancel]:`. Blank posts without expiry; `cancel` returns to the menu without posting; nonblank input is passed to `PostRequestOptions.Expiry`.

Success payload:

```json
{
  "statusCode": 201,
  "messageId": "..."
}
```

Validation:

- `--host`
- `--session-id`
- `--topic`
- `--message` or `--message-file`
- `--user`
- `--password`

```csharp
WrapperResponse ExpireRequest(
    string host,
    string sessionId,
    string messageId,
    string user,
    string password,
    bool includeRaw = false)
```

Success payload:

```json
{
  "statusCode": 204,
  "expired": true
}
```

Validation:

- `--host`
- `--session-id`
- `--message-id`
- `--user`
- `--password`

```csharp
WrapperResponse ReadResponse(
    string host,
    string sessionId,
    string requestMessageId,
    string user,
    string password,
    bool includeRaw = false)
```

Success payload:

```json
{
  "statusCode": 200,
  "requestMessageId": "...",
  "messageId": "...",
  "messageContent": "..."
}
```

Validation:

- `--host`
- `--session-id`
- `--request-message-id`
- `--user`
- `--password`

Use `requestMessageId` in the wrapper payload to avoid confusion with the response `messageId`.

```csharp
WrapperResponse RemoveResponse(
    string host,
    string sessionId,
    string requestMessageId,
    string user,
    string password,
    bool includeRaw = false)
```

Success payload:

```json
{
  "statusCode": 204,
  "removed": true
}
```

Validation:

- `--host`
- `--session-id`
- `--request-message-id`
- `--user`
- `--password`

```csharp
WrapperResponse CloseConsumerRequestSession(
    string host,
    string sessionId,
    string user,
    string password,
    bool includeRaw = false)
```

Success payload:

```json
{
  "statusCode": 204,
  "closed": true
}
```

Validation:

- `--host`
- `--session-id`
- `--user`
- `--password`

### Recommended ProviderRequestWrapper Methods

Implemented wrapper command names should match the operation names while keeping request/provider role explicit:

- `open-provider-request-session`
- `read-request`
- `post-response`
- `remove-request`
- `close-provider-request-session`

```csharp
WrapperResponse OpenProviderRequestSession(
    string host,
    string channel,
    string topic,
    string user,
    string password,
    string? listenerUrl = null,
    bool includeRaw = false)
```

Optionally add a future overload for `string[] topics` after the single-topic command is stable.

Success payload:

```json
{
  "statusCode": 201,
  "sessionId": "..."
}
```

Validation:

- `--host`
- `--channel`
- `--topic`
- `--user`
- `--password`

```csharp
WrapperResponse ReadRequest(
    string host,
    string sessionId,
    string user,
    string password,
    bool includeRaw = false)
```

Success payload:

```json
{
  "statusCode": 200,
  "messageId": "...",
  "messageContent": "..."
}
```

Validation:

- `--host`
- `--session-id`
- `--user`
- `--password`

```csharp
WrapperResponse RemoveRequest(
    string host,
    string sessionId,
    string user,
    string password,
    bool includeRaw = false)
```

Success payload:

```json
{
  "statusCode": 204,
  "removed": true
}
```

Validation:

- `--host`
- `--session-id`
- `--user`
- `--password`

The SDK does not accept a request message id for `ProviderRequestService.RemoveRequest`; it removes the current request attached to the provider request session.

```csharp
WrapperResponse PostResponse(
    string host,
    string sessionId,
    string requestMessageId,
    string messageContent,
    string user,
    string password,
    bool includeRaw = false)
```

Success payload:

```json
{
  "statusCode": 201,
  "requestMessageId": "...",
  "responseMessageId": "..."
}
```

Validation:

- `--host`
- `--session-id`
- `--request-message-id`
- `--message` or `--message-file`
- `--user`
- `--password`

```csharp
WrapperResponse CloseProviderRequestSession(
    string host,
    string sessionId,
    string user,
    string password,
    bool includeRaw = false)
```

Success payload:

```json
{
  "statusCode": 204,
  "closed": true
}
```

Validation:

- `--host`
- `--session-id`
- `--user`
- `--password`

### Transport Fault Handling

Use the existing `WrapperResponse.FaultResponse` behavior for non-success SDK responses. It already recognizes likely SDK transport failures where the SDK reports `400 Bad Request` with messages such as DNS, connection, timeout, or certificate failures.

Recommended behavior:

- SDK response with expected status code: success.
- SDK response with unexpected status code and likely transport text: `TransportFault`.
- SDK response with unexpected status code and ISBM/HTTP body: `Fault`.
- Wrapper-thrown exception: `TransportFault` from `ExceptionFailure`.
- Validation problem: `TransportFault` with `ValidationError`.

### Fault Handling

No SDK fault object should be expected. For request wrappers:

- Keep the default `Fault.Code` as the HTTP status code string.
- Keep `Fault.Message` as the raw body when present.
- Try to parse raw JSON details using existing `WrapperFault.FromSdkResponse`.
- Include `Raw` only when `includeRaw` is true.
- Never expose credentials or authorization headers.

## Controlled Workflow Recommendation

Controlled workflows should mirror the existing publication console philosophy: the menu should expose only operations that are valid for the current state and should retain current session/message ids when the SDK returns them.

### Consumer Request Workflow

Recommended states:

1. `NoConsumerRequestSession`
2. `ConsumerRequestSessionOpen`
3. `RequestPosted`
4. `ResponseRead`

Recommended transitions:

| State | Allowed Actions | Success Transition |
| --- | --- | --- |
| `NoConsumerRequestSession` | Open Consumer Request Session | `ConsumerRequestSessionOpen` with `sessionId` |
| `ConsumerRequestSessionOpen` | Post Request, Close Consumer Request Session | Post success moves to `RequestPosted` with `requestMessageId`; close success returns to `NoConsumerRequestSession` |
| `RequestPosted` | Read Response, Expire Request, Close Consumer Request Session | read success moves to `ResponseRead`; expire success can clear current request id; close success returns to `NoConsumerRequestSession` |
| `ResponseRead` | Remove Response, Read Response again, Close Consumer Request Session | remove success clears current response state but may retain request id for audit display; close success returns to `NoConsumerRequestSession` |

Notes:

- Store the request `MessageID` returned from `PostRequest` as `requestMessageId`.
- `ReadResponse` and `RemoveResponse` require the original request message id.
- Offer `ExpireRequest` only after a request has been posted and before a response is removed or the session is closed.
- Always offer close when a session is open.
- On failed operations, remain in the current state unless the user explicitly clears state.

### Provider Request Workflow

Recommended states:

1. `NoProviderRequestSession`
2. `ProviderRequestSessionOpen`
3. `RequestRead`
4. `ResponsePosted`

Recommended transitions:

| State | Allowed Actions | Success Transition |
| --- | --- | --- |
| `NoProviderRequestSession` | Open Provider Request Session | `ProviderRequestSessionOpen` with `sessionId` |
| `ProviderRequestSessionOpen` | Read Request, Close Provider Request Session | read success moves to `RequestRead` with `requestMessageId`; close success returns to `NoProviderRequestSession` |
| `RequestRead` | Post Response, Remove Request, Read Request again, Close Provider Request Session | post success moves to `ResponsePosted`; remove success clears current request id; close success returns to `NoProviderRequestSession` |
| `ResponsePosted` | Remove Request, Read Request, Close Provider Request Session | remove success clears current request id and returns to `ProviderRequestSessionOpen`; close success returns to `NoProviderRequestSession` |

Notes:

- `ReadRequest` returns the request `MessageID` and `MessageContent`.
- `PostResponse` requires the request message id returned by `ReadRequest`.
- `RemoveRequest` removes the current request from the provider session after processing.
- Keep the menu guarded so users cannot post a response before a request message id exists.
- Always offer close when a provider request session is open.
