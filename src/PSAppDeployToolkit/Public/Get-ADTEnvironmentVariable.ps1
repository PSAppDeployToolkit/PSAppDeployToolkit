#-----------------------------------------------------------------------------
#
# MARK: Get-ADTEnvironmentVariable
#
#-----------------------------------------------------------------------------

function Get-ADTEnvironmentVariable
{
    <#
    .SYNOPSIS
        Gets the value of the specified environment variable.

    .DESCRIPTION
        This function gets the value of the specified environment variable.

    .PARAMETER Variable
        The variable to get.

    .PARAMETER Target
        The target of the variable to get. This can be the machine, user, or process.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        This function returns the value of the specified environment variable.

    .EXAMPLE
        Get-ADTEnvironmentVariable -Variable Path

        Returns the value of the Path environment variable.

    .EXAMPLE
        Get-ADTEnvironmentVariable -Variable Path -Target Machine

        Returns the value of the Path environment variable for the machine.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTEnvironmentVariable
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
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
                        Write-ADTLogEntry -Message "Getting $(($logSuffix = "the environment variable [$($Variable)] for [$($runAsActiveUser.NTAccount)]"))."
                        return Invoke-ADTClientServerOperation -GetEnvironmentVariable -User $runAsActiveUser -Variable $Variable
                    }
                    Write-ADTLogEntry -Message "Getting $(($logSuffix = "the environment variable [$($Variable)] for [$Target]"))."
                    return [PSADT.Utilities.EnvironmentUtilities]::GetEnvironmentVariable($Variable, $Target)
                }
                Write-ADTLogEntry -Message "Getting $(($logSuffix = "the environment variable [$($Variable)]"))."
                return [PSADT.Utilities.EnvironmentUtilities]::GetEnvironmentVariable($Variable)
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to get $logSuffix."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
