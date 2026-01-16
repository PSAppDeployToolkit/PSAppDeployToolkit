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
        Get information from a .lnk or .url type shortcut. Returns a hashtable with details about the shortcut such as TargetPath, Arguments, Description, and more.

    .PARAMETER LiteralPath
        Path to the shortcut to get information from.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.ShortcutBase

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
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTShortcut
    #>

    [CmdletBinding()]
    [OutputType([PSADT.Types.ShortcutUrl])]
    [OutputType([PSADT.Types.ShortcutLnk])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateScript({
                if (!(Test-Path -LiteralPath $_ -PathType Leaf) -or (![System.IO.Path]::GetExtension($_).ToLowerInvariant().Equals('.lnk') -and ![System.IO.Path]::GetExtension($_).ToLowerInvariant().Equals('.url')))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist or does not have the correct extension.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        # Make sure .NET's current directory is synced with PowerShell's.
        try
        {
            try
            {
                [System.IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider FileSystem).ProviderPath)
                $Output = @{ Path = (Get-Item -LiteralPath $LiteralPath).FullName; TargetPath = $null; IconIndex = $null; IconLocation = $null }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Specified path [$LiteralPath] is not valid."
            return
        }

        try
        {
            try
            {
                # Build out remainder of object.
                if ([System.IO.Path]::GetExtension($Output.Path) -eq '.url')
                {
                    [System.IO.File]::ReadAllLines($Output.Path) | & {
                        process
                        {
                            switch ($_)
                            {
                                { $_.StartsWith('URL=') } { $Output.TargetPath = $_.Replace('URL=', [System.Management.Automation.Language.NullString]::Value); break }
                                { $_.StartsWith('IconIndex=') } { $Output.IconIndex = $_.Replace('IconIndex=', [System.Management.Automation.Language.NullString]::Value); break }
                                { $_.StartsWith('IconFile=') } { $Output.IconLocation = $_.Replace('IconFile=', [System.Management.Automation.Language.NullString]::Value); break }
                            }
                        }
                    }
                    return [PSADT.Types.ShortcutUrl]::new(
                        $Output.Path,
                        $Output.TargetPath,
                        $Output.IconLocation,
                        $Output.IconIndex
                    )
                }
                else
                {
                    $shortcut = [System.Activator]::CreateInstance([System.Type]::GetTypeFromProgID('WScript.Shell')).CreateShortcut($Output.Path)
                    $Output.IconLocation, $Output.IconIndex = $shortcut.IconLocation.Split(',').Trim()
                    return [PSADT.Types.ShortcutLnk]::new(
                        $Output.Path,
                        $shortcut.TargetPath,
                        $Output.IconLocation,
                        $Output.IconIndex,
                        $shortcut.Arguments,
                        $shortcut.Description,
                        $shortcut.WorkingDirectory,
                        $(switch ($shortcut.WindowStyle)
                            {
                                1 { 'Normal'; break }
                                3 { 'Maximized'; break }
                                7 { 'Minimized'; break }
                                default { 'Normal'; break }
                            }),
                        $shortcut.Hotkey,
                        !!([System.IO.File]::ReadAllBytes($Output.Path)[21] -band 32)
                    )
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
