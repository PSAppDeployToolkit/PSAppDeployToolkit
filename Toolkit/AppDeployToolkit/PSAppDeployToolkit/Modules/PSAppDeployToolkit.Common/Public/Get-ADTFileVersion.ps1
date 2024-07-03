function Get-ADTFileVersion
{
    <#

    .SYNOPSIS
    Gets the version of the specified file

    .DESCRIPTION
    Gets the version of the specified file

    .PARAMETER File
    Path of the file

    .PARAMETER ProductVersion
    Switch that makes the command return ProductVersion instead of FileVersion

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the version of the specified file.

    .EXAMPLE
    Get-ADTFileVersion -File "$envProgramFilesX86\Adobe\Reader 11.0\Reader\AcroRd32.exe"

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
            if (!$_.VersionInfo)
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName File -ProvidedValue $_ -ExceptionMessage 'The file does not exist or does not have any version info.'))
            }
            return !!$_.VersionInfo
        })]
        [System.IO.FileInfo]$File,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ProductVersion
    )

    begin {
        Initialize-ADTFunction -Cmdlet $PSCmdlet
    }

    process {
        if ($ProductVersion)
        {
            Write-ADTLogEntry -Message "Product version is [$($File.VersionInfo.ProductVersion)]."
            return $File.VersionInfo.ProductVersion
        }
        Write-ADTLogEntry -Message "File version is [$($File.VersionInfo.FileVersion)]."
        return $File.VersionInfo.FileVersion
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
