#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function New-ADTSession
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Install', 'Uninstall', 'Repair')]
        [System.String]$DeploymentType,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Interactive', 'NonInteractive', 'Silent')]
        [System.String]$DeployMode,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$AppVendor,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$AppName,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$AppVersion,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$AppArch,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$AppLang,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$AppRevision,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$AppScriptVersion,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$AppScriptDate,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$AppScriptAuthor,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$InstallName,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$InstallTitle,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$DeployAppScriptFriendlyName,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.Version]$DeployAppScriptVersion,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.String]$DeployAppScriptDate,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.Collections.Hashtable]$DeployAppScriptParameters,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$AllowRebootPassThru,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$TerminalServerMode,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableLogging
    )

    # Clamp the session count at one, for now.
    if ($Script:ADT.Sessions.Count)
    {
        throw [System.InvalidOperationException]::new("Only one $($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name) session is permitted at this time.")
    }

    # Initialise the PSADT environment before instantiating a session.
    Initialize-ADTVariableDatabase
    Import-ADTConfig
    Import-ADTLocalizedStrings

    # Instantiate a new ADT session and initialise it.
    $Script:ADT.Sessions.Add(($Script:ADT.CurrentSession = [ADTSession]::new($PSBoundParameters)))
    try
    {
        $Script:ADT.CurrentSession.Open()
    }
    catch
    {
        [System.Void]$Script:ADT.Sessions.Remove($Script:ADT.CurrentSession)
        $Script:ADT.CurrentSession = $null
        throw
    }

    # Export environment variables to the user's scope.
    Invoke-ScriptBlockInSessionState -SessionState $Cmdlet.SessionState -Arguments $Script:ADT.Environment -ScriptBlock {
        $args[0].GetEnumerator().ForEach({Set-Variable -Name $_.Name -Value $_.Value -Option ReadOnly -Force})
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
        return $Script:ADT.Sessions[-1]
    }
    catch
    {
        throw [System.InvalidOperationException]::new("Please ensure that [New-ADTSession] is called before using any $($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name) functions.")
    }
}
