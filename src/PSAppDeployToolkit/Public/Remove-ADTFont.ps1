#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTFont
#
#-----------------------------------------------------------------------------

function Remove-ADTFont
{
    <#
    .SYNOPSIS
        Removes a font from the system.

    .DESCRIPTION
        Removes a font from the system by removing the font resource, deleting the registry entry, and removing the file from the Windows Fonts directory.

    .PARAMETER Name
        The name of the font file (e.g., 'arial.ttf') or the font name as it appears in the registry.

    .INPUTS
        None

    .OUTPUTS
        None

    .EXAMPLE
        Remove-ADTFont -Name 'MyFont.ttf'

        Removes the MyFont.ttf font.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTFont
    #>

    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Name
    )
    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $fontsDir = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows), 'Fonts')
        $fontsRegKeyPath = 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts'
    }

    process
    {
        foreach ($fontName in $Name)
        {
            try
            {
                try
                {
                    # Check if the provided name is a registry value name or a file name, respectively.
                    Write-ADTLogEntry -Message "Removing font [$fontName]..."
                    $fileName = $null; $registryName = $null
                    if (Test-Path -Name (Join-Path $fontsDir $fontName))
                    {
                        # Get filename from registry.
                        $registryName = $fontName
                        if (!($fileName = Get-ADTRegistryKey -Key $fontsRegKeyPath -Name $registryName))
                        {
                            $naerParams = @{
                                Exception = [System.ArgumentException]::new("Font [$fontName] not found in registry or Fonts folder.")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                                ErrorId = 'FontNotFoundError'
                                TargetObject = $fontName
                                RecommendedAction = "Please confirm the supplied value is correct and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                    }
                    else
                    {
                        # Search registry for value data matching filename.
                        $fileName = $fontName
                        if (($regValues = Get-ADTRegistryKey -Key $fontsRegKeyPath))
                        {
                            $registryName = $regValues.PSObject.Properties | & { process { if (!$_.Name.StartsWith('PS*') -and ($_.Value -eq $fileName)) { return $_ } } } | Select-Object -First 1 -ExpandProperty Name
                        }
                    }

                    # Remove font resource, delete registry value and remove remaining file.
                    $null = [PSADT.Utilities.FontUtilities]::RemoveFont(($fontFilePath = Join-Path $fontsDir $fileName))
                    if ($registryName)
                    {
                        Remove-ADTRegistryKey -Key $fontsRegKeyPath -Name $registryName
                    }
                    if (Test-Path -LiteralPath $fontFilePath)
                    {
                        Remove-Item -LiteralPath $fontFilePath -Force
                    }
                    Write-ADTLogEntry -Message "Successfully uninstalled font [$fontName]."
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
                    LogMessage = "Failed to uninstall font [$fontName]."
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
