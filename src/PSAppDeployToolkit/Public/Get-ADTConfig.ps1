﻿#-----------------------------------------------------------------------------
#
# MARK: Get-ADTConfig
#
#-----------------------------------------------------------------------------

function Get-ADTConfig
{
    <#
    .SYNOPSIS
        Retrieves the configuration data for the ADT module.

    .DESCRIPTION
        The Get-ADTConfig function retrieves the configuration data for the ADT module. This function ensures that the ADT module has been initialized before attempting to retrieve the configuration data. If the module is not initialized, it throws an error.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Collections.Hashtable

        Returns the configuration data as a hashtable.

    .EXAMPLE
        $config = Get-ADTConfig

        This example retrieves the configuration data for the ADT module and stores it in the $config variable.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTConfig
    #>

    [CmdletBinding()]
    param
    (
    )

    # Return the config database if initialized.
    if (!$Script:ADT.Config -or !$Script:ADT.Config.Count)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("Please ensure that [Initialize-ADTModule] is called before using any $($MyInvocation.MyCommand.Module.Name) functions.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTConfigNotLoaded'
            TargetObject = $Script:ADT.Config
            RecommendedAction = "Please ensure the module is initialized via [Initialize-ADTModule] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $Script:ADT.Config
}
