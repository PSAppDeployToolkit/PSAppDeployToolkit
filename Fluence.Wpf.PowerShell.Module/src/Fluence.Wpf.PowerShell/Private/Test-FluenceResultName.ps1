function Test-FluenceResultName
{
    <#
    .SYNOPSIS
        Rejects ambiguous result keys before a dialog is dispatched.
    .DESCRIPTION
        Prompt names, button names and the module's result flags share one case-insensitive
        namespace. Duplicate or reserved keys would overwrite captured input or outcome flags.
    .PARAMETER Prompts
        The normalized prompt specifications.
    .PARAMETER Buttons
        The normalized button specifications.
    .NOTES
        Does not require a host application. Throws on an invalid specification.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$Prompts,

        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$Buttons
    )

    $names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($reserved in @('Cancelled', 'TimedOut', 'PSTypeName'))
    {
        $null = $names.Add($reserved)
    }
    foreach ($item in @($Prompts) + @($Buttons))
    {
        if ([string]::IsNullOrWhiteSpace([string]$item.Name))
        {
            throw 'Every prompt and button must have a non-empty result name.'
        }
        if (-not $names.Add([string]$item.Name))
        {
            throw "Result name '$($item.Name)' is duplicate or reserved. Prompt and button names must be unique and cannot be Cancelled, TimedOut or PSTypeName."
        }
    }
}
