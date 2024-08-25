#-----------------------------------------------------------------------------
#
# MARK: Open-ADTSession
#
#-----------------------------------------------------------------------------

function Open-ADTSession
{
    <#
    .SYNOPSIS
        Opens a new ADT session.

    .DESCRIPTION
        This function initializes and opens a new ADT session with the specified parameters. It handles the setup of the session environment and processes any callbacks defined for the session. If the session fails to open, it handles the error and closes the session if necessary.

    .PARAMETER SessionState
        Caller's SessionState.

    .PARAMETER DeploymentType
        Deploy-Application.ps1 Parameter. Specifies the type of deployment: Install, Uninstall, or Repair.

    .PARAMETER DeployMode
        Deploy-Application.ps1 Parameter. Specifies the deployment mode: Interactive, NonInteractive, or Silent.

    .PARAMETER AllowRebootPassThru
        Deploy-Application.ps1 Parameter. Allows reboot pass-through.

    .PARAMETER TerminalServerMode
        Deploy-Application.ps1 Parameter. Enables Terminal Server mode.

    .PARAMETER DisableLogging
        Deploy-Application.ps1 Parameter. Disables logging for the session.

    .PARAMETER AppVendor
        Deploy-Application.ps1 Parameter. Specifies the application vendor.

    .PARAMETER AppName
        Deploy-Application.ps1 Parameter. Specifies the application name.

    .PARAMETER AppVersion
        Deploy-Application.ps1 Parameter. Specifies the application version.

    .PARAMETER AppArch
        Deploy-Application.ps1 Parameter. Specifies the application architecture.

    .PARAMETER AppLang
        Deploy-Application.ps1 Parameter. Specifies the application language.

    .PARAMETER AppRevision
        Deploy-Application.ps1 Parameter. Specifies the application revision.

    .PARAMETER AppExitCodes
        Deploy-Application.ps1 Parameter. Specifies the application exit codes.

    .PARAMETER AppRebootCodes
        Deploy-Application.ps1 Parameter. Specifies the application reboot codes.

    .PARAMETER AppScriptVersion
        Deploy-Application.ps1 Parameter. Specifies the application script version.

    .PARAMETER AppScriptDate
        Deploy-Application.ps1 Parameter. Specifies the application script date.

    .PARAMETER AppScriptAuthor
        Deploy-Application.ps1 Parameter. Specifies the application script author.

    .PARAMETER InstallName
        Deploy-Application.ps1 Parameter. Specifies the install name.

    .PARAMETER InstallTitle
        Deploy-Application.ps1 Parameter. Specifies the install title.

    .PARAMETER DeployAppScriptFriendlyName
        Deploy-Application.ps1 Parameter. Specifies the friendly name of the deploy application script.

    .PARAMETER DeployAppScriptVersion
        Deploy-Application.ps1 Parameter. Specifies the version of the deploy application script.

    .PARAMETER DeployAppScriptDate
        Deploy-Application.ps1 Parameter. Specifies the date of the deploy application script.

    .PARAMETER DeployAppScriptParameters
        Deploy-Application.ps1 Parameter. Specifies the parameters for the deploy application script.

    .PARAMETER DirFiles
        Deploy-Application.ps1 Parameter. Specifies the path to Files.

    .PARAMETER DirSupportFiles
        Deploy-Application.ps1 Parameter. Specifies the path to SupportFiles.

    .PARAMETER DefaultMsiFile
        Deploy-Application.ps1 Parameter. Specifies the default MSI file.

    .PARAMETER DefaultMstFile
        Deploy-Application.ps1 Parameter. Specifies the default MST file.

    .PARAMETER DefaultMspFiles
        Deploy-Application.ps1 Parameter. Specifies the default MSP files.

    .PARAMETER PassThru
        Deploy-Application.ps1 Parameter. Passes the session object through the pipeline.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        ADTSession

        This function returns the session object if -PassThru is specified.

    .EXAMPLE
        Open-ADTSession -SessionState $ExecutionContext.SessionState -DeploymentType "Install" -DeployMode "Interactive"

        Opens a new ADT session with the specified parameters.

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
        [Parameter(Mandatory = $true, HelpMessage = "Caller's SessionState")]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Parameter')]
        [ValidateSet('Install', 'Uninstall', 'Repair')]
        [System.String]$DeploymentType,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Parameter')]
        [ValidateSet('Interactive', 'NonInteractive', 'Silent')]
        [System.String]$DeployMode,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Parameter')]
        [System.Management.Automation.SwitchParameter]$AllowRebootPassThru,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Parameter')]
        [System.Management.Automation.SwitchParameter]$TerminalServerMode,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Parameter')]
        [System.Management.Automation.SwitchParameter]$DisableLogging,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [AllowEmptyString()]
        [System.String]$AppVendor,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [AllowEmptyString()]
        [System.String]$AppName,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [AllowEmptyString()]
        [System.String]$AppVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [AllowEmptyString()]
        [System.String]$AppArch,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [AllowEmptyString()]
        [System.String]$AppLang,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [AllowEmptyString()]
        [System.String]$AppRevision,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$AppExitCodes,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$AppRebootCodes,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$AppScriptVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppScriptDate,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppScriptAuthor,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [AllowEmptyString()]
        [System.String]$InstallName,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [AllowEmptyString()]
        [System.String]$InstallTitle,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeployAppScriptFriendlyName,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$DeployAppScriptVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeployAppScriptDate,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [AllowEmptyCollection()]
        [System.Collections.IDictionary]$DeployAppScriptParameters,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DirFiles,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DirSupportFiles,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DefaultMsiFile,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DefaultMstFile,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$DefaultMspFiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Initialise function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $adtData = Get-ADTModuleData
        $adtSession = $null
        $errRecord = $null
    }

    process
    {
        # If this function is being called AppDeployToolkitMain.ps1 or the console, clear all previous sessions and go for full re-initialisation.
        if ((Test-ADTNonNativeCaller) -or ($PSBoundParameters.RunspaceOrigin = $MyInvocation.CommandOrigin.Equals([System.Management.Automation.CommandOrigin]::Runspace)))
        {
            $adtData.Sessions.Clear()
        }

        # Commence the opening process.
        try
        {
            try
            {
                # Initialise the module before opening the first session.
                if (!$adtData.Sessions.Count)
                {
                    Initialize-ADTModule
                }
                $adtData.Sessions.Add(($adtSession = [ADTSession]::new($PSBoundParameters)))
                $adtSession.Open()

                # Invoke all callbacks.
                foreach ($callback in $(if ($adtData.Sessions.Count.Equals(1)) { $adtData.Callbacks.Starting }; $adtData.Callbacks.Opening))
                {
                    & $callback
                }

                # Export the environment table to variables within the caller's scope.
                if ($adtData.Sessions.Count.Equals(1))
                {
                    $null = $ExecutionContext.InvokeCommand.InvokeScript($SessionState, { $args[1].GetEnumerator() | . { process { & $args[0] -Name $_.Key -Value $_.Value -Option ReadOnly -Force } } $args[0] }.Ast.GetScriptBlock(), $Script:CommandTable.'New-Variable', $adtData.Environment)
                }
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            $errRecord = $_
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failure occurred while opening new ADTSession object."
        }
        finally
        {
            if ($adtSession -and $errRecord)
            {
                Close-ADTSession -ExitCode 60008
            }
        }

        # Return the most recent session if passing through.
        if ($PassThru)
        {
            return $adtSession
        }
    }

    end
    {
        # Finalise function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
