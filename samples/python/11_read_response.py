"""
RapidRedPanda Wrapper Python Sample

Sample 11:
Read a response from an existing consumer request session.

This sample demonstrates:
- Executing the wrapper CLI from Python
- Reading a response for a posted request
- Parsing wrapper JSON responses
- Displaying response content

No third-party dependencies are required.
"""

import json
import subprocess
import sys
from cli_locator import CliNotFoundError, get_cli_command_prefix, print_cli_not_found
from config_loader import ConfigError, load_config



EXIT_SUCCESS = 0
EXIT_CLI_EXECUTION_FAILURE = 1
EXIT_INVALID_RESPONSE = 2
EXIT_READ_RESPONSE_FAILED = 3
EXIT_INVALID_USAGE = 4


def build_command(cli_command_prefix: list[str], config: dict[str, str], session_id: str, request_message_id: str) -> list[str]:
    """Build the dotnet CLI command-line arguments for read-response."""
    return cli_command_prefix + [
        "read-response",
        "--host",
        config["host"],
        "--session-id",
        session_id,
        "--request-message-id",
        request_message_id,
        "--user",
        config["user"],
        "--password",
        config["password"],
    ]


def print_usage() -> None:
    """Print command-line usage for this sample."""
    print("Usage:", file=sys.stderr)
    print("python samples/python/11_read_response.py <session-id> <request-message-id>", file=sys.stderr)


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


def print_content(content: object) -> None:
    """Pretty-print JSON content when possible; otherwise print it as-is."""
    if not isinstance(content, str):
        print(content)
        return

    try:
        parsed_content = json.loads(content)
    except json.JSONDecodeError:
        print(content)
        return

    print(json.dumps(parsed_content, indent=2))


def display_response(data: dict[str, object]) -> int:
    """Display response fields from a successful CLI response."""
    message_id = data.get("messageId")
    message_content = data.get("messageContent")

    if not isinstance(message_id, str) or not message_id:
        print("The successful CLI response is missing 'data.messageId'.", file=sys.stderr)
        return EXIT_INVALID_RESPONSE

    media_type = None
    content = message_content
    if isinstance(message_content, dict):
        media_type = message_content.get("mediaType")
        content = message_content.get("content")

    if media_type is not None and not isinstance(media_type, str):
        print("The successful CLI response contains invalid 'data.messageContent.mediaType'.", file=sys.stderr)
        return EXIT_INVALID_RESPONSE

    if content is None:
        print("The successful CLI response is missing response content.", file=sys.stderr)
        return EXIT_INVALID_RESPONSE

    print("Response received.")
    print()
    print("Message ID:")
    print(message_id)
    print()
    print("Media Type:")
    print(media_type or "")
    print()
    print("Content:")
    print_content(content)
    return EXIT_SUCCESS


def main() -> int:
    """Read and display a response from an existing consumer request session."""
    if len(sys.argv) != 3 or not sys.argv[1].strip() or not sys.argv[2].strip():
        print_usage()
        return EXIT_INVALID_USAGE

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
        print("Read response failed.")
        print_failure_details(response)
        return EXIT_READ_RESPONSE_FAILED

    data = response.get("data")
    if data is None:
        print("No response available.")
        return EXIT_SUCCESS

    if not isinstance(data, dict):
        print("The successful CLI response contains invalid 'data'.", file=sys.stderr)
        return EXIT_INVALID_RESPONSE

    return display_response(data)


if __name__ == "__main__":
    raise SystemExit(main())



