#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Update-ADTSessionEnvironmentVariables
{
    <#

    .SYNOPSIS
    Updates the environment variables for the current PowerShell session with any environment variable changes that may have occurred during script execution.

    .DESCRIPTION
    Environment variable changes that take place during script execution are not visible to the current PowerShell session.

    Use this function to refresh the current PowerShell session with all environment variable settings.

    .PARAMETER LoadLoggedOnUserEnvironmentVariables
    If script is running in SYSTEM context, this option allows loading environment variables from the active console user. If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return objects.

    .EXAMPLE
    Update-ADTSessionEnvironmentVariables

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$LoadLoggedOnUserEnvironmentVariables
    )

    begin
    {
        # Perform initial setup.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Determine the user SID to base things off of.
        $userSid = if ($LoadLoggedOnUserEnvironmentVariables -and ($runAsActiveUser = Get-ADTRunAsActiveUser))
        {
            $runAsActiveUser.SID
        }
        else
        {
            [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
        }
    }

    process
    {
        Write-ADTLogEntry -Message 'Refreshing the environment variables for this PowerShell session.'
        try
        {
            try
            {
                # Update all session environment variables. Ordering is important here: user variables comes second so that we can override system variables.
                & $Script:CommandTable.'Get-ItemProperty' -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment', "Registry::HKEY_USERS\$userSid\Environment" | & $Script:CommandTable.'ForEach-Object' {
                    $_.PSObject.Properties.Where({$_.Name -notmatch '^PS((Parent)?Path|ChildName|Provider)$'}).ForEach({
                        & $Script:CommandTable.'Set-Item' -LiteralPath "Env:$($_.Name)" -Value $_.Value
                    })
                }

                # Set PATH environment variable separately because it is a combination of the user and machine environment variables.
                & $Script:CommandTable.'Set-Item' -LiteralPath Env:PATH -Value ([System.String]::Join(';', (('Machine', 'User').ForEach({[System.Environment]::GetEnvironmentVariable('PATH', $_)}).Split(';').Where({$_}) | & $Script:CommandTable.'Select-Object' -Unique)))
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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
