"""
RapidRedPanda Wrapper Python Sample

Sample 15:
Open a provider request session.

This sample demonstrates:
- Executing the wrapper CLI from Python
- Opening an ISBM provider request session
- Parsing wrapper JSON responses
- Retrieving the provider request session ID

No third-party dependencies are required.
"""

import json
import subprocess
import sys
from pathlib import Path


# Configure these values for your ISBM environment.
HOST = "http://104.239.197.5/isbm/2.0"
CHANNEL = "/Miami-Dade/Flood+Control/ISO18435:D1.2/Request"
TOPIC = "OIIE:S32:V1.1/CCOM-JSON:GetMeasurements:V1.0"
USER = "Tester1"
PASSWORD = "Password1"

# The framework-dependent CLI assembly is expected in the Debug build output.
CLI_DLL = (
    Path(__file__).resolve().parents[2]
    / "src"
    / "RapidRedPanda.Wrapper.Cli"
    / "bin"
    / "Debug"
    / "net8.0"
    / "RapidRedPanda.Wrapper.Cli.dll"
)

EXIT_SUCCESS = 0
EXIT_CLI_EXECUTION_FAILURE = 1
EXIT_INVALID_RESPONSE = 2
EXIT_OPEN_PROVIDER_REQUEST_SESSION_FAILED = 3


def build_command() -> list[str]:
    """Build the dotnet CLI command-line arguments for open-provider-request-session."""
    return [
        "dotnet",
        str(CLI_DLL),
        "open-provider-request-session",
        "--host",
        HOST,
        "--channel",
        CHANNEL,
        "--topic",
        TOPIC,
        "--user",
        USER,
        "--password",
        PASSWORD,
    ]


def print_failure_details(response: dict[str, object]) -> None:
    """Print the fault information returned by the CLI."""
    fault = response.get("fault") or response.get("transportFault")
    if fault is None:
        print("The CLI did not provide fault details.")
        return

    print(json.dumps(fault, indent=2))


def run_cli() -> tuple[int, str, str]:
    """Execute the CLI and return process code, stdout, and stderr."""
    try:
        result = subprocess.run(
            build_command(),
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError:
        print("The .NET runtime or SDK could not be found.", file=sys.stderr)
        print(file=sys.stderr)
        print("Install .NET 8 and verify the 'dotnet' command is available.", file=sys.stderr)
        return EXIT_CLI_EXECUTION_FAILURE, "", ""
    except OSError as error:
        print(f"Unable to execute the CLI through dotnet: {error}", file=sys.stderr)
        return EXIT_CLI_EXECUTION_FAILURE, "", ""

    return result.returncode, result.stdout, result.stderr


def parse_response(stdout: str, stderr: str) -> dict[str, object] | None:
    """Parse and validate the CLI JSON response."""
    try:
        response = json.loads(stdout)
    except json.JSONDecodeError as error:
        print(f"The CLI returned invalid JSON: {error}", file=sys.stderr)
        if stderr:
            print(stderr.strip(), file=sys.stderr)
        return None

    if not isinstance(response, dict) or not isinstance(response.get("success"), bool):
        print("The CLI response is missing a valid 'success' value.", file=sys.stderr)
        return None

    return response


def main() -> int:
    """Open a provider request session and display the returned session ID."""
    print("RapidRedPanda Wrapper Demo")
    print("--------------------------")
    print()

    if not CLI_DLL.exists():
        print("RapidRedPanda Wrapper CLI DLL not found.", file=sys.stderr)
        print(file=sys.stderr)
        print("Expected location:", file=sys.stderr)
        print(CLI_DLL, file=sys.stderr)
        print(file=sys.stderr)
        print("Build the wrapper first:", file=sys.stderr)
        print(file=sys.stderr)
        print("dotnet build", file=sys.stderr)
        return EXIT_CLI_EXECUTION_FAILURE

    return_code, stdout, stderr = run_cli()
    if return_code == EXIT_CLI_EXECUTION_FAILURE and not stdout.strip():
        return EXIT_CLI_EXECUTION_FAILURE

    if return_code != 0 and not stdout.strip():
        print(f"The CLI process exited unexpectedly with code {return_code}.", file=sys.stderr)
        if stderr:
            print(stderr.strip(), file=sys.stderr)
        return EXIT_CLI_EXECUTION_FAILURE

    response = parse_response(stdout, stderr)
    if response is None:
        return EXIT_INVALID_RESPONSE

    if response["success"] is False:
        print("Open provider request session failed.")
        print_failure_details(response)
        return EXIT_OPEN_PROVIDER_REQUEST_SESSION_FAILED

    data = response.get("data")
    session_id = data.get("sessionId") if isinstance(data, dict) else None
    if not isinstance(session_id, str) or not session_id:
        print("The successful CLI response is missing 'data.sessionId'.", file=sys.stderr)
        return EXIT_INVALID_RESPONSE

    print("Provider request session opened successfully.")
    print()
    print("Session ID:")
    print(session_id)
    return EXIT_SUCCESS


if __name__ == "__main__":
    raise SystemExit(main())
