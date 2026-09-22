function Invoke-OnFluenceUi
{
    <#
    .SYNOPSIS
        Runs a script on a UI (STA) thread that owns the single WPF Application.
    .DESCRIPTION
        Get-FluenceUiHostMode chooses Inline for the application's own STA thread or Runspace for
        the module-owned STA thread. A foreign application on another thread is rejected before
        dispatch because a PowerShell callback cannot safely run without its execution context.

        Returns the script's result as a bare value in every case. Only the runspace path wraps the
        output in a single-element PSDataCollection (PowerShell.Invoke); that is unwrapped here so
        callers never re-implement the unwrap.
    .PARAMETER Script
        The script to run on the UI thread. On the runspace path it is transported by text, so it
        cannot capture the caller's variables, functions or closures.
    .PARAMETER ArgumentList
        Positional arguments bound to the script's param block.
    .NOTES
        Does not itself require a host application; it establishes one. State flag $script:OwnsApplication
        keeps routing stable once we have created our own application.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    param
    (
        [Parameter(Mandatory = $true)]
        [scriptblock]$Script,

        [Parameter()]
        [object[]]$ArgumentList = @()
    )

    $mode = Get-FluenceUiHostMode

    if ($mode -eq 'Inline')
    {
        $script:OwnsApplication = $true
        return (& $Script @ArgumentList)
    }

    $script:OwnsApplication = $true
    $output = Invoke-InFluenceStaRunspace -Script $Script -ArgumentList $ArgumentList

    # Normalize the return shape so every case yields the bare value. PowerShell.Invoke wraps output
    # in a PSDataCollection; the script's real payload is a single object (the dialog/window result
    # hashtable), so unwrap a non-empty collection to its last element and an empty collection to
    # $null. This keeps callers free of a duplicated IList-unwrap and lets them null-check the result.
    if ($output -is [System.Collections.IList])
    {
        if ($output.Count -ge 1)
        {
            return $output[$output.Count - 1]
        }
        return $null
    }
    return $output
}
