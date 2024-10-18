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

    .PARAMETER FilePath
        Path to the PE file to examine.

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

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.IO.FileInfo])]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateScript({
                if (!$_.Exists -or ($_ -notmatch '\.(exe|dll|ocx|drv|sys|scr|efi|cpl|fon)$'))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'One or more files either does not exist or has an invalid extension.'))
                }
                return !!$_
            })]
        [System.IO.FileInfo[]]$FilePath,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        [System.Int32]$MACHINE_OFFSET = 4
        [System.Int32]$PE_POINTER_OFFSET = 60
        [System.Byte[]]$data = [System.Byte[]]::new(4096)
    }

    process
    {
        foreach ($Path in $filePath)
        {
            try
            {
                try
                {
                    # Read the first 4096 bytes of the file.
                    $stream = [System.IO.FileStream]::new($Path.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
                    $null = $stream.Read($data, 0, $data.Count)
                    $stream.Flush()
                    $stream.Close()

                    # Get the file header from the header's address, factoring in any offsets.
                    $PEArchitecture = switch ([System.BitConverter]::ToUInt16($data, [System.BitConverter]::ToInt32($data, $PE_POINTER_OFFSET) + $MACHINE_OFFSET))
                    {
                        0
                        {
                            # The contents of this file are assumed to be applicable to any machine type
                            'Native'
                            break
                        }
                        0x014C
                        {
                            # File for Windows 32-bit systems
                            '32BIT'
                            break
                        }
                        0x0200
                        {
                            # File for Intel Itanium x64 processor family
                            'Itanium-x64'
                            break
                        }
                        0x8664
                        {
                            # File for Windows 64-bit systems
                            '64BIT'
                            break
                        }
                        default
                        {
                            'Unknown'
                            break
                        }
                    }
                    Write-ADTLogEntry -Message "File [$($Path.FullName)] has a detected file architecture of [$PEArchitecture]."
                    if ($PassThru)
                    {
                        return ($Path | Add-Member -MemberType NoteProperty -Name BinaryType -Value $PEArchitecture -Force -PassThru)
                    }
                    return $PEArchitecture
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
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
