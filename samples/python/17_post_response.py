"""
RapidRedPanda Wrapper Python Sample

Sample 17:
Post a response for a request.

This sample demonstrates:
- Executing the wrapper CLI from Python
- Posting a response message
- Parsing wrapper JSON responses
- Retrieving the response Message ID

No third-party dependencies are required.
"""

import json
import subprocess
import sys
from cli_locator import CliNotFoundError, get_cli_command_prefix, print_cli_not_found
from config_loader import ConfigError, load_config

PAYLOAD = {"messageType": "Response", "status": "Accepted", "detail": "Sample response payload"}


EXIT_SUCCESS = 0
EXIT_CLI_EXECUTION_FAILURE = 1
EXIT_INVALID_RESPONSE = 2
EXIT_POST_RESPONSE_FAILED = 3
EXIT_INVALID_USAGE = 4


def build_command(cli_command_prefix: list[str], config: dict[str, str], session_id: str, request_message_id: str) -> list[str]:
    """Build the dotnet CLI command-line arguments for post-response."""
    return cli_command_prefix + [
        "post-response",
        "--host",
        config["host"],
        "--session-id",
        session_id,
        "--request-message-id",
        request_message_id,
        "--media-type",
        "application/json",
        "--content",
        json.dumps(PAYLOAD, separators=(",", ":")),
        "--user",
        config["user"],
        "--password",
        config["password"],
    ]


def print_usage() -> None:
    """Print command-line usage for this sample."""
    print("Usage:", file=sys.stderr)
    print("python samples/python/17_post_response.py <session-id> <request-message-id>", file=sys.stderr)


def print_failure_details(response: dict[str, object]) -> None:
    """Print the fault information returned by the CLI."""
    fault = response.get("fault") or response.get("transportFault")
    if fault is None:
        print("The CLI did not provide fault details.")
        return

    print(json.dumps(fault, indent=2))


def run_cli(cli_command_prefix: list[str], config: dict[str, str], session_id: str, request_message_id: str) -> tuple[int, str, str]:
    """Execute the CLI and return process code, stdout, and stderr."""
    try:
        result = subprocess.run(
            build_command(cli_command_prefix, config, session_id, request_message_id),
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError:
        print("The .NET runtime or SDK could not be found.", file=sys.stderr)
        print(file=sys.stderr)
        print("Install .NET 8 when using the source-build DLL fallback, or use a self-contained release package.", file=sys.stderr)
        return EXIT_CLI_EXECUTION_FAILURE, "", ""
    except OSError as error:
        print(f"Unable to execute the CLI: {error}", file=sys.stderr)
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
    """Post a response and display the returned response Message ID."""
    if len(sys.argv) != 3 or not sys.argv[1].strip() or not sys.argv[2].strip():
        print_usage()
        return EXIT_INVALID_USAGE

    print("RapidRedPanda Wrapper Demo")
    print("--------------------------")
    print()

    try:
        config = load_config()
    except ConfigError as error:
        print(error, file=sys.stderr)
        return EXIT_CLI_EXECUTION_FAILURE

    try:
        cli_command_prefix = get_cli_command_prefix()
    except CliNotFoundError as error:
        print_cli_not_found(error)
        return EXIT_CLI_EXECUTION_FAILURE

    session_id = sys.argv[1].strip()
    request_message_id = sys.argv[2].strip()
    return_code, stdout, stderr = run_cli(cli_command_prefix, config, session_id, request_message_id)
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
        print("Post response failed.")
        print_failure_details(response)
        return EXIT_POST_RESPONSE_FAILED

    data = response.get("data")
    response_message_id = None
    if isinstance(data, dict):
        response_message_id = data.get("responseMessageId") or data.get("messageId")

    if not isinstance(response_message_id, str) or not response_message_id:
        print("The successful CLI response is missing response message id.", file=sys.stderr)
        return EXIT_INVALID_RESPONSE

    print("Response posted successfully.")
    print()
    print("Session ID:")
    print(session_id)
    print()
    print("Request Message ID:")
    print(request_message_id)
    print()
    print("Response Message ID:")
    print(response_message_id)
    return EXIT_SUCCESS


if __name__ == "__main__":
    raise SystemExit(main())



