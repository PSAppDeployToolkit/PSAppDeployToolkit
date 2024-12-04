<#
.SYNOPSIS
    Generates a directory tree structure for a Visual Studio project.

.DESCRIPTION
    This script navigates through the specified Visual Studio project directory and creates a text file
    that represents the project's folder and file structure in a tree format. You can also exclude certain
    files or directories using wildcards.

.PARAMETER Path
    The root path of the Visual Studio project for which the tree structure will be generated.

.PARAMETER OutputFile
    The path to the output text file where the tree structure will be saved.

.PARAMETER Excludes
    An array of file or directory patterns to exclude from the tree. Supports wildcards.

.EXAMPLE
    .\Generate-VsProjectTree.ps1 -Path "C:\MyProjects\SampleProject" -OutputFile "C:\MyProjects\ProjectTree.txt"

    This example generates a tree file named "ProjectTree.txt" for the "SampleProject" located at "C:\MyProjects\".

.EXAMPLE
    .\Generate-VsProjectTree.ps1 -Path "C:\MyProjects\SampleProject" -OutputFile "C:\MyProjects\ProjectTree.txt" -Excludes "*.bin", "*.obj", "bin", "obj"

    This example excludes any files with a .bin or .obj extension, and any folders named "bin" or "obj".
#>

param
(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$OutputFile,

    [Parameter(Mandatory = $false)]
    [string[]]$Excludes = @()
)

function Get-TreeStructure
{
    param
    (
        [string]$Path,
        [int]$Indent = 0,
        [bool]$IsLast = $false
    )

    # Define variables for the Unicode characters using code points
    $VerticalLine = [char]0x2502   # │ U+2502
    $CornerEnd = [char]0x2514      # └ U+2514
    $TeeConnector = [char]0x251C   # ├ U+251C
    $HorizontalLine = [char]0x2500 # ─ U+2500

    # Retrieve all directories and files in the current path
    [System.Collections.ObjectModel.Collection[System.IO.FileSystemInfo]]$Items = Get-ChildItem -Path $Path -Force | Sort-Object { $_.PSIsContainer } -Descending
    [int]$ItemCount = $Items.Count
    [int]$CurrentIndex = 0

    # Iterate over each item in the directory
    foreach ($Item in $Items)
    {
        $CurrentIndex++
        [bool]$IsLastItem = $CurrentIndex -eq $ItemCount

        # Skip excluded files or directories
        if ($Excludes)
        {
            foreach ($Exclude in $Excludes)
            {
                if ($Item.Name -like $Exclude)
                {
                    continue 2
                }
            }
        }

        # Generate the indentation and tree connector for the current item
        [string]$Indentation = "$VerticalLine   " * $Indent
        [string]$Connector = if ($IsLastItem) { "$CornerEnd$HorizontalLine$HorizontalLine " } else { "$TeeConnector$HorizontalLine$HorizontalLine " }

        if ($Item.PSIsContainer)
        {
            # Directory: Append to output file with a slash at the end
            [System.IO.File]::AppendAllText($OutputFile, "$Indentation$Connector$($Item.Name)/" + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
            Get-TreeStructure -Path $Item.FullName -Indent ($Indent + 1) -IsLast $IsLastItem
        }
        else
        {
            # File: Append to output file
            [System.IO.File]::AppendAllText($OutputFile, "$Indentation$Connector$($Item.Name)" + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
        }
    }
}

# Validate the project path
if (-not (Test-Path -Path $Path))
{
    Write-Error -Message "The specified project path does not exist: $Path"
    exit
}

# Initialize the output file
if (Test-Path -Path $OutputFile)
{
    Remove-Item -Path $OutputFile -Force
}

# Create the root entry in the output file
[System.IO.File]::AppendAllText($OutputFile, "$(Split-Path -Leaf $Path)/" + [Environment]::NewLine, [System.Text.Encoding]::UTF8)

# Start generating the tree structure
Get-TreeStructure -Path $Path

"Tree structure generated successfully at $OutputFile"
