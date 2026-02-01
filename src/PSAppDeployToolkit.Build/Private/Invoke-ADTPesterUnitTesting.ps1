#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTPesterUnitTesting
#
#-----------------------------------------------------------------------------

function Invoke-ADTPesterUnitTesting
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Perform unit testing.
        Write-ADTBuildLogEntry -Message "Commencing unit tests with Pester $((Get-Module -Name Pester).Version), this may take while."
        $null = [System.IO.Directory]::CreateDirectory($Script:ModuleConstants.Paths.TestOutput)
        $null = [System.IO.Directory]::CreateDirectory($Script:ModuleConstants.Paths.CodeCoverageOutput)
        $pesterConfig = New-PesterConfiguration
        $pesterConfig.Run.Path = $ModuleConstants.Paths.UnitTests
        $pesterConfig.Run.PassThru = $true
        $pesterConfig.Run.Exit = $false
        $pesterConfig.CodeCoverage.Enabled = $true
        $pesterConfig.CodeCoverage.Path = "$($ModuleConstants.Paths.ModuleSource)\*\*.ps1"
        $pesterConfig.CodeCoverage.CoveragePercentTarget = 100
        $pesterConfig.CodeCoverage.OutputPath = "$($ModuleConstants.Paths.CodeCoverageOutput)\CodeCoverage.xml"
        $pesterConfig.CodeCoverage.OutputFormat = 'JaCoCo'
        $pesterConfig.TestResult.Enabled = $true
        $pesterConfig.TestResult.OutputPath = "$($ModuleConstants.Paths.TestOutput)\PesterTests.xml"
        $pesterConfig.TestResult.OutputFormat = 'NUnitXML'
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
