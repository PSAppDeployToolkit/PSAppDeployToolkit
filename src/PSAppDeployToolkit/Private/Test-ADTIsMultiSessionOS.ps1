#-----------------------------------------------------------------------------
#
# MARK: Test-ADTIsMultiSessionOS
#
#-----------------------------------------------------------------------------

function Private:Test-ADTIsMultiSessionOS
{
    # The registry is significantly cheaper to query than a CIM instance.
    # https://www.jasonsamuel.com/2020/03/02/how-to-use-microsoft-wvd-windows-10-multi-session-fslogix-msix-app-attach-to-build-an-azure-powered-virtual-desktop-experience/
    return ([Microsoft.Win32.Registry]::GetValue('HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion', 'ProductName', $null) -match '^Microsoft Windows \d+ Enterprise (for Virtual Desktops|Multi-Session)$')
}
