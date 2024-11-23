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

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.DirectoryInfo]$Path,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableRecursion
    )

    begin {
        # Make this function continue on error.
        $OriginalErrorAction = if ($PSBoundParameters.ContainsKey('ErrorAction'))
        {
            $PSBoundParameters.ErrorAction
        }
        else
        {
            [System.Management.Automation.ActionPreference]::Continue
        }
        $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
        Write-ADTDebugHeader
    }

    process {
        # Return early if the folder doesn't exist.
        if (!($Path | Test-Path -PathType Container))
        {
            Write-ADTLogEntry -Message "Folder [$Path] does not exist."
            return
        }

        try
        {
            # With -Recurse, we can just send it and return early.
            if (!$DisableRecursion)
            {
                Write-ADTLogEntry -Message "Deleting folder [$Path] recursively..."
                Remove-Item -LiteralPath $Path -Force -Recurse
                return
            }

            # Without recursion, we can only send it if the folder has no items as Remove-Item will ask for confirmation without recursion.
            Write-ADTLogEntry -Message "Deleting folder [$Path] without recursion..."
            if (!($ListOfChildItems = Get-ChildItem -LiteralPath $Path -Force))
            {
                Remove-Item -LiteralPath $Path -Force
                return
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
                        $item | Remove-Item -Force
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
                    $item | Remove-Item -Force
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
            Write-ADTLogEntry -Message "Failed to delete folder(s) and file(s) from path [$Path].`n$(Resolve-ADTError)" -Severity 3
            $ErrorActionPreference = $OriginalErrorAction
            $PSCmdlet.WriteError($_)
        }
    }

    end {
        Write-ADTDebugFooter
    }
}
