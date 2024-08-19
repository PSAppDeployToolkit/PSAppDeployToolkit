# ADR-0012 - Ensure functions use right parameter types

Date: 08/08/2024

## Status

Decided.

## Context

Some functions like Show-InstallationWelcome have parameters that are string types, but would be better off with a System.DateTime parameter instead.

```pwsh
    .PARAMETER DeferDeadline
    Specify the deadline date until which the installation can be deferred.

    Specify the date in the local culture if the script is intended for that same culture.

    If the script is intended to run on EN-US machines, specify the date in the format: "08/25/2013" or "08-25-2013" or "08-25-2013 18:00:00"

    If the script is intended for multiple cultures, specify the date in the universal sortable date/time format: "2013-08-22 11:51:52Z"

    The deadline date will be displayed to the user in the format of their culture.

        [Parameter(Mandatory = $false)]
        [String]$DeferDeadline = '',
```

With this being a System.DateTime value instead, the casting will provide natural parameter validation and simplify a lot of code.

The change is feasible and should be executed along all the other PCs in order to make our functions modern, correct, and best practice.


## Decision

Change Approved.

## Consequences

- Cleaner, more concise code
- Natural parameter validation
- Improves how discoverable parameters are for the end user..
