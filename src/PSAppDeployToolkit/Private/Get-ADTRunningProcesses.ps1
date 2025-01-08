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
            # Process the cached processes into proper process names (i.e. remove paths).
            $processNames = $processNames | & {
                process
                {
                    if ([System.IO.Path]::IsPathRooted($_))
                    {
                        return [System.IO.Path]::GetFileNameWithoutExtension($_)
                    }
                    return $_
                }
            }

            # Cache all running processes.
            $allProcesses = Get-Process -Name $processNames -ErrorAction Ignore

            # Cache process info from WMI as it gives us our command line and associated arguments.
            $wmiProcesses = Get-CimInstance -ClassName Win32_Process -Verbose:$false

            # Member lookup table.
            $member = 'Name', 'Path'
        }

        process
        {
            # Get all processes that matches the object and add in extra properties.
            $processes = foreach ($process in $allProcesses)
            {
                # Continue if this isn't our process or it's ended since we cached it.
                if (($process.($member[[System.IO.Path]::IsPathRooted($_.Name)]) -notlike $_.Name) -or (!$process.Refresh() -and $process.HasExited))
                {
                    continue
                }

                # Find the correspoding WMI process object.
                $wmiProcess = $wmiProcesses | & { process { if ($_.ProcessId -eq $process.Id) { return $_ } } } | Select-Object -First 1

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

                # Add in Arguments and the full CommandLine property from WMI.
                $procProps.Add([System.Management.Automation.PSNoteProperty]::new('Arguments', $(if (![System.String]::IsNullOrWhiteSpace(($procArgs = $wmiProcess.CommandLine.Replace($wmiProcess.ExecutablePath, $null).TrimStart('"').Trim()))) { $procArgs } )))
                $procProps.Add([System.Management.Automation.PSNoteProperty]::new('CommandLine', $wmiProcess.CommandLine.Trim()))

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
                return $processes
            }
            return $processes | Where-Object -FilterScript $_.Filter
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
