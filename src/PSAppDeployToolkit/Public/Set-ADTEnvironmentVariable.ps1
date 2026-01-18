#-----------------------------------------------------------------------------
#
# MARK: Set-ADTEnvironmentVariable
#
#-----------------------------------------------------------------------------

function Set-ADTEnvironmentVariable
{
    <#
    .SYNOPSIS
        Sets the value for the specified environment variable.

    .DESCRIPTION
        This function sets the value for the specified environment variable.

    .PARAMETER Variable
        The variable to set.

    .PARAMETER Value
        The value to set to variable to.

    .PARAMETER Target
        The target of the variable to set. This can be the machine, user, or process.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Set-ADTEnvironmentVariable -Variable Path -Value C:\Windows

        Sets the value of the Path environment variable to C:\Windows.

    .EXAMPLE
        Set-ADTEnvironmentVariable -Variable Path -Value C:\Windows -Target Machine

        Sets the value of the Path environment variable to C:\Windows for the machine.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTEnvironmentVariable
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Variable,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Value,

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
                        Write-ADTLogEntry -Message "Setting $(($logSuffix = "the environment variable [$Variable] for [$($runAsActiveUser.NTAccount)] to [$Value]"))."
                        if ($PSCmdlet.ShouldProcess("$Variable (User: $($runAsActiveUser.NTAccount))", "Set environment variable to [$Value]"))
                        {
                            Invoke-ADTClientServerOperation -SetEnvironmentVariable -User $runAsActiveUser -Variable $Variable -Value $Value
                        }
                        return
                    }
                    Write-ADTLogEntry -Message "Setting $(($logSuffix = "the environment variable [$Variable] for [$Target] to [$Value]"))."
                    if ($PSCmdlet.ShouldProcess("$Variable (Target: $Target)", "Set environment variable to [$Value]"))
                    {
                        [System.Environment]::SetEnvironmentVariable($Variable, $Value, $Target)
                    }
                    return
                }
                Write-ADTLogEntry -Message "Setting $(($logSuffix = "the environment variable [$Variable] to [$Value]"))."
                if ($PSCmdlet.ShouldProcess($Variable, "Set environment variable to [$Value]"))
                {
                    [System.Environment]::SetEnvironmentVariable($Variable, $Value)
                }
                return
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to set $logSuffix."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
