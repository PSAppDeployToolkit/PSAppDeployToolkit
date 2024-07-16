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
