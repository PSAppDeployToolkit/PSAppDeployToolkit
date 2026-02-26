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
        foreach ($item in $Name)
        {
            try
            {
                try
                {
                    # Check if the provided name is a registry value name or a file name, respectively.
                    Write-ADTLogEntry -Message "Removing font [$item]..."
                    $regData = Get-ItemProperty -LiteralPath $fontsRegKeyPath
                    $fileName = $null; $displayName = $null
                    if (Test-Path -LiteralPath (Join-Path -Path $fontsDir -ChildPath $item))
                    {
                        # Get the display name from the registry.
                        $fileName = $item
                        if (!($displayName = $regData.PSObject.Properties | & { process { if ($_.Value -eq $fileName) { return $_.Name } } } | Select-Object -First 1))
                        {
                            $naerParams = @{
                                Exception = [System.ArgumentException]::new("The font [$item] could not be found in the registry.")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                                ErrorId = 'FontNotFoundError'
                                TargetObject = $item
                                RecommendedAction = "Please confirm the supplied value is correct and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                    }
                    else
                    {
                        # Get the file name from the registry.
                        $displayName = $item
                        if (!($fileName = $regData.PSObject.Properties | & { process { if ($_.Name -eq $displayName) { return $_.Value } } } | Select-Object -First 1))
                        {
                            $naerParams = @{
                                Exception = [System.ArgumentException]::new("The font [$item] could not be found in the registry.")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                                ErrorId = 'FontNotFoundError'
                                TargetObject = $item
                                RecommendedAction = "Please confirm the supplied value is correct and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                    }

                    # Remove font resource, delete registry value and remove remaining file.
                    [PSADT.Utilities.FontUtilities]::RemoveFont(($fontFilePath = Join-Path -Path $fontsDir -ChildPath $fileName))
                    if ($displayName)
                    {
                        Remove-ADTRegistryKey -Key $fontsRegKeyPath -Name $displayName -InformationAction SilentlyContinue
                    }
                    if (Test-Path -LiteralPath $fontFilePath)
                    {
                        Remove-Item -LiteralPath $fontFilePath -Force
                    }
                    Write-ADTLogEntry -Message "Successfully uninstalled font [$item]."
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
                    LogMessage = "Failed to uninstall font [$item]."
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
