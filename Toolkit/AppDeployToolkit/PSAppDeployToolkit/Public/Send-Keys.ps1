Function Send-Keys {
    <#
.SYNOPSIS

Send a sequence of keys to one or more application windows.

.DESCRIPTION

Send a sequence of keys to one or more application window. If window title searched for returns more than one window, then all of them will receive the sent keys.

Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

.PARAMETER WindowTitle

The title of the application window to search for using regex matching.

.PARAMETER GetAllWindowTitles

Get titles for all open windows on the system.

.PARAMETER WindowHandle

Send keys to a specific window where the Window Handle is already known.

.PARAMETER Keys

The sequence of keys to send. Info on Key input at: http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

.PARAMETER WaitSeconds

An optional number of seconds to wait after the sending of the keys.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world'

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".

.EXAMPLE

Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world' -WaitSeconds 5

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.

.EXAMPLE

Send-Keys -WindowHandle ([IntPtr]17368294) -Key 'Hello world'

Send the sequence of keys "Hello world" to the application with a Window Handle of '17368294'.

.NOTES

.LINK

http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false, Position = 0)]
        [AllowEmptyString()]
        [ValidateNotNull()]
        [String]$WindowTitle,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullorEmpty()]
        [Switch]$GetAllWindowTitles = $false,
        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullorEmpty()]
        [IntPtr]$WindowHandle,
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullorEmpty()]
        [String]$Keys,
        [Parameter(Mandatory = $false, Position = 4)]
        [ValidateNotNullorEmpty()]
        [Int32]$WaitSeconds
    )

    Begin {
        Write-ADTDebugHeader

        [ScriptBlock]$SendKeys = {
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [IntPtr]$WindowHandle
            )
            Try {
                ## Bring the window to the foreground
                [Boolean]$IsBringWindowToFrontSuccess = [PSADT.UiAutomation]::BringWindowToFront($WindowHandle)
                If (-not $IsBringWindowToFrontSuccess) {
                    Throw 'Failed to bring window to foreground.'
                }

                ## Send the Key sequence
                If ($Keys) {
                    If (-not [PSADT.UiAutomation]::IsWindowEnabled($WindowHandle)) {
                        Throw 'Unable to send keys to window because it may be disabled due to a modal dialog being shown.'
                    }
                    Write-ADTLogEntry -Message "Sending key(s) [$Keys] to window title [$($Window.WindowTitle)] with window handle [$WindowHandle]."
                    [Windows.Forms.SendKeys]::SendWait($Keys)
                    If ($WaitSeconds) {
                        Write-ADTLogEntry -Message "Sleeping for [$WaitSeconds] seconds."
                        Start-Sleep -Seconds $WaitSeconds
                    }
                }
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to send keys to window title [$($Window.WindowTitle)] with window handle [$WindowHandle]. `r`n$(Resolve-Error)" -Severity 3
            }
        }
    }
    Process {
        Try {
            If ($WindowHandle) {
                [PSObject]$Window = Get-ADTWindowTitle -GetAllWindowTitles | Where-Object { $_.WindowHandle -eq $WindowHandle }
                If (-not $Window) {
                    Write-ADTLogEntry -Message "No windows with Window Handle [$WindowHandle] were discovered." -Severity 2
                    Return
                }
                & $SendKeys -WindowHandle $Window.WindowHandle
            }
            Else {
                [Hashtable]$GetWindowTitleSplat = @{}
                If ($GetAllWindowTitles) {
                    $GetWindowTitleSplat.Add( 'GetAllWindowTitles', $GetAllWindowTitles)
                }
                Else {
                    $GetWindowTitleSplat.Add( 'WindowTitle', $WindowTitle)
                }
                [PSObject[]]$AllWindows = Get-ADTWindowTitle @GetWindowTitleSplat
                If (-not $AllWindows) {
                    Write-ADTLogEntry -Message 'No windows with the specified details were discovered.' -Severity 2
                    Return
                }

                ForEach ($Window in $AllWindows) {
                    & $SendKeys -WindowHandle $Window.WindowHandle
                }
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to send keys to specified window. `r`n$(Resolve-Error)" -Severity 3
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
