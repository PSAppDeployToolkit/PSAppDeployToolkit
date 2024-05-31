# ADR-0006 - Consider whether we expose ADTSession object or wrap access

Date: 05/30/2024

## Status

Decided.

## Context

The ADTSession object has the ability to be directly exposed to the caller, or can be hidden away as an internal object, with functions serving as getters/setters.

More layers of abstraction allow for easier refactoring and also allow us to provide more help to end users via comment-based help. Less layers provide a deeper understanding of PSADT's internals to the end user.

## Decision

Keep object hidden and make accessible only. through getters and setters.

## Consequences

- More abstraction for future refactoring.
- Comment-based help in getter/setters.
