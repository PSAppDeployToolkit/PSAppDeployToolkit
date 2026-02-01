#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTPesterIntegrationTesting
#
#-----------------------------------------------------------------------------

function Invoke-ADTPesterIntegrationTesting
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Perform unit testing.
        Write-ADTBuildLogEntry -Message "Commencing integration tests with Pester $((Get-Module -Name Pester).Version), this may take while."
        $pesterConfig = New-PesterConfiguration
        $pesterConfig.Run.Path = $ModuleConstants.Paths.IntegrationTests
        $pesterConfig.Run.PassThru = $true
        $pesterConfig.Run.Exit = $false
        $pesterConfig.CodeCoverage.Enabled = $false
        $pesterConfig.TestResult.Enabled = $false
        $pesterConfig.Output.Verbosity = 'Detailed'
        $results = Invoke-Pester -Configuration $pesterConfig 6>&1 | Invoke-ADTPesterOutputHandler

        # Throw if any tests failed.
        if ($results.FailedCount -gt 0)
        {
            throw "One or more unit tests failed which must be addressed."
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
