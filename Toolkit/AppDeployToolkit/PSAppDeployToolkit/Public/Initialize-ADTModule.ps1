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
        # Ensure this function isn't being called mid-flight.
        if (Test-ADTSessionActive)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("This function cannot be called while there is an active ADTSession in progress.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'InitWithActiveSessionError'
                TargetObject = Get-ADTSession
                RecommendedAction = "Please attempt module re-initialisation once the active ADTSession(s) have been closed."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
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
