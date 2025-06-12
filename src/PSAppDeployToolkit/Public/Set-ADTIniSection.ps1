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
		A hashtable or dictionary object containing the key-value pairs to set in the specified section. This will overwrite the entire section so that it only contains the content specified - if $null or an empty hashtable is provided, the section will be set to empty. Supply an ordered hashtable to maintain the order of keys in the INI file. Values can be strings, numbers, booleans, enums, or null.

    .PARAMETER Force
        Specifies whether the INI file should be created if it does not already exist.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Set-ADTIniSection -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Content @{'KeyFileName' = 'MyFile.ID'}

        Sets the 'Notes' section to only contain the content specified, supplied as hashtable.

    .EXAMPLE
        Set-ADTIniSection -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Content ([ordered]@{'KeyFileName' = 'MyFile.ID'; 'KeyFileType' = 'ID'})

        Sets the 'Notes' section to only contain the content specified, supplied as an ordered hashtable to maintain the desired order.

    .EXAMPLE
        Set-ADTIniSection -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Content @{}

       Sets the 'Notes' section to be empty by sending an empty hashtable.

    .EXAMPLE
        Set-ADTIniSection -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Content $null

       Sets the 'Notes' section to be empty by sending a null value.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTIniSection
    #>

    [CmdletBinding()]
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
                            ErrorId = 'FilePathNotFound'
                            TargetObject = $FilePath
                            RecommendedAction = "Please confirm the path of the specified file and try again, or add -Force to create a new file."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "Creating INI file: $FilePath."
                    $null = New-Item -Path $FilePath -ItemType File -Force
                }

                if ($null -ne $Content -and $Content.Count -gt 0)
                {
                    $logContent = $Content.GetEnumerator() | & { process { "`n$($_.Key)=$($_.Value)" } }
                    Write-ADTLogEntry -Message "Writing INI section: [FilePath = $FilePath] [Section = $Section] Content:$logContent"
                }
                else
                {
                    $Content = @{}
                    Write-ADTLogEntry -Message "Writing empty INI section: [FilePath = $FilePath] [Section = $Section]"
                }

                [PSADT.Utilities.IniUtilities]::WriteSection($FilePath, $Section, $Content)
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
