# ADR-0007 - Wrap all function accesses in prep for refactoring

Date: 05/30/2024

## Status

Decided.

## Context

In order to ensure backwards compatibility, we need to leverage the existing function names as wrappers against renamed functions in our module. Theres a lot of interfaces that require sanitising and this will ensure we can keep things backwards compatible for 3.x scripts while providing the new way forward for those prepared for some elbow-grease.

## Decision

Agreed to wrap functions.

## Consequences

- Allows legacy interface to remain while we clean up the internals.
- Allows us to right the wrongs from the past.
