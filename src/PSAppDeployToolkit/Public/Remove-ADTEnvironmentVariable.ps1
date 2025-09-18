#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTEnvironmentVariable
#
#-----------------------------------------------------------------------------

function Remove-ADTEnvironmentVariable
{
    <#
    .SYNOPSIS
        Removes the specified environment variable.

    .DESCRIPTION
        This function removes the specified environment variable.

    .PARAMETER Variable
        The variable to remove.

    .PARAMETER Target
        The target of the variable to remove. This can be the machine, user, or process.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Remove-ADTEnvironmentVariable -Variable Path

        Removes the Path environment variable.

    .EXAMPLE
        Remove-ADTEnvironmentVariable -Variable Path -Target Machine

        Removes the Path environment variable for the machine.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTEnvironmentVariable
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Variable,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.EnvironmentVariableTarget]$Target
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                if ($PSBoundParameters.ContainsKey('Target'))
                {
                    if ($Target.Equals([System.EnvironmentVariableTarget]::User))
                    {
                        if (!($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
                        {
                            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
                            return
                        }
                        Write-ADTLogEntry -Message "Removing $(($logSuffix = "the environment variable [$($PSBoundParameters.Variable)] for [$($runAsActiveUser.NTAccount)]"))."
                        Invoke-ADTClientServerOperation -RemoveEnvironmentVariable -User $runAsActiveUser -Variable $Variable
                        return;
                    }
                    Write-ADTLogEntry -Message "Removing $(($logSuffix = "the environment variable [$Variable] for [$Target]"))."
                    [System.Environment]::SetEnvironmentVariable($Variable, [System.Management.Automation.Language.NullString]::Value, $Target)
                    return;
                }
                Write-ADTLogEntry -Message "Removing $(($logSuffix = "the environment variable [$Variable]"))."
                [System.Environment]::SetEnvironmentVariable($Variable, [System.Management.Automation.Language.NullString]::Value)
                return;
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to remove $logSuffix."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
