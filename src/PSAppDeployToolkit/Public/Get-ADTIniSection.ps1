#-----------------------------------------------------------------------------
#
# MARK: Get-ADTIniSection
#
#-----------------------------------------------------------------------------

function Get-ADTIniSection
{
    <#
    .SYNOPSIS
        Parses an INI file and returns the specified section as an ordered hashtable of key value pairs.

    .DESCRIPTION
        Parses an INI file and returns the specified section as an ordered hashtable of key value pairs.

    .PARAMETER FilePath
        Path to the INI file.

    .PARAMETER Section
        Section within the INI file.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        Collections.Specialized.OrderedDictionary

        Returns the value of the specified section and key.

    .EXAMPLE
        Get-ADTIniSection -FilePath "$env:ProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes'

        This example retrieves the section of the 'Notes' of the specified INI file.

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
    [OutputType([Collections.Specialized.OrderedDictionary])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (!(Test-Path -LiteralPath $_ -PathType Leaf))
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
        [System.String]$Section
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message "Reading INI section: [FilePath = $FilePath] [Section = $Section]."
        try
        {
            try
            {
                # Get the section from the INI file
                $iniSection = [PSADT.Utilities.IniUtilities]::GetSection($FilePath, $Section)

                if ($null -eq $iniSection -or $iniSection.Count -eq 0)
                {
                    Write-ADTLogEntry -Message "INI section is empty."
                }
                else
                {
                    $logContent = $iniSection.GetEnumerator() | & { process { "`n$($_.Key)=$($_.Value)" } }
                    Write-ADTLogEntry -Message "INI section content: $logContent"
                }

                return $iniSection
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to read INI section."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
