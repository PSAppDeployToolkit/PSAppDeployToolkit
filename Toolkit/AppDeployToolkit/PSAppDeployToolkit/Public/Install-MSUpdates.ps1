Function Install-MSUpdates {
    <#
.SYNOPSIS

Install all Microsoft Updates in a given directory.

.DESCRIPTION

Install all Microsoft Updates of type ".exe", ".msu", or ".msp" in a given directory (recursively search directory).

.PARAMETER Directory

Directory containing the updates.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Install-MSUpdates -Directory "$dirFiles\MSUpdates"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Directory
    )

    Begin {
        Write-ADTDebugHeader
    }
    Process {
        Write-ADTLogEntry -Message "Recursively installing all Microsoft Updates in directory [$Directory]."

        ## KB Number pattern match
        $kbPattern = '(?i)kb\d{6,8}'

        ## Get all hotfixes and install if required
        [IO.FileInfo[]]$files = Get-ChildItem -LiteralPath $Directory -Recurse -Include ('*.exe', '*.msu', '*.msp')
        ForEach ($file in $files) {
            If ($file.Name -match 'redist') {
                [Version]$redistVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName).ProductVersion
                [String]$redistDescription = [Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName).FileDescription

                Write-ADTLogEntry -Message "Installing [$redistDescription $redistVersion]..."
                #  Handle older redistributables (ie, VC++ 2005)
                If ($redistDescription -match 'Win32 Cabinet Self-Extractor') {
                    Execute-Process -Path $file.FullName -Parameters '/q' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                }
                Else {
                    Execute-Process -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                }
            }
            Else {
                #  Get the KB number of the file
                [String]$kbNumber = [RegEx]::Match($file.Name, $kbPattern).ToString()
                If (-not $kbNumber) {
                    Continue
                }

                #  Check to see whether the KB is already installed
                If (-not (Test-MSUpdates -KBNumber $kbNumber)) {
                    Write-ADTLogEntry -Message "KB Number [$KBNumber] was not detected and will be installed."
                    Switch ($file.Extension) {
                        #  Installation type for executables (i.e., Microsoft Office Updates)
                        '.exe' {
                            Execute-Process -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                        }
                        #  Installation type for Windows updates using Windows Update Standalone Installer
                        '.msu' {
                            Execute-Process -Path $Script:ADT.Environment.exeWusa -Parameters "`"$($file.FullName)`" /quiet /norestart" -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                        }
                        #  Installation type for Windows Installer Patch
                        '.msp' {
                            Execute-MSI -Action 'Patch' -Path $file.FullName -IgnoreExitCodes '*'
                        }
                    }
                }
                Else {
                    Write-ADTLogEntry -Message "KB Number [$kbNumber] is already installed. Continue..."
                }
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
