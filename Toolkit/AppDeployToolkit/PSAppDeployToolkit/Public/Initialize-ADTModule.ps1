#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Initialize-ADTModule
{
    [CmdletBinding()]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $adtData = Get-ADTModuleData
    }

    process
    {
        try
        {
            try
            {
                # Initialise the module's global state.
                $adtData.Callbacks.Opening = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]$(
                    $MyInvocation.MyCommand.Module.ExportedCommands.'Enable-ADTTerminalServerInstallMode'
                )
                $adtData.Callbacks.Closing = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]$(
                    $MyInvocation.MyCommand.Module.ExportedCommands.'Unblock-ADTAppExecution'
                    $MyInvocation.MyCommand.Module.ExportedCommands.'Disable-ADTTerminalServerInstallMode'
                )
                $adtData.Environment = New-ADTEnvironmentTable
                $adtData.Config = Import-ADTConfig
                $adtData.Language = Get-ADTStringLanguage
                $adtData.Strings = Import-ADTLocalizedStrings -UICulture $adtData.Language
                $adtData.LastExitCode = 0
                $adtData.Initialised = $true
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
