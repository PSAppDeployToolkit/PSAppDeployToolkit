# ADR-0009 - All function must use proper verbs

Date: 08/08/2024

## Status

Decided.

## Context

The wrappers from the previous PC need to detect whether the session is running in legacy mode or not and if so, throw and abort so that they cannot be used for new development.

This change is feasible as the proposed module setup will flag whether the currently executed session is in legacy mode or not (that is, the immediate caller is AppDeployToolkitMain.ps1).

## Decision

Change Approved.

## Consequences

- Eliminates warnings out of the module
- Best practice and makes functions more discoverable
- Clearer intention as to what the function does.
