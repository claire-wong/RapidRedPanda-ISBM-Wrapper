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
dotnet build RapidRedPanda.Wrapper.sln
```

## Local configuration

The console app can load `appsettings.json` or `appsettings.Development.json` from the app base directory, current directory, or `src/RapidRedPanda.Wrapper.Console`.
Use `samples/appsettings.Development.example.json` as the template for local development settings.
