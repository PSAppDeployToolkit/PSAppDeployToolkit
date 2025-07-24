#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTFolder
#
#-----------------------------------------------------------------------------

function Remove-ADTFolder
{
    <#
    .SYNOPSIS
        Remove folder and files if they exist.

    .DESCRIPTION
        This function removes a folder and all files within it, with or without recursion, in a given path. If the specified folder does not exist, it logs a warning instead of throwing an error. The function can also delete items recursively if the DisableRecursion parameter is not specified.

    .PARAMETER Path
        A path to the folder to remove. Can contain wildcards.

    .PARAMETER LiteralPath
        A literal path to the folder to remove.

    .PARAMETER InputObject
        A DirectoryInfo object to remove. Available for pipelining.

    .PARAMETER DisableRecursion
        Disables recursion while deleting.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Remove-ADTFolder -Path "$envWinDir\Downloaded Program Files"

        Deletes all files and subfolders in the Windows\Downloaded Program Files folder.

    .EXAMPLE
        Remove-ADTFolder -Path "$envTemp\MyAppCache" -DisableRecursion

        Deletes all files in the Temp\MyAppCache folder but does not delete any subfolders.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTFolder
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LiteralPath', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [Alias('PSPath')]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $true, ParameterSetName = 'InputObject', ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.DirectoryInfo]$InputObject,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableRecursion
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        # Grab and cache all directories.
        $directories = if (!$PSCmdlet.ParameterSetName.Equals('InputObject'))
        {
            $gciParams = @{$PSCmdlet.ParameterSetName = Get-Variable -Name $PSCmdlet.ParameterSetName -ValueOnly }
            Get-ChildItem @gciParams -Directory
        }
        else
        {
            $InputObject
        }

        # Loop through each specified path.
        foreach ($item in $directories)
        {
            # Return early if the folder doesn't exist.
            if (!(Test-Path -LiteralPath $item -PathType Container))
            {
                Write-ADTLogEntry -Message "Folder [$item] does not exist."
                continue
            }

            try
            {
                try
                {
                    # With -Recurse, we can just send it and return early.
                    if (!$DisableRecursion)
                    {
                        Write-ADTLogEntry -Message "Deleting folder [$item] recursively..."
                        Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Remove-Item' -LiteralPath $item -Force -Recurse
                        continue
                    }

                    # Without recursion, we can only send it if the folder has no items as Remove-Item will ask for confirmation without recursion.
                    Write-ADTLogEntry -Message "Deleting folder [$item] without recursion..."
                    if (!($ListOfChildItems = Get-ChildItem -LiteralPath $item -Force))
                    {
                        Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Remove-Item' -LiteralPath $item -Force
                        continue
                    }

                    # We must have some subfolders, let's see what we can do.
                    $SubfoldersSkipped = foreach ($item in $ListOfChildItems)
                    {
                        # Check whether this item is a folder
                        if ($item -is [System.IO.DirectoryInfo])
                        {
                            # Item is a folder. Check if its empty.
                            if (($item | Get-ChildItem -Force | Measure-Object).Count -eq 0)
                            {
                                # The folder is empty, delete it
                                Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Remove-Item' -LiteralPath $item.FullName -Force
                            }
                            else
                            {
                                # Folder is not empty, skip it.
                                $item
                            }
                        }
                        else
                        {
                            # Item is a file. Delete it.
                            Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Remove-Item' -LiteralPath $item.FullName -Force
                        }
                    }
                    if ($SubfoldersSkipped)
                    {
                        $naerParams = @{
                            Exception = [System.IO.IOException]::new("The following folders are not empty ['$($SubfoldersSkipped.FullName.Replace($item.FullName, $null) -join "'; '")'].")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                            ErrorId = 'NonEmptySubfolderError'
                            TargetObject = $SubfoldersSkipped
                            RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to delete folder(s) and file(s) from path [$item]."
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
