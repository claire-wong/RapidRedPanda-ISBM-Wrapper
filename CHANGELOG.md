# Changelog

All notable changes to this project should be documented in this file.

This project does not currently have a published release history in this repository. Start new entries at the top of this file when preparing future releases.

## Unreleased

## v0.3.5 - 2026-09-19

### Added

- Added optional MediaType forwarding for Post Publication, Post Request, and Post Response operations.
- Added async wrapper support for the three post operations.
- Added ClientAdapter 2.2.0 filter expression compatibility, including namespace pass-through support.

### Changed

- Upgraded `RapidRedPanda.ISBM.ClientAdapter` to 2.2.0.
- Forwarded CLI `--media-type` values to the corresponding post operations.
- Preserved existing synchronous APIs and native JSON behavior when MediaType is omitted.
- Aligned Python native-JSON samples and documentation with omitted-MediaType behavior.

### Documentation

- Clarified project positioning, CLI usage, JSON response format, configuration safety, and troubleshooting in `README.md`.
- Added contribution and security guidance for public repository readiness.

### Changed

- Relaxed unused CLI argument requirements so `open-publication-session`, `open-request-session`, and post commands no longer require arguments that are not consumed by the current wrappers.

## Release note template

```markdown
## vX.Y.Z - YYYY-MM-DD

### Added

- ...

### Changed

- ...

### Fixed

- ...

### Security

- ...
```
