#-----------------------------------------------------------------------------
#
# MARK: Get-ADTShortcut
#
#-----------------------------------------------------------------------------

function Get-ADTShortcut
{
    <#
    .SYNOPSIS
        Get information from a .lnk or .url type shortcut.

    .DESCRIPTION
        The `Get-ADTShortcut` function gets information from a .lnk or .url type shortcut. Returns a IShortcutLinkInfo object with details about the shortcut such as TargetPath, Arguments, Description, and more.

    .PARAMETER LiteralPath
        Path to the shortcut to get information from.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.ShortcutManagement.IShortcutLinkInfo

        Returns an object with the following properties:
        - TargetPath
        - Arguments
        - Description
        - WorkingDirectory
        - WindowStyle
        - Hotkey
        - IconLocation
        - IconIndex
        - RunAsAdmin

    .EXAMPLE
        Get-ADTShortcut -LiteralPath "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk"

        Retrieves information from the specified .lnk shortcut.

    .NOTES
        An active ADT session is NOT required to use this function.

        Url shortcuts only support TargetPath, IconLocation, and IconIndex.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTShortcut
    #>

    [CmdletBinding()]
    [OutputType([PSADT.ShortcutManagement.IShortcutLinkInfo])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateScript({
                if (![System.IO.Path]::GetExtension($_).ToLowerInvariant().Equals('.lnk') -and ![System.IO.Path]::GetExtension($_).ToLowerInvariant().Equals('.url'))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LiteralPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not have the correct extension.'))
                }
                if (!(Test-Path -LiteralPath $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LiteralPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        try
        {
            $LiteralPath = Resolve-ADTFileSystemPath -LiteralPath $LiteralPath -File
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    process
    {
        try
        {
            try
            {
                if ([System.IO.Path]::GetExtension($LiteralPath) -eq '.url')
                {
                    return [PSADT.ShortcutManagement.InternetShortcutInfo]::Get($LiteralPath)
                }
                else
                {
                    return [PSADT.ShortcutManagement.ShellLinkInfo]::Get($LiteralPath)
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to read the shortcut [$($Output.Path)]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
