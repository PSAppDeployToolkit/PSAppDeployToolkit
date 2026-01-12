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

    .PARAMETER IgnoreErrors
        Ignore errors during removal and continue.

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
        [System.String[]]$Name,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IgnoreErrors
    )
    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        $fontsDir = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows), 'Fonts')
        $fontsRegKeyPath = 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts'
    }

    process
    {
        foreach ($fontName in $Name)
        {
            try
            {
                try
                {
                    Write-ADTLogEntry -Message "Removing font [$fontName]..."

                    $fileName = $fontName
                    $registryName = $null

                    # Check if the provided name is a registry value name or a file name
                    if (Test-Path -LiteralPath (Join-Path $fontsDir $fontName))
                    {
                        # It's a file name, we need to find the registry key that points to it
                        $fileName = $fontName

                        # Search registry for value data matching filename
                        $regValues = Get-ADTRegistryKey -Key $fontsRegKeyPath
                        if ($regValues)
                        {
                            $registryName = $regValues.PSObject.Properties | Where-Object { $_.Name -notlike 'PS*' -and $_.Value -eq $fileName } | Select-Object -First 1 -ExpandProperty Name
                        }
                    }
                    else
                    {
                        # Assume it's a registry name (Font Title)
                        $registryName = $fontName

                        # Get filename from registry
                        try
                        {
                            $fileName = Get-ADTRegistryKey -Key $fontsRegKeyPath -Name $registryName -ErrorAction Stop
                            if (-not $fileName)
                            {
                                throw "Registry value not found"
                            }
                        }
                        catch
                        {
                            # If not found in registry directly, maybe it was just a file name that doesn't exist?
                            Write-ADTLogEntry -Message "Font [$fontName] not found in registry or Fonts folder." -Severity 2
                            continue
                        }
                    }

                    $fontFilePath = Join-Path $fontsDir $fileName

                    # 1. Remove font resource
                    $result = [PSADT.FontManagement.FontUtilities]::RemoveFont($fontFilePath)

                    if (-not $result)
                    {
                        Write-ADTLogEntry -Message "Failed to remove font resource for [$fontFilePath]. It may not be loaded." -Severity 2
                    }

                    # 2. Delete registry value
                    if ($registryName)
                    {
                        Remove-ADTRegistryKey -Key $fontsRegKeyPath -Name $registryName
                    }

                    # 3. Delete file
                    if (Test-Path -LiteralPath $fontFilePath)
                    {
                        Remove-Item -LiteralPath $fontFilePath -Force -ErrorAction Stop
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
