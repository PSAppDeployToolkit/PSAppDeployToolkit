﻿#-----------------------------------------------------------------------------
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
        The Get-ADTIniValue function parses an INI file and returns the value of the specified section and key.

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

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTIniValue
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
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if ([string]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified section cannot be null, empty, or whitespace.'))
                }
                return $true
            })]
        [System.String]$Section,

        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if ([string]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified key cannot be null, empty, or whitespace.'))
                }
                return $true
            })]
        [System.String]$Key
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message "Reading INI value: [FilePath = $FilePath] [Section = $Section] [Key = $Key]."
        try
        {
            try
            {
                $iniValue = [PSADT.Utilities.IniUtilities]::GetSectionKeyValue($Section, $Key, $FilePath)
                Write-ADTLogEntry -Message "INI value: [Value = $iniValue]."
                return $iniValue
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to read INI value."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
