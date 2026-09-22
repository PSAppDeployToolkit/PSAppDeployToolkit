function Resolve-FluenceClickedButton
{
    <#
    .SYNOPSIS
        Returns the name of the button that caused the dialog to close.
    .DESCRIPTION
        Given a dialog result hashtable and the button list, resolves which button was clicked.
        If a non-cancel button's flag is true in Result, returns that button's Name.
        If Result.TimedOut is true and -DefaultButton names a button, returns that name.
        If Result.Cancelled or Result.TimedOut is true and a cancel button exists in the set, returns
        its Name; with no cancel button the last (right-most, safe) button's Name is returned.
        Otherwise returns $null (window closed without a recognized action).
    .PARAMETER Result
        The raw result hashtable (or Fluence.DialogResult) from Show-FluenceDialog.
    .PARAMETER Buttons
        The array of Fluence.Button objects that were passed to Show-FluenceDialog.
    .PARAMETER DefaultButton
        The button name a timeout resolves to. When omitted, a timeout maps like a dismissal.
    .OUTPUTS
        System.String
    .NOTES
        Does not require a host application; pure logic helper for Show-FluenceMessage.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [object]$Result,

        [Parameter(Mandatory = $true)]
        [object[]]$Buttons,

        [Parameter()]
        [string]$DefaultButton
    )

    # Normalize Result to a plain hashtable so the same logic works for both
    # a raw [hashtable] (unit tests) and a [pscustomobject] / Fluence.DialogResult
    # (the runtime shape returned by ConvertTo-FluenceResult).
    $values = @{}
    if ($Result -is [System.Collections.IDictionary])
    {
        foreach ($key in $Result.Keys)
        {
            $values[$key] = $Result[$key]
        }
    }
    else
    {
        foreach ($property in $Result.PSObject.Properties)
        {
            $values[$property.Name] = $property.Value
        }
    }

    # Check non-cancel buttons first: if any flag is true, that button was clicked.
    foreach ($button in $Buttons)
    {
        if (-not $button.IsCancel)
        {
            $flag = $values[$button.Name]
            if ($flag -eq $true)
            {
                return $button.Name
            }
        }
    }

    # A timeout resolves to the caller's chosen default when one is named. Without one it maps like
    # a dismissal below, so an unattended dialog still lands on the safe choice.
    $timedOut = ($values['TimedOut'] -eq $true)
    if ($timedOut -and -not [string]::IsNullOrWhiteSpace($DefaultButton))
    {
        return $DefaultButton
    }

    # Dismissal path (title-bar X, Esc, or an unattended timeout).
    $isCancelled = ($values['Cancelled'] -eq $true)
    if ($isCancelled -or $timedOut)
    {
        $cancelButton = $Buttons | Where-Object { $_.IsCancel -eq $true } | Select-Object -First 1
        if ($null -ne $cancelButton)
        {
            return $cancelButton.Name
        }

        # No explicit cancel button (for example the YesNo or OK preset): a dismissal maps to the
        # safe/negative choice, the last (right-most) button in the set, by the Win32 convention that
        # the dismissing action sits last (YesNo -> No, OK -> OK). This avoids returning $null, which
        # a caller guard like `if ($answer -ne 'No')` would treat as "proceed" and run a destructive
        # branch on a dismiss.
        if ($Buttons.Count -ge 1)
        {
            return $Buttons[$Buttons.Count - 1].Name
        }
    }

    return $null
}
