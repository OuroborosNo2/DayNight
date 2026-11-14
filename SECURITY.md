# Security Policy

## Supported versions

Only the latest release on [GitHub Releases](https://github.com/OuroborosNo2/DayNight/releases) is supported with security fixes.

## Reporting a vulnerability

Please **do not** open a public issue for security problems.

Use [GitHub Private Vulnerability Reporting](https://github.com/OuroborosNo2/DayNight/security/advisories/new) if available, or open a minimal private channel via the repository security tab.

Include:

- DayNight version / artifact name
- Windows version (e.g. Win11 build)
- Steps to reproduce
- Impact assessment

We aim to acknowledge reports within a reasonable time. Please allow time for a fix before public disclosure.

## Scope notes

DayNight is a local tray utility. It writes Windows theme-related settings under the current user hive and optionally display-related keys. It is not a network service and does not collect telemetry.
