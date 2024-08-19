# ADR-0008 - Ensure wrappers only work in legacy mode.

Date: 08/08/2024

## Status

Decided.

## Context

The wrappers from the previous PC need to detect whether the session is running in legacy mode or not and if so, throw and abort so that they cannot be used for new development.

This change is feasible as the proposed module setup will flag whether the currently executed session is in legacy mode or not (that is, the immediate caller is AppDeployToolkitMain.ps1).

## Decision

Change approved.

## Consequences

- Ensures bad setups and interfaces don't remain in prod usage.
- Moves in the right direction.
