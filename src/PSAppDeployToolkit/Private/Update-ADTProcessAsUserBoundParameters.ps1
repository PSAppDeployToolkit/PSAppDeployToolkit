#-----------------------------------------------------------------------------
#
# MARK: Update-ADTProcessAsUserBoundParameters
#
#-----------------------------------------------------------------------------

function Update-ADTProcessAsUserBoundParameters
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Convert the Username field into a RunAsActiveUser object as required by the subsystem.
    $gacsuParams = @{}; if ($Cmdlet.MyInvocation.BoundParameters.ContainsKey('Username'))
    {
        $gacsuParams.Add('Username', $Cmdlet.MyInvocation.BoundParameters.Username)
        $gacsuParams.Add('AllowAnyValidSession', $true)
    }
    if (!($Cmdlet.MyInvocation.BoundParameters.RunAsActiveUser = Get-ADTClientServerUser @gacsuParams))
    {
        if (!$Cmdlet.MyInvocation.BoundParameters.ContainsKey('ContinueWhenNoUserLoggedOn') -or !$Cmdlet.MyInvocation.BoundParameters.ContinueWhenNoUserLoggedOn)
        {
            try
            {
                $naerParams = @{
                    Exception = [System.ArgumentNullException]::new("Could not find a valid logged on user session$(if ($Cmdlet.MyInvocation.BoundParameters.ContainsKey('Username')) { " for [$($Cmdlet.MyInvocation.BoundParameters.Username)]" }).", $null)
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'NoActiveUserError'
                    TargetObject = $(if ($Cmdlet.MyInvocation.BoundParameters.ContainsKey('Username')) { $Cmdlet.MyInvocation.BoundParameters.Username })
                    RecommendedAction = "Please re-run this command while a user is logged onto the device and try again."
                }
                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $Cmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
                return $false
            }
        }
        else
        {
            Write-ADTLogEntry -Message "Could not find a valid logged on user session and [-ContinueWhenNoUserLoggedOn] specified, returning early." -Severity Warning
            return $false
        }
    }
    $null = $Cmdlet.MyInvocation.BoundParameters.Remove('ContinueWhenNoUserLoggedOn')
    $null = $Cmdlet.MyInvocation.BoundParameters.Remove('Username')
    return $true
}
