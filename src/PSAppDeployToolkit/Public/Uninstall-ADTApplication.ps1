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

    .PARAMETER SuccessExitCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootExitCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

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

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Uninstall-ADTApplication
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'NameMatch', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ApplicationType', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'IncludeUpdatesAndHotfixes', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LoggingOptions', Justification = "This parameter is used/retrieved via Get-ADTBoundParametersAndDefaultValues, which is too advanced for PSScriptAnalyzer to comprehend.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LogFileName', Justification = "This parameter is used/retrieved via Get-ADTBoundParametersAndDefaultValues, which is too advanced for PSScriptAnalyzer to comprehend.")]
    [CmdletBinding()]
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
        [ValidateNotNullOrEmpty()]
        [System.String]$ArgumentList = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$AdditionalArgumentList = [System.Management.Automation.Language.NullString]::Value,

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
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

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
            $gaiaParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Exclude ArgumentList, AdditionalArgumentList, LoggingOptions, LogFileName, PassThru, SecureArgumentList, SuccessExitCodes, RebootExitCodes
            if (($installedApps = Get-ADTApplication @gaiaParams))
            {
                $InstalledApplication = $installedApps
            }
        }

        # Build the hashtable with the options that will be passed to Start-ADTMsiProcess using splatting
        $sampParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Exclude InstalledApplication, Name, NameMatch, ProductCode, FilterScript, ApplicationType, SuccessExitCodes, RebootExitCodes
        $sampParams.Action = 'Uninstall'

        # Build the hashtable with the options that will be passed to Start-ADTProcess using splatting.
        $sapParams = @{
            SecureArgumentList = $SecureArgumentList
            WaitForMsiExec = $true
            CreateNoWindow = $true
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

        # Build out regex for determining valid exe uninstall strings.
        $invalidFileNameChars = [System.Text.RegularExpressions.Regex]::Escape([System.String]::Join([System.String]::Empty, [System.IO.Path]::GetInvalidFileNameChars()))
        $invalidPathChars = [System.Text.RegularExpressions.Regex]::Escape([System.String]::Join([System.String]::Empty, [System.IO.Path]::GetInvalidPathChars()))
        $validUninstallString = [System.Text.RegularExpressions.Regex]::new("^`"?([^$invalidFileNameChars\s]+(?=\s|$)|[^$invalidPathChars]+?\.(?:exe|cmd|bat|vbs))`"?(?:\s(.*))?$", [System.Text.RegularExpressions.RegexOptions]::Compiled)
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
                    Write-ADTLogEntry -Message "Removing MSI application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)] with ProductCode [$($removeApplication.ProductCode.ToString('B'))]."
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
                    $uninstallString = if (![System.String]::IsNullOrWhiteSpace($removeApplication.QuietUninstallString))
                    {
                        $removeApplication.QuietUninstallString
                    }
                    elseif (![System.String]::IsNullOrWhiteSpace($removeApplication.UninstallString))
                    {
                        $removeApplication.UninstallString
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "No UninstallString found for EXE application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]. Skipping removal."
                        continue
                    }

                    if (!($results = $validUninstallString.Matches($uninstallString)).Success)
                    {
                        Write-ADTLogEntry -Message "Invalid UninstallString [$uninstallString] found for EXE application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]. Skipping removal."
                        continue
                    }
                    $sapParams.FilePath = [System.Environment]::ExpandEnvironmentVariables($results.Groups[1].Value)
                    if (!(Test-Path -LiteralPath $sapParams.FilePath -PathType Leaf) -and ($commandPath = Get-Command -Name $sapParams.FilePath -ErrorAction Ignore))
                    {
                        $sapParams.FilePath = $commandPath.Source
                    }

                    if (![System.String]::IsNullOrWhiteSpace($ArgumentList))
                    {
                        $sapParams.ArgumentList = $ArgumentList
                    }
                    elseif (![System.String]::IsNullOrWhiteSpace($results.Groups[2].Value))
                    {
                        $sapParams.ArgumentList = [System.Environment]::ExpandEnvironmentVariables($results.Groups[2].Value.Trim())
                    }
                    else
                    {
                        $null = $sapParams.Remove('ArgumentList')
                    }
                    if ($AdditionalArgumentList)
                    {
                        if ($sapParams.ContainsKey('ArgumentList'))
                        {
                            $sapParams.ArgumentList += " $([System.String]::Join(' ', $AdditionalArgumentList))"
                        }
                        else
                        {
                            $sapParams.ArgumentList = $AdditionalArgumentList
                        }
                    }

                    Write-ADTLogEntry -Message "Removing EXE application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]."
                    try
                    {
                        Start-ADTProcess @sapParams -ErrorAction $OriginalErrorAction
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
