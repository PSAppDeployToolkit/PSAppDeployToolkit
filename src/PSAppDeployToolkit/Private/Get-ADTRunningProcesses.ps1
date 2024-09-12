#-----------------------------------------------------------------------------
#
# MARK: Get-ADTRunningProcesses
#
#-----------------------------------------------------------------------------

function Get-ADTRunningProcesses
{
    <#

    .SYNOPSIS
    Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

    .DESCRIPTION
    Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

    .PARAMETER InputObject
    Custom object containing the process objects to search for.

    .PARAMETER Silent
    Disables function logging.

    .INPUTS
    System.Management.Automation.PSObject. One or more process objects as established in the Winforms code.

    .OUTPUTS
    System.Diagnostics.Process. Returns one or more process objects representing each running process found.

    .EXAMPLE
    $processObjects | Get-ADTRunningProcesses

    .NOTES
    This is an internal script function and should typically not be called directly.

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    [OutputType([System.Diagnostics.Process])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [PSADT.Types.ProcessObject]$InputObject,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Silent
    )

    begin
    {
        # Initalize function.
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $processObjects = [System.Collections.Generic.List[PSADT.Types.ProcessObject]]::new()
    }

    process
    {
        # Filter out any null input.
        if ($null -ne $InputObject)
        {
            $processObjects.Add($InputObject)
        }
    }

    end
    {
        # Proceed only if we collected process objects.
        if ($processObjects.Count)
        {
            try
            {
                try
                {
                    # Get all running processes and append properties.
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Checking for running applications: [$($processObjects.Name -join ',')]" -DebugMessage:$Silent
                    $runningProcesses = & $Script:CommandTable.'Get-Process' -Name $processObjects.Name -ErrorAction Ignore | & {
                        process
                        {
                            return $_ | & $Script:CommandTable.'Add-Member' -MemberType NoteProperty -Name ProcessDescription -Force -PassThru -Value $(
                                if (![System.String]::IsNullOrWhiteSpace(($objDescription = $processObjects | & $Script:CommandTable.'Where-Object' -Property Name -EQ -Value $_.ProcessName | & $Script:CommandTable.'Select-Object' -ExpandProperty Description -ErrorAction Ignore)))
                                {
                                    # The description of the process provided with the object.
                                    $objDescription
                                }
                                elseif ($_.Description)
                                {
                                    # If the process already has a description field specified, then use it.
                                    $_.Description
                                }
                                else
                                {
                                    # Fall back on the process name if no description is provided by the process or as a parameter to the function.
                                    $_.ProcessName
                                }
                            )
                        }
                    }

                    # Return output if there's any.
                    if ($runningProcesses)
                    {
                        & $Script:CommandTable.'Write-ADTLogEntry' -Message "The following processes are running: [$(($runningProcesses.ProcessName | & $Script:CommandTable.'Select-Object' -Unique) -join ',')]." -DebugMessage:$Silent
                        return ($runningProcesses | & $Script:CommandTable.'Sort-Object' -Property ProcessDescription)
                    }
                    else
                    {
                        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Specified applications are not running.' -DebugMessage:$Silent
                    }
                }
                catch
                {
                    & $Script:CommandTable.'Write-Error' -ErrorRecord $_
                }
            }
            catch
            {
                & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
            }
        }

        # Finalize function.
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
