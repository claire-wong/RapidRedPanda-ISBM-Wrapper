"""
RapidRedPanda Wrapper Python Sample

Sample 07:
Expire a previously posted publication.

This sample demonstrates:
- Executing the wrapper CLI from Python
- Expiring publication messages
- Parsing wrapper JSON responses
- Handling operation failures

No third-party dependencies are required.
"""

import json
import subprocess
import sys
from pathlib import Path


HOST = "http://104.239.197.5/isbm/2.0"
USER = "Tester1"
PASSWORD = "Password1"

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
EXIT_EXPIRE_PUBLICATION_FAILED = 3
EXIT_INVALID_USAGE = 4


def build_command(session_id: str, message_id: str) -> list[str]:
    return [
        "dotnet",
        str(CLI_DLL),
        "expire-publication",
        "--host",
        HOST,
        "--session-id",
        session_id,
        "--message-id",
        message_id,
        "--user",
        USER,
        "--password",
        PASSWORD,
    ]


def print_usage() -> None:
    print("Usage:", file=sys.stderr)
    print("python samples/python/07_expire_publication.py <session-id> <message-id>", file=sys.stderr)


def print_failure_details(response: dict[str, object]) -> None:
    fault = response.get("fault") or response.get("transportFault")
    print(json.dumps(fault, indent=2) if fault is not None else "The CLI did not provide fault details.")


def run_cli(session_id: str, message_id: str) -> tuple[int, str, str]:
    try:
        result = subprocess.run(build_command(session_id, message_id), capture_output=True, text=True, check=False)
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
    if len(sys.argv) != 3 or not sys.argv[1].strip() or not sys.argv[2].strip():
        print_usage()
        return EXIT_INVALID_USAGE

    session_id = sys.argv[1].strip()
    message_id = sys.argv[2].strip()

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

    return_code, stdout, stderr = run_cli(session_id, message_id)
    if return_code != 0 and not stdout.strip():
        print(f"The CLI process exited unexpectedly with code {return_code}.", file=sys.stderr)
        if stderr:
            print(stderr.strip(), file=sys.stderr)
        return EXIT_CLI_EXECUTION_FAILURE

    response = parse_response(stdout, stderr)
    if response is None:
        return EXIT_INVALID_RESPONSE

    if response["success"] is False:
        print("Expire publication failed.")
        print_failure_details(response)
        return EXIT_EXPIRE_PUBLICATION_FAILED

    print("Publication expired successfully.")
    print()
    print("Session ID:")
    print(session_id)
    print()
    print("Message ID:")
    print(message_id)
    return EXIT_SUCCESS


if __name__ == "__main__":
    raise SystemExit(main())
