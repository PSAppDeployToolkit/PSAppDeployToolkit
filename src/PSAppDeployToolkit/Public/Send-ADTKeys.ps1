#-----------------------------------------------------------------------------
#
# MARK: Send-ADTKeys
#
#-----------------------------------------------------------------------------

function Send-ADTKeys
{
    <#
    .SYNOPSIS
        Send a sequence of keys to one or more application windows.

    .DESCRIPTION
        Send a sequence of keys to one or more application windows. If the window title searched for returns more than one window, then all of them will receive the sent keys.

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
        Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world'

        Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".

    .EXAMPLE
        Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world' -WaitSeconds 5

        Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.

    .EXAMPLE
        Send-ADTKeys -WindowHandle ([IntPtr]17368294) -Keys 'Hello World'

        Send the sequence of keys "Hello World" to the application with a Window Handle of '17368294'.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'WindowTitle')]
        [AllowEmptyString()]
        [ValidateNotNull()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $true, Position = 1, ParameterSetName = 'AllWindowTitles')]
        [System.Management.Automation.SwitchParameter]$GetAllWindowTitles,

        [Parameter(Mandatory = $true, Position = 2, ParameterSetName = 'WindowHandle')]
        [ValidateNotNullOrEmpty()]
        [System.IntPtr]$WindowHandle,

        [Parameter(Mandatory = $true, Position = 3, ParameterSetName = 'WindowTitle')]
        [Parameter(Mandatory = $true, Position = 3, ParameterSetName = 'AllWindowTitles')]
        [Parameter(Mandatory = $true, Position = 3, ParameterSetName = 'WindowHandle')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Keys,

        [Parameter(Mandatory = $false, Position = 4, ParameterSetName = 'WindowTitle')]
        [Parameter(Mandatory = $false, Position = 4, ParameterSetName = 'AllWindowTitles')]
        [Parameter(Mandatory = $false, Position = 4, ParameterSetName = 'WindowHandle')]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$WaitSeconds
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Internal worker filter.
        filter Send-ADTKeysToWindow
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
                [ValidateNotNullOrEmpty()]
                [System.IntPtr]$WindowHandle,

                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$Keys,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.Int32]$WaitSeconds
            )

            try
            {
                try
                {
                    # Bring the window to the foreground and make sure it's enabled.
                    if (![PSADT.GUI.UiAutomation]::BringWindowToFront($WindowHandle))
                    {
                        $naerParams = @{
                            Exception = [System.ApplicationException]::new('Failed to bring window to foreground.')
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'WindowHandleForegroundError'
                            TargetObject = $WindowHandle
                            RecommendedAction = "Please check the status of this window and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    if (![PSADT.PInvoke.NativeMethods]::IsWindowEnabled($WindowHandle))
                    {
                        $naerParams = @{
                            Exception = [System.ApplicationException]::new('Unable to send keys to window because it may be disabled due to a modal dialog being shown.')
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'WindowHandleDisabledError'
                            TargetObject = $WindowHandle
                            RecommendedAction = "Please check the status of this window and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }

                    # Send the Key sequence.
                    Write-ADTLogEntry -Message "Sending key(s) [$Keys] to window title [$($Window.WindowTitle)] with window handle [$WindowHandle]."
                    [System.Windows.Forms.SendKeys]::SendWait($Keys)
                    if ($WaitSeconds)
                    {
                        Write-ADTLogEntry -Message "Sleeping for [$WaitSeconds] seconds."
                        Start-Sleep -Seconds $WaitSeconds
                    }
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to send keys to window title [$($Window.WindowTitle)] with window handle [$WindowHandle].`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 3
            }
        }

        # Set up parameter splat for worker filter.
        $sktwParams = @{ Keys = $Keys }; if ($PSBoundParameters.ContainsKey('Keys')) { $sktwParams.Add('WaitSeconds', $WaitSeconds) }
    }

    process
    {
        try
        {
            try
            {
                # Process the specified input.
                if ($WindowHandle)
                {
                    if (!($Window = Get-ADTWindowTitle -GetAllWindowTitles | & { process { if ($_.WindowHandle -eq $WindowHandle) { return $_ } } } | Select-Object -First 1))
                    {
                        Write-ADTLogEntry -Message "No windows with Window Handle [$WindowHandle] were discovered." -Severity 2
                        return
                    }
                    Send-ADTKeysToWindow -WindowHandle $Window.WindowHandle @sktwParams
                }
                else
                {
                    if (!($AllWindows = if ($GetAllWindowTitles) { Get-ADTWindowTitle -GetAllWindowTitles $GetAllWindowTitles } else { Get-ADTWindowTitle -WindowTitle $WindowTitle } ))
                    {
                        Write-ADTLogEntry -Message 'No windows with the specified details were discovered.' -Severity 2
                        return
                    }
                    $AllWindows | Send-ADTKeysToWindow @sktwParams
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to send keys to specified window."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
