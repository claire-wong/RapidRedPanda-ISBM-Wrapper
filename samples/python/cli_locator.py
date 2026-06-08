"""Locate the RapidRedPanda Wrapper CLI for Python samples."""

import sys
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
DEPLOYED_CLI = REPOSITORY_ROOT / "RapidRedPanda.Wrapper.Cli"
DEPLOYED_CLI_EXE = REPOSITORY_ROOT / "RapidRedPanda.Wrapper.Cli.exe"
SOURCE_CLI_DLL = (
    REPOSITORY_ROOT
    / "src"
    / "RapidRedPanda.Wrapper.Cli"
    / "bin"
    / "Debug"
    / "net8.0"
    / "RapidRedPanda.Wrapper.Cli.dll"
)

CHECKED_PATHS = [
    DEPLOYED_CLI,
    DEPLOYED_CLI_EXE,
    SOURCE_CLI_DLL,
]


class CliNotFoundError(Exception):
    """Raised when no deployed CLI executable or source-build DLL is found."""

    def __init__(self, checked_paths: list[Path]) -> None:
        self.checked_paths = checked_paths
        super().__init__("RapidRedPanda Wrapper CLI was not found.")


def get_cli_command_prefix() -> list[str]:
    """Return the subprocess command prefix for the deployed CLI or Debug DLL."""
    if DEPLOYED_CLI.exists():
        return [str(DEPLOYED_CLI)]

    if DEPLOYED_CLI_EXE.exists():
        return [str(DEPLOYED_CLI_EXE)]

    if SOURCE_CLI_DLL.exists():
        return ["dotnet", str(SOURCE_CLI_DLL)]

    raise CliNotFoundError(CHECKED_PATHS)


def print_cli_not_found(error: CliNotFoundError) -> None:
    """Print user-friendly CLI discovery failure details."""
    print("RapidRedPanda Wrapper CLI not found.", file=sys.stderr)
    print(file=sys.stderr)
    print("Checked locations:", file=sys.stderr)
    for path in error.checked_paths:
        print(path, file=sys.stderr)
    print(file=sys.stderr)
    print("Use a self-contained release package, or build from source first:", file=sys.stderr)
    print(file=sys.stderr)
    print("dotnet build", file=sys.stderr)
