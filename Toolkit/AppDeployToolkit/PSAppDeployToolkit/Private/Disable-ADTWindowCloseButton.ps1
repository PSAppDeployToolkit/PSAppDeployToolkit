function Disable-ADTWindowCloseButton
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({if (!$_ -or $_.Equals([System.IntPtr]::Zero)) {throw "The provided window handle is invalid."}; $_})]
        [System.IntPtr]$WindowHandle
    )

    if (($menuHandle = [PSADT.UiAutomation]::GetSystemMenu($WindowHandle, $false)) -and ($menuHandle -ne [System.IntPtr]::Zero))
    {
        [PSADT.UiAutomation]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
        [PSADT.UiAutomation]::DestroyMenu($menuHandle)
    }
}
