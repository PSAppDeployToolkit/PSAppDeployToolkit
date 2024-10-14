#-----------------------------------------------------------------------------
#
# MARK: Uninstall-ADTApplication
#
#-----------------------------------------------------------------------------

function Uninstall-ADTApplication
{
    <#
    .SYNOPSIS
        Removes all MSI applications matching the specified application name.

    .DESCRIPTION
        Removes all MSI applications matching the specified application name and filter.
        Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code.

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

    .PARAMETER Parameters
        Overrides the default MSI parameters specified in the configuration file, or the parameters found in QuietUninstallString/UninstallString for EXE applications.

    .PARAMETER AddParameters
        Adds to the default parameters specified in the configuration file, or the parameters found in QuietUninstallString/UninstallString for EXE applications.

    .PARAMETER SecureParameters
        Hides all parameters passed to the executable from the Toolkit log file.

    .PARAMETER LoggingOptions
        Overrides the default MSI logging options specified in the configuration file. Default options are: "/L*v".

    .PARAMETER LogFileName
        Overrides the default log file name for MSI applications. The default log file name is generated from the MSI file name. If LogFileName does not end in .log, it will be automatically appended.

        For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

    .PARAMETER PassThru
        Returns ExitCode, STDOut, and STDErr output from the process.

    .INPUTS
        None

        You cannot pipe objects to this function.

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
        Uninstall-ADTApplication -FilterScript {$_.DisplayName -match '^Vim\s'} -Verbose -ApplicationType EXE -Parameters '/S'

        Remove all EXE applications starting with the name 'Vim' followed by a space, using the '/S' parameter.

    .NOTES
        An active ADT session is NOT required to use this function.

        More reading on how to create filterscripts https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/where-object?view=powershell-5.1#description

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'NameMatch', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ApplicationType', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'IncludeUpdatesAndHotfixes', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LoggingOptions', Justification = "This parameter is used/retrieved via Get-ADTBoundParametersAndDefaultValues, which is too advanced for PSScriptAnalyzer to comprehend.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LogFileName', Justification = "This parameter is used/retrieved via Get-ADTBoundParametersAndDefaultValues, which is too advanced for PSScriptAnalyzer to comprehend.")]
    [CmdletBinding()]
    [OutputType([PSADT.Types.ProcessResult])]
    [OutputType([PSADT.Types.ProcessInfo])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'InstalledApplication', ValueFromPipeline = $true)]
        [PSADT.Types.InstalledApplication[]]$InstalledApplication,

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Name,

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [ValidateSet('Contains', 'Exact', 'Wildcard', 'Regex')]
        [System.String]$NameMatch = 'Contains',

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ProductCode,

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [ValidateSet('All', 'MSI', 'EXE')]
        [System.String]$ApplicationType = 'All',

        [Parameter(Mandatory = $false, ParameterSetName = 'Search')]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false, ParameterSetName = 'Search', Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$FilterScript,

        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Parameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$AddParameters,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureParameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LoggingOptions,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogFileName,

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
            if (!$PSBoundParameters.ContainsKey('Name') -and !$PSBoundParameters.ContainsKey('ProductCode') -and !$PSBoundParameters.ContainsKey('FilterScript'))
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
            $gaiaParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -ParameterSetName $PSCmdlet.ParameterSetName -Exclude Parameters, AddParameters, LoggingOptions, LogFileName, PassThru
            $InstalledApplication = Get-ADTApplication @gaiaParams
        }

        # Build the hashtable with the options that will be passed to Start-ADTMsiProcess using splatting
        $sampParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -ParameterSetName $PSCmdlet.ParameterSetName -Exclude InstalledApplication, Name, NameMatch, ProductCode, FilterScript, ApplicationType
        $sampParams.Action = 'Uninstall'

        # Build the hashtable with the options that will be passed to Start-ADTProcess using splatting.
        $sapParams = @{
            SecureParameters = $SecureParameters
            NoExitOnProcessFailure = $true
            WaitForMsiExec = $true
            CreateNoWindow = $true
            PassThru = $PassThru
            Path = $null
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
                    if ([string]::IsNullOrWhiteSpace($removeApplication.ProductCode))
                    {
                        Write-ADTLogEntry -Message "No ProductCode found for MSI application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]. Skipping removal."
                        continue
                    }
                    $sampParams.Path = $removeApplication.ProductCode
                    Write-ADTLogEntry -Message "Removing MSI application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)] with ProductCode [$($removeApplication.ProductCode)]."
                    try
                    {
                        Start-ADTMsiProcess @sampParams
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

                    $invalidFileNameChars = [System.Text.RegularExpressions.Regex]::Escape([System.String]::Join($null, [System.IO.Path]::GetInvalidFileNameChars()))
                    $invalidPathChars = [System.Text.RegularExpressions.Regex]::Escape([System.String]::Join($null, [System.IO.Path]::GetInvalidPathChars()))

                    if ($uninstallString -match "^`"?([^$invalidFileNameChars\s]+(?=\s|$)|[^$invalidPathChars]+?\.(?:exe|cmd|bat|vbs))`"?(?:\s(.*))?$")
                    {
                        $sapParams.Path = [System.Environment]::ExpandEnvironmentVariables($matches[1])
                        if (![System.IO.File]::Exists($sapParams.Path) -and ($commandPath = Get-Command -Name $sapParams.Path -ErrorAction Ignore))
                        {
                            $sapParams.Path = $commandPath.Source
                        }
                        $uninstallStringParams = if ($matches.Count -gt 2)
                        {
                            [System.Environment]::ExpandEnvironmentVariables($matches[2].Trim())
                        }
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Invalid UninstallString [$uninstallString] found for EXE application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]. Skipping removal."
                        continue
                    }

                    if (![System.String]::IsNullOrWhiteSpace($Parameters))
                    {
                        $sapParams.Parameters = $Parameters
                    }
                    elseif (![System.String]::IsNullOrWhiteSpace($uninstallStringParams))
                    {
                        $sapParams.Parameters = $uninstallStringParams
                    }
                    else
                    {
                        $sapParams.Remove('Parameters')
                    }
                    if ($AddParameters)
                    {
                        if ($sapParams.ContainsKey('Parameters'))
                        {
                            $sapParams.Parameters += " $AddParameters"
                        }
                        else
                        {
                            $sapParams.Parameters = $AddParameters
                        }
                    }

                    Write-ADTLogEntry -Message "Removing EXE application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]."
                    try
                    {
                        Start-ADTProcess @sapParams
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
