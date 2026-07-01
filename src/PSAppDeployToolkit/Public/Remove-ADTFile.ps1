#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTFile
#
#-----------------------------------------------------------------------------

function Remove-ADTFile
{
    <#
    .SYNOPSIS
        Removes one or more items from a given path on the filesystem.

    .DESCRIPTION
        The `Remove-ADTFile` function removes one or more items from a given path on the filesystem. It can handle both wildcard paths and literal paths. If the specified path does not exist, it logs a warning instead of throwing an error. The function can also delete items recursively if the `-Recurse` parameter is specified.

    .PARAMETER Path
        Specifies the file on the filesystem to be removed. The value of Path will accept wildcards. Will accept an array of values.

    .PARAMETER LiteralPath
        Specifies the file on the filesystem to be removed. The value of `-LiteralPath` is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

    .PARAMETER InputObject
        A FileInfo object to remove. Available for pipelining.

    .PARAMETER Recurse
        Deletes the files in the specified location(s) and in all child items of the location(s).

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Remove-ADTFile -LiteralPath 'C:\Windows\Downloaded Program Files\Temp.inf'

        Removes the specified file.

    .EXAMPLE
        Remove-ADTFile -LiteralPath 'C:\Windows\Downloaded Program Files' -Recurse

        Removes the specified folder and all its contents recursively.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function continues on received errors by default. To have the function stop on an error, please provide `-ErrorAction Stop` on the end of your call.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTFile

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Remove-ADTFile.ps1
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LiteralPath', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [SupportsWildcards()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [Alias('PSPath')]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $true, ParameterSetName = 'InputObject', ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo]$InputObject,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        # Grab and cache all directories.
        $files = if (!$PSCmdlet.ParameterSetName.Equals('InputObject'))
        {
            $PSBoundParameters[$PSCmdlet.ParameterSetName] | & {
                process
                {
                    try
                    {
                        try
                        {
                            $giParams = @{ $PSCmdlet.ParameterSetName = $path }
                            if ($items = Get-Item @giParams -Force | Select-Object -ExpandProperty FullName)
                            {
                                return $items
                            }
                            Write-ADTLogEntry -Message "Unable to resolve the path [$path] because it does not exist." -Severity Warning
                        }
                        catch [System.Management.Automation.ItemNotFoundException]
                        {
                            Write-ADTLogEntry -Message "Unable to resolve the path [$path] because it does not exist." -Severity Warning
                        }
                        catch [System.Management.Automation.DriveNotFoundException]
                        {
                            Write-ADTLogEntry -Message "Unable to resolve the path [$path] because the drive does not exist." -Severity Warning
                        }
                        catch
                        {
                            Write-Error -ErrorRecord $_
                        }
                    }
                    catch
                    {
                        Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to resolve the path for deletion [$path]."
                    }
                }
            }
        }
        else
        {
            $InputObject.Refresh(); if (!$InputObject.Exists)
            {
                Write-ADTLogEntry -Message "File [$InputObject] does not exist."
                return
            }
            $InputObject.FullName
        }

        # Loop through each specified path.
        foreach ($item in $files)
        {
            try
            {
                try
                {
                    if (Test-Path -LiteralPath $item -PathType Container)
                    {
                        if (!$Recurse)
                        {
                            Write-ADTLogEntry -Message "Skipping folder [$item] because the Recurse switch was not specified."
                            continue
                        }
                        Write-ADTLogEntry -Message "Deleting file(s) recursively in path [$item]..."
                        if ($PSCmdlet.ShouldProcess($item, 'Delete folder recursively'))
                        {
                            $null = Remove-Item -LiteralPath $item -Recurse:$Recurse -Force
                        }
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Deleting file in path [$item]..."
                        if ($PSCmdlet.ShouldProcess($item, 'Delete file'))
                        {
                            $null = Remove-Item -LiteralPath $item -Recurse:$Recurse -Force
                        }
                    }
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to delete items in path [$item]."
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
