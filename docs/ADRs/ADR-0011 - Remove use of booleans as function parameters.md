# ADR-0011 - Remove use of booleans as function parameters

Date: 08/08/2024

## Status

Decided.

## Context

PSAppDeployToolkit makes significant use of booleans as parameters. I'm not sure for the reason behind this as I believe that SwitchParameter has been available since the dawn of time.

Further to this, a number of booleans have strange default values whereas a replacement switch should be used to negate or change default behavior.

The change is feasible and should be executed along all the other PCs in order to make our functions modern, correct, and best practice.

Incorrect:
```pwsh
    [Parameter(Mandatory = $false)]
    [ValidateNotNullorEmpty()]
    [Boolean]$TopMost = $true
```

Correct:
```pwsh
    [Parameter(Mandatory = $false)]
    [switch]$NotTopMost
```

## Decision

Change Approved.

## Consequences

- Changes function signatures to be correct and best practice
- Makes functions work as one would expect from PowerShell
- Switches work properly from the command line (-File), etc.
