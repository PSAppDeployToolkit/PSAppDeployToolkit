function Update-ADTGroupPolicy
{
    <#

    .SYNOPSIS
    Performs a gpupdate command to refresh Group Policies on the local machine.

    .DESCRIPTION
    Performs a gpupdate command to refresh Group Policies on the local machine.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Update-ADTGroupPolicy

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        foreach ($target in ('Computer', 'User'))
        {
            Write-ADTLogEntry -Message "$(($msg = "Updating Group Policies for the $target"))."
            $gpUpdateResult = cmd.exe /c "echo N | gpupdate.exe /Target:$target /Force" 2>&1
            if ($LASTEXITCODE)
            {
                Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$LASTEXITCODE].") -Severity 3
                $naerParams = @{
                    Exception = [System.ApplicationException]::new($msg)
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'GpUpdateFailure'
                    TargetObject = $gpUpdateResult
                    RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                }
                New-ADTErrorRecord @naerParams | Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
