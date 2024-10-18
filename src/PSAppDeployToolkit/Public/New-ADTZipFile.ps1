#-----------------------------------------------------------------------------
#
# MARK: New-ADTZipFile
#
#-----------------------------------------------------------------------------

function New-ADTZipFile
{
    <#
    .SYNOPSIS
        Create a new zip archive or add content to an existing archive.

    .DESCRIPTION
        Create a new zip archive or add content to an existing archive by using PowerShell's Compress-Archive.

    .PARAMETER Path
        One or more paths to compress. Supports wildcards.

    .PARAMETER LiteralPath
        One or more literal paths to compress.

    .PARAMETER DestinationPath
        The file path for where the zip file should be created.

    .PARAMETER CompressionLevel
        The level of compression to apply to the zip file.

    .PARAMETER Update
        Specifies whether to update an existing zip file or not.

    .PARAMETER Force
        Specifies whether an existing zip file should be overwritten.

    .PARAMETER RemoveSourceAfterArchiving
        Remove the source path after successfully archiving the content.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        New-ADTZipFile -SourceDirectory 'E:\Testing\Logs' -DestinationPath 'E:\Testing\TestingLogs.zip'

    .NOTES
        This is an internal script function and should typically not be called directly.

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DestinationPath,

        [Parameter(Mandatory = $false, ParameterSetName = 'Path')]
        [Parameter(Mandatory = $false, ParameterSetName = 'LiteralPath')]
        [ValidateSet('Fastest', 'NoCompression', 'Optimal')]
        [System.String]$CompressionLevel,

        [Parameter(Mandatory = $false, ParameterSetName = 'Path')]
        [Parameter(Mandatory = $false, ParameterSetName = 'LiteralPath')]
        [System.Management.Automation.SwitchParameter]$Update,

        [Parameter(Mandatory = $false, ParameterSetName = 'Path')]
        [Parameter(Mandatory = $false, ParameterSetName = 'LiteralPath')]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false, ParameterSetName = 'Path')]
        [Parameter(Mandatory = $false, ParameterSetName = 'LiteralPath')]
        [System.Management.Automation.SwitchParameter]$RemoveSourceAfterArchiving
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Remove invalid characters from the supplied filename.
        if (($DestinationArchiveFileName = Remove-ADTInvalidFileNameChars -Name $DestinationArchiveFileName).Length -eq 0)
        {
            $naerParams = @{
                Exception = [System.ArgumentException]::new('Invalid filename characters replacement resulted into an empty string.', $_)
                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                ErrorId = 'DestinationArchiveFileNameInvalid'
                TargetObject = $DestinationArchiveFileName
                RecommendedAction = "Please review the supplied value to '-DestinationArchiveFileName' and try again."
            }
            throw (New-ADTErrorRecord @naerParams)
        }

        # Remove parameters from PSBoundParameters that don't apply to Compress-Archive.
        if ($PSBoundParameters.ContainsKey('RemoveSourceAfterArchiving'))
        {
            $null = $PSBoundParameters.Remove('RemoveSourceAfterArchiving')
        }

        # Get the specified source variable.
        $sourcePath = Get-Variable -Name $PSCmdlet.ParameterSetName -ValueOnly
    }

    process
    {
        try
        {
            try
            {
                # Get the full destination path where the archive will be stored.
                Write-ADTLogEntry -Message "Creating a zip archive with the requested content at destination path [$DestinationPath]."

                # If the destination archive already exists, delete it if the -OverwriteArchive option was selected.
                if ([System.IO.File]::Exists($DestinationPath) -and $OverwriteArchive)
                {
                    Write-ADTLogEntry -Message "An archive at the destination path already exists, deleting file [$DestinationPath]."
                    $null = Remove-Item -LiteralPath $DestinationPath -Force
                }

                # Create the archive file.
                Write-ADTLogEntry -Message "Compressing [$sourcePath] to destination path [$DestinationPath]..."
                Compress-Archive @PSBoundParameters

                # If option was selected, recursively delete the source directory after successfully archiving the contents.
                if ($RemoveSourceAfterArchiving)
                {
                    try
                    {
                        Write-ADTLogEntry -Message "Recursively deleting [$sourcePath] as contents have been successfully archived."
                        $null = Remove-Item -LiteralPath $Directory -Recurse -Force
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message "Failed to recursively delete [$sourcePath].`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 2
                    }
                }

                # If the archive was created in session 0 or by an Admin, then it may only be readable by elevated users.
                # Apply the parent folder's permissions to the archive file to fix the problem.
                $parentPath = [System.IO.Path]::GetDirectoryName($DestinationPath)
                Write-ADTLogEntry -Message "If the archive was created in session 0 or by an Admin, then it may only be readable by elevated users. Apply permissions from parent folder [$parentPath] to file [$DestinationPath]."
                try
                {
                    Set-Acl -LiteralPath $DestinationPath -AclObject (Get-Acl -Path $parentPath)
                }
                catch
                {
                    Write-ADTLogEntry -Message "Failed to apply parent folder's [$parentPath] permissions to file [$DestinationPath].`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 2
                }
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Object ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to archive the requested file(s)."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
