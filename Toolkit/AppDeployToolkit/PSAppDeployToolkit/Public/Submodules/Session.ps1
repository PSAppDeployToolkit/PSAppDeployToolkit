#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Open-ADTSession
{
    param (
        [Parameter(Mandatory = $true, HelpMessage = "Caller's CmdletBinding Object")]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet,

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
        [ValidateNotNullOrEmpty()]
        [System.String]$AppVendor,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppName,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppArch,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppLang,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppRevision,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$AppExitCodes,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppScriptVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppScriptDate,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppScriptAuthor,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$InstallName,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$InstallTitle,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeployAppScriptFriendlyName,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$DeployAppScriptVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Deploy-Application.ps1 Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeployAppScriptDate
    )

    # Clamp the session count at one, for now.
    if ($Script:SessionBuffer.Count)
    {
        throw [System.InvalidOperationException]::new("Only one $($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name) session is permitted at this time.")
    }

    # Initialise the PSADT environment before instantiating a session.
    Initialize-ADTVariableDatabase
    Import-ADTConfig
    Import-ADTLocalizedStrings
    $Script:ADT.LastExitCode = 0

    # Instantiate a new ADT session and initialise it.
    $Script:SessionBuffer.Add(($Script:ADT.CurrentSession = [ADTSession]::new($PSBoundParameters)))
    try
    {
        $Script:ADT.CurrentSession.Open()
    }
    catch
    {
        Restore-ADTPreviousSession
        throw
    }

    # Export environment variables to the user's scope.
    $Script:ADT.Environment.GetEnumerator().ForEach({$Cmdlet.SessionState.PSVariable.Set([psvariable]::new($_.Name, $_.Value, 'Constant'))})
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Close-ADTSession
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ExitCode
    )

    # Close the Installation Progress Dialog if running.
    if ($Script:SessionBuffer.Count.Equals(1))
    {
        Close-InstallationProgress
    }

    # Close out the active session and clean up session state.
    $Script:ADT.CurrentSession.Close($ExitCode)
    Restore-ADTPreviousSession

    # If this was the last session, exit out with our code.
    if (!$Script:SessionBuffer.Count)
    {
        exit $Script:ADT.LastExitCode
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Restore-ADTPreviousSession
{
    # Destruct the active session and restore the previous one if available.
    $Host.UI.RawUI.WindowTitle = $Script:ADT.CurrentSession.OldPSWindowTitle
    [System.Void]$Script:SessionBuffer.Remove($Script:ADT.CurrentSession)
    $Script:SessionCallers.Remove($Script:ADT.CurrentSession)
    $Script:ADT.CurrentSession = if ($Script:SessionBuffer.Count)
    {
        $Script:SessionBuffer[-1]
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTSession
{
    # Return the most recent session in the database.
    try
    {
        return $Script:SessionBuffer[-1]
    }
    catch
    {
        throw [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any $($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name) functions.")
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Export-ADTModuleState
{
    # Sync all property values and export to registry.
    $Script:ADT.CurrentSession.SyncPropertyValues()
    $Script:Serialisation.Hive.CreateSubKey($Script:Serialisation.Key).SetValue($Script:Serialisation.Name, [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Management.Automation.PSSerializer]::Serialize($Script:ADT, [System.Int32]::MaxValue))), $Script:Serialisation.Type)
}

#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Import-ADTModuleState
{
    # Restore the previously exported session and prepare it for asynchronous operation. The serialised state may be on-disk during BlockExecution operations.
    Set-Variable -Name ADT -Scope Script -Option ReadOnly -Force -Value $(if ([System.IO.File]::Exists(($onDiskClixml = $Script:MyInvocation.MyCommand.Path.Replace('.psm1', '.xml'))))
    {
        Import-Clixml -LiteralPath $onDiskClixml
    }
    else
    {
        [System.Management.Automation.PSSerializer]::Deserialize([System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String(($regPath = $Script:Serialisation.Hive.OpenSubKey($Script:Serialisation.Key, $true)).GetValue($Script:Serialisation.Name))))
        $regPath.DeleteValue($Script:Serialisation.Name, $true)
    })

    # Create new object based on serialised state and configure for async operations.
    $Script:ADT.CurrentSession = [ADTSession]::new($Script:ADT.CurrentSession)
    $Script:ADT.CurrentSession.Properties.InstallPhase = 'Asynchronous'
    $Script:ADT.CurrentSession.LegacyMode = $false
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTDeployApplicationParameters
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Throw if called outside of AppDeployToolkitMain.ps1.
    if (!(Get-PSCallStack).Command.Contains('AppDeployToolkitMain.ps1'))
    {
        throw [System.InvalidOperationException]::new("The function [$($MyInvocation.MyCommand.Name)] is only supported for legacy Deploy-Application.ps1 scripts.")
    }

    # Open hashtable for returning at the end. We return it even if it's empty.
    $daParams = @{Cmdlet = $Cmdlet}

    # Get all relevant parameters from the targeted function, then check whether they're defined and not empty.
    foreach ($param in (Get-Item -LiteralPath Function:Open-ADTSession).Parameters.Values.Where({$_.ParameterSets.Values.HelpMessage -match '^Deploy-Application\.ps1'}).Name)
    {
        # Return early if the parameter doesn't exist.
        if (!($value = $Cmdlet.SessionState.PSVariable.GetValue($param, $null)))
        {
            continue
        }

        # Return early if the parameter is null or empty.
        if ([System.String]::IsNullOrWhiteSpace((Out-String -InputObject $value)))
        {
            continue
        }

        # Add the parameter to the collector.
        $daParams.Add($param, $value)
    }

    # Return the hashtable to the caller, they'll splat it onto Open-ADTSession.
    return $daParams
}
