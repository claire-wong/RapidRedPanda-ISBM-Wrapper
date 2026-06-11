# RapidRedPanda-ISBM-Wrapper

RapidRedPanda-ISBM-Wrapper is a .NET 8 wrapper library and machine-friendly JSON CLI for automating ISA-95 ISBM 2.0 publication/subscription and request/response workflows through `RapidRedPanda.ISBM.ClientAdapter`.

The repository is intended for:

- Python applications that need to call ISBM operations through `subprocess`.
- Shell scripts and automation jobs that need stable JSON output.
- AI agents and orchestration tools that need predictable command results.
- .NET developers who want a small wrapper over the RapidRedPanda SDK.

The relationship between the pieces is:

```text
Python / shell / AI agent
-> RapidRedPanda.Wrapper.Cli
-> RapidRedPanda.Wrapper
-> RapidRedPanda.ISBM.ClientAdapter
-> ISBM 2.0 server
```

## Repository layout

- `src/RapidRedPanda.Wrapper` - reusable .NET wrapper library over `RapidRedPanda.ISBM.ClientAdapter`.
- `src/RapidRedPanda.Wrapper.Cli` - non-interactive JSON CLI for Python, AI agents, and automation clients.
- `samples/python` - Python standard-library examples that invoke the CLI.
- `samples/csharp/RapidRedPanda.Wrapper.Console` - C# testing/demo console app with interactive workflows.
- `docs/SDK_STUDY.md` - research notes on the wrapped SDK and observed publication behavior.
- `docs/REQUEST_SERVICES_SDK_STUDY.md` - research notes on request/response SDK behavior.
- `tests` - executable test harness project.

## Quickstart from source

Prerequisites:

- .NET 8 SDK
- Python 3.10+ for the Python samples

Build the solution:

```powershell
dotnet restore RapidRedPanda.Wrapper.sln
dotnet build RapidRedPanda.Wrapper.sln -m:1
```

Run the test harness:

```powershell
dotnet run --project tests/RapidRedPanda.Wrapper.Tests/RapidRedPanda.Wrapper.Tests.csproj
```

Run a CLI command from the built Debug DLL:

```powershell
dotnet .\src\RapidRedPanda.Wrapper.Cli\bin\Debug\net8.0\RapidRedPanda.Wrapper.Cli.dll open-subscription --host http://your-server/isbm/2.0 --channel /YourOrganization/Publication --topic YourPublicationTopic --user your-username --password your-password
```

## Python samples

Python samples use only the Python standard library. They invoke `RapidRedPanda.Wrapper.Cli` through `subprocess`; they do not load the .NET wrapper library directly.

See `samples/python/README.md` for examples covering:

- Consumer Publication
- Provider Publication
- Consumer Request
- Provider Request

Create local Python sample config from the template:

```powershell
Copy-Item samples/python/sample_config.example.json samples/python/sample_config.json
```

Then update `samples/python/sample_config.json` for your ISBM environment.

## CLI command overview

The production CLI writes JSON to standard output and exits with:

- `0` for successful wrapper operations.
- `1` for CLI execution failures.
- `2` for invalid arguments.
- `3` for valid wrapper responses that indicate operation failure.

Consumer publication commands:

- `open-subscription --host <url> --channel <channel> --topic <topic> --user <user> --password <password> [--raw]`
- `read-publication --host <url> --session-id <id> --user <user> --password <password> [--raw]`
- `remove-publication --host <url> --session-id <id> --user <user> --password <password> [--raw]`
- `close-subscription --host <url> --session-id <id> --user <user> --password <password> [--raw]`

Provider publication commands:

- `open-publication-session --host <url> --channel <channel> --user <user> --password <password> [--topic <topic>] [--raw]`
- `post-publication --host <url> --session-id <id> --topic <topic> --content <json> --user <user> --password <password> [--media-type application/json] [--expiry <value>] [--raw]`
- `expire-publication --host <url> --session-id <id> --message-id <id> --user <user> --password <password> [--raw]`
- `close-publication-session --host <url> --session-id <id> --user <user> --password <password> [--raw]`

Consumer request commands:

- `open-request-session --host <url> --channel <channel> --user <user> --password <password> [--topic <topic>] [--raw]`
- `post-request --host <url> --session-id <id> --topic <topic> --content <json> --user <user> --password <password> [--media-type application/json] [--expiry <value>] [--raw]`
- `read-response --host <url> --session-id <id> --request-message-id <id> --user <user> --password <password> [--raw]`
- `remove-response --host <url> --session-id <id> --request-message-id <id> --user <user> --password <password> [--raw]`
- `expire-request --host <url> --session-id <id> --message-id <id> --user <user> --password <password> [--raw]`
- `close-request-session --host <url> --session-id <id> --user <user> --password <password> [--raw]`

