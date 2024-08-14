function Set-ADTIniValue
{
    <#

    .SYNOPSIS
    Opens an INI file and sets the value of the specified section and key.

    .DESCRIPTION
    Opens an INI file and sets the value of the specified section and key.

    .PARAMETER FilePath
    Path to the INI file.

    .PARAMETER Section
    Section within the INI file.

    .PARAMETER Key
    Key within the section of the INI file.

    .PARAMETER Value
    Value for the key within the section of the INI file. To remove a value, set this variable to $null.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any output.

    .EXAMPLE
    Set-ADTIniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName' -Value 'MyFile.ID'

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
            if (![System.IO.File]::Exists($_))
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'The specified file does not exist.'))
            }
            return !!$_
        })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Section,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Key,

        [Parameter(Mandatory = $true)]
        [AllowNull()]
        [System.Object]$Value
    )

    begin {
        Write-ADTDebugHeader
    }

    process {
        Write-ADTLogEntry -Message "Writing INI Key Value: [Section = $Section] [Key = $Key] [Value = $Value]."
        [PSADT.IniFile]::SetIniValue($Section, $Key, ([Text.StringBuilder]$Value), $FilePath)
    }

    end {
        Write-ADTDebugFooter
    }
}
