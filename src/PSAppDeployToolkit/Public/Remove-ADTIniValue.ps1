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
        [ValidateNotNullOrEmpty()]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Section,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
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
                if ($null -eq $Key)
                {
                    Write-ADTLogEntry -Message "Removing INI section: [$Section]."
                }
                else
                {
                    Write-ADTLogEntry -Message "Removing INI value: [Section = $Section] [Key = $Key]."
                }

                # If $Key is null, it will remove the entire section, otherwise it will remove just the key. [NullString]::Value is used as $null was being interpreted as an empty string.
                [PSADT.Utilities.IniUtilities]::WriteSectionKeyValue($Section, $Key, [NullString]::Value, $FilePath)

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
