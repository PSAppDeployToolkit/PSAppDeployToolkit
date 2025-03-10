#-----------------------------------------------------------------------------
#
# MARK: Export-ADTEnvironmentTableToSessionState
#
#-----------------------------------------------------------------------------

function Export-ADTEnvironmentTableToSessionState
{
    <#
    .SYNOPSIS
        Exports the content of `Get-ADTEnvironmentTable` to the provided SessionState as variables.

    .DESCRIPTION
        This function exports the content of `Get-ADTEnvironmentTable` to the provided SessionState as variables.

    .PARAMETER SessionState
        Caller's SessionState.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Export-ADTEnvironmentTableToSessionState -SessionState $ExecutionContext.SessionState

        Invokes the Export-ADTEnvironmentTableToSessionState function and exports the module's environment table to the provided SessionState.

    .EXAMPLE
        Export-ADTEnvironmentTableToSessionState -SessionState $PSCmdlet.SessionState

        Invokes the Export-ADTEnvironmentTableToSessionState function and exports the module's environment table to the provided SessionState.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Export-ADTEnvironmentTableToSessionState
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState
    )

    begin
    {
        # Store the environment table on the stack and initialize function.
        try
        {
            $adtEnv = Get-ADTEnvironmentTable
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
                $null = $ExecutionContext.InvokeCommand.InvokeScript($SessionState, { $args[1].GetEnumerator() | . { process { & $args[0] -Name $_.Key -Value $_.Value -Option ReadOnly -Force } } $args[0] }.Ast.GetScriptBlock(), $Script:CommandTable.'New-Variable', $adtEnv)
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
