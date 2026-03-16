#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTFile
#
#-----------------------------------------------------------------------------

function Remove-ADTFile
{
    <#
    .SYNOPSIS
        Removes one or more items from a given path on the filesystem.

    .DESCRIPTION
        This function removes one or more items from a given path on the filesystem. It can handle both wildcard paths and literal paths. If the specified path does not exist, it logs a warning instead of throwing an error. The function can also delete items recursively if the Recurse parameter is specified.

        This function delegates deletion to Remove-ADTItem.

    .PARAMETER Path
        Specifies the file on the filesystem to be removed. The value of Path will accept wildcards. Will accept an array of values.

    .PARAMETER LiteralPath
        Specifies the file on the filesystem to be removed. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

    .PARAMETER Recurse
        Deletes the files in the specified location(s) and in all child items of the location(s).

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Remove-ADTFile -LiteralPath 'C:\Windows\Downloaded Program Files\Temp.inf'

        Removes the specified file.

    .EXAMPLE
        Remove-ADTFile -LiteralPath 'C:\Windows\Downloaded Program Files' -Recurse

        Removes the specified folder and all its contents recursively.

    .NOTES
        An active ADT session is NOT required to use this function.

        Remove-ADTItem is the unified filesystem removal function. Prefer Remove-ADTItem for new script development.

        This function continues on received errors by default. To have the function stop on an error, please provide `-ErrorAction Stop` on the end of your call.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTFile
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LiteralPath', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [SupportsWildcards()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [Alias('PSPath')]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated and will be removed in PSAppDeployToolkit 4.3.0. Please use [Remove-ADTItem] instead." -Severity Warning
    }

    process
    {
        $riParams = @{ $PSCmdlet.ParameterSetName = $PSBoundParameters[$PSCmdlet.ParameterSetName]; Recurse = $Recurse }
        try
        {
            try
            {
                Remove-ADTItem @riParams
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -Silent
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
