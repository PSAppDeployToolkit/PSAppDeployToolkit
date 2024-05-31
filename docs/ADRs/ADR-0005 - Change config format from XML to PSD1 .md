# ADR-0005 - Change config format from XML to PSD1

Date: 05/30/2024

## Status

Decided.

## Context

XML was understandably used back in the day because .NET can natively parse it, but its a terrible format with no proper typing which adds an administrative overheard upon us as we have to write our own parser to cover the deficiencies of the format.

There's pros/cons to all format, but some considerations could be:

- JSON
  - Natively supported in PowerShell.
  - Supports typing as expected.
  - Does not support comments (is this even that important?)

- PSD1
  - Natively supported in PowerShell.
  - Uses natural PowerShell syntax (effectively a hashtable).
  - Supports typing.
  - Supports comments.

- YAML
  - Not natively supported in PowerShell.
  - Supports typing.
  - Supports comments.

- TOML
  - Not natively supported in PowerShell.
  - Supports typing.
  - Supports comments.

## Decision

Migrate configuration settings to PSD1 format.

## Consequences

- Better config formats support typing.
- Better config formats reduce our technical debt.
- Better config formats are faster to process.
