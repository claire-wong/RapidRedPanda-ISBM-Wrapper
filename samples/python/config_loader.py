"""Shared configuration loader for RapidRedPanda Python samples."""

import json
from pathlib import Path
from typing import Any


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

OPTIONAL_FILTER_SECTIONS = [
    "subscriptionFilterExpression",
    "providerRequestFilterExpression",
]


class ConfigError(Exception):
    """Raised when the Python sample configuration is missing or invalid."""


def load_config() -> dict[str, Any]:
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

    config: dict[str, Any] = {}
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

    for section_name in OPTIONAL_FILTER_SECTIONS:
        if section_name in raw_config:
            config[section_name] = raw_config[section_name]

    return config


def build_filter_arguments(config: dict[str, Any], section_name: str) -> list[str]:
    """Build optional CLI filter arguments from a named filterExpression section."""
    filter_config = config.get(section_name)
    if filter_config is None:
        return []

    if not isinstance(filter_config, dict):
        raise ConfigError(f"Configuration field '{section_name}' must contain a JSON object.")

    expression = _get_optional_string(filter_config, "expression", section_name)
    language = _get_optional_string(filter_config, "language", section_name)
    language_version = _get_optional_string(filter_config, "languageVersion", section_name)
    media_type = _get_optional_string(filter_config, "mediaType", section_name)

    if expression is None:
        raise ConfigError(f"Configuration field '{section_name}.expression' is required when '{section_name}' is provided.")

    if language is None:
        raise ConfigError(f"Configuration field '{section_name}.language' is required when '{section_name}' is provided.")

    arguments = [
        "--filter-expression",
        expression,
        "--filter-language",
        language,
    ]

    if language_version is not None:
        arguments.extend(["--filter-language-version", language_version])

    if media_type is not None:
        arguments.extend(["--filter-media-type", media_type])

    return arguments


def _get_optional_string(config: dict[str, Any], field_name: str, section_name: str) -> str | None:
    value = config.get(field_name)
    if value is None:
        return None

    if not isinstance(value, str) or not value.strip():
        raise ConfigError(f"Configuration field '{section_name}.{field_name}' must be a non-empty string.")

    return value.strip()
