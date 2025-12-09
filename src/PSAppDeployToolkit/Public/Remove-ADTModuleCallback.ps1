#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTModuleCallback
#
#-----------------------------------------------------------------------------

function Remove-ADTModuleCallback
{
    <#
    .SYNOPSIS
        Removes a callback function from the nominated hooking point.

    .DESCRIPTION
        This function removes a specified callback function from the nominated hooking point.

    .PARAMETER Hookpoint
        Where you wish for the callback to be removed from.

    .PARAMETER Callback
        The callback function to remove from the nominated hooking point.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Remove-ADTModuleCallback -Hookpoint PostOpen -Callback (Get-Command -Name 'MyCallbackFunction')

        Removes the specified callback function from being invoked after a DeploymentSession has opened.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTModuleCallback
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Hookpoint', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Core.CallbackType]$Hookpoint,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.CommandInfo[]]$Callback
    )

    # Remove all specified callbacks.
    try
    {
        $null = $Callback | & {
            begin
            {
                $callbacks = $Script:ADT.Callbacks.$Hookpoint
            }
            process
            {
                $callbacks.Remove($_)
            }
        }
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
