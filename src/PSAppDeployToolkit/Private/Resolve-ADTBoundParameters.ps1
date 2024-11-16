#-----------------------------------------------------------------------------
#
# MARK: Resolve-ADTBoundParameters
#
#-----------------------------------------------------------------------------

function Resolve-ADTBoundParameters
{
    <#

    .SYNOPSIS
    Resolve the parameters of a function call to a string.

    .DESCRIPTION
    Resolve the parameters of a function call to a string.

    .PARAMETER InputObject
    The $PSBoundParameters object to process.

    .PARAMETER Exclude
    The names of parameters to exclude from the final result.

    .INPUTS
    System.Collections.Generic.Dictionary[System.String, System.Object]. Resolve-ADTBoundParameters accepts a $PSBoundParameters value via the pipeline for processing.

    .OUTPUTS
    System.String. Resolve-ADTBoundParameters returns a string output of all provided parameters that can be used against powershell.exe or pwsh.exe.

    .EXAMPLE
    $PSBoundParameters | Resolve-ADTBoundParameters

    .EXAMPLE
    Resolve-ADTBoundParameters -InputObject $PSBoundParameters

    .NOTES
    This is an internal script function and should typically not be called directly.

    .NOTES
    An active ADT session is NOT required to use this function.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Exclude', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$InputObject,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Exclude
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Send this out to the backend for processing.
                return [PSADT.Shared.Utility]::ConvertDictToPowerShellArgs($InputObject, $Exclude);
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
