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
                $adtData.Callbacks.Starting = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                $adtData.Callbacks.Opening = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]$(
                    $MyInvocation.MyCommand.Module.ExportedCommands.'Enable-ADTTerminalServerInstallMode'
                )
                $adtData.Callbacks.Closing = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]$(
                    $MyInvocation.MyCommand.Module.ExportedCommands.'Disable-ADTTerminalServerInstallMode'
                )
                $adtData.Callbacks.Finishing = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]$(
                    $MyInvocation.MyCommand.Module.ExportedCommands.'Unblock-ADTAppExecution'
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
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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
