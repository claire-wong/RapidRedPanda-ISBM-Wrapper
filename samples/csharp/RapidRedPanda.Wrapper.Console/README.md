# RapidRedPanda Wrapper C# Console Sample

This folder contains the C# developer testing/demo console application.

`RapidRedPanda.Wrapper.Console` is not the production automation bridge. For Python, shell scripts, AI agents, and other subprocess automation, use `RapidRedPanda.Wrapper.Cli`.

## Configuration

Copy:

```text
appsettings.example.json
```

to:

```text
appsettings.Development.json
```

Then replace the placeholder values with your own ISBM host, username, password, channels, and topics.

Do not commit `appsettings.Development.json`.

## Running

From a source checkout:

```powershell
dotnet run --project samples/csharp/RapidRedPanda.Wrapper.Console/RapidRedPanda.Wrapper.Console.csproj
```

From a release package, run the published console executable in this folder.
