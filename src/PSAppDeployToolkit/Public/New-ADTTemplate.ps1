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
        Name of the newly created folder. Default is PSAppDeployToolkit_Version.

    .PARAMETER Version
        Defaults to 4 for the standard v4 template. Use 3 for the v3 compatibility mode template.

    .PARAMETER Show
        Opens the newly created folder in Windows Explorer.

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
        New-ADTTemplate -Destination 'C:\Temp' -Name 'PSAppDeployToolkitv4'

        Creates a new v4 template named PSAppDeployToolkitv4 under C:\Temp.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -Name 'PSAppDeployToolkitv3' -Version 3

        Creates a new v3 compatibility mode template named PSAppDeployToolkitv3 under C:\Temp.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/New-ADTTemplate
    #>

    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Destination = $ExecutionContext.SessionState.Path.CurrentLocation.Path,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = "PSAppDeployToolkit_<ModuleVersion>")]
        [System.String]$Name = "$($MyInvocation.MyCommand.Module.Name)_$($MyInvocation.MyCommand.Module.Version)",

        [Parameter(Mandatory = $false)]
        [ValidateRange(3, 4)]
        [System.Int32]$Version = 4,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Show,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Initialize the function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Resolve the path to handle setups like ".\", etc.
        # We can't use things like a DirectoryInfo cast as .NET doesn't
        # track when the current location in PowerShell has been changed.
        if (($resolvedDest = Resolve-Path -LiteralPath $Destination -ErrorAction Ignore))
        {
            $Destination = $resolvedDest.Path
        }

        # Set up remaining variables.
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
                # If we're running a release module, ensure the psd1 files haven't been tampered with.
                if (($badFiles = Test-ADTReleaseBuildFileValidity -LiteralPath $Script:PSScriptRoot))
                {
                    $naerParams = @{
                        Exception = [System.InvalidOperationException]::new("One or more files within this module have invalid digital signatures.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidData
                        ErrorId = 'ADTDataFileSignatureError'
                        TargetObject = $badFiles
                        RecommendedAction = "Please re-download $($MyInvocation.MyCommand.Module.Name) and try again."
                    }
                    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                }

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

                # Add in some empty files to the Files/SupportFiles folders to stop GitHub upload-artifact from dropping the empty folders.
                $null = New-Item -Name 'Add Setup Files Here.txt' -Path "$templatePath\Files" -ItemType File -Force
                $null = New-Item -Name 'Add Supporting Files Here.txt' -Path "$templatePath\SupportFiles" -ItemType File -Force

                # Copy in the frontend files and the config/assets/strings.
                Copy-Item -Path "$([System.Management.Automation.WildcardPattern]::Escape("$Script:PSScriptRoot\Frontend\v$Version"))\*" -Destination $templatePath -Recurse -Force
                Copy-Item -LiteralPath "$Script:PSScriptRoot\Assets" -Destination $templatePath -Recurse -Force
                Copy-Item -LiteralPath "$Script:PSScriptRoot\Config" -Destination $templatePath -Recurse -Force
                Copy-Item -LiteralPath "$Script:PSScriptRoot\Strings" -Destination $templatePath -Recurse -Force

                # Remove any digital signatures from the ps*1 files.
                Get-ChildItem -LiteralPath $templatePath -File -Filter *.ps*1 -Recurse | & {
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
                Copy-Item -Path "$([System.Management.Automation.WildcardPattern]::Escape("$Script:PSScriptRoot"))\*" -Destination $templateModulePath -Recurse -Force

                # Make the shipped module and its files read-only.
                $(Get-Item -LiteralPath $templateModulePath; Get-ChildItem -LiteralPath $templateModulePath -Recurse) | & {
                    process
                    {
                        $_.Attributes = 'ReadOnly'
                    }
                }

                # Process the generated script to ensure the Import-Module is correct.
                if ($Version.Equals(4))
                {
                    $params = @{
                        LiteralPath = "$templatePath\Invoke-AppDeployToolkit.ps1"
                        Encoding = if ($PSVersionTable.PSEdition.Equals('Core')) { 'utf8BOM' } else { 'utf8' }
                    }
                    Out-File -InputObject (Get-Content @params -Raw).Replace('..\..\..\', $null).Replace('2000-12-31', [System.DateTime]::Now.ToString('O').Split('T')[0]) @params -Width ([System.Int32]::MaxValue) -Force
                }

                # Display the newly created folder in Windows Explorer.
                if ($Show)
                {
                    & ([System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows), 'explorer.exe')) $templatePath
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