Provider request commands:

- `open-provider-request-session --host <url> --channel <channel> --topic <topic> --user <user> --password <password> [--raw]`
- `read-request --host <url> --session-id <id> --user <user> --password <password> [--raw]`
- `post-response --host <url> --session-id <id> --request-message-id <id> --content <json> --user <user> --password <password> [--media-type application/json] [--raw]`
- `remove-request --host <url> --session-id <id> --user <user> --password <password> [--raw]`
- `close-provider-request-session --host <url> --session-id <id> --user <user> --password <password> [--raw]`

`--media-type` is accepted on post commands for compatibility with existing examples. The current wrapper passes message content through the installed SDK's JSON message-content behavior.

## JSON response format

Every CLI command writes a JSON response with this envelope:

```json
{
  "success": true,
  "command": "open-subscription",
  "timestampUtc": "2026-06-04T12:00:00.0000000Z",
  "data": {
    "statusCode": 201,
    "sessionId": "example-session-id"
  },
  "raw": null,
  "fault": null,
  "transportFault": null
}
```

On success, `data` contains operation-specific fields such as `sessionId`, `messageId`, `messageContent`, `topics`, `closed`, `removed`, or `expired`.

On an ISBM/HTTP operation failure, `success` is `false` and `fault` contains:

- `code`
- `message`
- `details`

On validation, transport, or CLI execution failures, `success` is `false` and `transportFault` contains:

- `type`
- `message`
- `details`

Use `--raw` to include the SDK raw response body when the operation returns one. Credentials and authorization headers are not included in wrapper JSON responses.

## Local configuration and security

Use example files as templates:

- `samples/python/sample_config.example.json`
- `samples/csharp/RapidRedPanda.Wrapper.Console/appsettings.example.json`

Local files such as `samples/python/sample_config.json` and `appsettings.Development.json` can contain credentials and must not be committed. They are ignored by `.gitignore`.

Passing `--password` on a command line can expose the value through shell history or process inspection. Prefer local config files or your platform's secret-management tooling for shared or production environments.

## Creating release packages

Create self-contained release assets for Windows x64, Linux x64, and Linux ARM64:

```powershell
.\scripts\release.ps1 0.3.4
```

Generated release assets are placed in:

```text
artifacts/
```

The generated `.zip` and `.tar.gz` files can be uploaded to a GitHub Release.

Each package extracts to a versioned folder containing:

- The self-contained `RapidRedPanda.Wrapper.Cli` executable.
- Root documentation: `README.md`, `CHANGELOG.md`, `CONTRIBUTING.md`, and `SECURITY.md`.
- Python samples and `sample_config.example.json` under `samples/python`.
- The published C# console sample under `samples/csharp-console`.

## Troubleshooting

- If the CLI DLL is missing, run `dotnet build RapidRedPanda.Wrapper.sln -m:1`.
- If Python samples cannot find the CLI, check the locations listed in `samples/python/README.md`.
- If a command returns `transportFault`, check the ISBM host URL, DNS, network access, credentials, and TLS/certificate configuration.
- If a command returns `fault`, the ISBM server or SDK returned an unexpected status or fault body.
- If `raw` is `null`, either `--raw` was not used or the SDK response body was empty.

## License

License selection is pending owner decision. Add a `LICENSE` file before publishing or distributing release assets broadly.

## Post publication expiry

`post-publication` accepts an optional `--expiry <value>` parameter. The value is passed through to the SDK `PostPublicationOptions.Expiry` field.

`post-request` also accepts optional `--expiry <value>` and passes it through to the SDK `PostRequestOptions.Expiry` field.

## Subscription filter expressions

The C# testing/demo console and production JSON CLI support optional filter flags for `open-subscription` and `open-provider-request-session`.

```powershell
dotnet run --project samples/csharp/RapidRedPanda.Wrapper.Console -- open-subscription --host http://your-isbm-server/isbm/2.0 --channel /YourOrganization/Publication --topic YourPublicationTopic --user your-username --password your-password --filter-media-type application/json --filter-language JSONPath --filter-language-version com.jayway.jsonpath:json-path:2.4.0 --filter-expression "$['LoremIpsum'][?(@.field == 'SomeText')]"
```

The installed `RapidRedPanda.ISBM.ClientAdapter` package exposes applicable media types and expression strings for subscription filters. It does not expose a namespace collection on `FilterExpression` in version `2.0.2.4`, so namespace values are returned as wrapper validation failures.

Filter expressions are supported only where the current SDK exposes `FilterExpressions` options:

- Consumer Publication `OpenSubscriptionSession`
- Provider Request `OpenProviderRequestSession`

Filter expressions are not supported for Provider Publication or Consumer Request session operations.
