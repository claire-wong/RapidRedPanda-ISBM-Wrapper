# RapidRedPanda Wrapper Python Samples

These samples demonstrate how Python applications can invoke `RapidRedPanda.Wrapper.Cli` and consume its JSON responses using only the Python standard library.

No third-party Python packages are required.

The CLI is a non-interactive, machine-friendly JSON command-line application. Python invokes it through `subprocess`; it does not use Python.NET or directly load `RapidRedPanda.Wrapper.dll`.

`RapidRedPanda.Wrapper.Console` is a C# testing/demo application and is not used by the Python samples.

## Prerequisites

- Python 3.10+
- One of:
  - A self-contained RapidRedPanda Wrapper release package
  - A source checkout built with `dotnet build`

.NET 8 is required only when running from a source checkout with the Debug CLI DLL fallback. A self-contained release package can run the CLI executable directly.

## Configuration

The samples read connection settings from `samples/python/sample_config.json`.

1. Copy `sample_config.example.json`:

```powershell
Copy-Item samples/python/sample_config.example.json samples/python/sample_config.json
```

2. Rename the copy to `sample_config.json`.

3. Replace all placeholder values with your own ISBM environment settings.

4. Do not commit `sample_config.json`.

The config file contains the host, credentials, and publication/request channels/topics used by the sample workflows. `sample_config.json` is ignored by Git so local credentials are not committed.

## CLI Discovery

The Python samples automatically locate `RapidRedPanda.Wrapper.Cli` and do not need to be edited when moving between Windows, Linux, macOS, and source checkouts.

From `samples/python/*.py`, the samples check these locations in order:

1. `../../RapidRedPanda.Wrapper.Cli`
2. `../../RapidRedPanda.Wrapper.Cli.exe`
3. `../../src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll`

When a deployed executable is found, it is invoked directly. When the Debug DLL is found, it is invoked through `dotnet`.

## Sample 01: Open Subscription

`01_open_subscription.py` is a small "Hello World" example that:

- Runs the CLI's `open-subscription` command.
- Parses the JSON response.
- Checks whether the operation succeeded.
- Displays the returned ISBM subscription session ID.

Before running the sample, create `samples/python/sample_config.json` as described in the Configuration section.

This sample invokes either the deployed self-contained CLI executable or the source-build Debug CLI DLL, depending on which one is available.

## Build

From a source checkout, build the solution in Debug configuration:

```powershell
dotnet build
```

## Run Sample 01

From the repository root:

```powershell
python samples/python/01_open_subscription.py
```

## Release Package Usage

After extracting a self-contained release package, create `samples/python/sample_config.json` from `samples/python/sample_config.example.json`, update it for your ISBM environment, and run the samples directly from the package root.

From a deployed Linux or macOS release package:

```bash
cd RapidRedPanda-ISBM-Wrapper
python3 samples/python/01_open_subscription.py
```

From a deployed Windows release package:

```powershell
cd RapidRedPanda-ISBM-Wrapper
python samples\python\01_open_subscription.py
```

## Test The CLI Manually

From the repository root:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll open-subscription --host http://your-server/isbm/2.0 --channel /Miami-Dade/Flood+Control/ISO18435:D1.2/Publication --topic OIIE:S30:V1.1/CCOM-JSON:SyncMeasurements:V1.0 --user your-username --password your-password
```

## Expected JSON Response

The CLI writes a response like this to standard output:

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

## Example Successful Output

```text
RapidRedPanda Wrapper Demo
--------------------------

Subscription session opened successfully.

Session ID:
example-session-id
```

The sample exits with `0` on success, `1` when the CLI cannot be executed, `2` for an invalid CLI response, and `3` when the subscription operation fails.

## Sample 02: Read Publication

`02_read_publication.py` builds on Sample 01 by reading a publication from an existing subscription session:

```text
01_open_subscription.py
  Open a subscription session.

02_read_publication.py
  Read a publication from that subscription session.

03_remove_publication.py
  Remove the publication from that subscription session.

04_close_subscription.py
  Close the subscription session.
```

Before running the sample, create `samples/python/sample_config.json` as described in the Configuration section.

Usage:

```powershell
python samples/python/02_read_publication.py <session-id>
```

Example:

```powershell
python samples/python/02_read_publication.py c065133a-440f-4235-8fab-81bcc6355289
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll read-publication --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --user your-username --password your-password
```

If no publication is available, the sample prints:

```text
No publication available.
```

## Sample 03: Remove Publication

`03_remove_publication.py` removes a publication from an existing subscription session.

Usage:

```powershell
python samples/python/03_remove_publication.py <session-id>
```

Example:

```powershell
python samples/python/03_remove_publication.py c065133a-440f-4235-8fab-81bcc6355289
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll remove-publication --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --user your-username --password your-password
```

## Sample 04: Close Subscription

`04_close_subscription.py` closes an existing subscription session.

Usage:

```powershell
python samples/python/04_close_subscription.py <session-id>
```

Example:

```powershell
python samples/python/04_close_subscription.py c065133a-440f-4235-8fab-81bcc6355289
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll close-subscription --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --user your-username --password your-password
```

## Full Consumer Publication Workflow

1. Open subscription:

```powershell
python samples/python/01_open_subscription.py
```

2. Read publication:

```powershell
python samples/python/02_read_publication.py <session-id>
```

3. Remove publication:

```powershell
python samples/python/03_remove_publication.py <session-id>
```

4. Close subscription:

```powershell
python samples/python/04_close_subscription.py <session-id>
```

## Provider Publication Workflow

The provider publication samples demonstrate this workflow:

```text
Open Publication Session
  -> Post Publication
  -> Expire Publication
  -> Close Publication Session
