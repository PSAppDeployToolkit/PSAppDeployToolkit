#region Function Get-PEFileArchitecture
Function Get-PEFileArchitecture {
<#
.SYNOPSIS
	Determine if a PE file is a 32-bit or a 64-bit file.
.DESCRIPTION
	Determine if a PE file is a 32-bit or a 64-bit file by examining the file's image file header.
	PE file extensions: .exe, .dll, .ocx, .drv, .sys, .scr, .efi, .cpl, .fon
.PARAMETER FilePath
	Path to the PE file to examine.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.PARAMETER PassThru
	Get the file object, attach a property indicating the file binary type, and write to pipeline
.EXAMPLE
	Get-PEFileArchitecture -FilePath "$env:windir\notepad.exe"
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
		[ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
		[IO.FileInfo[]]$FilePath,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true,
		[Parameter(Mandatory=$false)]
		[switch]$PassThru
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		[string[]]$PEFileExtensions = '.exe', '.dll', '.ocx', '.drv', '.sys', '.scr', '.efi', '.cpl', '.fon'
		[int32]$MACHINE_OFFSET = 4
		[int32]$PE_POINTER_OFFSET = 60
	}
	Process {
		ForEach ($Path in $filePath) {
			Try {
				If ($PEFileExtensions -notcontains $Path.Extension) {
					Throw "Invalid file type. Please specify one of the following PE file types: $($PEFileExtensions -join ', ')"
				}

				[byte[]]$data = New-Object -TypeName 'System.Byte[]' -ArgumentList 4096
				$stream = New-Object -TypeName 'System.IO.FileStream' -ArgumentList ($Path.FullName, 'Open', 'Read')
				$null = $stream.Read($data, 0, 4096)
				$stream.Flush()
				$stream.Close()

				[int32]$PE_HEADER_ADDR = [BitConverter]::ToInt32($data, $PE_POINTER_OFFSET)
				[uint16]$PE_IMAGE_FILE_HEADER = [BitConverter]::ToUInt16($data, $PE_HEADER_ADDR + $MACHINE_OFFSET)
				Switch ($PE_IMAGE_FILE_HEADER) {
					0 { $PEArchitecture = 'Native' } # The contents of this file are assumed to be applicable to any machine type
					0x014c { $PEArchitecture = '32BIT' } # File for Windows 32-bit systems
					0x0200 { $PEArchitecture = 'Itanium-x64' } # File for Intel Itanium x64 processor family
					0x8664 { $PEArchitecture = '64BIT' } # File for Windows 64-bit systems
					Default { $PEArchitecture = 'Unknown' }
				}
				Write-Log -Message "File [$($Path.FullName)] has a detected file architecture of [$PEArchitecture]." -Source ${CmdletName}

				If ($PassThru) {
					#  Get the file object, attach a property indicating the type, and write to pipeline
					Get-Item -LiteralPath $Path.FullName -Force | Add-Member -MemberType 'NoteProperty' -Name 'BinaryType' -Value $PEArchitecture -Force -PassThru | Write-Output
				}
				Else {
					Write-Output -InputObject $PEArchitecture
				}
			}
			Catch {
				Write-Log -Message "Failed to get the PE file architecture. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "Failed to get the PE file architecture: $($_.Exception.Message)"
				}
				Continue
			}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
