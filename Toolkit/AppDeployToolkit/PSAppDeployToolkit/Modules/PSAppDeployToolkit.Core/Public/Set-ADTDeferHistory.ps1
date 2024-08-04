function Set-ADTDeferHistory
{
    <#

    .SYNOPSIS
    Set the history of deferrals in the registry for the current application.

    .DESCRIPTION
    Set the history of deferrals in the registry for the current application.

    .PARAMETER DeferTimesRemaining
    Specify the number of deferrals remaining.

    .PARAMETER DeferDeadline
    Specify the deadline for the deferral.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Set-ADTDeferHistory

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$DeferTimesRemaining,

        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [System.String]$DeferDeadline
    )

    begin
    {
        try
        {
            $regKeyDeferHistory = (Get-ADTSession).RegKeyDeferHistory
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                if ($PSBoundParameters.ContainsKey('DeferTimesRemaining'))
                {
                    Write-ADTLogEntry -Message "Setting deferral history: [DeferTimesRemaining = $DeferTimesRemaining]."
                    Set-ADTRegistryKey -Key $regKeyDeferHistory -Name 'DeferTimesRemaining' -Value $DeferTimesRemaining -ErrorAction Ignore
                }
                if (![System.String]::IsNullOrWhiteSpace($DeferDeadline))
                {
                    Write-ADTLogEntry -Message "Setting deferral history: [DeferDeadline = $DeferDeadline]."
                    Set-ADTRegistryKey -Key $regKeyDeferHistory -Name 'DeferDeadline' -Value $DeferDeadline -ErrorAction Ignore
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
