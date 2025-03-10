#-----------------------------------------------------------------------------
#
# MARK: Get-ADTSchedulerTask
#
#-----------------------------------------------------------------------------

function Get-ADTSchedulerTask
{
    <#
    .SYNOPSIS
        Retrieve all details for scheduled tasks on the local computer.

    .DESCRIPTION
        Retrieve all details for scheduled tasks on the local computer using schtasks.exe. All property names have spaces and colons removed.

        This function is deprecated. Please migrate your scripts to use the built-in Get-ScheduledTask Cmdlet.

    .PARAMETER TaskName
        Specify the name of the scheduled task to retrieve details for. Uses regex match to find scheduled task.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.PSObject

        This function returns a PSObject with all scheduled task properties.

    .EXAMPLE
        Get-ADTSchedulerTask

        This example retrieves a list of all scheduled task properties.

    .EXAMPLE
        Get-ADTSchedulerTask | Out-GridView

        This example displays a grid view of all scheduled task properties.

    .EXAMPLE
        Get-ADTSchedulerTask | Select-Object -Property TaskName

        This example displays a list of all scheduled task names.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTSchedulerTask
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'TaskName', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$TaskName
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Advise that this function is considered deprecated.
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use the built-in [Get-ScheduledTask] Cmdlet." -Severity 2
    }

    process
    {
        Write-ADTLogEntry -Message 'Retrieving Scheduled Tasks...'
        try
        {
            try
            {
                # Get CSV data from the binary and confirm success.
                $exeSchtasksResults = & "$([System.Environment]::SystemDirectory)\schtasks.exe" /Query /V /FO CSV 2>&1
                if ($Global:LASTEXITCODE -ne 0)
                {
                    $naerParams = @{
                        Exception = [System.Runtime.InteropServices.ExternalException]::new("The call to [$([System.Environment]::SystemDirectory)\schtasks.exe] failed with exit code [$Global:LASTEXITCODE].", $Global:LASTEXITCODE)
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'SchTasksExecutableFailure'
                        TargetObject = $exeSchtasksResults
                        RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Convert CSV data to objects and re-process to remove non-word characters before returning data to the caller.
                if (($schTasks = $exeSchtasksResults | ConvertFrom-Csv | & { process { if (($_.TaskName -match '^\\') -and ([string]::IsNullOrWhiteSpace($TaskName) -or $_.TaskName -match $TaskName)) { return $_ } } }))
                {
                    return $schTasks | Select-Object -Property ($schTasks[0].PSObject.Properties.Name | & {
                            process
                            {
                                @{ Label = $_ -replace '[^\w]'; Expression = [scriptblock]::Create("`$_.'$_'") }
                            }
                        })
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to retrieve scheduled tasks."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
