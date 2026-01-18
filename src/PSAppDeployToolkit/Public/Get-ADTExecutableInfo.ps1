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
        PSADT.FileSystem.ExecutableInfo

        This function returns an ExecutableInfo object for the given FilePath.

    .EXAMPLE
        Get-ADTExecutableInfo -LiteralPath C:\Windows\system32\cmd.exe

        Invokes the Get-ADTExecutableInfo function and returns an ExecutableInfo object.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LiteralPath', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [CmdletBinding()]
    [OutputType([PSADT.FileSystem.ExecutableInfo])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [Alias('PSPath')]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $true, ParameterSetName = 'InputObject', ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo]$InputObject
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Grab and cache all files.
        $files = if (!$PSCmdlet.ParameterSetName.Equals('InputObject'))
        {
            $gciParams = @{$PSCmdlet.ParameterSetName = Get-Variable -Name $PSCmdlet.ParameterSetName -ValueOnly }
            Get-ChildItem @gciParams -File
        }
        else
        {
            $InputObject
        }

        # Return the executable info for each file, continuing to the next file on error by default.
        Write-ADTLogEntry -Message "Retrieving executable info for ['$([System.String]::Join("', '", $files.FullName))']."
        foreach ($file in $files)
        {
            try
            {
                try
                {
                    [PSADT.FileSystem.ExecutableInfo]::Get($file.FullName)
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
            }
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
