#-----------------------------------------------------------------------------
#
# MARK: Uninstall-ADTApplication
#
#-----------------------------------------------------------------------------

function Uninstall-ADTApplication
{
    <#
    .SYNOPSIS
        Removes one or more applications specified by name, filter script, or InstalledApplication object from Get-ADTApplication.

    .DESCRIPTION
        Removes one or more applications specified by name, filter script, or InstalledApplication object from Get-ADTApplication.

        Enumerates the registry for installed applications via Get-ADTApplication, matching the specified application name and uninstalls that application using its uninstall string, with the ability to specify additional uninstall parameters also.

        The application will be uninstalled using its QuietUninstallString where possible. If it doesn't exist, is null, is invalid, or `-ForceUninstallString` is specified, the UninstallString will be used.

    .PARAMETER InstalledApplication
        Specifies the [PSADT.Types.InstalledApplication] object to remove. This parameter is typically used when piping Get-ADTApplication to this function.

    .PARAMETER Name
        The name of the application to retrieve information for. Performs a contains match on the application display name by default.

    .PARAMETER NameMatch
        Specifies the type of match to perform on the application name. Valid values are 'Contains', 'Exact', 'Wildcard', and 'Regex'. The default value is 'Contains'.

    .PARAMETER ProductCode
        The product code of the application to retrieve information for.

    .PARAMETER ApplicationType
        Specifies the type of application to remove. Valid values are 'All', 'MSI', and 'EXE'. The default value is 'All'.

    .PARAMETER IncludeUpdatesAndHotfixes
        Include matches against updates and hotfixes in results.

    .PARAMETER FilterScript
        A script used to filter the results as they're processed.

    .PARAMETER ForceUninstallString
        Forcibly uses the UninstallString instead of QuietUninstallString.

    .PARAMETER ArgumentList
        Overrides the default MSI parameters specified in the config.psd1 file, or the parameters found in QuietUninstallString/UninstallString for EXE applications.

    .PARAMETER AdditionalArgumentList
        Adds to the default parameters specified in the config.psd1 file, or the parameters found in QuietUninstallString/UninstallString for EXE applications.

    .PARAMETER SecureArgumentList
        Hides all parameters passed to the executable from the Toolkit log file.

    .PARAMETER LoggingOptions
        Overrides the default MSI logging options specified in the config.psd1 file. Default options are: "/L*v".

    .PARAMETER LogFileName
        Overrides the default log file name for MSI applications. The default log file name is generated from the MSI file name. If LogFileName does not end in .log, it will be automatically appended.

        For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

    .PARAMETER WaitForChildProcesses
        Specifies whether the started process should be considered finished only when any child processes it spawns have finished also.

    .PARAMETER KillChildProcessesWithParent
        Specifies whether any child processes started by the provided executable should be closed when the provided executable closes. This is handy for application installs that open web browsers and other programs that cannot be suppressed.

    .PARAMETER SuccessExitCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootExitCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

    .PARAMETER IgnoreExitCodes
        List the exit codes to ignore or * to ignore all exit codes. Where possible, please use `-SuccessExitCodes` and/or `-RebootExitCodes` instead.

    .PARAMETER ExitOnProcessFailure
        Automatically closes the active deployment session via Close-ADTSession in the event the process exits with a non-success or non-ignored exit code.

    .PARAMETER PassThru
        Returns a PSADT.Types.ProcessResult object, providing the ExitCode, StdOut, and StdErr output from the uninstallation.

    .INPUTS
        PSADT.Types.InstalledApplication

        This function can receive one or more InstalledApplication objects for uninstallation.

    .OUTPUTS
        PSADT.Types.ProcessResult

        Returns an object with the results of the installation if -PassThru is specified.
        - ExitCode
        - StdOut
        - StdErr

    .EXAMPLE
        Uninstall-ADTApplication -Name 'Acrobat' -ApplicationType 'MSI' -FilterScript { $_.Publisher -match 'Adobe' }

        Removes all MSI applications that contain the name 'Acrobat' in the DisplayName and 'Adobe' in the Publisher name.

    .EXAMPLE
        Uninstall-ADTApplication -Name 'Java' -FilterScript {$_.Publisher -eq 'Oracle Corporation' -and $_.Is64BitApplication -eq $true -and $_.DisplayVersion -notlike '8.*'}

        Removes all MSI applications that contain the name 'Java' in the DisplayName, with Publisher as 'Oracle Corporation', are 64-bit, and not version 8.x.

    .EXAMPLE
        Uninstall-ADTApplication -FilterScript {$_.DisplayName -match '^Vim\s'} -Verbose -ApplicationType EXE -ArgumentList '/S'

        Remove all EXE applications starting with the name 'Vim' followed by a space, using the '/S' parameter.

    .NOTES
        An active ADT session is NOT required to use this function.

        More reading on how to create filterscripts https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/where-object?view=powershell-5.1#description

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Uninstall-ADTApplication
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'NameMatch', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ApplicationType', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'IncludeUpdatesAndHotfixes', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LoggingOptions', Justification = "This parameter is used/retrieved via Get-ADTBoundParametersAndDefaultValues, which is too advanced for PSScriptAnalyzer to comprehend.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LogFileName', Justification = "This parameter is used/retrieved via Get-ADTBoundParametersAndDefaultValues, which is too advanced for PSScriptAnalyzer to comprehend.")]
    [CmdletBinding(SupportsShouldProcess = $true)]
    [OutputType([PSADT.ProcessManagement.ProcessResult])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'InstalledApplication', ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.InstalledApplication[]]$InstalledApplication,

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Name,

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [ValidateSet('Contains', 'Exact', 'Wildcard', 'Regex')]
        [System.String]$NameMatch = 'Contains',

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [ValidateNotNullOrEmpty()]
        [System.Guid[]]$ProductCode,

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [ValidateSet('All', 'MSI', 'EXE')]
        [System.String]$ApplicationType = 'All',

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false, ParameterSetName = 'Search', Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$FilterScript,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ForceUninstallString,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ArgumentList,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$AdditionalArgumentList,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureArgumentList,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LoggingOptions = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogFileName = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$WaitForChildProcesses,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$KillChildProcessesWithParent,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExitOnProcessFailure,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Get the InstalledApplication object based on provided input.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        if ($PSCmdlet.ParameterSetName -ne 'InstalledApplication')
        {
            if (!($PSBoundParameters.Keys -match '^(Name|ProductCode|FilterScript)$'))
            {
                $naerParams = @{
                    Exception = [System.ArgumentNullException]::new('Either Name, ProductCode or FilterScript are required if not using pipeline.')
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'NullParameterValue'
                    RecommendedAction = "Review the supplied parameter values and try again."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }

            # Build the hashtable with the options that will be passed to Get-ADTApplication using splatting
            $gaiaParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Exclude ArgumentList, AdditionalArgumentList, LoggingOptions, LogFileName, PassThru, SecureArgumentList, SuccessExitCodes, RebootExitCodes, IgnoreExitCodes, WaitForChildProcesses, KillChildProcessesWithParent, ExitOnProcessFailure
            if (($installedApps = Get-ADTApplication @gaiaParams))
            {
                $InstalledApplication = $installedApps
            }
        }

        # Build the hashtable with the options that will be passed to Start-ADTMsiProcess using splatting
        $sampParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Exclude InstalledApplication, Name, NameMatch, ProductCode, FilterScript, ApplicationType, WaitForChildProcesses, KillChildProcessesWithParent
        $sampParams.Action = 'Uninstall'

        # Build the hashtable with the options that will be passed to Start-ADTProcess using splatting.
        $sapParams = @{
            SecureArgumentList = $SecureArgumentList
            WaitForChildProcesses = $WaitForChildProcesses
            KillChildProcessesWithParent = $KillChildProcessesWithParent
            ExitOnProcessFailure = $ExitOnProcessFailure
            ExpandEnvironmentVariables = $true
            WaitForMsiExec = $true
            PassThru = $PassThru
        }
        if ($PSBoundParameters.ContainsKey('SuccessExitCodes'))
        {
            $sapParams.Add('SuccessExitCodes', $SuccessExitCodes)
        }
        if ($PSBoundParameters.ContainsKey('RebootExitCodes'))
        {
            $sapParams.Add('RebootExitCodes', $RebootExitCodes)
        }
        if ($PSBoundParameters.ContainsKey('IgnoreExitCodes'))
        {
            $sapParams.Add('IgnoreExitCodes', $IgnoreExitCodes)
        }
    }

    process
    {
        if (!$InstalledApplication)
        {
            Write-ADTLogEntry -Message 'No applications found for removal.'
            return
        }

        foreach ($removeApplication in $InstalledApplication)
        {
            try
            {
                if ($removeApplication.WindowsInstaller)
                {
                    if (!$removeApplication.ProductCode)
                    {
                        Write-ADTLogEntry -Message "No ProductCode found for MSI application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]. Skipping removal."
                        continue
                    }
                    Write-ADTLogEntry -Message "Removing MSI application [$($removeApplication.DisplayName)$(if ($removeApplication.DisplayVersion -and !$removeApplication.DisplayName.Contains($removeApplication.DisplayVersion)) { " $($removeApplication.DisplayVersion)" })] with ProductCode [$($removeApplication.ProductCode.ToString('B'))]."
                    if (!$PSCmdlet.ShouldProcess("MSI Application [$($removeApplication.DisplayName)]", 'Uninstall'))
                    {
                        continue
                    }
                    try
                    {
                        if ($sampParams.ContainsKey('FilePath'))
                        {
                            $null = $sampParams.Remove('FilePath')
                        }
                        $removeApplication | Start-ADTMsiProcess @sampParams -ErrorAction $OriginalErrorAction
                    }
                    catch
                    {
                        Write-Error -ErrorRecord $_
                    }
                }
                else
                {
                    # Set up the FilePath to use for the uninstall.
                    $uninstallProperty = if (![System.String]::IsNullOrWhiteSpace($removeApplication.QuietUninstallStringFilePath) -and !$ForceUninstallString)
                    {
                        "QuietUninstallString"
                    }
                    elseif (![System.String]::IsNullOrWhiteSpace($removeApplication.UninstallStringFilePath))
                    {
                        "UninstallString"
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "No UninstallString found for EXE application [$($removeApplication.DisplayName)$(if ($removeApplication.DisplayVersion -and !$removeApplication.DisplayName.Contains($removeApplication.DisplayVersion)) { " $($removeApplication.DisplayVersion)" })]. Skipping removal."
                        continue
                    }
                    $sapParams.FilePath = $removeApplication."$($uninstallProperty)FilePath"
                    if (!(Test-Path -LiteralPath $sapParams.FilePath -PathType Leaf) -and ($commandPath = Get-Command -Name $sapParams.FilePath -ErrorAction Ignore))
                    {
                        $sapParams.FilePath = $commandPath.Source
                    }

                    # Set up the ArgumentList for the uninstall.
                    if ($PSBoundParameters.ContainsKey('ArgumentList'))
                    {
                        $sapParams.ArgumentList = $ArgumentList
                    }
                    elseif (($null -ne ([System.String[]]$argv = $($removeApplication."$($uninstallProperty)ArgumentList"))) -and ($argv.Count -gt 0))
                    {
                        $sapParams.ArgumentList = $argv
                    }
                    else
                    {
                        $null = $sapParams.Remove('ArgumentList')
                    }

                    # Handle any additional arguments to pass.
                    if ($AdditionalArgumentList)
                    {
                        if ($sapParams.ContainsKey('ArgumentList'))
                        {
                            if ($AdditionalArgumentList.Length -eq 1)
                            {
                                $sapParams.ArgumentList += [PSADT.ProcessManagement.CommandLineUtilities]::CommandLineToArgumentList($AdditionalArgumentList[0])
                            }
                            else
                            {
                                $sapParams.ArgumentList += $AdditionalArgumentList
                            }
                        }
                        else
                        {
                            $sapParams.ArgumentList = $AdditionalArgumentList
                        }
                    }

                    Write-ADTLogEntry -Message "Removing EXE application [$($removeApplication.DisplayName)$(if ($removeApplication.DisplayVersion -and !$removeApplication.DisplayName.Contains($removeApplication.DisplayVersion)) { " $($removeApplication.DisplayVersion)" })]."
                    if (!$PSCmdlet.ShouldProcess("EXE Application [$($removeApplication.DisplayName)]", 'Uninstall'))
                    {
                        continue
                    }
                    try
                    {
                        Start-ADTProcess @sapParams -CreateNoWindow:(![PSADT.FileSystem.ExecutableInfo]::Get($sapParams.FilePath).Subsystem.Equals([PSADT.LibraryInterfaces.IMAGE_SUBSYSTEM]::IMAGE_SUBSYSTEM_WINDOWS_GUI)) -ErrorAction $OriginalErrorAction
                    }
                    catch
                    {
                        Write-Error -ErrorRecord $_
                    }
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
