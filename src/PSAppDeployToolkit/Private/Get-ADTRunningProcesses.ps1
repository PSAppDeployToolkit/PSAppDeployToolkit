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
    PSADT.UserInterface.Services.AppProcessInfo. Returns a custom object representing each app's process info.

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
    [OutputType([PSADT.UserInterface.Services.AppProcessInfo])]
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
            # Get icon so we can convert it into a media image for the UI.
            $icon = try
            {
                [PSADT.UserInterface.Utilities.ProcessExtensions]::GetIcon($_, $true)
            }
            catch
            {
                $null = $null
            }

            # Instantiate and return a new AppProcessInfo object.
            return [PSADT.UserInterface.Services.AppProcessInfo]::new(
                $_.Name,
                $(
                    if (![System.String]::IsNullOrWhiteSpace(($objDescription = $ProcessObjects | Where-Object -Property Name -EQ -Value $_.ProcessName | Select-Object -First 1 -ExpandProperty Description -ErrorAction Ignore)))
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
                ),
                $_.Product,
                $_.Company,
                $(if ($icon) { [PSADT.UserInterface.Utilities.BitmapExtensions]::ConvertToImageSource($icon.ToBitmap()) }),
                $_.StartTime,
                $_.MainWindowHandle
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
