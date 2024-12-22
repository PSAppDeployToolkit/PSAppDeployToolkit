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

    .PARAMETER ProcessObjects
    Custom object containing the process objects to search for.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.Diagnostics.Process. Returns one or more process objects representing each running process found.

    .EXAMPLE
    Get-ADTRunningProcesses -ProcessObjects $processObjects

    .NOTES
    This is an internal script function and should typically not be called directly.

    .NOTES
    An active ADT session is NOT required to use this function.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    [OutputType([System.Diagnostics.Process])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowNull()][AllowEmptyCollection()]
        [PSADT.Types.ProcessObject[]]$ProcessObjects
    )

    # Return early if we've received no input.
    if ($null -eq $ProcessObjects)
    {
        return
    }

    # Process provided process objects.
    Write-ADTLogEntry -Message "Checking for running applications: ['$([System.String]::Join("', '", ($processNames = $ProcessObjects.Name)))']"
    $runningProcesses = $ProcessObjects | & {
        begin
        {
            # Cache all running processes.
            $allProcesses = Get-Process -Name $processNames -ErrorAction Ignore
        }

        process
        {
            # Get all processes that matches the object and add in extra properties.
            $processes = foreach ($process in $allProcesses)
            {
                # Continue if this isn't our process.
                if ($process.Name -ne $_.Name)
                {
                    continue
                }

                # Cache the process object's properties.
                $procProps = $process.PSObject.Properties

                # Add in our ProcessDescription field.
                if (![System.String]::IsNullOrWhiteSpace($_.Description))
                {
                    # The description of the process provided with the object.
                    $procProps.Add([System.Management.Automation.PSNoteProperty]::new('ProcessDescription', $_.Description))
                }
                elseif (![System.String]::IsNullOrWhiteSpace($process.Description))
                {
                    # If the process already has a description field specified, then use it.
                    $procProps.Add([System.Management.Automation.PSNoteProperty]::new('ProcessDescription', $process.Description))
                }
                else
                {
                    # Fall back on the process name if no description is provided by the process or as a parameter to the function.
                    $procProps.Add([System.Management.Automation.PSNoteProperty]::new('ProcessDescription', $process.ProcessName))
                }

                # Output the process object for collection.
                $process
            }

            # Return early if we have nothing.
            if (!$processes)
            {
                return
            }

            # Return filtered list of processes if there is one.
            if ($null -eq $_.Filter)
            {
                return $process
            }
            $process | Where-Object -FilterScript $_.Filter
        }
    }

    # Return output if there's any.
    if ($runningProcesses)
    {
        Write-ADTLogEntry -Message "The following processes are running: [$(($runningProcesses.ProcessName | Select-Object -Unique) -join ',')]."
        return ($runningProcesses | Sort-Object -Property ProcessDescription)
    }
    Write-ADTLogEntry -Message 'Specified applications are not running.'
}
