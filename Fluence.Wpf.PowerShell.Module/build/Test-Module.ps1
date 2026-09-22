<#
.SYNOPSIS
    Runs the Fluence.Wpf.PowerShell quality gate: PSScriptAnalyzer plus the Pester logic lane.
.DESCRIPTION
    Two gates, in order:

      1. PSScriptAnalyzer 1.25.0 over src/, build/, examples/ and tests/ with the shipped
         PSScriptAnalyzerSettings.psd1, plus a second pass for PSPlaceOpenBrace (a formatting rule,
         which the default rule set leaves out) so the Allman brace style is enforced rather than
         merely documented. Any Error or Warning finding from either pass fails the run.
      2. The Pester 5.8.0 suite under tests/. By default the UI-tagged cases (real windows) are
         excluded; -IncludeUi sets FLUENCE_PS_UI=1 and runs them too.

    The Fluence.Wpf assemblies must already be staged under src/Fluence.Wpf.PowerShell/lib (see
    Build-Module.ps1); the Pester lane imports the module and fails fast without them.

    Runs on Windows PowerShell 5.1 and PowerShell 7.4+. Run it under both to cover both editions.
.PARAMETER IncludeUi
    Also run the UI-tagged Pester cases. They open and self-close real windows, so run this on an
    interactive desktop, one batch at a time.
.PARAMETER SkipAnalyzer
    Skip the PSScriptAnalyzer gate (Pester only).
.PARAMETER SkipPester
    Skip the Pester gate (analyzer only).
.PARAMETER TestResultPath
    Optional path for an NUnit 2.5 XML test-result file (for CI upload).
.EXAMPLE
    pwsh -NoProfile -File build/Test-Module.ps1
.EXAMPLE
    powershell.exe -NoProfile -STA -File build/Test-Module.ps1 -IncludeUi
.NOTES
    Run from any location. Does not require a host application. Exits 1 on any analyzer finding or
    Pester failure so a CI step fails closed. Run in a dedicated process: after Pester, the
    standalone runner shuts down a WPF dispatcher owned by its thread. Remaining registered
    windows fail the gate before terminal cleanup; an MTA dispatcher is handled by the module
    when the primary console session exits.
#>
[CmdletBinding()]
param
(
    [Parameter()]
    [switch]$IncludeUi,

    [Parameter()]
    [switch]$SkipAnalyzer,

    [Parameter()]
    [switch]$SkipPester,

    [Parameter()]
    [string]$TestResultPath
)

$ErrorActionPreference = 'Stop'
$moduleRoot = Split-Path $PSScriptRoot -Parent
$failed = $false

# Pester 5.8.0 is imported before the analyzer runs, even for an analyzer-only run. PSScriptAnalyzer
# resolves the commands it sees through Get-Command, and Get-Command on Describe or It autoloads the
# newest installed Pester (6.x on a machine that has it). Pester 5 then fails to import because an
# assembly with the same name is already loaded. Importing 5.8.0 first pins the resolution.
Import-Module Pester -RequiredVersion 5.8.0 -ErrorAction Stop

