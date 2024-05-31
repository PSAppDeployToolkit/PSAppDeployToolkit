# ADR-0003 - Convert to module

Date: 05/30/2024

## Status

Decided.

## Context

As part of making PSAppDeployToolkit work with more complete abstraction from the caller's scope, it's time for it to become a module.

Making PSAppDeployTookit become a module is not without its challenges. Years of development have gone into a solution with its internals almost directly exposed to callers and handling backwards compatibility requires a lot of consideration.

With a module, we can have truly reliable repeat PSAppDeployToolkit calls as we'll containerise all variables into a session class object. With that in mind, complex deployments can become a reality where multiple PSAppDeployToolkit sessions can be in use so callers don't have to write monolithic deployment scripts.

## Decision

Convert to module.

## Consequences

- Complete separation of PSAppDeployToolkit code from the callers.
- Reliable repeat calls to the toolkit.
- Provides our clients with what they're after.
