#-----------------------------------------------------------------------------
#
# The script module half of the test fixture.
#
# LogUtilities reaches PowerShell commands through $Script:CommandTable and invokes them via
# ModuleDatabase.InvokeScript, so a test's session state has to be a real module's or logging fails.
#
# Only the commands on a path under test are resolved here, and they are resolved eagerly so that a
# missing one fails at import with a clear error rather than midway through a test.
#
#-----------------------------------------------------------------------------

Set-StrictMode -Version 3

New-Variable -Name CommandTable -Option Constant -Value ([System.Collections.Generic.Dictionary[System.String, System.Management.Automation.CommandInfo]]::new([System.StringComparer]::OrdinalIgnoreCase))

# Get-PSCallStack resolves the caller for a log entry written while a runspace is open. The three
# Write-* commands are the host and verbose output streams LogUtilities writes through.
@(
    'Get-PSCallStack'
    'Write-Host'
    'Write-Verbose'
    'Write-Warning'
) | & {
    process
    {
        $CommandTable.Add($_, (Get-Command -Name $_ -ErrorAction Stop))
    }
}

function Get-FixtureSessionState
{
    <#
    .SYNOPSIS
        Returns this module's session state, for a test to place in the module database.

    .DESCRIPTION
        $ExecutionContext.SessionState inside a module function is the module's own session state,
        which is the only kind that can resolve $Script:CommandTable. Handing it out is the whole
        purpose of this module.

    .OUTPUTS
        System.Management.Automation.SessionState
    #>

    [CmdletBinding()]
    [OutputType([System.Management.Automation.SessionState])]
    param
    (
    )

    return $ExecutionContext.SessionState
}

function Get-FixtureCommandTable
{
    <#
    .SYNOPSIS
        Returns the command table this module resolved, so a test can assert what is in it.

    .OUTPUTS
        System.Collections.Generic.Dictionary`2[[System.String],[System.Management.Automation.CommandInfo]]
    #>

    [CmdletBinding()]
    [OutputType([System.Collections.Generic.Dictionary[System.String, System.Management.Automation.CommandInfo]])]
    param
    (
    )

    return $CommandTable
}

Export-ModuleMember -Function Get-FixtureSessionState, Get-FixtureCommandTable
