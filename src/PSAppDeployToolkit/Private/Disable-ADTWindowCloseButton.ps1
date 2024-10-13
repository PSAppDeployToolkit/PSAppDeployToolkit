#-----------------------------------------------------------------------------
#
# MARK: Disable-ADTWindowCloseButton
#
#-----------------------------------------------------------------------------

function Disable-ADTWindowCloseButton
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (($null -eq $_) -or $_.Equals([System.IntPtr]::Zero))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName WindowHandle -ProvidedValue $_ -ExceptionMessage 'The provided window handle is invalid.'))
                }
                return !!$_
            })]
        [System.IntPtr]$WindowHandle
    )

    $null = if (($menuHandle = [PSADT.PInvoke.NativeMethods]::GetSystemMenu($WindowHandle, $false)) -and ($menuHandle -ne [System.IntPtr]::Zero))
    {
        [PSADT.PInvoke.NativeMethods]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
        [PSADT.PInvoke.NativeMethods]::DestroyMenu($menuHandle)
    }
}
