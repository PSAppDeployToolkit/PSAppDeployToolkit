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
        This function removes one or more items from a given path on the filesystem. It can handle both wildcard paths and literal paths. If the specified path does not exist, it logs a warning instead of throwing an error. The function can also delete items recursively if the Recurse parameter is specified.

    .PARAMETER Path
        Specifies the file on the filesystem to be removed. The value of Path will accept wildcards. Will accept an array of values.

    .PARAMETER LiteralPath
        Specifies the file on the filesystem to be removed. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

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

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTFile
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LiteralPath', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [Alias('PSPath')]
        [System.String[]]$LiteralPath,

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
        foreach ($Value in $PSBoundParameters[$PSCmdlet.ParameterSetName])
        {
            # Resolve the specified path, if the path does not exist, display a warning instead of an error.
            try
            {
                try
                {
                    $giParams = @{ $PSCmdlet.ParameterSetName = $Value }
                    if (!($Items = Get-Item @giParams -Force | Select-Object -ExpandProperty FullName))
                    {
                        Write-ADTLogEntry -Message "Unable to resolve the path [$Value] because it does not exist." -Severity 2
                        continue
                    }
                }
                catch [System.Management.Automation.ItemNotFoundException]
                {
                    Write-ADTLogEntry -Message "Unable to resolve the path [$Value] because it does not exist." -Severity 2
                    continue
                }
                catch [System.Management.Automation.DriveNotFoundException]
                {
                    Write-ADTLogEntry -Message "Unable to resolve the path [$Value] because the drive does not exist." -Severity 2
                    continue
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to resolve the path for deletion [$Value]."
                continue
            }

            # Delete specified path if it was successfully resolved.
            try
            {
                foreach ($Item in $Items)
                {
                    try
                    {
                        if (Test-Path -LiteralPath $Item -PathType Container)
                        {
                            if (!$Recurse)
                            {
                                Write-ADTLogEntry -Message "Skipping folder [$Item] because the Recurse switch was not specified."
                                continue
                            }
                            Write-ADTLogEntry -Message "Deleting file(s) recursively in path [$Item]..."
                            if ($PSCmdlet.ShouldProcess($Item, 'Delete folder recursively'))
                            {
                                $null = Remove-Item -LiteralPath $Item -Recurse:$Recurse -Force
                            }
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "Deleting file in path [$Item]..."
                            if ($PSCmdlet.ShouldProcess($Item, 'Delete file'))
                            {
                                $null = Remove-Item -LiteralPath $Item -Recurse:$Recurse -Force
                            }
                        }
                    }
                    catch
                    {
                        Write-Error -ErrorRecord $_
                    }
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to delete items in path [$Item]."
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
