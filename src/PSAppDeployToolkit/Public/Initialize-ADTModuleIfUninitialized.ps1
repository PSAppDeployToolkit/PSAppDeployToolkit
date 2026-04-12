#-----------------------------------------------------------------------------
#
# MARK: Initialize-ADTModuleIfUninitialized
#
#-----------------------------------------------------------------------------

function Initialize-ADTModuleIfUninitialized
{
    <#
    .SYNOPSIS
        Convenience function to initialize the module if required, optionally returning the active session if available.

    .DESCRIPTION
        The `Initialize-ADTModuleIfUninitialized` function initializes the PSAppDeployToolkit module if it is not already initialized, using `Initialize-ADTModule` and optionally returns the active session, if one is available. This is available as a shorthand function for extension module developers and will likely serve no benefit for regular deployment scripts.

    .PARAMETER Cmdlet
        The cmdlet that is being initialized.

    .PARAMETER ScriptDirectory
        An override directory to use for config and string loading.

    .PARAMETER AdditionalEnvironmentVariables
        A dictionary of key/value pairs to inject into the generated environment table.

    .PARAMETER PassThruActiveSession
        Returns the active DeploymentSession if available.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        By default, this function returns no output.

    .OUTPUTS
        PSAppDeployToolkit.Foundation.DeploymentSession

        If an active DeploymentSession is available and the `-PassThruActiveSession` parameter is provided, this function returns the active DeploymentSession.

    .EXAMPLE
        Initialize-ADTModuleIfUninitialized -Cmdlet $PSCmdlet

        Initializes the ADT module with the default settings and configurations if it is uninitialized.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Initialize-ADTModuleIfUninitialized
    #>

    [CmdletBinding()]
    [OutputType([PSAppDeployToolkit.Foundation.DeploymentSession])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ScriptDirectory -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (!(Test-Path -LiteralPath $_ -PathType Container))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ScriptDirectory -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [System.String[]]$ScriptDirectory,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$AdditionalEnvironmentVariables,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThruActiveSession
    )

    # Initialize the module if there's no session and it hasn't been previously initialized.
    if (!($adtSession = if (Test-ADTSessionActive) { Get-ADTSession }) -and !(Test-ADTModuleInitialized))
    {
        $null = $PSBoundParameters.Remove('PassThruActiveSession')
        $null = $PSBoundParameters.Remove('Cmdlet')
        try
        {
            Initialize-ADTModule @PSBoundParameters
        }
        catch
        {
            $Cmdlet.ThrowTerminatingError($_)
        }
    }

    # Return the current session if we happened to get one.
    if ($adtSession -and $PassThruActiveSession)
    {
        return $adtSession
    }
}
