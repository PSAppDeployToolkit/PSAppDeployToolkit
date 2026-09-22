function Close-FluenceProgress
{
    <#
    .SYNOPSIS
        Closes a progress window opened by Show-FluenceProgress.
    .DESCRIPTION
        Closes the window on its UI thread and marks the handle closed. Closing an already closed
        handle is a no-op, so a finally block can call this unconditionally. On an MTA host this also
        ends the UI pump that kept the window live and releases the module's UI runspace for the next
        dialog.
    .PARAMETER Handle
        The Fluence.ProgressHandle returned by Show-FluenceProgress.
    .EXAMPLE
        try { $progress = Show-FluenceProgress -Message 'Working...'; Invoke-Work } finally { Close-FluenceProgress -Handle $progress }
    .NOTES
        Runs on the UI thread that owns the window. A window implies a running Application, so no
        application is created here.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Closes a transient in-process window the caller opened; makes no persistent or destructive system change.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [PSTypeName('Fluence.ProgressHandle')]
        [object]$Handle
    )

    $ownsPump = $null -ne $script:ProgressHandle -and $script:ProgressHandle.Id -eq $Handle.Id
    if (-not $Handle.IsOpen -and -not $ownsPump)
    {
        return
    }

    if ($Handle.Mode -eq 'Runspace' -and $ownsPump -and $null -ne $script:UiPump)
    {
        $pump = $script:UiPump
        $Handle.State.CloseRequested = $true
        if (-not $pump.AsyncResult.AsyncWaitHandle.WaitOne(5000))
        {
            throw 'The Fluence UI thread did not finish closing within five seconds. Its close request remains pending; retry after the active dialog or callback returns.'
        }
        try
        {
            $null = $pump.PowerShell.EndInvoke($pump.AsyncResult)
        }
        finally
        {
            $pump.PowerShell.Dispose()
            $pump.Shown.Dispose()
            $script:UiPump = $null
            $Handle.IsOpen = $false
            $script:ProgressHandle = $null
        }
        return
    }

    $null = Invoke-OnFluenceUi -Script {
        param($h)
        $h.State.CloseRequested = $true
        if ($null -ne $h.Parts -and $h.Parts.Window.IsVisible)
        {
            $h.Parts.Window.Close()
        }
        if ($h.Mode -eq 'Inline')
        {
            Invoke-FluenceDispatcherPump
        }
    } -ArgumentList @($Handle)
    $Handle.IsOpen = $false
    if ($null -ne $script:ProgressHandle -and $script:ProgressHandle.Id -eq $Handle.Id)
    {
        $script:ProgressHandle = $null
    }
}
