function Test-ADTMSUpdates
{
    <#

    .SYNOPSIS
    Test whether a Microsoft Windows update is installed.

    .DESCRIPTION
    Test whether a Microsoft Windows update is installed.

    .PARAMETER KbNumber
    KBNumber of the update.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.Boolean. Returns $true if the update is installed, otherwise returns $false.

    .EXAMPLE
    Test-ADTMSUpdates -KBNumber 'KB2549864'

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter the KB Number for the Microsoft Update')]
        [ValidateNotNullOrEmpty()]
        [System.String]$KbNumber
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        Write-ADTLogEntry -Message "Checking if Microsoft Update [$KbNumber] is installed."
        try
        {
            try
            {
                # Attempt to get the update via Get-HotFix first as it's cheap.
                if (!($kbFound = !!(Get-HotFix -Id $KbNumber -ErrorAction Ignore)))
                {
                    Write-ADTLogEntry -Message 'Unable to detect Windows update history via Get-Hotfix cmdlet. Trying via COM object.'
                    $updateSearcher = (New-Object -ComObject Microsoft.Update.Session).CreateUpdateSearcher()
                    $updateSearcher.IncludePotentiallySupersededUpdates = $false
                    $updateSearcher.Online = $false
                    if (($updateHistoryCount = $updateSearcher.GetTotalHistoryCount()) -gt 0)
                    {
                        $kbFound = !!($updateSearcher.QueryHistory(0, $updateHistoryCount) | Where-Object {($_.Operation -ne 'Other') -and ($_.Title -match "\($KBNumber\)") -and ($_.Operation -eq 1) -and ($_.ResultCode -eq 2)})
                    }
                    else
                    {
                        Write-ADTLogEntry -Message 'Unable to detect Windows Update history via COM object.'
                        return
                    }
                }

                # Return result.
                if ($kbFound)
                {
                    Write-ADTLogEntry -Message "Microsoft Update [$KbNumber] is installed."
                    return $true
                }
                Write-ADTLogEntry -Message "Microsoft Update [$KbNumber] is not installed."
                return $false
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed discovering Microsoft Update [$kbNumber]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
