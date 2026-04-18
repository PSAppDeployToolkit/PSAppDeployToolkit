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
        The `Remove-ADTIniValue` function opens an INI file and removes the specified key or section.

        Please note that the INI file provided cannot have a byte order mark (BOM) present as the underlying Win32 API cannot process it correctly.

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

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTIniValue
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Section,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Key
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        try
        {
            $FilePath = Resolve-ADTFileSystemPath -LiteralPath $FilePath -File
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
                Write-ADTLogEntry -Message "Removing INI value: [Section = $Section] [Key = $Key]."
                if ($PSCmdlet.ShouldProcess("$FilePath\$Section\$Key", 'Remove INI value'))
                {
                    [PSADT.Utilities.IniUtilities]::WriteSectionKeyValue($FilePath, $Section, $Key, [System.Management.Automation.Language.NullString]::Value)
                }
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
