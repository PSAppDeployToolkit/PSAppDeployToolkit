# ADR-0004 - Enable strict mode in module and new deploy scripts

Date: 05/30/2024

## Status

Decided.

## Context

Currently, PSAppDeployToolkit operates with strict mode turned off. This allows a very loose utilisation of PowerShell whereby non-existent variables are treated as null, among other conditions.

As part of fortifying our product to ensure no undefined behaviour can occur, strict mode should be enabled within our module. This will also be particularly useful as we transition towards a module to ensure we don't miss any variable renaming.

### Strict Mode Versions

- 1.0
  - Prohibits references to uninitialized variables, except for uninitialized variables in strings.

- 2.0
  - Prohibits references to uninitialized variables. This includes uninitialized variables in strings.
  - Prohibits references to non-existent properties of an object.
  - Prohibits function calls that use the syntax for calling methods.

- 3.0
  - Prohibits references to uninitialized variables. This includes uninitialized variables in strings.
  - Prohibits references to non-existent properties of an object.
  - Prohibits function calls that use the syntax for calling methods.
  - Prohibit out of bounds or unresolvable array indexes.

- Latest
  - Selects the latest version available. The latest version is the most strict. Use this value to make sure that scripts use the strictest available version, even when new versions are added to PowerShell.

## Decision

Implement Strict Mode as follows:

- Module - ```Set-StrictMode -Version 3```
- AppDeployToolkitMain - Remove prior to release
- Deploy v4 template - ```Set-StrictMode -Version 1```
- Deploy v3 template - Do not add or set

## Consequences

- Elimination of undefined behaviour.
- Fortification of product to make it more unbreakable.
