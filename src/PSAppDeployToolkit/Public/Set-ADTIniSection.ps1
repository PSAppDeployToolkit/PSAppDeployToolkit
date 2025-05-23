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
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if ([string]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified section cannot be null, empty, or whitespace.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [string]$Section,

        [Parameter(Mandatory = $true)]
        [AllowNull()]
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

                if ($null -eq $Content -or $Content.Count -eq 0)
                {
                    $Content = @{}
                    Write-ADTLogEntry -Message "Writing empty INI section: [FilePath = $FilePath] [Section = $Section]"
                }
                else
                {
                    $logContent = $Content.GetEnumerator() | & { process { "`n$($_.Key)=$($_.Value)" } }
                    Write-ADTLogEntry -Message "Writing INI section: [FilePath = $FilePath] [Section = $Section] Content:$logContent"
                }

                [PSADT.Utilities.IniUtilities]::WriteSection($Section, $Content, $FilePath)
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
