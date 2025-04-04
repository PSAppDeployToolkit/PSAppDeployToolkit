#-----------------------------------------------------------------------------
#
# MARK: Get-ADTExecutableInfo
#
#-----------------------------------------------------------------------------

function Get-ADTExecutableInfo
{
    <#
    .SYNOPSIS
        Retrieves information about any valid Windows PE executable.

    .DESCRIPTION
        This function retrieves information about any valid Windows PE executable, such as version, bitness, and other characteristics.

    .PARAMETER Path
        One or more expandable executable paths to retrieve info from.

    .PARAMETER LiteralPath
        One or more literal executable paths to retrieve info from.

    .PARAMETER InputObject
        A FileInfo object to retrieve executable info from. Available for pipelining.

    .INPUTS
        System.IO.FileInfo

        This function accepts FileInfo objects via the pipeline for processing, such as output from Get-ChildItem.

    .OUTPUTS
        PSADT.Execution.ExecutableInfo

        This function returns an ExecutableInfo object for the given FilePath.

    .EXAMPLE
        Get-ADTExecutableInfo -LiteralPath C:\Windows\system32\cmd.exe

        Invokes the Get-ADTExecutableInfo function and returns an ExecutableInfo object.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: © 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LiteralPath', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'InputObject', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $true, ParameterSetName = 'InputObject', ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo]$InputObject
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Sanitise inputs via Get-ChildItem before farming it out to the backend.
                ($values = Get-Variable -Name $PSCmdlet.ParameterSetName -ValueOnly) | Get-ChildItem | & {
                    begin
                    {
                        Write-ADTLogEntry -Message "Retrieiving executable info for ['$([System.String]::Join($values, "', '"))']."
                    }

                    process
                    {
                        [PSADT.Execution.ExecutableUtilities]::GetExecutableInfo($_.FullName)
                    }
                }
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
