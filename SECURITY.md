# Security Policy

## Reporting a vulnerability

Please report suspected security issues privately to the repository owner or maintainer. Do not open a public issue for vulnerabilities, credentials, tokens, or private infrastructure details.

If GitHub private vulnerability reporting is enabled for this repository, use that channel. Otherwise, contact the owner through the contact method listed on the GitHub profile or repository.

## Secrets and local configuration

Do not commit real credentials, authorization headers, tokens, private hostnames, or customer payloads.

The repository includes example configuration templates:

- `samples/python/sample_config.example.json`
- `samples/csharp/RapidRedPanda.Wrapper.Console/appsettings.example.json`

Local config files such as `samples/python/sample_config.json` and `appsettings.Development.json` are intended for development only and are ignored by `.gitignore`.

Passing credentials on the command line can expose them through shell history or process inspection. Prefer local config files or platform secret-management tooling for shared or production environments.

## Raw output

The CLI `--raw` option can include the SDK raw response body. Review raw output before sharing logs, because ISBM server responses may contain environment-specific details or message payloads.

Wrapper JSON responses do not include credentials or authorization headers.
