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
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Convert the Username field into a RunAsActiveUser object as required by the subsystem.
    $gacsuParams = @{}; if ($Cmdlet.get_MyInvocation().get_BoundParameters().ContainsKey('Username'))
    {
        $gacsuParams.Add('Username', $Cmdlet.get_MyInvocation().get_BoundParameters().Username)
        $gacsuParams.Add('AllowAnyValidSession', $true)
    }
    if (!($Cmdlet.get_MyInvocation().get_BoundParameters().RunAsActiveUser = Get-ADTClientServerUser @gacsuParams))
    {
        if (!$Cmdlet.get_MyInvocation().get_BoundParameters().ContainsKey('ContinueWhenNoUserLoggedOn') -or !$Cmdlet.get_MyInvocation().get_BoundParameters().ContinueWhenNoUserLoggedOn)
        {
            try
            {
                $naerParams = @{
                    Exception = [System.InvalidOperationException]::new("Could not find a valid logged on user session$(if ($Cmdlet.get_MyInvocation().get_BoundParameters().ContainsKey('Username')) { " for [$($Cmdlet.get_MyInvocation().get_BoundParameters().Username)]" }).")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'NoActiveUserError'
                    TargetObject = $(if ($Cmdlet.get_MyInvocation().get_BoundParameters().ContainsKey('Username')) { $Cmdlet.get_MyInvocation().get_BoundParameters().Username })
                    RecommendedAction = "Please re-run this command while a user is logged onto the device and try again."
                }
                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $Cmdlet -SessionState $ExecutionContext.get_SessionState() -ErrorRecord $_
                return $false
            }
        }
        else
        {
            Write-ADTLogEntry -Message "Could not find a valid logged on user session and [-ContinueWhenNoUserLoggedOn] specified, returning early." -Severity Warning
            return $false
        }
    }
    $null = $Cmdlet.get_MyInvocation().get_BoundParameters().Remove('ContinueWhenNoUserLoggedOn')
    $null = $Cmdlet.get_MyInvocation().get_BoundParameters().Remove('Username')
    return $true
}
