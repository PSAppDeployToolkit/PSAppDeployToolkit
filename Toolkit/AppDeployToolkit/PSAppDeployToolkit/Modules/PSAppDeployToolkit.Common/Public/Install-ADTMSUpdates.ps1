function Install-ADTMSUpdates
{
    <#

    .SYNOPSIS
    Install all Microsoft Updates in a given directory.

    .DESCRIPTION
    Install all Microsoft Updates of type ".exe", ".msu", or ".msp" in a given directory (recursively search directory).

    .PARAMETER Directory
    Directory containing the updates.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Install-ADTMSUpdates -Directory "$dirFiles\MSUpdates"

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Directory
    )

    begin {
        # KB Number pattern match.
        $kbPattern = '(?i)kb\d{6,8}'
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process {
        # Get all hotfixes and install if required.
        Write-ADTLogEntry -Message "Recursively installing all Microsoft Updates in directory [$Directory]."
        foreach ($file in (Get-ChildItem -LiteralPath $Directory -Recurse -Include ('*.exe', '*.msu', '*.msp')))
        {
            if ($file.Name -match 'redist')
            {
                # Handle older redistributables (ie, VC++ 2005)
                [System.Version]$redistVersion = $file.VersionInfo.ProductVersion
                [System.String]$redistDescription = $file.VersionInfo.FileDescription
                Write-ADTLogEntry -Message "Installing [$redistDescription $redistVersion]..."
                if ($redistDescription -match 'Win32 Cabinet Self-Extractor')
                {
                    Start-ADTProcess -Path $file.FullName -Parameters '/q' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                }
                else
                {
                    Start-ADTProcess -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                }
            }
            elseif ($kbNumber = [System.Text.RegularExpressions.Regex]::Match($file.Name, $kbPattern).ToString())
            {
                # Check to see whether the KB is already installed
                if (Test-ADTMSUpdates -KBNumber $kbNumber)
                {
                    Write-ADTLogEntry -Message "KB Number [$kbNumber] is already installed. Continue..."
                    continue
                }
                Write-ADTLogEntry -Message "KB Number [$KBNumber] was not detected and will be installed."
                switch ($file.Extension)
                {
                    '.exe' {
                        # Installation type for executables (i.e., Microsoft Office Updates).
                        Start-ADTProcess -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                    }
                    '.msu' {
                        # Installation type for Windows updates using Windows Update Standalone Installer.
                        Start-ADTProcess -Path "$([System.Environment]::SystemDirectory)\wusa.exe" -Parameters "`"$($file.FullName)`" /quiet /norestart" -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                    }
                    '.msp' {
                        # Installation type for Windows Installer Patch
                        Start-ADTMsiProcess -Action 'Patch' -Path $file.FullName -IgnoreExitCodes '*'
                    }
                }
            }
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
