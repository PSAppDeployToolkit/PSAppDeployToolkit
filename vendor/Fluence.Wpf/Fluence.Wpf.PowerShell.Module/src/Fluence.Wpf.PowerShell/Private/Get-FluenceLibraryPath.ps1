function Get-FluenceLibraryPath
{
    <#
    .SYNOPSIS
        Resolves the path to Fluence.Wpf.dll for the running PowerShell edition.
    .DESCRIPTION
        Maps the edition to the staged target framework folder under the module's lib directory:
        net8.0-windows10.0.26100.0 for Core (PowerShell 7, which rolls forward onto later runtimes)
        and net472 for Desktop (Windows PowerShell 5.1). Throws when that build was never staged,
        naming the path it looked for.
    .PARAMETER ModuleRoot
        The module's own folder, the parent of lib.
    .PARAMETER Edition
        The PowerShell edition to resolve for. Defaults to the running edition ($PSEdition).
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$ModuleRoot,

        [Parameter()]
        [string]$Edition = $PSEdition
    )

    if ($Edition -eq 'Core')
    {
        $tfm = 'net8.0-windows10.0.26100.0'
    }
    else
    {
        $tfm = 'net472'
    }

    $dll = [System.IO.Path]::Combine($ModuleRoot, 'lib', $tfm, 'Fluence.Wpf.dll')
    if (-not (Test-Path -LiteralPath $dll))
    {
        throw "Fluence.Wpf.dll not found for edition '$Edition' at: $dll"
    }

    return $dll
}
