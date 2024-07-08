function Initialize-ADTModule
{
    [CmdletBinding()]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Initialise the module's global state.
                $adtData = Get-ADTModuleData
                Initialize-ADTEnvironment
                Import-ADTConfig
                Import-ADTLocalizedStrings
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
