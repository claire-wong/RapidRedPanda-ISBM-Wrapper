"""
RapidRedPanda Wrapper Python Sample

Sample 01:
Open a subscription session using the RapidRedPanda Wrapper CLI.

This sample demonstrates:
- Executing the CLI from Python
- Parsing the CLI JSON response
- Retrieving the ISBM session ID

No third-party dependencies are required.
"""

import json
import subprocess
import sys
from cli_locator import CliNotFoundError, get_cli_command_prefix, print_cli_not_found
from config_loader import ConfigError, build_filter_arguments, load_config



EXIT_SUCCESS = 0
EXIT_WRAPPER_EXECUTION_FAILURE = 1
EXIT_INVALID_RESPONSE = 2
EXIT_SUBSCRIPTION_FAILED = 3


def build_command(cli_command_prefix: list[str], config: dict[str, object]) -> list[str]:
    """Build the dotnet CLI command-line arguments for open-subscription."""
    command = cli_command_prefix + [
        "open-subscription",
        "--host",
        config["host"],
        "--channel",
        config["publicationChannel"],
        "--topic",
        config["publicationTopic"],
        "--user",
        config["user"],
        "--password",
        config["password"],
    ]
    command.extend(build_filter_arguments(config, "subscriptionFilterExpression"))
    return command


def print_failure_details(response: dict[str, object]) -> None:
    """Print the fault information returned by the CLI."""
    fault = response.get("fault") or response.get("transportFault")
    if fault is None:
        print("The CLI did not provide fault details.")
        return

    print(json.dumps(fault, indent=2))


def main() -> int:
    """Open a subscription session and display its session ID."""
    print("RapidRedPanda Wrapper Demo")
    print("--------------------------")
    print()

    # Verify the framework-dependent CLI assembly has been built.
    try:
        config = load_config()
    except ConfigError as error:
        print(error, file=sys.stderr)
        return EXIT_WRAPPER_EXECUTION_FAILURE

    try:
        cli_command_prefix = get_cli_command_prefix()
    except CliNotFoundError as error:
        print_cli_not_found(error)
        return EXIT_WRAPPER_EXECUTION_FAILURE

    # Execute the CLI and capture its JSON output.
    try:
        result = subprocess.run(
            build_command(cli_command_prefix, config),
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError:
        print("The .NET runtime or SDK could not be found.", file=sys.stderr)
        print(file=sys.stderr)
        print("Install .NET 8 when using the source-build DLL fallback, or use a self-contained release package.", file=sys.stderr)
        return EXIT_WRAPPER_EXECUTION_FAILURE
    except OSError as error:
        print(f"Unable to execute the CLI: {error}", file=sys.stderr)
        return EXIT_WRAPPER_EXECUTION_FAILURE

    if result.returncode != 0 and not result.stdout.strip():
        print(
            f"The CLI process exited unexpectedly with code {result.returncode}.",
            file=sys.stderr,
        )
        if result.stderr:
            print(result.stderr.strip(), file=sys.stderr)
        return EXIT_WRAPPER_EXECUTION_FAILURE

    # Parse the CLI's standard output as JSON.
    try:
        response = json.loads(result.stdout)
    except json.JSONDecodeError as error:
        print(f"The CLI returned invalid JSON: {error}", file=sys.stderr)
        if result.stderr:
            print(result.stderr.strip(), file=sys.stderr)
        return EXIT_INVALID_RESPONSE

    if not isinstance(response, dict) or not isinstance(response.get("success"), bool):
        print("The CLI response is missing a valid 'success' value.", file=sys.stderr)
        return EXIT_INVALID_RESPONSE

    # A valid CLI operation failure is distinct from a process execution failure.
    if response["success"] is False:
        print("Open subscription failed.")
        print_failure_details(response)
        return EXIT_SUBSCRIPTION_FAILED

    if result.returncode != 0:
        print(
            f"The CLI process exited unexpectedly with code {result.returncode}.",
            file=sys.stderr,
        )
        return EXIT_WRAPPER_EXECUTION_FAILURE

    # Retrieve and display the session ID from a successful response.
    data = response.get("data")
    session_id = data.get("sessionId") if isinstance(data, dict) else None
    if not isinstance(session_id, str) or not session_id:
        print("The successful CLI response is missing 'data.sessionId'.", file=sys.stderr)
        return EXIT_INVALID_RESPONSE

    print("Subscription session opened successfully.")
    print()
    print("Session ID:")
    print(session_id)
    return EXIT_SUCCESS


if __name__ == "__main__":
    raise SystemExit(main())



