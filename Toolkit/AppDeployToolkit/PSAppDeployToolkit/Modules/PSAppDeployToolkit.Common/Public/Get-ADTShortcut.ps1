function Get-ADTShortcut
{
    <#

    .SYNOPSIS
    Get information from a new .lnk or .url type shortcut

    .DESCRIPTION
    Get information from a new .lnk or .url type shortcut. Returns a hashtable.

    .PARAMETER Path
    Path to the shortcut to get information from.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.Collections.Hashtable. Returns a hashtable with the following keys:
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
    Get-ADTShortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk"

    .NOTES
    Url shortcuts only support TargetPath, IconLocation and IconIndex.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateScript({
            if (![System.IO.File]::Exists($_) -or (![System.IO.Path]::GetExtension($Path).ToLower().Equals('.lnk') -and ![System.IO.Path]::GetExtension($Path).ToLower().Equals('.url')))
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist or does not have the correct extension.'))
            }
            return !!$_
        })]
        [System.String]$Path
    )

    begin {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process {
        try
        {
            # Make sure .NET's current directory is synced with PowerShell's.
            try
            {
                [System.IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider FileSystem).ProviderPath)
                $Output = @{Path = [System.IO.Path]::GetFullPath($Path)}
            }
            catch
            {
                Write-ADTLogEntry -Message "Specified path [$Path] is not valid." -Severity 3
                throw
            }

            # Build out remainder of object.
            if ($Path -match '\.url$')
            {
                [System.IO.File]::ReadAllLines($Path).ForEach({
                    switch ($_)
                    {
                        {$_.StartsWith('URL=')} {$Output.TargetPath = $_.Replace('URL=', $null)}
                        {$_.StartsWith('IconIndex=')} {$Output.IconIndex = $_.Replace('IconIndex=', $null)}
                        {$_.StartsWith('IconFile=')} {$Output.IconLocation = $_.Replace('URIconFileL=', $null)}
                    }
                })
                return [PSADT.Types.ShortcutUrl]$Output
            }
            else
            {
                $shortcut = [System.Activator]::CreateInstance([System.Type]::GetTypeFromProgID('WScript.Shell')).CreateShortcut($FullPath)
                $Output.TargetPath = $shortcut.TargetPath
                $Output.Arguments = $shortcut.Arguments
                $Output.Description = $shortcut.Description
                $Output.WorkingDirectory = $shortcut.WorkingDirectory
                $Output.Hotkey = $shortcut.Hotkey
                $Output.IconLocation, $Output.IconIndex = $shortcut.IconLocation.Split(',')
                $Output.RunAsAdmin = !!([Systen.IO.FIle]::ReadAllBytes($FullPath)[21] -band 32)
                $Output.WindowStyle = switch ($shortcut.WindowStyle)
                {
                    1 {'Normal'}
                    3 {'Maximized'}
                    7 {'Minimized'}
                    default {'Normal'}
                }
                return [PSADT.Types.ShortcutLnk]$Output
            }
        }
        catch
        {
            Write-ADTLogEntry -Message "Failed to read the shortcut [$Path].`n$(Resolve-ADTError)" -Severity 3
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
