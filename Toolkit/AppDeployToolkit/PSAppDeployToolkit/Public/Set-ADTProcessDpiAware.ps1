#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Set-ADTProcessDpiAware
{
    # Use the most recent API supported by the operating system.
    if ($Host.Name.Equals('ConsoleHost'))
    {
        switch ([System.Environment]::OSVersion.Version)
        {
            {$_ -ge '10.0.15063.0'} {
                $null = [PSADT.UiAutomation]::SetProcessDpiAwarenessContext([PSADT.UiAutomation+DPI_AWARENESS_CONTEXT]::DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)
                break
            }
            {$_ -ge '10.0.14393.0'} {
                $null = [PSADT.UiAutomation]::SetProcessDpiAwarenessContext([PSADT.UiAutomation+DPI_AWARENESS_CONTEXT]::DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE)
                break
            }
            {$_ -ge '6.3.9600.0'} {
                $null = [PSADT.UiAutomation]::SetProcessDpiAwareness([PSADT.UiAutomation+PROCESS_DPI_AWARENESS]::PROCESS_PER_MONITOR_DPI_AWARE)
                break
            }
            {$_ -ge '6.0.6000.0'} {
                $null = [PSADT.UiAutomation]::SetProcessDPIAware()
                break
            }
        }
    }
}
