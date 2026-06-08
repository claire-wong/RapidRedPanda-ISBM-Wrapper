"""
RapidRedPanda Wrapper Python Sample

Sample 08:
Close a provider publication session.

This sample demonstrates:
- Executing the wrapper CLI from Python
- Closing publication sessions
- Parsing wrapper JSON responses
- Handling operation failures

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
EXIT_CLOSE_PUBLICATION_FAILED = 3
EXIT_INVALID_USAGE = 4


def build_command(cli_command_prefix: list[str], config: dict[str, str], session_id: str) -> list[str]:
    return cli_command_prefix + [
        "close-publication-session",
        "--host",
        config["host"],
        "--session-id",
        session_id,
        "--user",
        config["user"],
        "--password",
        config["password"],
    ]


def print_usage() -> None:
    print("Usage:", file=sys.stderr)
    print("python samples/python/08_close_publication_session.py <session-id>", file=sys.stderr)


def print_failure_details(response: dict[str, object]) -> None:
    fault = response.get("fault") or response.get("transportFault")
    print(json.dumps(fault, indent=2) if fault is not None else "The CLI did not provide fault details.")


def run_cli(cli_command_prefix: list[str], config: dict[str, str], session_id: str) -> tuple[int, str, str]:
    try:
        result = subprocess.run(build_command(cli_command_prefix, config, session_id), capture_output=True, text=True, check=False)
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
    if len(sys.argv) != 2 or not sys.argv[1].strip():
        print_usage()
        return EXIT_INVALID_USAGE

    session_id = sys.argv[1].strip()

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

    return_code, stdout, stderr = run_cli(cli_command_prefix, config, session_id)
    if return_code != 0 and not stdout.strip():
        print(f"The CLI process exited unexpectedly with code {return_code}.", file=sys.stderr)
        if stderr:
            print(stderr.strip(), file=sys.stderr)
        return EXIT_CLI_EXECUTION_FAILURE

    response = parse_response(stdout, stderr)
    if response is None:
        return EXIT_INVALID_RESPONSE

    if response["success"] is False:
        print("Close publication session failed.")
        print_failure_details(response)
        return EXIT_CLOSE_PUBLICATION_FAILED

    print("Publication session closed successfully.")
    print()
    print("Session ID:")
    print(session_id)
    return EXIT_SUCCESS


if __name__ == "__main__":
    raise SystemExit(main())



