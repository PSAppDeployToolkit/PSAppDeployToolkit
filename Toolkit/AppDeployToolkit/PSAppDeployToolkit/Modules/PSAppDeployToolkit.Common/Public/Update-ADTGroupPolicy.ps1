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
    Param (
    )

    begin {
        # Make this function continue on error.
        $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
        if (!$PSBoundParameters.ContainsKey('ErrorAction'))
        {
            $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Continue
        }
        Write-ADTDebugHeader
    }

    process {
        foreach ($target in ('Computer', 'User'))
        {
            Write-ADTLogEntry -Message "$(($msg = "Updating Group Policies for the $target"))."
            [System.Void](cmd.exe /c "echo N | gpupdate.exe /Target:$target /Force")
            if ($LASTEXITCODE -and ($PSBoundParameters.ErrorAction -notmatch '^(Ignore|SilentlyContinue)$'))
            {
                Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$LASTEXITCODE].") -Severity 3
                if ($PSBoundParameters.ErrorAction.Equals([System.Management.Automation.ActionPreference]::Stop))
                {
                    throw $msg
                }
            }
        }
    }

    end {
        Write-ADTDebugFooter
    }
}
