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

    .PARAMETER PSCore
        Include additional dlls needed for operation under PowerShell Core (v7+).

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
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
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
        [System.Management.Automation.SwitchParameter]$PSCore,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $templatePath = Join-Path -Path $Destination -ChildPath $Name
        $templateModulePath = if ($Version.Equals(3))
        {
            [System.IO.Path]::Combine($templatePath, 'AppDeployToolkit', $MyInvocation.MyCommand.Module.Name)
        }
        else
        {
            [System.IO.Path]::Combine($templatePath, $MyInvocation.MyCommand.Module.Name)
        }
    }

    process
    {
        try
        {
            try
            {
                if (![System.IO.Directory]::Exists($Destination))
                {
                    $null = New-Item -Path $Destination -ItemType Directory -Force
                }
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
                $null = New-Item -Path "$templatePath\SuppportFiles" -ItemType Directory -Force
                $null = New-Item -Path $templateModulePath -ItemType Directory -Force
                Copy-Item -Path "$ModulePath\*" -Destination $templateModulePath -Recurse -Force
                Copy-Item -Path "$ModulePath\Frontend\v$Version\*" -Destination $templatePath -Recurse -Force

                if (!$PSCore)
                {
                    $folderToRemove = "$templateModulePath\lib\net6.0"
                    $filesToRemove = @(
                        "$folderToRemove\Microsoft.Windows.SDK.NET.dll"
                        "$folderToRemove\Microsoft.Windows.SDK.NET.xml"
                        "$folderToRemove\WinRT.Runtime.dll"
                    )
                    foreach ($file in $filesToRemove)
                    {
                        if ([System.IO.File]::Exists($file))
                        {
                            Remove-Item -LiteralPath $file -Force
                        }
                    }
                    if ([System.IO.Directory]::Exists($folderToRemove) -and !([System.IO.Directory]::GetFileSystemEntries($folderToRemove)))
                    {
                        Remove-Item -LiteralPath $folderToRemove -Recurse -Force
                    }
                }

                # Process the generated script to ensure the Import-Module is correct.
                if ($Version.Equals(4))
                {
                    $astLambda = {
                        ($args[0] -is [System.Management.Automation.Language.ExpandableStringExpressionAst]) -and
                        ($args[0].Parent -is [System.Management.Automation.Language.CommandAst]) -and
                        ($args[0].Parent.CommandElements.Count) -and
                        ($args[0].Parent.CommandElements[0].Value.Equals('Import-Module'))
                    }
                    $scriptText = [System.IO.File]::ReadAllText(($scriptFile = "$templateModulePath\..\Invoke-AppDeployToolkit.ps1"))
                    $scriptAst = [System.Management.Automation.Language.Parser]::ParseInput($scriptText, [ref]($scriptTokens = $null), [ref]($scriptErrors = $null))
                    $astExtent = $scriptAst.FindAll($astLambda, $false).Extent
                    $scriptText = $scriptText.Remove($astExtent.StartOffset, $astExtent.EndOffset - $astExtent.StartOffset)
                    $scriptText = $scriptText.Insert($astExtent.StartOffset, "`$PSScriptRoot\$($MyInvocation.MyCommand.Module.Name)")
                    [System.IO.File]::WriteAllText($scriptFile, $scriptText, [System.Text.UTF8Encoding]::new($true))
                }

                if ($PassThru)
                {
                    Get-Item -LiteralPath $templatePath
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
