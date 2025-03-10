#-----------------------------------------------------------------------------
#
# MARK: Dismount-ADTWimFile
#
#-----------------------------------------------------------------------------

function Dismount-ADTWimFile
{
    <#
    .SYNOPSIS
        Dismounts a WIM file from the specified mount point.

    .DESCRIPTION
        The Dismount-ADTWimFile function dismounts a WIM file from the specified mount point and discards all changes. This function ensures that the specified path is a valid WIM mount point before attempting to dismount.

    .PARAMETER ImagePath
        The path to the WIM file.

    .PARAMETER Path
        The path to the WIM mount point.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Dismount-ADTWimFile -ImagePath 'C:\Path\To\File.wim'

        This example dismounts the WIM file from all its mount points and discards all changes.

    .EXAMPLE
        Dismount-ADTWimFile -Path 'C:\Mount\WIM'

        This example dismounts the WIM file from the specified mount point and discards all changes.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: © 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Dismount-ADTWimFile
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'ImagePath')]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo[]]$ImagePath,

        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [System.IO.DirectoryInfo[]]$Path
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Loop through all found mounted images.
        foreach ($wimFile in (Get-ADTMountedWimFile @PSBoundParameters))
        {
            # Announce commencement.
            Write-ADTLogEntry -Message "Dismounting WIM file at path [$($wimFile.Path)]."
            try
            {
                try
                {
                    # Perform the dismount and discard all changes.
                    try
                    {
                        $null = Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Dismount-WindowsImage' -Path $wimFile.Path -Discard
                    }
                    catch
                    {
                        # Re-throw if this error is anything other than a file-locked error.
                        if (!$_.Exception.ErrorCode.Equals(-1052638953))
                        {
                            throw
                        }

                        # Get all open file handles for our path.
                        Write-ADTLogEntry -Message "The directory could not be completely unmounted. Checking for any open file handles that can be closed."
                        $exeHandle = "$Script:PSScriptRoot\bin\$([PSADT.OperatingSystem.OSHelper]::GetArchitecture())\handle\handle.exe"
                        $pathRegex = "^$([System.Text.RegularExpressions.Regex]::Escape($($wimFile.Path)))"
                        $pathHandles = Get-ADTProcessHandles | & { process { if ($_.Name -match $pathRegex) { return $_ } } }

                        # Throw if we have no handles to close, it means we don't know why the WIM didn't dismount.
                        if (!$pathHandles)
                        {
                            throw
                        }

                        # Close all open file handles.
                        foreach ($handle in $pathHandles)
                        {
                            # Close handle using handle.exe. An exit code of 0 is considered successful.
                            Write-ADTLogEntry -Message "$(($msg = "Closing handle [$($handle.Handle)] for process [$($handle.Process) ($($handle.PID))]"))."
                            $handleResult = & $exeHandle -accepteula -nobanner -c $handle.Handle -p $handle.PID -y
                            if ($Global:LASTEXITCODE.Equals(0))
                            {
                                continue
                            }

                            # If we're here, we had a bad exit code.
                            Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$Global:LASTEXITCODE]: $handleResult") -Severity 3
                            $naerParams = @{
                                Exception = [System.Runtime.InteropServices.ExternalException]::new($msg, $Global:LASTEXITCODE)
                                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                ErrorId = 'HandleClosureFailure'
                                TargetObject = $handleResult
                                RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }

                        # Attempt the dismount again.
                        $null = Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Dismount-WindowsImage' -Path $wimFile.Path -Discard
                    }
                    Write-ADTLogEntry -Message "Successfully dismounted WIM file."
                    Remove-Item -LiteralPath $wimFile.Path -Force -Confirm:$false
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage 'Error occurred while attempting to dismount WIM file.' -ErrorAction SilentlyContinue
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
