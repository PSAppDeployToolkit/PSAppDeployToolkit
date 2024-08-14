function Send-ADTKeys
{
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
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    # Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".
    Send-ADTKeys -WindowTitle 'foobar - Notepad' -Key 'Hello world'

    .EXAMPLE
    # Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.
    Send-ADTKeys -WindowTitle 'foobar - Notepad' -Key 'Hello world' -WaitSeconds 5

    .EXAMPLE
    # Send the sequence of keys "Hello World" to the application with a Window Handle of '17368294'.
    Send-ADTKeys -WindowHandle ([IntPtr]17368294) -Key 'Hello World'

    .LINK
    http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false, Position = 0)]
        [AllowEmptyString()]
        [ValidateNotNull()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $false, Position = 1)]
        [System.Management.Automation.SwitchParameter]$GetAllWindowTitles,

        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [System.IntPtr]$WindowHandle,

        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Keys,

        [Parameter(Mandatory = $false, Position = 4)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$WaitSeconds
    )

    begin {
        function Send-ADTKeysToWindow
        {
            [CmdletBinding()]
            param (
                [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
                [ValidateNotNullOrEmpty()]
                [System.IntPtr]$WindowHandle
            )

            try
            {
                # Bring the window to the foreground and make sure it's enabled.
                if (![PSADT.UiAutomation]::BringWindowToFront($WindowHandle))
                {
                    $naerParams = @{
                        Exception = [System.ApplicationException]::new('Failed to bring window to foreground.')
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'WindowHandleForegroundError'
                        TargetObject = $WindowHandle
                        RecommendedAction = "Please check the status of this window and try again."
                    }
                    Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
                }
                if (![PSADT.UiAutomation]::IsWindowEnabled($WindowHandle))
                {
                    $naerParams = @{
                        Exception = [System.ApplicationException]::new('Unable to send keys to window because it may be disabled due to a modal dialog being shown.')
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'WindowHandleDisabledError'
                        TargetObject = $WindowHandle
                        RecommendedAction = "Please check the status of this window and try again."
                    }
                    Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
                }

                # Send the Key sequence.
                Write-ADTLogEntry -Message "Sending key(s) [$Keys] to window title [$($Window.WindowTitle)] with window handle [$WindowHandle]."
                [Windows.Forms.SendKeys]::SendWait($Keys)
                if ($WaitSeconds)
                {
                    Write-ADTLogEntry -Message "Sleeping for [$WaitSeconds] seconds."
                    Start-Sleep -Seconds $WaitSeconds
                }
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to send keys to window title [$($Window.WindowTitle)] with window handle [$WindowHandle].`n$(Resolve-ADTError)" -Severity 3
            }
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet
    }

    process {
        try
        {
            if ($WindowHandle)
            {
                if (!($Window = Get-ADTWindowTitle -GetAllWindowTitles | Where-Object {$_.WindowHandle -eq $WindowHandle}))
                {
                    Write-ADTLogEntry -Message "No windows with Window Handle [$WindowHandle] were discovered." -Severity 2
                    return
                }
                Send-ADTKeysToWindow -WindowHandle $Window.WindowHandle
            }
            else
            {
                if (!($AllWindows = if ($GetAllWindowTitles) {Get-ADTWindowTitle -GetAllWindowTitles $GetAllWindowTitles} else {Get-ADTWindowTitle -WindowTitle $WindowTitle}))
                {
                    Write-ADTLogEntry -Message 'No windows with the specified details were discovered.' -Severity 2
                    return
                }
                $AllWindows | Send-ADTKeysToWindow
            }
        }
        catch
        {
            Write-ADTLogEntry -Message "Failed to send keys to specified window.`n$(Resolve-ADTError)" -Severity 3
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
