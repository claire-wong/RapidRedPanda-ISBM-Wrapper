"""Shared configuration loader for RapidRedPanda Python samples."""

import json
from pathlib import Path


CONFIG_PATH = Path(__file__).resolve().parent / "sample_config.json"

REQUIRED_FIELDS = [
    "host",
    "publicationChannel",
    "requestChannel",
    "publicationTopic",
    "requestTopic",
    "user",
    "password",
]


class ConfigError(Exception):
    """Raised when the Python sample configuration is missing or invalid."""


def load_config() -> dict[str, str]:
    """Load and validate sample_config.json."""
    if not CONFIG_PATH.exists():
        raise ConfigError(
            "Configuration file not found:\n"
            "samples/python/sample_config.json\n\n"
            "Copy:\n"
            "samples/python/sample_config.example.json\n\n"
            "to:\n"
            "samples/python/sample_config.json\n\n"
            "and update the values."
        )

    try:
        raw_config = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        raise ConfigError(f"Configuration file contains invalid JSON: {error}") from error

    if not isinstance(raw_config, dict):
        raise ConfigError("Configuration file must contain a JSON object.")

    config: dict[str, str] = {}
    missing_fields: list[str] = []
    invalid_fields: list[str] = []

    for field in REQUIRED_FIELDS:
        value = raw_config.get(field)
        if value is None:
            missing_fields.append(field)
        elif not isinstance(value, str) or not value.strip():
            invalid_fields.append(field)
        else:
            config[field] = value.strip()

    if missing_fields:
        missing = ", ".join(missing_fields)
        raise ConfigError(f"Configuration file is missing required field(s): {missing}.")

    if invalid_fields:
        invalid = ", ".join(invalid_fields)
        raise ConfigError(f"Configuration field(s) must be non-empty strings: {invalid}.")

    return config
