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
    This function can be called without an active ADT session.

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

    # Get all running processes and append properties.
    Write-ADTLogEntry -Message "Checking for running applications: [$($ProcessObjects.Name -join ',')]"
    $runningProcesses = Get-Process -Name $ProcessObjects.Name -ErrorAction Ignore | & {
        process
        {
            return $_ | Add-Member -MemberType NoteProperty -Name ProcessDescription -Force -PassThru -Value $(
                if (![System.String]::IsNullOrWhiteSpace(($objDescription = $ProcessObjects | Where-Object -Property Name -EQ -Value $_.ProcessName | Select-Object -ExpandProperty Description -ErrorAction Ignore)))
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
        Write-ADTLogEntry -Message "The following processes are running: [$(($runningProcesses.ProcessName | Select-Object -Unique) -join ',')]."
        return ($runningProcesses | Sort-Object -Property ProcessDescription)
    }
    Write-ADTLogEntry -Message 'Specified applications are not running.'
}
