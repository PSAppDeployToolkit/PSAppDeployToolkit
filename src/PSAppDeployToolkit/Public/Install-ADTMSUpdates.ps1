#-----------------------------------------------------------------------------
#
# MARK: Install-ADTMSUpdates
#
#-----------------------------------------------------------------------------

function Install-ADTMSUpdates
{
    <#
    .SYNOPSIS
        Install all Microsoft Updates in a given directory.

    .DESCRIPTION
        Install all Microsoft Updates of type ".exe", ".msu", or ".msp" in a given directory (recursively search directory). The function will check if the update is already installed and skip it if it is. It handles older redistributables and different types of updates appropriately.

    .PARAMETER Directory
        Directory containing the updates.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Install-ADTMSUpdates -Directory "$($adtSession.DirFiles)\MSUpdates"

        Installs all Microsoft Updates found in the specified directory.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Install-ADTMSUpdates
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Directory
    )

    begin
    {
        # Announce deprecation to callers.
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated and will be removed in PSAppDeployToolkit 4.2.0. Please raise a case at [https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues] if you require this function." -Severity 2
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $kbPattern = '(?i)kb\d{6,8}'
    }

    process
    {
        # Get all hotfixes and install if required.
        Write-ADTLogEntry -Message "Recursively installing all Microsoft Updates in directory [$Directory]."
        foreach ($file in (Get-ChildItem -LiteralPath $Directory -Recurse -Include ('*.exe', '*.msu', '*.msp')))
        {
            try
            {
                try
                {
                    if ($file.Name -match 'redist')
                    {
                        # Handle older redistributables (ie, VC++ 2005)
                        [System.Version]$redistVersion = $file.VersionInfo.ProductVersion
                        [System.String]$redistDescription = $file.VersionInfo.FileDescription
                        Write-ADTLogEntry -Message "Installing [$redistDescription $redistVersion]..."
                        if ($redistDescription -match 'Win32 Cabinet Self-Extractor')
                        {
                            Start-ADTProcess -FilePath $file.FullName -ArgumentList '/q' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                        }
                        else
                        {
                            Start-ADTProcess -FilePath $file.FullName -ArgumentList '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                        }
                    }
                    elseif ($kbNumber = [System.Text.RegularExpressions.Regex]::Match($file.Name, $kbPattern).ToString())
                    {
                        # Check to see whether the KB is already installed
                        if (Test-ADTMSUpdates -KbNumber $kbNumber)
                        {
                            Write-ADTLogEntry -Message "KB Number [$kbNumber] is already installed. Continue..."
                            continue
                        }
                        Write-ADTLogEntry -Message "KB Number [$KBNumber] was not detected and will be installed."
                        switch ($file.Extension)
                        {
                            '.exe'
                            {
                                # Installation type for executables (i.e., Microsoft Office Updates).
                                Start-ADTProcess -FilePath $file.FullName -ArgumentList '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                                break
                            }
                            '.msu'
                            {
                                # Installation type for Windows updates using Windows Update Standalone Installer.
                                Start-ADTProcess -FilePath "$([System.Environment]::SystemDirectory)\wusa.exe" -ArgumentList "`"$($file.FullName)`" /quiet /norestart" -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                                break
                            }
                            '.msp'
                            {
                                # Installation type for Windows Installer Patch
                                Start-ADTMsiProcess -Action 'Patch' -Path $file.FullName -IgnoreExitCodes '*'
                                break
                            }
                        }
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
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
