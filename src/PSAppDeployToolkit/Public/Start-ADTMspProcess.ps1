#-----------------------------------------------------------------------------
#
# MARK: Start-ADTMspProcess
#
#-----------------------------------------------------------------------------

function Start-ADTMspProcess
{
    <#
    .SYNOPSIS
        Executes an MSP file using the same logic as `Start-ADTMsiProcess`.

    .DESCRIPTION
        Reads SummaryInfo targeted product codes in MSP file and determines if the MSP file applies to any installed products. If a valid installed product is found, triggers the `Start-ADTMsiProcess` function to patch the installation.

        Uses default config MSI parameters. You can use the `-AdditionalArgumentList` parameter to add additional parameters.

    .PARAMETER FilePath
        Path to the MSP file.

    .PARAMETER AdditionalArgumentList
        Additional parameters.

    .PARAMETER SecureArgumentList
        Hides all parameters passed to the MSI or MSP file from the toolkit log file.

    .PARAMETER RunAsActiveUser
        A RunAsActiveUser object to invoke the process as.

    .PARAMETER UseLinkedAdminToken
        Use a user's linked administrative token while running the process under their context.

    .PARAMETER UseHighestAvailableToken
        Use a user's linked administrative token if it's available while running the process under their context.

    .PARAMETER InheritEnvironmentVariables
        Specifies whether the process running as a user should inherit the SYSTEM account's environment variables.

    .PARAMETER DenyUserTermination
        Specifies that users cannot terminate the process started in their context. The user will still be able to terminate the process if they're an administrator, though.

    .PARAMETER UseUnelevatedToken
        If the current process is elevated, starts the new process unelevated using the user's unelevated linked token.

    .PARAMETER ExpandEnvironmentVariables
        Specifies whether to expand any Windows/DOS-style environment variables in the specified `-FilePath` and `-AdditionalArgumentList` parameters.

    .PARAMETER LoggingOptions
        Overrides the default logging options specified in the config.psd1 file.

    .PARAMETER LogFileName
        Overrides the default log file name. The default log file name is generated from the MSI file name. If the value of `-LogFileName` does not end in a common log file extension (.log, .logx, .txt, or .out), '.log' will be automatically appended.

        For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

    .PARAMETER SuccessExitCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootExitCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

    .PARAMETER IgnoreExitCodes
        List the exit codes to ignore or * to ignore all exit codes. Where possible, please use `-SuccessExitCodes` and/or `-RebootExitCodes` instead, or `-ErrorAction SilentlyContinue` as this parameter is deprecated and will be removed in PSAppDeployToolkit 4.3.0.

    .PARAMETER PriorityClass
        Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime.

    .PARAMETER ExitOnProcessFailure
        Automatically closes the active deployment session via Close-ADTSession in the event the process exits with a non-success or non-ignored exit code.

    .PARAMETER NoDesktopRefresh
        If specifies, doesn't refresh the desktop and environment after successful MSI installation.

    .PARAMETER NoWait
        Immediately continue after executing the process.

    .PARAMETER PassThru
        If `-NoWait` is not specified, returns an object with ExitCode, StdOut, and StdErr output from the process. If `-NoWait` is specified, returns a task that can be awaited. Note that a failed execution will only return an object if either `-ErrorAction` is set to `SilentlyContinue`/`Ignore`, or if `-SuccessExitCodes` is used.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        By default, this function returns no output.

    .OUTPUTS
        PSADT.ProcessManagement.ProcessResult

        Returns an object with the results of the installation if `-PassThru` is specified.
        - Process
        - LaunchInfo
        - CommandLine
        - ExitCode
        - StdOut
        - StdErr
        - Interleaved

    .OUTPUTS
        PSADT.ProcessManagement.ProcessHandle

        Returns an object with the handle of the installation process if `-PassThru` and `-NoWait` are specified.
        - Process
        - LaunchInfo
        - CommandLine
        - Task

    .EXAMPLE
        Start-ADTMspProcess -FilePath 'Adobe_Reader_11.0.3_EN.msp'

        Executes the specified MSP file for Adobe Reader 11.0.3.

    .EXAMPLE
        Start-ADTMspProcess -FilePath 'AcroRdr2017Upd1701130143_MUI.msp' -AdditionalArgumentList 'ALLUSERS=1'

        Executes the specified MSP file for Acrobat Reader 2017 with additional parameters.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTMspProcess
    #>

    [CmdletBinding(SupportsShouldProcess = $true, DefaultParameterSetName = 'None')]
    [OutputType([PSADT.ProcessManagement.ProcessResult])]
    [OutputType([PSADT.ProcessManagement.ProcessHandle])]
    param
    (
        [Parameter(Mandatory = $true, HelpMessage = 'Please supply the path to the MSP file to process.')]
        [ValidateScript({
                if ([System.IO.Path]::GetExtension($_) -notmatch '^\.msp$')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'The specified input has an invalid file extension.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [System.String[]]$AdditionalArgumentList,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureArgumentList,

        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser')]
        [ValidateNotNullOrEmpty()]
        [PSADT.Foundation.RunAsActiveUser]$RunAsActiveUser,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser')]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser')]
        [System.Management.Automation.SwitchParameter]$UseHighestAvailableToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser')]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser')]
        [System.Management.Automation.SwitchParameter]$DenyUserTermination,

        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken')]
        [System.Management.Automation.SwitchParameter]$UseUnelevatedToken,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExpandEnvironmentVariables,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$LoggingOptions,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$LogFileName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootExitCodes,

        [Parameter(Mandatory = $false)]
        [System.Obsolete("Please use '-ErrorAction SilentlyContinue' instead as this will be removed in PSAppDeployToolkit 4.3.0.")]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExitOnProcessFailure,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoDesktopRefresh,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Just proxy this through to `Start-ADTMsiProcess` as it does everything.
        if (!$PSCmdlet.ShouldProcess("MSP file [$FilePath]", 'Patch'))
        {
            return
        }
        try
        {
            if (($result = Start-ADTMsiProcess -Action Patch @PSBoundParameters) -and $PassThru)
            {
                return $result
            }
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