```

Sample 06 returns a Message ID. Sample 07 requires both the Session ID and the Message ID. Sample 08 requires only the Session ID.

1. Open publication session:

```powershell
python samples/python/05_open_publication_session.py
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll open-publication-session --host http://your-server/isbm/2.0 --channel /Miami-Dade/Flood+Control/ISO18435:D1.2/Publication --topic OIIE:S30:V1.1/CCOM-JSON:SyncMeasurements:V1.0 --user your-username --password your-password
```

2. Post publication:

```powershell
python samples/python/06_post_publication.py <session-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll post-publication --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --topic OIIE:S30:V1.1/CCOM-JSON:SyncMeasurements:V1.0 --media-type application/json --content '{"messageType":"SyncMeasurements"}' --user your-username --password your-password
```

3. Expire publication:

```powershell
python samples/python/07_expire_publication.py <session-id> <message-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll expire-publication --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --message-id message-123 --user your-username --password your-password
```

4. Close publication session:

```powershell
python samples/python/08_close_publication_session.py <session-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll close-publication-session --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --user your-username --password your-password
```

## Consumer Request Workflow

The consumer request samples demonstrate this workflow:

```text
Open Request Session
  -> Post Request
  -> Read Response
  -> Remove Response
  -> Expire Request
  -> Close Request Session
```

Sample 10 returns a request Message ID. Sample 13 requires both the Session ID and request Message ID. Sample 14 requires only the Session ID.

Note: the RapidRedPanda SDK requires the original request Message ID when reading or removing a response, so Samples 11 and 12 accept `<request-message-id>` in addition to `<session-id>`.

Expire Request requires:

- Session ID
- Message ID

1. Open request session:

```powershell
python samples/python/09_open_request_session.py
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll open-request-session --host http://your-server/isbm/2.0 --channel /Miami-Dade/Flood+Control/ISO18435:D1.2/Request --topic OIIE:S32:V1.1/CCOM-JSON:GetMeasurements:V1.0 --user your-username --password your-password
```

2. Post request:

```powershell
python samples/python/10_post_request.py <session-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll post-request --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --topic OIIE:S32:V1.1/CCOM-JSON:GetMeasurements:V1.0 --media-type application/json --content '{"messageType":"GetMeasurements"}' --user your-username --password your-password
```

3. Read response:

```powershell
python samples/python/11_read_response.py <session-id> <request-message-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll read-response --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --request-message-id request-123 --user your-username --password your-password
```

4. Remove response:

```powershell
python samples/python/12_remove_response.py <session-id> <request-message-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll remove-response --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --request-message-id request-123 --user your-username --password your-password
```

5. Expire request:

```powershell
python samples/python/13_expire_request.py <session-id> <message-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll expire-request --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --message-id request-123 --user your-username --password your-password
```

6. Close request session:

```powershell
python samples/python/14_close_request_session.py <session-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll close-request-session --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --user your-username --password your-password
```

## Provider Request Workflow

The provider request samples demonstrate this workflow:

```text
Open Provider Request Session
  -> Read Request
  -> Post Response
  -> Remove Request
  -> Close Provider Request Session
```

Sample 16 returns the request Message ID. Sample 17 requires both the Session ID and request Message ID. Sample 19 requires only the Session ID.

1. Open provider request session:

```powershell
python samples/python/15_open_provider_request_session.py
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll open-provider-request-session --host http://your-server/isbm/2.0 --channel /Miami-Dade/Flood+Control/ISO18435:D1.2/Request --topic OIIE:S32:V1.1/CCOM-JSON:GetMeasurements:V1.0 --user your-username --password your-password
```

2. Read request:

```powershell
python samples/python/16_read_request.py <session-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll read-request --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --user your-username --password your-password
```

3. Post response:

```powershell
python samples/python/17_post_response.py <session-id> <request-message-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll post-response --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --request-message-id request-123 --media-type application/json --content '{"messageType":"Response","status":"Accepted"}' --user your-username --password your-password
```

4. Remove request:

```powershell
python samples/python/18_remove_request.py <session-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll remove-request --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --user your-username --password your-password
```

5. Close provider request session:

```powershell
python samples/python/19_close_provider_request_session.py <session-id>
```

Manual CLI test:

```powershell
dotnet ./src/RapidRedPanda.Wrapper.Cli/bin/Debug/net8.0/RapidRedPanda.Wrapper.Cli.dll close-provider-request-session --host http://your-server/isbm/2.0 --session-id c065133a-440f-4235-8fab-81bcc6355289 --user your-username --password your-password
```


