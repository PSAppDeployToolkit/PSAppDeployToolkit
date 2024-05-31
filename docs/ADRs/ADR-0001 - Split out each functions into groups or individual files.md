# ADR-0001 - Split out each functions into groups or individual files

Date: 05/30/2024

## Status

Decided.

## Context

As part of converting PSAppDeployToolkit into a module, AppDeployToolkitMain.ps1 needs to be split up to remove its function definitions from the procedural code.

This code should be split up two ways:

- Private: All functions that state "This is an internal script function and should typically not be called directly." within its comment-based help.
- Public: All other functions.

During the transitional period, all relocated functions can still be dot-sourced into AppDeployToolkitMain.ps1.

A decision should be made whether to group like-functions together, or have a singular function per file.

## Decision

Decision is to split into individual functions per file.

## Consequences

Will make functions easier to find than searching through a monolithic script.
