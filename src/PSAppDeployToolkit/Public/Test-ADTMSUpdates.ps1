#-----------------------------------------------------------------------------
#
# MARK: Test-ADTMSUpdates
#
#-----------------------------------------------------------------------------

function Test-ADTMSUpdates
{
    <#
    .SYNOPSIS
        Test whether a Microsoft Windows update is installed.

    .DESCRIPTION
        This function checks if a specified Microsoft Windows update, identified by its KB number, is installed on the local machine. It first attempts to find the update using the Get-HotFix cmdlet and, if unsuccessful, uses a COM object to search the update history.

    .PARAMETER KbNumber
        KBNumber of the update.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if the update is installed, otherwise returns $false.

    .EXAMPLE
        Test-ADTMSUpdates -KBNumber 'KB2549864'

        Checks if the Microsoft Update 'KB2549864' is installed and returns true or false.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter the KB Number for the Microsoft Update')]
        [ValidateNotNullOrEmpty()]
        [System.String]$KbNumber
    )

    begin
    {
        # Make this function continue on error.
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Checking if Microsoft Update [$KbNumber] is installed."
        try
        {
            try
            {
                # Attempt to get the update via Get-HotFix first as it's cheap.
                if (!($kbFound = !!(& $Script:CommandTable.'Get-HotFix' -Id $KbNumber -ErrorAction Ignore)))
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Unable to detect Windows update history via Get-Hotfix cmdlet. Trying via COM object.'
                    $updateSearcher = (& $Script:CommandTable.'New-Object' -ComObject Microsoft.Update.Session).CreateUpdateSearcher()
                    $updateSearcher.IncludePotentiallySupersededUpdates = $false
                    $updateSearcher.Online = $false
                    if (($updateHistoryCount = $updateSearcher.GetTotalHistoryCount()) -gt 0)
                    {
                        $kbFound = !!($updateSearcher.QueryHistory(0, $updateHistoryCount) | & { process { if (($_.Operation -ne 'Other') -and ($_.Title -match "\($KBNumber\)") -and ($_.Operation -eq 1) -and ($_.ResultCode -eq 2)) { return $_ } } } | & $Script:CommandTable.'Select-Object' -First 1)
                    }
                    else
                    {
                        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Unable to detect Windows Update history via COM object.'
                        return
                    }
                }

                # Return result.
                if ($kbFound)
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Microsoft Update [$KbNumber] is installed."
                    return $true
                }
                & $Script:CommandTable.'Write-ADTLogEntry' -Message "Microsoft Update [$KbNumber] is not installed."
                return $false
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed discovering Microsoft Update [$kbNumber]."
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
