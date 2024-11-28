#-----------------------------------------------------------------------------
#
# MARK: New-ADTTemplate
#
#-----------------------------------------------------------------------------

function New-ADTTemplate
{
    <#
    .SYNOPSIS
        Creates a new folder containing a template front end and module folder, ready to customise.

    .DESCRIPTION
        Specify a destination path where a new folder will be created. You also have the option of creating a template for v3 compatibility mode.

    .PARAMETER Destination
        Path where the new folder should be created. Default is the current working directory.

    .PARAMETER Name
        Name of the newly created folder. Default is PSAppDeployToolkit.

    .PARAMETER ModulePath
        Override the default module path to include with the template.

    .PARAMETER Version
        Defaults to 4 for the standard v4 template. Use 3 for the v3 compatibility mode template.

    .PARAMETER Force
        If the destination folder already exists, this switch will force the creation of the new folder.

    .PARAMETER PassThru
        Returns the newly created folder object.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        New-ADTTemplate -Path 'C:\Temp' -Name 'PSAppDeployToolkitv4'

        Creates a new v4 template named PSAppDeployToolkitv4 under C:\Temp.

    .EXAMPLE
        New-ADTTemplate -Path 'C:\Temp' -Name 'PSAppDeployToolkitv3' -Version 3

        Creates a new v3 compatibility mode template named PSAppDeployToolkitv3 under C:\Temp.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Destination = $PWD,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name = $MyInvocation.MyCommand.Module.Name,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ModulePath = $MyInvocation.MyCommand.Module.ModuleBase,

        [Parameter(Mandatory = $false)]
        [ValidateSet(3, 4)]
        [System.Int32]$Version = 4,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $moduleName = $MyInvocation.MyCommand.Module.Name
        $templatePath = Join-Path -Path $Destination -ChildPath $Name
        $templateModulePath = if ($Version.Equals(3))
        {
            [System.IO.Path]::Combine($templatePath, 'AppDeployToolkit', $moduleName)
        }
        else
        {
            [System.IO.Path]::Combine($templatePath, $moduleName)
        }
    }

    process
    {
        try
        {
            try
            {
                # Create directories.
                if ([System.IO.Directory]::Exists($templatePath) -and [System.IO.Directory]::GetFileSystemEntries($templatePath))
                {
                    if (!$Force)
                    {
                        $naerParams = @{
                            Exception = [System.IO.IOException]::new("Folders [$templatePath] already exists and is not empty.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                            ErrorId = 'NonEmptySubfolderError'
                            TargetObject = $templatePath
                            RecommendedAction = "Please remove the existing folder, supply a new name, or add the -Force parameter and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    $null = Remove-Item -LiteralPath $templatePath -Recurse -Force
                }
                $null = New-Item -Path "$templatePath\Files" -ItemType Directory -Force
                $null = New-Item -Path "$templatePath\SupportFiles" -ItemType Directory -Force

                # Copy in the frontend files and the config/assets/strings.
                Copy-Item -Path "$ModulePath\Frontend\v$Version\*" -Destination $templatePath -Recurse -Force
                Copy-Item -LiteralPath "$ModulePath\Assets" -Destination $templatePath -Recurse -Force
                Copy-Item -LiteralPath "$ModulePath\Config" -Destination $templatePath -Recurse -Force
                Copy-Item -LiteralPath "$ModulePath\Strings" -Destination $templatePath -Recurse -Force

                # Remove any digital signatures from the ps*1 files.
                Get-ChildItem -Path "$templatePath\*.ps*1" -Recurse | & {
                    process
                    {
                        if (($sigLine = $(($fileLines = [System.IO.File]::ReadAllLines($_.FullName)) -match '^# SIG # Begin signature block$')))
                        {
                            [System.IO.File]::WriteAllLines($_.FullName, $fileLines[0..($fileLines.IndexOf($sigLine) - 2)])
                        }
                    }
                }

                # Copy in the module files.
                $null = New-Item -Path $templateModulePath -ItemType Directory -Force
                Copy-Item -Path "$ModulePath\*" -Destination $templateModulePath -Recurse -Force

                # Make the shipped module and its files read-only.
                $(Get-Item -LiteralPath $templateModulePath; Get-ChildItem -LiteralPath $templateModulePath -Recurse) | & {
                    process
                    {
                        $_.Attributes = 'ReadOnly'
                    }
                }

                # Remove any PDB files that might have snuck in.
                Get-ChildItem -LiteralPath $templatePath -Filter *.pdb -Recurse | Remove-Item -Force

                # Process the generated script to ensure the Import-Module is correct.
                if ($Version.Equals(4))
                {
                    $scriptText = [System.IO.File]::ReadAllText(($scriptFile = "$templatePath\Invoke-AppDeployToolkit.ps1"))
                    $scriptText = $scriptText.Replace("`$PSScriptRoot\..\..\..\$moduleName", "`$PSScriptRoot\$moduleName")
                    [System.IO.File]::WriteAllText($scriptFile, $scriptText, [System.Text.UTF8Encoding]::new($true))
                }

                # Return a DirectoryInfo object if passing through.
                if ($PassThru)
                {
                    return (Get-Item -LiteralPath $templatePath)
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
