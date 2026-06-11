# Contributing

Thanks for helping improve RapidRedPanda-ISBM-Wrapper.

## Development setup

Prerequisites:

- .NET 8 SDK
- Python 3.10+ for Python sample validation

Build the solution:

```powershell
dotnet restore RapidRedPanda.Wrapper.sln
dotnet build RapidRedPanda.Wrapper.sln -m:1
```

Run the test harness:

```powershell
dotnet run --project tests/RapidRedPanda.Wrapper.Tests/RapidRedPanda.Wrapper.Tests.csproj
```

## Local configuration

Use the checked-in example files as templates:

- `samples/python/sample_config.example.json`
- `samples/csharp/RapidRedPanda.Wrapper.Console/appsettings.example.json`

Do not commit local configuration files containing real hosts, usernames, passwords, tokens, or customer data. Local config files such as `samples/python/sample_config.json` and `appsettings.Development.json` are ignored by `.gitignore`.

## Pull request expectations

- Keep changes small and reviewable.
- Do not change CLI argument behavior without updating README and sample documentation.
- Keep CLI stdout machine-readable JSON.
- Avoid adding third-party dependencies to Python samples unless there is a strong reason.
- Update `CHANGELOG.md` for user-visible changes.

## Documentation changes

When changing wrapper behavior, update the relevant documentation:

- `README.md` for public quickstart, command overview, and response format.
- `samples/python/README.md` for Python sample workflows.
- `docs/` research notes only when the underlying SDK observations change.
