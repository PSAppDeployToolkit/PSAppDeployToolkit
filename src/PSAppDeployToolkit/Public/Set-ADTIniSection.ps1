#-----------------------------------------------------------------------------
#
# MARK: Get-ADTIniSection
#
#-----------------------------------------------------------------------------

function Set-ADTIniSection
{
    <#
    .SYNOPSIS
        Opens an INI file and sets the values of the specified section.

    .DESCRIPTION
        Opens an INI file and sets the values of the specified section.

    .PARAMETER FilePath
        Path to the INI file.

    .PARAMETER Section
        Section within the INI file.

	.PARAMETER Content
		A hashtable or dictionary object containing the key-value pairs to set in the specified section.
        Supply an ordered hashtable to preserve the order of supplied entries. Values can be strings, numbers, booleans, enums, or null.
        Supply $null or an empty hashtable in combination with -Overwrite to empty an entire section.

    .PARAMETER Overwrite
        Specifies whether the provided INI content should overwrite all existing section content.

    .PARAMETER Force
        Specifies whether the INI file should be created if it does not already exist.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.


    .EXAMPLE
        Set-ADTIniSection -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Content ([ordered]@{'KeyFileName' = 'MyFile.ID'; 'KeyFileType' = 'ID'})

        Adds the provided content to the 'Notes' section, preserving input order

    .EXAMPLE
        Set-ADTIniSection -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Content @{'KeyFileName' = 'MyFile.ID'} -Overwrite

        Overwrites the 'Notes' section to only contain the content specified.


    .EXAMPLE
        Set-ADTIniSection -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Content $null -Overwrite

        Sets the 'Notes' section to be empty by sending null content in combination with the -Overwrite switch.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTIniSection
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
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

        [Parameter(Mandatory = $true)]
        [AllowNull()]
        [AllowEmptyCollection()]
        [System.Collections.IDictionary]$Content,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Overwrite,

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
                    $null = New-Item -Path $FilePath -ItemType File -Force
                }

                if ($null -eq $Content)
                {
                    $Content = @{}
                }

                if (!$Overwrite)
                {
                    if ($Content.Count -eq 0)
                    {
                        Write-ADTLogEntry -Message "No content provided to write to INI section: [FilePath = $FilePath] [Section = $Section]."
                        return
                    }
                    try
                    {
                        $writeContent = [PSADT.Utilities.IniUtilities]::GetSection($FilePath, $Section)
                        foreach ($key in $Content.Keys)
                        {
                            $writeContent[$key] = $Content[$key]
                        }
                    }
                    catch
                    {
                        # Expected to end up here if the section does not currently exist
                        $writeContent = $Content
                    }
                }
                else
                {
                    $writeContent = $Content
                }

                Write-ADTLogEntry -Message "$(('Writing', 'Overwriting')[$Overwrite.ToBool()]) INI section: [FilePath = $FilePath] [Section = $Section] Content:$($Content.GetEnumerator() | & { process { "`n$($_.Key)=$($_.Value)" } })"
                if ($PSCmdlet.ShouldProcess("$FilePath\$Section", "$(('Write', 'Overwrite')[$Overwrite.ToBool()]) INI section"))
                {
                    [PSADT.Utilities.IniUtilities]::WriteSection($FilePath, $Section, $writeContent)
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to write INI section."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
