#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTIniValue
#
#-----------------------------------------------------------------------------

function Remove-ADTIniValue
{
    <#
    .SYNOPSIS
        Opens an INI file and removes the specified key or section.

    .DESCRIPTION
        Opens an INI file and removes the specified key or section.

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
        None

        This function does not return any output.

    .EXAMPLE
        Remove-ADTIniValue -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName'

        Removes the 'KeyFileName' key from the 'Notes' section of the 'notes.ini' file.

    .EXAMPLE
        Remove-ADTIniValue -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes'

        Removes the entire 'Notes' section of the 'notes.ini' file.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTIniValue
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
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Section -ProvidedValue $_ -ExceptionMessage 'The specified section cannot be null, empty, or whitespace.'))
                }
                return $true
            })]
        [System.String]$Section,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Key -ProvidedValue $_ -ExceptionMessage 'The specified key cannot be null, empty, or whitespace.'))
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
        try
        {
            try
            {
                Write-ADTLogEntry -Message "Removing INI value: [Section = $Section] [Key = $Key]."
                [PSADT.Utilities.IniUtilities]::WriteSectionKeyValue($FilePath, $Section, $Key, [NullString]::Value)
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to remove INI value."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
