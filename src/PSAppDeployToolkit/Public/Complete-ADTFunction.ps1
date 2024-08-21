#-----------------------------------------------------------------------------
#
# MARK: Complete-ADTFunction
#
#-----------------------------------------------------------------------------

function Complete-ADTFunction
{
    <#
    .SYNOPSIS
        Completes the execution of an ADT function.

    .DESCRIPTION
        The Complete-ADTFunction function finalizes the execution of an ADT function by writing a debug log message and restoring the original global verbosity if it was archived off.

    .PARAMETER Cmdlet
        The PSCmdlet object representing the cmdlet being completed.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Complete-ADTFunction -Cmdlet $PSCmdlet

        This example completes the execution of the current ADT function.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Write debug log messages.
    Write-ADTLogEntry -Message 'Function End' -Source $Cmdlet.MyInvocation.MyCommand.Name -DebugMessage

    # Restore original global verbosity if a value was archived off.
    if ($null -ne ($OriginalVerbosity = $Cmdlet.SessionState.PSVariable.GetValue('OriginalVerbosity', $null)))
    {
        $Global:VerbosePreference = $OriginalVerbosity
    }
}
