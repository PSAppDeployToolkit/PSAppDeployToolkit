#-----------------------------------------------------------------------------
#
# MARK: Get-ADTIniValue
#
#-----------------------------------------------------------------------------

function Get-ADTIniValue
{
    <#
    .SYNOPSIS
        Parses an INI file and returns the value of the specified section and key.

    .DESCRIPTION
        The Get-ADTIniValue function parses an INI file and returns the value of the specified section and key. This function is useful for retrieving configuration settings stored in INI files.

    .PARAMETER FilePath
        Path to the INI file.

    .PARAMETER Section
        Section within the INI file.

    .PARAMETER Key
        Key within the section of the INI file.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the value of the specified section and key.

    .EXAMPLE
        Get-ADTIniValue -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName'

        This example retrieves the value of the 'KeyFileName' key in the 'Notes' section of the specified INI file.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
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
                $iniValue = [PSADT.Configuration.IniFile]::GetSectionKeyValue($Section, $Key, $FilePath)
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to read INI file key value."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
