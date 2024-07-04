function Get-ADTRunningProcesses
{
    <#

    .SYNOPSIS
    Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

    .DESCRIPTION
    Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

    .PARAMETER InputObject
    Custom object containing the process objects to search for.

    .PARAMETER DisableLogging
    Disables function logging.

    .INPUTS
    System.Management.Automation.PSObject. One or more process objects as established in the Winforms code.

    .OUTPUTS
    System.Diagnostics.Process. Returns one or more process objects representing each running process found.

    .EXAMPLE
    $processObjects | Get-ADTRunningProcesses

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.ProcessObject]$InputObject,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableLogging
    )

    begin {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    end {
        # Confirm input isn't null before proceeding.
        if ($processObjects = $input.Where({$null -ne $_}))
        {
            # Get all running processes and append properties.
            Write-ADTLogEntry -Message "Checking for running applications: [$($processObjects.Name -join ',')]" -DebugMessage:$DisableLogging
            $runningProcesses = Get-Process -Name $processObjects.Name -ErrorAction Ignore | ForEach-Object {
                $_ | Add-Member -MemberType NoteProperty -Name ProcessDescription -Force -PassThru -Value $(
                    if (![System.String]::IsNullOrWhiteSpace(($objDescription = $processObjects | Where-Object -Property Name -EQ -Value $_.ProcessName | Select-Object -ExpandProperty Description -ErrorAction Ignore)))
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

            # Return output if there's any.
            if ($runningProcesses)
            {
                Write-ADTLogEntry -Message "The following processes are running: [$(($runningProcesses.ProcessName | Select-Object -Unique) -join ',')]." -DebugMessage:$DisableLogging
                $runningProcesses | Sort-Object -Property ProcessDescription
            }
            else
            {
                Write-ADTLogEntry -Message 'Specified applications are not running.' -DebugMessage:$DisableLogging
            }
        }
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
