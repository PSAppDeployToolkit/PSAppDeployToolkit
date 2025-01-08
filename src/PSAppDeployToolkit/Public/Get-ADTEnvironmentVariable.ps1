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

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.EnvironmentVariableTarget]$Target
    )

    dynamicparam
    {
        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Variable', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Variable', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $true; HelpMessage = "The variable to get." }
                    if ($PSBoundParameters.ContainsKey('Target'))
                    {
                        [System.Management.Automation.ValidateSetAttribute]::new([System.String[]]([System.Environment]::GetEnvironmentVariables($PSBoundParameters.Target).Keys | Sort-Object))
                    }
                    else
                    {
                        [System.Management.Automation.ValidateSetAttribute]::new([System.String[]]([System.Environment]::GetEnvironmentVariables().Keys | Sort-Object))
                    }
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $logSuffix = if ($Target)
        {
            "the environment variable [$($PSBoundParameters.Variable)] for [$Target]"
        }
        else
        {
            "the environment variable [$($PSBoundParameters.Variable)]"
        }
    }

    process
    {
        try
        {
            try
            {
                if ($Target)
                {
                    Write-ADTLogEntry -Message "Getting $logSuffix."
                    return [System.Environment]::GetEnvironmentVariable($PSBoundParameters.Variable, $Target)
                }
                Write-ADTLogEntry -Message "Getting $logSuffix."
                return [System.Environment]::GetEnvironmentVariable($PSBoundParameters.Variable)
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
