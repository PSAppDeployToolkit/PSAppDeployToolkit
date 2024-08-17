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
                $adtData.Callbacks.Starting.Clear()
                $adtData.Callbacks.Opening.Clear()
                $adtData.Callbacks.Closing.Clear()
                $adtData.Callbacks.Finishing.Clear()
                $adtData.Sessions.Clear()
                $adtData.Environment = New-ADTEnvironmentTable
                $adtData.Config = Import-ADTConfig
                $adtData.Language = Get-ADTStringLanguage
                $adtData.Strings = Import-ADTStringTable -UICulture $adtData.Language
                $adtData.LastExitCode = 0
                $adtData.TerminalServerMode = $false
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
