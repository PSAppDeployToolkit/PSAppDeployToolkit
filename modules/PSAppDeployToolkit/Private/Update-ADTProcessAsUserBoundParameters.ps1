#-----------------------------------------------------------------------------
#
# MARK: Update-ADTProcessAsUserBoundParameters
#
#-----------------------------------------------------------------------------

function Private:Update-ADTProcessAsUserBoundParameters
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.Dictionary[System.String, System.Object]]$BoundParameters
    )

    # Convert the Username field into a RunAsActiveUser object as required by the subsystem.
    $gacsuParams = @{}; if ($BoundParameters.ContainsKey('Username'))
    {
        $gacsuParams.Add('Username', $BoundParameters.Username)
        $gacsuParams.Add('AllowAnyValidSession', $true)
    }
    if (!($BoundParameters.RunAsActiveUser = Get-ADTClientServerUser @gacsuParams))
    {
        if (!$BoundParameters.ContainsKey('ContinueWhenNoUserLoggedOn') -or !$BoundParameters.ContinueWhenNoUserLoggedOn)
        {
            try
            {
                $naerParams = @{
                    Exception = [System.InvalidOperationException]::new("Could not find a valid logged on user session$(if ($BoundParameters.ContainsKey('Username')) { " for [$($BoundParameters.Username)]" }).")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'NoActiveUserError'
                    TargetObject = $(if ($BoundParameters.ContainsKey('Username')) { $BoundParameters.Username })
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
    $null = $BoundParameters.Remove('ContinueWhenNoUserLoggedOn')
    $null = $BoundParameters.Remove('Username')
    return $true
}
