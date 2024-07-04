function Get-ADTSchedulerTask
{
    <#

    .SYNOPSIS
    Retrieve all details for scheduled tasks on the local computer.

    .DESCRIPTION
    Retrieve all details for scheduled tasks on the local computer using schtasks.exe. All property names have spaces and colons removed.

    .PARAMETER TaskName
    Specify the name of the scheduled task to retrieve details for. Uses regex match to find scheduled task.

    .PARAMETER ContinueOnError
    Continue if an error is encountered. Default: $true.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    PSObject. This function returns a PSObject with all scheduled task properties.

    .EXAMPLE
    # To display a list of all scheduled task properties.
    Get-ADTSchedulerTask

    .EXAMPLE
    # Display a grid view of all scheduled task properties.
    Get-ADTSchedulerTask | Out-GridView

    .EXAMPLE
    # Display a list of all scheduled task names.
    Get-ADTSchedulerTask | Select-Object -Property TaskName

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$TaskName
    )

    begin {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction Continue

        # Advise that this function is considered deprecated.
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated. Please migrate your scripts to use the built-in [Get-ScheduledTask] Cmdlet." -Severity 2
    }

    process {
        try
        {
            # Get CSV data from the binary and confirm success.
            Write-ADTLogEntry -Message 'Retrieving Scheduled Tasks...'
            $exeSchtasksResults = & "$([System.Environment]::SystemDirectory)\schtasks.exe" /Query /V /FO CSV
            if ($Global:LastExitCode -ne 0)
            {
                $naerParams = @{
                    Exception = [System.ApplicationException]::new("Failed to retrieve scheduled tasks using [$([System.Environment]::SystemDirectory)\schtasks.exe].")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'SchTasksExecutableFailure'
                    TargetObject = $exeSchtasksResults
                    RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                }
                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
            }

            # Convert CSV data to objects and re-process to remove non-word characters before returning data to the caller.
            if ($schTasks = $exeSchtasksResults | ConvertFrom-Csv | Where-Object {$_.TaskName.StartsWith('\') -and ($_.TaskName -match $TaskName)})
            {
                return $schTasks | Select-Object -Property $schTasks[0].PSObject.Properties.Name.ForEach({
                    @{Label = $_ -replace '[^\w]'; Expression = [scriptblock]::Create("`$_.'$_'")}
                })
            }
        }
        catch
        {
            Write-ADTLogEntry -Message "Failed to retrieve scheduled tasks.`n$(Resolve-ADTError)" -Severity 3
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
