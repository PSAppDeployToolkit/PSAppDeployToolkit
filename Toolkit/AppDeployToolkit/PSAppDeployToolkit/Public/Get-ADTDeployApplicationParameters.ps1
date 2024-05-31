function Get-ADTDeployApplicationParameters
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Throw if called outside of AppDeployToolkitMain.ps1.
    if (!(Get-PSCallStack).Command.Contains('AppDeployToolkitMain.ps1'))
    {
        throw [System.InvalidOperationException]::new("The function [$($MyInvocation.MyCommand.Name)] is only supported for legacy Deploy-Application.ps1 scripts.")
    }

    # Open hashtable for returning at the end. We return it even if it's empty.
    $daParams = @{Cmdlet = $Cmdlet}

    # Get all relevant parameters from the targeted function, then check whether they're defined and not empty.
    foreach ($param in (Get-Item -LiteralPath Function:Open-ADTSession).Parameters.Values.Where({$_.ParameterSets.Values.HelpMessage -match '^Deploy-Application\.ps1'}).Name)
    {
        # Return early if the parameter doesn't exist.
        if (!($value = $Cmdlet.SessionState.PSVariable.GetValue($param, $null)))
        {
            continue
        }

        # Return early if the parameter is null or empty.
        if ([System.String]::IsNullOrWhiteSpace((Out-String -InputObject $value)))
        {
            continue
        }

        # Add the parameter to the collector.
        $daParams.Add($param, $value)
    }

    # Return the hashtable to the caller, they'll splat it onto Open-ADTSession.
    return $daParams
}
