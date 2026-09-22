function Resolve-FluenceUserBlock
{
    <#
    .SYNOPSIS
        Selects the live user scriptblock when the UI thread shares the caller's runspace, otherwise
        recreates it from text so it binds to the UI runspace's module session state.
    .DESCRIPTION
        On an inline-STA or runspace-sharing foreign host the live block is returned with closures and
        caller scope intact. On an MTA host the UI thread is a separate module-owned runspace, so the
        block is recreated from its text (closures and caller-scope references are not available there).
    .PARAMETER LiveBlock
        The original scriptblock as transported (a live object; valid only on the caller's runspace).
    .PARAMETER BlockText
        The .ToString() text of the block, captured at the caller.
    .PARAMETER CallerRunspaceId
        The InstanceId of the runspace that called Show-FluenceWindow.
    .NOTES
        Runs on the UI thread inside Set-FluenceWindowContent.
    #>
    [CmdletBinding()]
    [OutputType([scriptblock])]
    param
    (
        [Parameter()]
        [scriptblock]$LiveBlock,

        [Parameter()]
        [string]$BlockText,

        [Parameter(Mandatory = $true)]
        [guid]$CallerRunspaceId
    )

    $current = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace
    if ($null -ne $current -and $current.InstanceId -eq $CallerRunspaceId -and $null -ne $LiveBlock)
    {
        return $LiveBlock
    }
    if ([string]::IsNullOrWhiteSpace($BlockText))
    {
        return $null
    }
    return [scriptblock]::Create($BlockText)
}
