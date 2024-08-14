#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Remove-ADTFolder
{
    <#

    .SYNOPSIS
    Remove folder and files if they exist.

    .DESCRIPTION
    Remove folder and all files with or without recursion in a given path.

    .PARAMETER Path
    Path to the folder to remove.

    .PARAMETER DisableRecursion
    Disables recursion while deleting.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    # Delete all files and subfolders in the Windows\Downloads Program Files folder.
    Remove-ADTFolder -Path "$envWinDir\Downloaded Program Files"

    .EXAMPLE
    # Delete all files in the Temp\MyAppCache folder but does not delete any subfolders.
    Remove-ADTFolder -Path "$envTemp\MyAppCache" -DisableRecursion

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.DirectoryInfo]$Path,

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
        # Return early if the folder doesn't exist.
        if (!($Path | & $Script:CommandTable.'Test-Path' -PathType Container))
        {
            Write-ADTLogEntry -Message "Folder [$Path] does not exist."
            return
        }

        try
        {
            try
            {
                # With -Recurse, we can just send it and return early.
                if (!$DisableRecursion)
                {
                    Write-ADTLogEntry -Message "Deleting folder [$Path] recursively..."
                    & $Script:CommandTable.'Remove-Item' -LiteralPath $Path -Force -Recurse
                    return
                }

                # Without recursion, we can only send it if the folder has no items as Remove-Item will ask for confirmation without recursion.
                Write-ADTLogEntry -Message "Deleting folder [$Path] without recursion..."
                if (!($ListOfChildItems = & $Script:CommandTable.'Get-ChildItem' -LiteralPath $Path -Force))
                {
                    & $Script:CommandTable.'Remove-Item' -LiteralPath $Path -Force
                    return
                }

                # We must have some subfolders, let's see what we can do.
                $SubfoldersSkipped = foreach ($item in $ListOfChildItems)
                {
                    # Check whether this item is a folder
                    if ($item -is [System.IO.DirectoryInfo])
                    {
                        # Item is a folder. Check if its empty.
                        if (($item | & $Script:CommandTable.'Get-ChildItem' -Force | & $Script:CommandTable.'Measure-Object').Count -eq 0)
                        {
                            # The folder is empty, delete it
                            $item | & $Script:CommandTable.'Remove-Item' -Force
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
                        $item | & $Script:CommandTable.'Remove-Item' -Force
                    }
                }
                if ($SubfoldersSkipped)
                {
                    $naerParams = @{
                        Exception = [System.IO.IOException]::new("The following folders are not empty ['$($SubfoldersSkipped.FullName.Replace($Path.FullName, $null) -join "'; '")'].")
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
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to delete folder(s) and file(s) from path [$Path]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
