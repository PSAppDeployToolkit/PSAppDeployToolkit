# ADR-0002 - Move environment variable code into function

Date: 05/30/2024

## Status

Decided.

## Context

As part of turning PSAppDeployToolkit into a module, all procedural code requires refactoring into functional code for repeated usage.

The environment variables that PSAppDeployToolkit generates should be moved into a function that generates the variable data and returns them as a dictionary to the caller for further processing.

The caller then can take this received dictionary and output the key/value pairs as variables within whatever session is required, such as while expanding variables in the XML file, or exporting the variables into the caller's script scope for usage on their side.

## Decision

Environment variables that PSAppDeployToolkit generates will be moved into  function.

## Consequences

- Makes a significant portion of the code reusable.
- Removes exposure to temporary variables needed to generate the exported data. Currently these live on in Main.ps1 and contribute to variable soup.
- Allows generated variable data to be exported into whatever scope its required in, particularly useful for as we step closer to becoming a module.
