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

    .PARAMETER LiteralPath
        The path to the font file or directory containing font files.

    .PARAMETER Recurse
        Recursively search for font files in subdirectories.

    .INPUTS
        None

    .OUTPUTS
        None

    .EXAMPLE
        Add-ADTFont -LiteralPath "$($adtSession.DirFiles)\MyFont.ttf"

        Installs the MyFont.ttf font file.

    .EXAMPLE
        Add-ADTFont -LiteralPath "$($adtSession.DirFiles)\Fonts" -Recurse

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
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $fontsDir = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows), 'Fonts')
        $fontsRegKeyPath = 'Microsoft.Win32.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts'

        # Dictionary to map extensions to registry suffixes
        $fontTypes = @{
            '.ttf' = ' (TrueType)'
            '.ttc' = ' (TrueType)'
            '.otf' = ' (OpenType)'
        }
    }

    process
    {
        foreach ($item in $LiteralPath)
        {
            try
            {
                try
                {
                    # Test whether we've got a file or directory.
                    $resolvedPath = Resolve-Path -Path $item
                    if (Test-Path -Path $resolvedPath -PathType Leaf)
                    {
                        # If we're here, it's a file. Make sure it's valid before proceeding.
                        $fileItem = Get-Item -LiteralPath $resolvedPath -Force
                        $extension = $fileItem.Extension.ToLower()
                        if (!$fontTypes.ContainsKey($extension))
                        {
                            $naerParams = @{
                                Exception = [System.ArgumentException]::new("File [$($fileItem.Name)] is not a supported font type.")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                                ErrorId = 'FontFileUnsupportedExtensionError'
                                TargetObject = $fileItem
                                RecommendedAction = "Please confirm the supplied value is correct and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }

                        # Copy file to Fonts directory.
                        Write-ADTLogEntry -Message "Installing font [$($fileItem.Name)]..."
                        $destPath = Join-Path -Path $fontsDir -ChildPath $fileItem.Name
                        if (!(Test-Path -LiteralPath $destPath))
                        {
                            Copy-Item -LiteralPath $fileItem.FullName -Destination $destPath -Force
                        }

                        # Register font resource and set up the font name correctly in the registry.
                        $null = [PSADT.Utilities.FontUtilities]::AddFont($destPath)
                        $regName = "$([PSADT.Utilities.FontUtilities]::GetFontTitle($destPath))$($fontTypes[$extension])"
                        Set-ADTRegistryKey -Key $fontsRegKeyPath -Name $regName -Value $fileItem.Name
                        Write-ADTLogEntry -Message "Successfully installed font [$($fileItem.Name)] as [$regName]."
                    }
                    elseif (Test-Path -Path $resolvedPath -PathType Container)
                    {
                        # We've got a directory. Get all the files and pipe them through.
                        $null = $PSBoundParameters.Remove('LiteralPath')
                        Get-ChildItem -Path $resolvedPath -File -Recurse:$Recurse | Add-ADTFont @PSBoundParameters
                        continue
                    }
                    else
                    {
                        # Whatever we have isn't valid. Throw and let the caller handle it.
                        $naerParams = @{
                            Exception = [System.ArgumentException]::new("The specified LiteralPath of [$item] could not be found.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                            ErrorId = 'LiteralPathInvalidError'
                            TargetObject = $item
                            RecommendedAction = "Please confirm the supplied value is correct and try again."
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
                $iafehParams = @{
                    Cmdlet = $PSCmdlet
                    SessionState = $ExecutionContext.SessionState
                    ErrorRecord = $_
                    LogMessage = "Failed to install font [$item]."
                }
                if ($PSBoundParameters.ContainsKey('ErrorAction'))
                {
                    $iafehParams.Add('ErrorAction', $PSBoundParameters.ErrorAction)
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
