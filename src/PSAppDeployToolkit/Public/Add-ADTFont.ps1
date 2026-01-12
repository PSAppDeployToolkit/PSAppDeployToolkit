#-----------------------------------------------------------------------------
#
# MARK: Add-ADTFont
#
#-----------------------------------------------------------------------------

function Add-ADTFont
{
    <#
    .SYNOPSIS
        Installs a font file to the system.

    .DESCRIPTION
        Installs a font file to the system by copying it to the Windows Fonts directory, registering it with the system, and creating the registry entry.
        Supports .ttf, .ttc, and .otf file types.

    .PARAMETER Path
        The path to the font file or directory containing font files.

    .PARAMETER Recurse
        Recursively search for font files in subdirectories.

    .PARAMETER IgnoreErrors
        Ignore errors during installation and continue.

    .INPUTS
        None

    .OUTPUTS
        None

    .EXAMPLE
        Add-ADTFont -Path "$($adtSession.DirFiles)\MyFont.ttf"

        Installs the MyFont.ttf font file.

    .EXAMPLE
        Add-ADTFont -Path "$($adtSession.DirFiles)\Fonts" -Recurse

        Installs all font files in the Fonts directory and its subdirectories.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Add-ADTFont
    #>

    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SupportsWildcards()]
        [Alias('FullName')]
        [System.String[]]$Path,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IgnoreErrors
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Dictionary to map extensions to registry suffixes
        $fontTypes = @{
            '.ttf' = ' (TrueType)'
            '.ttc' = ' (TrueType)'
            '.otf' = ' (OpenType)'
        }

        $fontsDir = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows), 'Fonts')
        $fontsRegKeyPath = 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts'
    }

    process
    {
        foreach ($item in $Path)
        {
            try
            {
                try
                {
                    Write-ADTLogEntry -Message "Installing font [$item]..."

                    $resolvedPath = Resolve-Path -Path $item -ErrorAction Stop

                    # If it's a directory, get files
                    if (Test-Path -Path $resolvedPath -PathType Container)
                    {
                        $searchParams = @{
                            Path = $resolvedPath
                            File = $true
                        }
                        if ($Recurse) { $searchParams['Recurse'] = $true }

                        $files = Get-ChildItem @searchParams

                        foreach ($file in $files)
                        {
                            if ($fontTypes.ContainsKey($file.Extension.ToLower()))
                            {
                                Add-ADTFont -Path $file.FullName -IgnoreErrors:$IgnoreErrors
                                Write-ADTLogEntry -Message "Installed font [$($file.Name)]..."
                            }
                            else
                            {
                                Write-ADTLogEntry -Message "File [$($file.Name)] is not a supported font type. Skipping." -Severity 2
                            }
                        }
                    }
                    else
                    {
                        # It's a file
                        $fileItem = Get-Item -LiteralPath $resolvedPath -Force
                        $extension = $fileItem.Extension.ToLower()

                        if (-not $fontTypes.ContainsKey($extension))
                        {
                            Write-ADTLogEntry -Message "File [$($fileItem.Name)] is not a supported font type. Skipping." -Severity 2
                            continue
                        }

                        Write-ADTLogEntry -Message "Installing font [$($fileItem.Name)]..."

                        # 1. Copy file to Fonts directory
                        $destPath = Join-Path -Path $fontsDir -ChildPath $fileItem.Name

                        if (-not (Test-Path -LiteralPath $destPath))
                        {
                            Copy-Item -LiteralPath $fileItem.FullName -Destination $destPath -Force -ErrorAction Stop
                        }

                        # 2. Register font resource
                        try
                        {
                            $result = [PSADT.FontManagement.FontUtilities]::AddFont($destPath)

                            if ($result -eq 0)
                            {
                                throw "Failed to add font resource. Return code: $result"
                            }
                        }
                        catch
                        {
                            throw "Error calling AddFont: $_"
                        }

                        # Try to get font title using Shell.Application
                        $fontTitle = $fileItem.BaseName
                        try
                        {
                            $shell = New-Object -ComObject Shell.Application
                            $folder = $shell.Namespace($fileItem.DirectoryName)
                            $fileObj = $folder.ParseName($fileItem.Name)
                            # Column 21 is typically 'Title' or 'Font title' in Windows
                            $shellTitle = $folder.GetDetailsOf($fileObj, 21)
                            if (-not [string]::IsNullOrEmpty($shellTitle))
                            {
                                $fontTitle = $shellTitle
                            }
                        }
                        catch
                        {
                            Write-ADTLogEntry -Message "Could not retrieve font title from metadata, using filename..." -Severity 2
                        }

                        $regName = "$fontTitle$($fontTypes[$extension])"

                        Set-ADTRegistryKey -Key $fontsRegKeyPath -Name $regName -Value $fileItem.Name -ErrorAction Stop

                        Write-ADTLogEntry -Message "Successfully installed font [$($fileItem.Name)] as [$regName]."
                    }
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                $iafehParams = @{
                    Cmdlet = $PSCmdlet
                    SessionState = $ExecutionContext.SessionState
                    ErrorRecord = $_
                    LogMessage = "Failed to install font [$item]."
                }
                if ($IgnoreErrors)
                {
                    $iafehParams.Add('ErrorAction', [System.Management.Automation.ActionPreference]::SilentlyContinue)
                }
                Invoke-ADTFunctionErrorHandler @iafehParams
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
