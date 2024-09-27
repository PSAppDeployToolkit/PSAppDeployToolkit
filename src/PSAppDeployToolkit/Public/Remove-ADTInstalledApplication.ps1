#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTInstalledApplication
#
#-----------------------------------------------------------------------------

function Remove-ADTInstalledApplication
{
    <#
.SYNOPSIS

Removes all MSI applications matching the specified application name.

.DESCRIPTION

Removes all MSI applications matching the specified application name.
Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code.

.PARAMETER FilterScript

Specifies a script block to filter the applications to be removed. The script block is evaluated for each application, and if it returns $true, the application is selected for removal.

.PARAMETER ApplicationType

Specifies the type of application to remove. Valid values are 'Any', 'MSI', and 'EXE'. The default value is 'MSI'.

.PARAMETER Parameters

Overrides the default MSI parameters specified in the configuration file, or the parameters found in QuietUninstallString/UninstallString for EXE applications.

.PARAMETER AddParameters

Adds to the default parameters specified in the configuration file, or the parameters found in QuietUninstallString/UninstallString for EXE applications.

.PARAMETER IncludeUpdatesAndHotfixes

Include matches against updates and hotfixes in results.

.PARAMETER LoggingOptions

Overrides the default logging options specified in the configuration file. Default options are: "/L*v".

.PARAMETER LogName

Overrides the default log file name for MSI applications. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.
For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

.PARAMETER PassThru

Returns ExitCode, STDOut, and STDErr output from the process.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns an object with the following properties:
- ExitCode
- StdOut
- StdErr

.EXAMPLE

Remove-ADTInstalledApplication -FilterScript {$_.DisplayName -match 'Java'}

Removes all MSI applications that contain the name 'Java' in the DisplayName.

.EXAMPLE

Remove-ADTInstalledApplication -FilterScript {$_.DisplayName -match 'Java' -and $_.Publisher -eq 'Oracle Corporation' -and $_.Is64BitApplication -eq $true -and $_.DisplayVersion -notlike '8.*'}

Removes all MSI applications that contain the name 'Java' in the DisplayName, with Publisher as 'Oracle Corporation', 64-bit, and not version 8.x.

.EXAMPLE

Remove-ADTInstalledApplication -FilterScript {$_.DisplayName -match '^Vim\s'} -Verbose -ApplicationType EXE -Parameters '/S'

Remove all EXE applications starting with the name 'Vim' followed by a space, using the '/S' parameter.

.NOTES

More reading on how to create filterscripts https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/where-object?view=powershell-5.1#description

.NOTES

This function can be called without an active ADT session..

.LINK

https://psappdeploytoolkit.com
#>
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'FilterScript', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$FilterScript,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Any', 'MSI', 'EXE')]
        [System.String]$ApplicationType = 'MSI',

        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullorEmpty()]
        [System.String]$Parameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.String]$AddParameters,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.String]$LoggingOptions,

        [Parameter(Mandatory = $false)]
        [System.String]$LogName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Make this function continue on error.
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Build the hashtable with the options that will be passed to Get-ADTInstalledApplication using splatting
        $gaiaParams = @{
            FilterScript              = $FilterScript
            IncludeUpdatesAndHotfixes = $IncludeUpdatesAndHotfixes
        }

        # Build the hashtable with the options that will be passed to Start-ADTMsiProcess using splatting
        $sampParams = & $Script:CommandTable.'Get-ADTBoundParametersAndDefaultValues' -Invocation $MyInvocation -Exclude FilterScript
        $sampParams.Action = 'Uninstall'

        # Build the hashtable with the options that will be passed to Start-ADTProcess using splatting.
        $sapParams = @{
            WaitForMsiExec         = $true
            NoExitOnProcessFailure = $true
            WindowStyle            = 'Hidden'
            CreateNoWindow         = $true
            PassThru               = $PassThru
            Path                   = $null
        }
    }
    process
    {
        try
        {
            [PSADT.Types.InstalledApplication[]]$removeApplications = & $Script:CommandTable.'Get-ADTInstalledApplication' @gaiaParams

            # Filter the results to restrict to specified applciation type
            if ($ApplicationType -eq 'MSI')
            {
                $removeApplications = $removeApplications.Where({ $_.WindowsInstaller -and $_.ProductCode })
            }
            elseif ($ApplicationType -eq 'EXE')
            {
                $removeApplications = $removeApplications.Where({ -not $_.WindowsInstaller })
            }

            $ExecuteResults = if ($null -ne $removeApplications)
            {
                & $Script:CommandTable.'Write-ADTLogEntry' -Message "Found [$($removeApplications.Count)] application(s) of type [$ApplicationType] that matched the specified criteria [$FilterScript]."

                foreach ($removeApplication in $removeApplications)
                {
                    if ($removeApplication.WindowsInstaller)
                    {
                        if ($null -eq $removeApplication.ProductCode)
                        {
                            & $Script:CommandTable.'Write-ADTLogEntry' -Message "No ProductCode found for MSI application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]. Skipping removal."
                            continue
                        }
                        $sampParams.Path = $removeApplication.ProductCode
                        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Removing MSI application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)] with ProductCode [$($removeApplication.ProductCode)]."
                        try
                        {
                            & $Script:CommandTable.'Start-ADTMsiProcess' @sampParams
                        }
                        catch
                        {
                            & $Script:CommandTable.'Write-Error' -ErrorRecord $_
                        }
                    }
                    else
                    {
                        $uninstallString = if (![string]::IsNullOrWhiteSpace($removeApplication.QuietUninstallString))
                        {
                            $removeApplication.QuietUninstallString
                        }
                        elseif (![string]::IsNullOrWhiteSpace($removeApplication.UninstallString))
                        {
                            $removeApplication.UninstallString
                        }
                        else
                        {
                            & $Script:CommandTable.'Write-ADTLogEntry' -Message "No UninstallString found for EXE application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]. Skipping removal."
                            continue
                        }

                        if ($uninstallString -match '^"(.+?\.exe)"(?:\s(.*))?$')
                        {
                            $sapParams.Path = [System.Environment]::ExpandEnvironmentVariables($matches[1])
                            $uninstallStringParams = [System.Environment]::ExpandEnvironmentVariables($matches[2].Trim())
                        }
                        elseif ($uninstallString -match '^(\S+?\.exe)(?:\s(.*))?$')
                        {
                            $sapParams.Path = [System.Environment]::ExpandEnvironmentVariables($matches[1])
                            $uninstallStringParams = [System.Environment]::ExpandEnvironmentVariables($matches[2].Trim())
                        }
                        elseif ($uninstallString -match '^"?(.+?\.exe)"?$')
                        {
                            $sapParams.Path = [System.Environment]::ExpandEnvironmentVariables($matches[1])
                            $uninstallStringParams = $null
                        }
                        else
                        {
                            & $Script:CommandTable.'Write-ADTLogEntry' -Message "Invalid UninstallString [$uninstallString] found for EXE application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]. Skipping removal."
                            continue
                        }

                        if (![string]::IsNullOrWhiteSpace($Parameters))
                        {
                            $sapParams.Parameters = $Parameters
                        }
                        elseif (![string]::IsNullOrWhiteSpace($uninstallStringParams))
                        {
                            $sapParams.Parameters = $uninstallStringParams
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

                        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Removing EXE application [$($removeApplication.DisplayName) $($removeApplication.DisplayVersion)]."
                        try
                        {
                            & $Script:CommandTable.'Start-ADTProcess' @sapParams
                        }
                        catch
                        {
                            & $Script:CommandTable.'Write-Error' -ErrorRecord $_
                        }
                    }

                }
            }
            else
            {
                & $Script:CommandTable.'Write-ADTLogEntry' -Message 'No applications found for removal. Continue...'
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }
    end
    {
        if ($PassThru -and $ExecuteResults)
        {
            & $Script:CommandTable.'Write-Output' -InputObject ($ExecuteResults)
        }
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
