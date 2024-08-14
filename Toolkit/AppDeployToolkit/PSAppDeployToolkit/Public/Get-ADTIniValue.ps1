#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTIniValue
{
    <#

    .SYNOPSIS
    Parses an INI file and returns the value of the specified section and key.

    .DESCRIPTION
    Parses an INI file and returns the value of the specified section and key.

    .PARAMETER FilePath
    Path to the INI file.

    .PARAMETER Section
    Section within the INI file.

    .PARAMETER Key
    Key within the section of the INI file.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the value of the specified section and key.

    .EXAMPLE
    Get-ADTIniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName'

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
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
        [System.String]$Key
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message "Reading INI Key: [Section = $Section] [Key = $Key]."
        try
        {
            try
            {
                $iniValue = [PSADT.IniFile]::GetIniValue($Section, $Key, $FilePath)
                Write-ADTLogEntry -Message "INI Key Value: [Section = $Section] [Key = $Key] [Value = $iniValue]."
                return $iniValue
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
