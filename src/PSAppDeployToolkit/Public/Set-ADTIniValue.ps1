#-----------------------------------------------------------------------------
#
# MARK: Set-ADTIniValue
#
#-----------------------------------------------------------------------------

function Set-ADTIniValue
{
    <#
    .SYNOPSIS
        Opens an INI file and sets the value of the specified section and key.

    .DESCRIPTION
        Opens an INI file and sets the value of the specified section and key. If the value is set to $null, the key will be removed from the section.

    .PARAMETER FilePath
        Path to the INI file.

    .PARAMETER Section
        Section within the INI file.

    .PARAMETER Key
        Key within the section of the INI file. To remove a section, set this parameter to $null.

    .PARAMETER Value
        Value for the key within the section of the INI file. To remove a value, set this parameter to $null.

    .PARAMETER Force
        Specifies whether the INI file should be created if it does not already exist.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Set-ADTIniValue -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName' -Value 'MyFile.ID'

        Sets the 'KeyFileName' key in the 'Notes' section of the 'notes.ini' file to 'MyFile.ID'.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTIniValue
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

        [Parameter(Mandatory = $true)]
        [AllowNull()][AllowEmptyString()]
        [System.String]$Key,

        [Parameter(Mandatory = $true)]
        [AllowNull()][AllowEmptyString()]
        [System.String]$Value,

        [System.Management.Automation.SwitchParameter]$Force
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
                # Create the INI file if it does not exist.
                if (![System.IO.File]::Exists($FilePath))
                {
                    if (!$Force)
                    {
                        $naerParams = @{
                            Exception = [System.IO.FileNotFoundException]::new("The file [$FilePath] is invalid or was unable to be found.")
                            Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                            ErrorId = 'PathFileNotFound'
                            TargetObject = $FilePath
                            RecommendedAction = "Please confirm the path of the specified file and try again, or add -Force to create a new file."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "Creating INI file: $FilePath."
                    $null = New-Item -Path $FilePath -ItemType File -Force
                }

                # Null keys/values removes the section/key, respectively.
                if ([System.String]::IsNullOrWhiteSpace($Key))
                {
                    Write-ADTLogEntry -Message "Removing INI Section: [$Section]."
                    [PSADT.Utilities.IniUtilities]::WriteSectionKeyValue($Section, $null, $null, $FilePath)
                }
                elseif ([System.String]::IsNullOrWhiteSpace($Value))
                {
                    Write-ADTLogEntry -Message "Removing INI Key Value: [Section = $Section] [Key = $Key]."
                    [PSADT.Utilities.IniUtilities]::WriteSectionKeyValue($Section, $Key, $null, $FilePath)
                }
                else
                {
                    Write-ADTLogEntry -Message "Writing INI Key Value: [Section = $Section] [Key = $Key] [Value = $Value]."
                    [PSADT.Utilities.IniUtilities]::WriteSectionKeyValue($Section, $Key, $Value, $FilePath)
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to write INI file key value."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