if (-not $SkipAnalyzer)
{
    Import-Module PSScriptAnalyzer -RequiredVersion 1.25.0 -ErrorAction Stop
    $settings = Join-Path $moduleRoot 'PSScriptAnalyzerSettings.psd1'
    $targets = @('src', 'build', 'examples', 'tests') | ForEach-Object { Join-Path $moduleRoot $_ }

    Write-Output "PSScriptAnalyzer $((Get-Module PSScriptAnalyzer).Version) with $settings"
    # The Allman brace requirement is a PSScriptAnalyzer FORMATTING rule, and formatting rules are
    # not part of the default rule set. It cannot go in PSScriptAnalyzerSettings.psd1 either:
    # IncludeRules there replaces the run set rather than adding to it, which would silence every
    # default rule. A second pass with -IncludeRule is what makes the handbook's brace style a gate.
    $braceRule = @{
        Rules = @{
            PSPlaceOpenBrace = @{
                Enable             = $true
                OnSameLine         = $false
                NewLineAfter       = $true
                IgnoreOneLineBlock = $true
            }
        }
    }

    $findings = @()
    foreach ($target in $targets)
    {
        $findings += @(Invoke-ScriptAnalyzer -Path $target -Recurse -Settings $settings)
        $findings += @(Invoke-ScriptAnalyzer -Path $target -Recurse -Settings $braceRule -IncludeRule 'PSPlaceOpenBrace')
    }

    if ($findings.Count -gt 0)
    {
        $failed = $true
        $findings |
            Sort-Object ScriptPath, Line |
            ForEach-Object { Write-Output ("  {0} {1}:{2} {3}: {4}" -f $_.Severity, $_.ScriptName, $_.Line, $_.RuleName, $_.Message) }
        Write-Output "PSScriptAnalyzer: $($findings.Count) finding(s)."
    }
    else
    {
        Write-Output 'PSScriptAnalyzer: no findings.'
    }
}

if (-not $SkipPester)
{
    $previousUi = $env:FLUENCE_PS_UI
    try
    {
        if ($IncludeUi)
        {
            $env:FLUENCE_PS_UI = '1'
        }

        $configuration = New-PesterConfiguration
        $configuration.Run.Path = Join-Path $moduleRoot 'tests'
        $configuration.Run.PassThru = $true
        $configuration.Output.Verbosity = 'Detailed'
        if (-not $IncludeUi)
        {
            $configuration.Filter.ExcludeTag = 'UI'
        }
        if (-not [string]::IsNullOrWhiteSpace($TestResultPath))
        {
            $configuration.TestResult.Enabled = $true
            $configuration.TestResult.OutputFormat = 'NUnitXml'
            $configuration.TestResult.OutputPath = $TestResultPath
        }

        $apartment = [System.Threading.Thread]::CurrentThread.GetApartmentState()
        Write-Output "Pester $((Get-Module Pester).Version) on $PSEdition ($apartment); UI lane: $([bool]$IncludeUi)"
        $result = Invoke-Pester -Configuration $configuration

        Write-Output ("Pester: total {0}, passed {1}, failed {2}, skipped {3}, not run {4}." -f
            $result.TotalCount, $result.PassedCount, $result.FailedCount, $result.SkippedCount, $result.NotRunCount)
        # $result.Result also catches a discovery failure, which produces no failed case and so slips
        # past FailedCount: a file that cannot be discovered simply contributes nothing to the totals.
        if ($result.FailedCount -gt 0 -or $result.TotalCount -eq 0 -or $result.Result -ne 'Passed')
        {
            $failed = $true
        }
    }
    finally
    {
        $env:FLUENCE_PS_UI = $previousUi

        # This executable test gate owns its process lifetime. An inline dispatcher must finish
        # while its pipeline thread still exists, before the explicit exit below tears it down.
        # The shipped module preserves this dispatcher across imports and does not own that choice.
        $applicationType = 'System.Windows.Application' -as [type]
        if ($null -ne $applicationType)
        {
            $application = $applicationType::Current
            if ($null -ne $application -and $application.Dispatcher.CheckAccess())
            {
                try
                {
                    if ($application.Windows.Count -gt 0)
                    {
                        $failed = $true
                        Write-Output "Pester cleanup: FAILED; $($application.Windows.Count) registered WPF window(s) remain."
                    }
                    if (-not $application.Dispatcher.HasShutdownStarted)
                    {
                        $application.Dispatcher.InvokeShutdown()
                    }
                    if (-not $application.Dispatcher.HasShutdownFinished)
                    {
                        $failed = $true
                        Write-Output 'Pester cleanup: FAILED; the inline WPF dispatcher did not finish shutdown.'
                    }
                }
                catch
                {
                    $failed = $true
                    Write-Output "Pester cleanup: FAILED; $_"
                }
            }
        }
    }
}

if ($failed)
{
    Write-Output 'Test-Module: FAILED'
    exit 1
}

Write-Output 'Test-Module: passed'
exit 0
