function Initialize-ADTFunction
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState
    )

    # Ensure this function always stops, no matter what.
    $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop

    # Internal worker function to set variables within the caller's scope.
    function Set-CallerVariable
    {
        [CmdletBinding()]
        param (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.String]$Name,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Object]$Value
        )

        # Directly go up the scope tree if its an in-session function.
        if ($SessionState.Equals($ExecutionContext.SessionState))
        {
            Set-Variable -Name $Name -Value $Value -Scope 2 -Force -Confirm:$false -WhatIf:$false
        }
        else
        {
            $SessionState.PSVariable.Set($Name, $Value)
        }
    }

    # Write debug log messages.
    Write-ADTLogEntry -Message 'Function Start' -Source $Cmdlet.MyInvocation.MyCommand.Name -DebugMessage
    if ($CmdletBoundParameters = $Cmdlet.MyInvocation.BoundParameters | Format-Table -Property @{ Label = 'Parameter'; Expression = { "[-$($_.Key)]" } }, @{ Label = 'Value'; Expression = { $_.Value }; Alignment = 'Left' }, @{ Label = 'Type'; Expression = { $_.Value.GetType().Name }; Alignment = 'Left' } -AutoSize -Wrap | Out-String)
    {
        Write-ADTLogEntry -Message "Function invoked with bound parameter(s):`n$CmdletBoundParameters" -Source $Cmdlet.MyInvocation.MyCommand.Name -DebugMessage
    }
    else
    {
        Write-ADTLogEntry -Message 'Function invoked without any bound parameters.' -Source $Cmdlet.MyInvocation.MyCommand.Name -DebugMessage
    }

    # Amend the caller's $ErrorActionPreference to archive off their provided value so we can always stop on a dime.
    # For the caller-provided values, we deliberately use a string value to escape issues when 'Ignore' is passed.
    # https://github.com/PowerShell/PowerShell/issues/1759#issuecomment-442916350
    Set-CallerVariable -Name OriginalErrorAction -Value $(if ($Cmdlet.MyInvocation.BoundParameters.ContainsKey('ErrorAction'))
    {
        # Caller's value directly against the function.
        $Cmdlet.MyInvocation.BoundParameters.ErrorAction.ToString()
    }
    elseif ($PSBoundParameters.ContainsKey('ErrorAction'))
    {
        # A function's own specified override.
        $PSBoundParameters.ErrorAction.ToString()
    }
    else
    {
        # The module's default ErrorActionPreference.
        $Script:ErrorActionPreference
    })
    Set-CallerVariable -Name ErrorActionPreference -Value $Script:ErrorActionPreference

    # Handle the caller's -Verbose parameter, which doesn't always work between them and the module barrier.
    # https://github.com/PowerShell/PowerShell/issues/4568
    if ($Cmdlet.MyInvocation.BoundParameters.ContainsKey('Verbose'))
    {
        $Cmdlet.SessionState.PSVariable.Set('OriginalVerbosity', $Global:VerbosePreference)
        $Global:VerbosePreference = [System.Management.Automation.ActionPreference]::Continue
    }
}
