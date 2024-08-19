# ADR-0010 - Eliminate setups like "-ContinueOnError"

Date: 08/08/2024

## Status

Decided.

## Context

PSAppDeployToolkit has a significant amount of homegrown error handling. This error handling is unnatural and should be removed so that PowerShell's native error handling is used, such as -ErrorAction and try/catch blocks.


The change is feasible and should be executed along all the other PCs in order to make our functions modern, correct, and best practice.

## Decision

Change Approved.

## Consequences

- More natural way to code
- Ensure people who know PowerShell can start using the solution
- Ensure people learning PowerShell learn PowerShell, not PSADT.
