# RapidRedPanda-ISBM-Wrapper
.NET 8 wrapper and CLI toolkit for RapidRedPanda ISBM Client Adapter.

## Repository layout

- `src/RapidRedPanda.Wrapper` - wrapper library over `RapidRedPanda.ISBM.ClientAdapter`.
- `src/RapidRedPanda.Wrapper.Console` - CLI commands and interactive publication/subscription workflows.
- `docs/SDK_STUDY.md` - notes on the wrapped SDK and observed ISBM behavior.
- `samples/appsettings.Development.example.json` - sample local settings for the console app.
- `tests` - reserved for future test projects.

## Build

```powershell
dotnet restore RapidRedPanda.Wrapper.sln
dotnet build RapidRedPanda.Wrapper.sln -m:1
```

## Tests

The test project is a small executable harness with no third-party test framework dependency, so it can run offline after restore.

```powershell
dotnet run --project tests/RapidRedPanda.Wrapper.Tests/RapidRedPanda.Wrapper.Tests.csproj
```

## Local configuration

The console app can load `appsettings.json` or `appsettings.Development.json` from the app base directory, current directory, or `src/RapidRedPanda.Wrapper.Console`.
Use `samples/appsettings.Development.example.json` as the template for local development settings.

## Post Publication Expiry

`post-publication` accepts an optional `--expiry <value>` parameter. The value is passed through to the SDK `PostPublicationOptions.Expiry` field.

## Subscription Filter Expressions

`open-subscription` accepts optional JSONPath filter flags. The wrapper models filter expressions as a collection because the ISBM `filterExpressions` field is an array, and maps them to the SDK `OpenSubscriptionSessionOptions.FilterExpressions` model:

```powershell
dotnet run --project src/RapidRedPanda.Wrapper.Console -- open-subscription --host http://your-isbm-server/isbm/2.0 --channel /YourOrganization/YourPublicationChannel --topic YourPublicationTopic --user YourUser --password YourPassword --filter-media-type application/json --filter-language JSONPath --filter-language-version com.jayway.jsonpath:json-path:2.4.0 --filter-expression "$['LoremIpsum'][?(@.field == 'SomeText')]"
```

The installed `RapidRedPanda.ISBM.ClientAdapter` package exposes applicable media types and expression strings for subscription filters. It does not expose a namespace collection on `FilterExpression` in version `2.0.2.4`, so namespace values are returned as wrapper validation failures.

The interactive Consumer Publication Console can send multiple filter expressions when opening a subscription session:

```text
Use filter expressions? [y/N]: y
Filter Expression #1
Applicable media types, comma-separated [application/json]:
Filter language [JSONPath]:
Filter language version [com.jayway.jsonpath:json-path:2.4.0]:
Filter expression: $['LoremIpsum'][?(@.field == 'SomeText')]
Add another filter expression? [y/N]: y
Filter Expression #2
Applicable media types, comma-separated [application/json]:
Filter language [JSONPath]:
Filter language version [com.jayway.jsonpath:json-path:2.4.0]:
Filter expression: $['LoremIpsum'][?(@.priority == 'High')]
Add another filter expression? [y/N]: n
```

Filter expressions are supported only where the current SDK exposes `FilterExpressions` options:

- Consumer Publication `OpenSubscriptionSession`
- Provider Request `OpenProviderRequestSession`

Filter expressions are not supported for Provider Publication or Consumer Request session operations.
