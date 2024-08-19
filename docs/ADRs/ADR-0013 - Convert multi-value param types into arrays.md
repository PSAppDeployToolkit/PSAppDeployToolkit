# ADR-0013 -  Convert multi-value param types into arrays

Date: 08/08/2024

## Status

Decided.

## Context

A number of PSAppDeployToolkit functions accept multiple values as a comma-delimited string, which is then split internally.

This is not standard practice and is difficult to understand. The example parameter should be a [string[]] object and allow the caller to naturally provide multiple values.

The change is feasible and should be executed along all the other PCs in order to make our functions modern, correct, and best practice.

Incorrect:
```pwsh
    [Parameter(Mandatory = $false)]
    [ValidateNotNullorEmpty()]
    [String]$CloseApps,
```

Correct:
```pwsh
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string[]]$CloseApps,
```

## Decision

Change Approved.

## Consequences

- Aligns function definitions to be within best practice guidelines
- Makes functions more discoverable and more natural to use
- Faster processing by not having the caller join arrays only to split them again.
