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
        Opens an INI file and sets the value of the specified section and key.

    .PARAMETER FilePath
        Path to the INI file.

    .PARAMETER Section
        Section within the INI file.

    .PARAMETER Key
        Key within the section of the INI file.

    .PARAMETER Value
        Value for the key within the section of the INI file.

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

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTIniValue
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true)]
        [ValidateScript ({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Section -ProvidedValue $_ -ExceptionMessage 'The specified section cannot be null, empty, or whitespace.'))
                }
                return $true
            })]
        [System.String]$Section,

        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Key -ProvidedValue $_ -ExceptionMessage 'The specified key cannot be null, empty, or whitespace.'))
                }
                return $true
            })]
        [System.String]$Key,

        [Parameter(Mandatory = $true)]
        [AllowNull()]
        [AllowEmptyString()]
        [System.String]$Value,

        [Parameter(Mandatory = $false)]
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
                if (!(Test-Path -LiteralPath $FilePath -PathType Leaf))
                {
                    if (!$Force)
                    {
                        $naerParams = @{
                            Exception = [System.IO.FileNotFoundException]::new("The file [$FilePath] is invalid or was unable to be found.")
                            Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                            ErrorId = 'FilePathNotFound'
                            TargetObject = $FilePath
                            RecommendedAction = "Please confirm the path of the specified file and try again, or add -Force to create a new file."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "Creating INI file: $FilePath."
                    if ($PSCmdlet.ShouldProcess($FilePath, 'Create INI file'))
                    {
                        $null = New-Item -Path $FilePath -ItemType File -Force
                    }
                }

                # Write out the section key/value pair to the file.
                Write-ADTLogEntry -Message "Writing INI value: [FilePath = $FilePath] [Section = $Section] [Key = $Key] [Value = $Value]."
                if ($PSCmdlet.ShouldProcess("$FilePath\$Section\$Key", "Set INI value to [$Value]"))
                {
                    [PSADT.Utilities.IniUtilities]::WriteSectionKeyValue($FilePath, $Section, $Key, $Value)
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to write INI value."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
