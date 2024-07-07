function Open-ADTSession
{
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
        [ValidateNotNullOrEmpty()]
        [System.String]$DefaultMsiFile,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DefaultMstFile,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$DefaultMspFiles,

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

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Initialise function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $adtData = Get-ADTModuleData
    }

    process
    {
        # If this function is being called AppDeployToolkitMain.ps1 or the console, clear all previous sessions and go for full re-initialisation.
        if ((Test-ADTNonNativeCaller) -or ($PSBoundParameters.RunspaceOrigin = $MyInvocation.CommandOrigin.Equals([System.Management.Automation.CommandOrigin]::Runspace)))
        {
            $adtData.Sessions.Clear()
        }

        # Initialise the module, instantiate a new ADT session and open it for usage.
        try
        {
            if (!$adtData.Sessions.Count)
            {
                Initialize-ADTModule
            }
            $adtData.Sessions.Add($PSBoundParameters)
            try
            {
                $adtData.Sessions[-1].Open()
                if ($adtData.Sessions.Count.Equals(1))
                {
                    [System.Void]$ExecutionContext.InvokeCommand.InvokeScript($SessionState, {$args[0].GetEnumerator().ForEach({New-Variable -Name $_.Key -Value $_.Value -Option ReadOnly -Force})}.Ast.GetScriptBlock(), $adtData.Environment)
                }
            }
            catch
            {
                [System.Void]$adtData.Sessions.Remove($adtData.Sessions[-1])
                throw
            }
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }

        # Return the most recent session if passing through.
        if ($PassThru)
        {
            return $adtData.Sessions[-1]
        }
    }

    end
    {
        # Finalise function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
