#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTItem
#
#-----------------------------------------------------------------------------

function Remove-ADTItem
{
    <#
    .SYNOPSIS
        Removes one or more filesystem items.

    .DESCRIPTION
        This function removes one or more files and folders from the filesystem. It can handle wildcard paths, literal paths, or pipelined filesystem objects.

        For folders, recursive removal can be enabled with Recurse.

        When Recurse is not specified, empty folders can still be removed.

        If the specified path does not exist, the function logs a warning instead of throwing an error.

    .PARAMETER Path
        Specifies the filesystem item(s) to remove. The value of Path accepts wildcards. Will accept an array of values.

    .PARAMETER LiteralPath
        Specifies the filesystem item(s) to remove. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

    .PARAMETER InputObject
        Specifies a FileInfo or DirectoryInfo object to remove. Available for pipelining.

    .PARAMETER Recurse
        Deletes folders and all child items recursively.

    .PARAMETER OnlyIfEmpty
        When used with Recurse, performs a bottom-up folder pass and removes only folders that are empty. Files are never removed in this mode.

    .INPUTS
        System.IO.FileSystemInfo

        You can pipe FileInfo and DirectoryInfo objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Remove-ADTItem -LiteralPath 'C:\Windows\Downloaded Program Files\Temp.inf'

        Removes the specified file.

    .EXAMPLE
        Remove-ADTItem -LiteralPath 'C:\Windows\Downloaded Program Files' -Recurse

        Removes the specified folder and all its contents recursively.

    .EXAMPLE
        Get-Item 'C:\Windows\Downloaded Program Files' | Remove-ADTItem

        Removes the folder only when it is empty; otherwise it is skipped because Recurse was not provided.

    .EXAMPLE
        Remove-ADTItem -LiteralPath 'C:\Temp\FolderToPrune' -Recurse -OnlyIfEmpty

        Removes empty folders from the deepest level up inside the target path.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function continues on received errors by default. To have the function stop on an error, provide -ErrorAction Stop on the end of your call.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTItem
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LiteralPath', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'Path')]
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'PathOnlyIfEmpty')]
        [PSAppDeployToolkit.Foundation.ValidateNotNullOrWhiteSpace()]
        [SupportsWildcards()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'LiteralPath')]
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'LiteralPathOnlyIfEmpty')]
        [PSAppDeployToolkit.Foundation.ValidateNotNullOrWhiteSpace()]
        [Alias('PSPath')]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'InputObject', ValueFromPipeline = $true)]
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'InputObjectOnlyIfEmpty', ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileSystemInfo]$InputObject,

        [Parameter(Mandatory = $false, ParameterSetName = 'Path')]
        [Parameter(Mandatory = $false, ParameterSetName = 'LiteralPath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InputObject')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PathOnlyIfEmpty')]
        [Parameter(Mandatory = $false, ParameterSetName = 'LiteralPathOnlyIfEmpty')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InputObjectOnlyIfEmpty')]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $true, ParameterSetName = 'PathOnlyIfEmpty')]
        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPathOnlyIfEmpty')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InputObjectOnlyIfEmpty')]
        [System.Management.Automation.SwitchParameter]$OnlyIfEmpty
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
        if ($OnlyIfEmpty -and !$Recurse)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The [-OnlyIfEmpty] parameter can only be used when [-Recurse] is specified.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                ErrorId = 'OnlyIfEmptyRequiresRecurse'
                TargetObject = $PSBoundParameters
                RecommendedAction = "Please specify the [-Recurse] parameter together with [-OnlyIfEmpty]."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
    }

    process
    {
        # Grab and cache all filesystem items.
        $inputParameterName = $PSCmdlet.ParameterSetName -replace 'OnlyIfEmpty'
        $items = if (!$inputParameterName.Equals('InputObject'))
        {
            foreach ($value in $PSBoundParameters[$inputParameterName])
            {
                try
                {
                    $giParams = @{ $inputParameterName = $value }
                    if (!($resolvedItems = Get-Item @giParams -Force))
                    {
                        Write-ADTLogEntry -Message "Unable to resolve the path [$value] because it does not exist." -Severity Warning
                        continue
                    }
                    $resolvedItems
                }
                catch [System.Management.Automation.ItemNotFoundException]
                {
                    Write-ADTLogEntry -Message "Unable to resolve the path [$value] because it does not exist." -Severity Warning
                    continue
                }
                catch [System.Management.Automation.DriveNotFoundException]
                {
                    Write-ADTLogEntry -Message "Unable to resolve the path [$value] because the drive does not exist." -Severity Warning
                    continue
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
        }
        else
        {
            if (!$InputObject.Exists)
            {
                Write-ADTLogEntry -Message "Item [$InputObject] does not exist." -Severity Warning
                return
            }
            $InputObject
        }

        # Process each found item.
        foreach ($item in $items)
        {
            try
            {
                try
                {
                    # Folder deletion mode depends on recursion settings.
                    if ($item -is [System.IO.DirectoryInfo])
                    {
                        # With recurse+only-if-empty mode, walk deepest-first so parent folders can become empty after child folder deletions.
                        if ($Recurse -and $OnlyIfEmpty)
                        {
                            Write-ADTLogEntry -Message "Deleting empty folders in path [$($item.FullName)] from deepest level first..."
                            foreach ($directory in $($item; Get-ChildItem -LiteralPath $item.FullName -Directory -Recurse -Force) | Sort-Object -Property @{ Expression = { $_.FullName.Length }; Descending = $true })
                            {
                                if (!$directory.Exists)
                                {
                                    continue
                                }
                                if (Get-ChildItem -LiteralPath $directory.FullName -Force)
                                {
                                    Write-ADTLogEntry -Message "Skipping folder [$($directory.FullName)] because it is not empty."
                                    continue
                                }
                                Write-ADTLogEntry -Message "Deleting empty folder [$($directory.FullName)]..."
                                if ($PSCmdlet.ShouldProcess($directory.FullName, 'Delete empty folder'))
                                {
                                    Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Remove-Item' -LiteralPath $directory.FullName -Force
                                }
                            }
                            continue
                        }

                        # With recursion, no extra checks are necessary, we can just get it done.
                        if ($Recurse)
                        {
                            Write-ADTLogEntry -Message "Deleting folder [$($item.FullName)] recursively..."
                            if ($PSCmdlet.ShouldProcess($item.FullName, 'Delete folder recursively'))
                            {
                                Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Remove-Item' -LiteralPath $item.FullName -Force -Recurse
                            }
                            continue
                        }

                        # Without recursion, only empty folders can be removed.
                        if (Get-ChildItem -LiteralPath $item.FullName -Force)
                        {
                            Write-ADTLogEntry -Message "Skipping folder [$($item.FullName)] because the Recurse switch was not specified and the folder is not empty."
                            continue
                        }
                        Write-ADTLogEntry -Message "Deleting empty folder [$($item.FullName)]..."
                        if ($PSCmdlet.ShouldProcess($item.FullName, 'Delete empty folder'))
                        {
                            Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Remove-Item' -LiteralPath $item.FullName -Force
                        }
                        continue
                    }

                    # File deletion is straightforward.
                    if ($item -is [System.IO.FileInfo])
                    {
                        Write-ADTLogEntry -Message "Deleting file in path [$($item.FullName)]..."
                        if ($PSCmdlet.ShouldProcess($item.FullName, 'Delete file'))
                        {
                            Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Remove-Item' -LiteralPath $item.FullName -Force
                        }
                        continue
                    }
                    Write-ADTLogEntry -Message "Skipping path [$($item.FullName)] because it is not a filesystem file or folder." -Severity Warning
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to delete item [$($item.FullName)]."
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
