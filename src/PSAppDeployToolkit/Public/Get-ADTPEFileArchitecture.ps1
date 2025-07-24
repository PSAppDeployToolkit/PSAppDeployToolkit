#-----------------------------------------------------------------------------
#
# MARK: Get-ADTPEFileArchitecture
#
#-----------------------------------------------------------------------------

function Get-ADTPEFileArchitecture
{
    <#
    .SYNOPSIS
        Determine if a PE file is a 32-bit or a 64-bit file.

    .DESCRIPTION
        Determine if a PE file is a 32-bit or a 64-bit file by examining the file's image file header.

        PE file extensions: .exe, .dll, .ocx, .drv, .sys, .scr, .efi, .cpl, .fon

    .PARAMETER Path
        One or more expandable executable paths to retrieve info from.

    .PARAMETER LiteralPath
        One or more literal executable paths to retrieve info from.

    .PARAMETER InputObject
        A FileInfo object to retrieve executable info from. Available for pipelining.

    .PARAMETER PassThru
        Get the file object, attach a property indicating the file binary type, and write to pipeline.

    .INPUTS
        System.IO.FileInfo

        Accepts a FileInfo object from the pipeline.

    .OUTPUTS
        System.String

        Returns a string indicating the file binary type.

    .EXAMPLE
        Get-ADTPEFileArchitecture -FilePath "$env:windir\notepad.exe"

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTPEFileArchitecture
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LiteralPath', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [CmdletBinding()]
    [OutputType([PSADT.LibraryInterfaces.IMAGE_FILE_MACHINE])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [Alias('PSPath', 'FilePath')]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $true, ParameterSetName = 'InputObject', ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo]$InputObject,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Set up required constants for processing each requested file.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        [System.Int32]$PE_POINTER_OFFSET = 60; [System.Int32]$MACHINE_OFFSET = 4
        [System.Byte[]]$data = [System.Byte[]]::new(4096)
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

        # Process each found file.
        foreach ($file in $files)
        {
            try
            {
                try
                {
                    # Read the first 4096 bytes of the file.
                    $stream = [System.IO.FileStream]::new($file.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
                    $null = $stream.Read($data, 0, $data.Count)
                    $stream.Flush(); $stream.Close()

                    # Get the file header from the header's address, factoring in any offsets.
                    $peArchValue = [System.BitConverter]::ToUInt16($data, [System.BitConverter]::ToInt32($data, $PE_POINTER_OFFSET) + $MACHINE_OFFSET)
                    $peArchEnum = [PSADT.LibraryInterfaces.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_UNKNOWN; $null = [PSADT.LibraryInterfaces.IMAGE_FILE_MACHINE]::TryParse($peArchValue, [ref]$peArchEnum)
                    Write-ADTLogEntry -Message "File [$($file.FullName)] has a detected file architecture of [$peArchEnum]."
                    if ($PassThru)
                    {
                        $file | Add-Member -MemberType NoteProperty -Name BinaryType -Value $peArchEnum -Force -PassThru
                    }
                    else
                    {
                        $peArchEnum
                    }
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
