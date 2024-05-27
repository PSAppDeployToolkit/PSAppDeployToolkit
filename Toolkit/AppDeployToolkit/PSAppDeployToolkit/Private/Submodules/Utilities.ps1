#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function New-ZipFile {
    <#
.SYNOPSIS

Create a new zip archive or add content to an existing archive.

.DESCRIPTION

Create a new zip archive or add content to an existing archive by using the Shell object .CopyHere method.

.PARAMETER DestinationArchiveDirectoryPath

The path to the directory path where the zip archive will be saved.

.PARAMETER DestinationArchiveFileName

The name of the zip archive.

.PARAMETER SourceDirectoryPath

The path to the directory to be archived, specified as absolute paths.

.PARAMETER SourceFilePath

The path to the file to be archived, specified as absolute paths.

.PARAMETER RemoveSourceAfterArchiving

Remove the source path after successfully archiving the content. Default is: $false.

.PARAMETER OverWriteArchive

Overwrite the destination archive path if it already exists. Default is: $false.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

New-ZipFile -DestinationArchiveDirectoryPath 'E:\Testing' -DestinationArchiveFileName 'TestingLogs.zip' -SourceDirectory 'E:\Testing\Logs'

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding(DefaultParameterSetName = 'CreateFromDirectory')]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullorEmpty()]
        [String]$DestinationArchiveDirectoryPath,
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String]$DestinationArchiveFileName,
        [Parameter(Mandatory = $true, Position = 2, ParameterSetName = 'CreateFromDirectory')]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Container' })]
        [String[]]$SourceDirectoryPath,
        [Parameter(Mandatory = $true, Position = 2, ParameterSetName = 'CreateFromFile')]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String[]]$SourceFilePath,
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullorEmpty()]
        [Switch]$RemoveSourceAfterArchiving = $false,
        [Parameter(Mandatory = $false, Position = 4)]
        [ValidateNotNullorEmpty()]
        [Switch]$OverWriteArchive = $false,
        [Parameter(Mandatory = $false, Position = 5)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        Write-DebugHeader
    }
    Process {
        Try {
            ## Remove invalid characters from the supplied filename
            $DestinationArchiveFileName = Remove-ADTInvalidFileNameChars -Name $DestinationArchiveFileName
            If ($DestinationArchiveFileName.length -eq 0) {
                Throw 'Invalid filename characters replacement resulted into an empty string.'
            }
            ## Get the full destination path where the archive will be stored
            [String]$DestinationPath = Join-Path -Path $DestinationArchiveDirectoryPath -ChildPath $DestinationArchiveFileName -ErrorAction 'Stop'
            Write-ADTLogEntry -Message "Creating a zip archive with the requested content at destination path [$DestinationPath]."

            ## If the destination archive already exists, delete it if the -OverWriteArchive option was selected
            If (($OverWriteArchive) -and (Test-Path -LiteralPath $DestinationPath)) {
                Write-ADTLogEntry -Message "An archive at the destination path already exists, deleting file [$DestinationPath]."
                $null = Remove-Item -LiteralPath $DestinationPath -Force -ErrorAction 'Stop'
            }

            ## If archive file does not exist, then create a zero-byte zip archive
            If (-not (Test-Path -LiteralPath $DestinationPath)) {
                ## Create a zero-byte file
                Write-ADTLogEntry -Message "Creating a zero-byte file [$DestinationPath]."
                $null = New-Item -Path $DestinationArchiveDirectoryPath -Name $DestinationArchiveFileName -ItemType 'File' -Force -ErrorAction 'Stop'

                ## Write the file header for a zip file to the zero-byte file
                [Byte[]]$ZipArchiveByteHeader = 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                [IO.FileStream]$FileStream = New-Object -TypeName 'System.IO.FileStream' -ArgumentList ($DestinationPath, ([IO.FileMode]::Create))
                [IO.BinaryWriter]$BinaryWriter = New-Object -TypeName 'System.IO.BinaryWriter' -ArgumentList ($FileStream)
                Write-ADTLogEntry -Message "Write the file header for a zip archive to the zero-byte file [$DestinationPath]."
                $null = $BinaryWriter.Write($ZipArchiveByteHeader)
                $BinaryWriter.Close()
                $FileStream.Close()
            }

            ## Create an object representing the archive file
            [__ComObject]$Archive = $Script:ADT.Environment.ShellApp.NameSpace($DestinationPath)

            ## Create the archive file
            If ($PSCmdlet.ParameterSetName -eq 'CreateFromDirectory') {
                ## Create the archive file from a source directory
                ForEach ($Directory in $SourceDirectoryPath) {
                    Try {
                        #  Create an object representing the source directory
                        [__ComObject]$CreateFromDirectory = $Script:ADT.Environment.ShellApp.NameSpace($Directory)
                        #  Copy all of the files and folders from the source directory to the archive
                        $null = $Archive.CopyHere($CreateFromDirectory.Items())
                        #  Wait for archive operation to complete. Archive file count property returns 0 if archive operation is in progress.
                        Write-ADTLogEntry -Message "Compressing [$($CreateFromDirectory.Count)] file(s) in source directory [$Directory] to destination path [$DestinationPath]..."
                        Do {
                            Start-Sleep -Milliseconds 250
                        } While ($Archive.Items().Count -eq 0)
                    }
                    Finally {
                        #  Release the ComObject representing the source directory
                        $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($CreateFromDirectory)
                    }

                    #  If option was selected, recursively delete the source directory after successfully archiving the contents
                    If ($RemoveSourceAfterArchiving) {
                        Try {
                            Write-ADTLogEntry -Message "Recursively deleting the source directory [$Directory] as contents have been successfully archived."
                            $null = Remove-Item -LiteralPath $Directory -Recurse -Force -ErrorAction 'Stop'
                        }
                        Catch {
                            Write-ADTLogEntry -Message "Failed to recursively delete the source directory [$Directory]. `r`n$(Resolve-Error)" -Severity 2
                        }
                    }
                }
            }
            Else {
                ## Create the archive file from a list of one or more files
                [IO.FileInfo[]]$SourceFilePath = [IO.FileInfo[]]$SourceFilePath
                ForEach ($File in $SourceFilePath) {
                    #  Copy the files and folders from the source directory to the archive
                    $null = $Archive.CopyHere($File.FullName)
                    #  Wait for archive operation to complete. Archive file count property returns 0 if archive operation is in progress.
                    Write-ADTLogEntry -Message "Compressing file [$($File.FullName)] to destination path [$DestinationPath]..."
                    Do {
                        Start-Sleep -Milliseconds 250
                    } While ($Archive.Items().Count -eq 0)

                    #  If option was selected, delete the source file after successfully archiving the content
                    If ($RemoveSourceAfterArchiving) {
                        Try {
                            Write-ADTLogEntry -Message "Deleting the source file [$($File.FullName)] as it has been successfully archived."
                            $null = Remove-Item -LiteralPath $File.FullName -Force -ErrorAction 'Stop'
                        }
                        Catch {
                            Write-ADTLogEntry -Message "Failed to delete the source file [$($File.FullName)]. `r`n$(Resolve-Error)" -Severity 2
                        }
                    }
                }
            }

            ## If the archive was created in session 0 or by an Admin, then it may only be readable by elevated users.
            #  Apply the parent folder's permissions to the archive file to fix the problem.
            Write-ADTLogEntry -Message "If the archive was created in session 0 or by an Admin, then it may only be readable by elevated users. Apply permissions from parent folder [$DestinationArchiveDirectoryPath] to file [$DestinationPath]."
            Try {
                [Security.AccessControl.DirectorySecurity]$DestinationArchiveDirectoryPathAcl = Get-Acl -Path $DestinationArchiveDirectoryPath -ErrorAction 'Stop'
                Set-Acl -Path $DestinationPath -AclObject $DestinationArchiveDirectoryPathAcl -ErrorAction 'Stop'
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to apply parent folder's [$DestinationArchiveDirectoryPath] permissions to file [$DestinationPath]. `r`n$(Resolve-Error)" -Severity 2
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to archive the requested file(s). `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to archive the requested file(s): $($_.Exception.Message)"
            }
        }
        Finally {
            ## Release the ComObject representing the archive
            If ($Archive) {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Archive)
            }
        }
    }
    End {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function ConvertTo-NTAccountOrSID {
    <#
.SYNOPSIS

Convert between NT Account names and their security identifiers (SIDs).

.DESCRIPTION

Specify either the NT Account name or the SID and get the other. Can also convert well known sid types.

.PARAMETER AccountName

The Windows NT Account name specified in <domain>\<username> format.
Use fully qualified account names (e.g., <domain>\<username>) instead of isolated names (e.g, <username>) because they are unambiguous and provide better performance.

.PARAMETER SID

The Windows NT Account SID.

.PARAMETER WellKnownSIDName

Specify the Well Known SID name translate to the actual SID (e.g., LocalServiceSid).

To get all well known SIDs available on system: [Enum]::GetNames([Security.Principal.WellKnownSidType])

.PARAMETER WellKnownToNTAccount

Convert the Well Known SID to an NTAccount name

.INPUTS

System.String

Accepts a string containing the NT Account name or SID.

.OUTPUTS

System.String

Returns the NT Account name or SID.

.EXAMPLE

ConvertTo-NTAccountOrSID -AccountName 'CONTOSO\User1'

Converts a Windows NT Account name to the corresponding SID

.EXAMPLE

ConvertTo-NTAccountOrSID -SID 'S-1-5-21-1220945662-2111687655-725345543-14012660'

Converts a Windows NT Account SID to the corresponding NT Account Name

.EXAMPLE

ConvertTo-NTAccountOrSID -WellKnownSIDName 'NetworkServiceSid'

Converts a Well Known SID name to a SID

.NOTES

This is an internal script function and should typically not be called directly.

The conversion can return an empty result if the user account does not exist anymore or if translation fails.

http://blogs.technet.com/b/askds/archive/2011/07/28/troubleshooting-sid-translation-failures-from-the-obvious-to-the-not-so-obvious.aspx

.LINK

https://psappdeploytoolkit.com

.LINK

http://msdn.microsoft.com/en-us/library/system.security.principal.wellknownsidtype(v=vs.110).aspx

#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'NTAccountToSID', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$AccountName,
        [Parameter(Mandatory = $true, ParameterSetName = 'SIDToNTAccount', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $true, ParameterSetName = 'WellKnownName', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$WellKnownSIDName,
        [Parameter(Mandatory = $false, ParameterSetName = 'WellKnownName')]
        [ValidateNotNullOrEmpty()]
        [Switch]$WellKnownToNTAccount
    )

    Begin {
        Write-DebugHeader
    }
    Process {
        Try {
            Switch ($PSCmdlet.ParameterSetName) {
                'SIDToNTAccount' {
                    [String]$msg = "the SID [$SID] to an NT Account name"
                    Write-ADTLogEntry -Message "Converting $msg."

                    Try {
                        $NTAccountSID = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ($SID)
                        $NTAccount = $NTAccountSID.Translate([Security.Principal.NTAccount])
                        Write-Output -InputObject ($NTAccount)
                    }
                    Catch {
                        Write-ADTLogEntry -Message "Unable to convert $msg. It may not be a valid account anymore or there is some other problem. `r`n$(Resolve-Error)" -Severity 2
                    }
                }
                'NTAccountToSID' {
                    [String]$msg = "the NT Account [$AccountName] to a SID"
                    Write-ADTLogEntry -Message "Converting $msg."

                    Try {
                        $NTAccount = New-Object -TypeName 'System.Security.Principal.NTAccount' -ArgumentList ($AccountName)
                        $NTAccountSID = $NTAccount.Translate([Security.Principal.SecurityIdentifier])
                        Write-Output -InputObject ($NTAccountSID)
                    }
                    Catch {
                        Write-ADTLogEntry -Message "Unable to convert $msg. It may not be a valid account anymore or there is some other problem. `r`n$(Resolve-Error)" -Severity 2
                    }
                }
                'WellKnownName' {
                    If ($WellKnownToNTAccount) {
                        [String]$ConversionType = 'NTAccount'
                    }
                    Else {
                        [String]$ConversionType = 'SID'
                    }
                    [String]$msg = "the Well Known SID Name [$WellKnownSIDName] to a $ConversionType"
                    Write-ADTLogEntry -Message "Converting $msg."

                    #  Get the SID for the root domain
                    Try {
                        $MachineRootDomain = (Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'Stop').Domain.ToLower()
                        $ADDomainObj = New-Object -TypeName 'System.DirectoryServices.DirectoryEntry' -ArgumentList ("LDAP://$MachineRootDomain")
                        $DomainSidInBinary = $ADDomainObj.ObjectSid
                        $DomainSid = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ($DomainSidInBinary[0], 0)
                    }
                    Catch {
                        Write-ADTLogEntry -Message 'Unable to get Domain SID from Active Directory. Setting Domain SID to $null.' -Severity 2
                        $DomainSid = $null
                    }

                    #  Get the SID for the well known SID name
                    $WellKnownSidType = [Security.Principal.WellKnownSidType]::$WellKnownSIDName
                    $NTAccountSID = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ($WellKnownSidType, $DomainSid)

                    If ($WellKnownToNTAccount) {
                        $NTAccount = $NTAccountSID.Translate([Security.Principal.NTAccount])
                        Write-Output -InputObject ($NTAccount)
                    }
                    Else {
                        Write-Output -InputObject ($NTAccountSID)
                    }
                }
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to convert $msg. It may not be a valid account anymore or there is some other problem. `r`n$(Resolve-Error)" -Severity 3
        }
    }
    End {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTRunningProcesses
{
    <#

    .SYNOPSIS
    Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

    .DESCRIPTION
    Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

    .PARAMETER InputObject
    Custom object containing the process objects to search for.

    .PARAMETER DisableLogging
    Disables function logging.

    .INPUTS
    System.Management.Automation.PSObject. One or more process objects as established in the Winforms code.

    .OUTPUTS
    System.Diagnostics.Process. Returns one or more process objects representing each running process found.

    .EXAMPLE
    $processObjects | Get-ADTRunningProcesses

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSObject]$InputObject,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableLogging
    )

    begin {
        Write-DebugHeader
    }

    end {
        # Confirm input isn't null before proceeding.
        if ($processObjects = $input.Where({$null -ne $_}))
        {
            # Get all running processes and append properties.
            Write-ADTLogEntry -Message "Checking for running applications: [$($processObjects.ProcessName -join ',')]" -DebugMessage:$DisableLogging
            $runningProcesses = Get-Process -Name $processObjects.ProcessName -ErrorAction Ignore | ForEach-Object {
                $_ | Add-Member -MemberType NoteProperty -Name ProcessDescription -Force -PassThru -Value $(
                    if (![System.String]::IsNullOrWhiteSpace(($objDescription = $processObjects | Where-Object -Property ProcessName -EQ -Value $_.ProcessName | Select-Object -ExpandProperty ProcessDescription -ErrorAction Ignore)))
                    {
                        # The description of the process provided as a Parameter to the function, e.g. -ProcessName "winword=Microsoft Office Word".
                        $objDescription
                    }
                    elseif ($_.Description)
                    {
                        # If the process already has a description field specified, then use it.
                        $_.Description
                    }
                    else
                    {
                        # Fall back on the process name if no description is provided by the process or as a parameter to the function.
                        $_.ProcessName
                    }
                )
            }

            # Return output if there's any.
            if ($runningProcesses)
            {
                Write-ADTLogEntry -Message "The following processes are running: [$(($runningProcesses.ProcessName | Select-Object -Unique) -join ',')]." -DebugMessage:$DisableLogging
                $runningProcesses | Sort-Object -Property ProcessDescription
            }
            else
            {
                Write-ADTLogEntry -Message 'Specified applications are not running.' -DebugMessage:$DisableLogging
            }
        }
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

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

.INPUTS

System.IO.FileInfo.

Accepts a FileInfo object from the pipeline.

.OUTPUTS

System.String

Returns a string indicating the file binary type.

.EXAMPLE

Get-PEFileArchitecture -FilePath "$env:windir\notepad.exe"

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [IO.FileInfo[]]$FilePath,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true,
        [Parameter(Mandatory = $false)]
        [Switch]$PassThru
    )

    Begin {
        Write-DebugHeader

        [String[]]$PEFileExtensions = '.exe', '.dll', '.ocx', '.drv', '.sys', '.scr', '.efi', '.cpl', '.fon'
        [Int32]$MACHINE_OFFSET = 4
        [Int32]$PE_POINTER_OFFSET = 60
    }
    Process {
        ForEach ($Path in $filePath) {
            Try {
                If ($PEFileExtensions -notcontains $Path.Extension) {
                    Throw "Invalid file type. Please specify one of the following PE file types: $($PEFileExtensions -join ', ')"
                }

                [Byte[]]$data = New-Object -TypeName 'System.Byte[]' -ArgumentList (4096)
                $stream = New-Object -TypeName 'System.IO.FileStream' -ArgumentList ($Path.FullName, 'Open', 'Read')
                $null = $stream.Read($data, 0, 4096)
                $stream.Flush()
                $stream.Close()

                [Int32]$PE_HEADER_ADDR = [BitConverter]::ToInt32($data, $PE_POINTER_OFFSET)
                [UInt16]$PE_IMAGE_FILE_HEADER = [BitConverter]::ToUInt16($data, $PE_HEADER_ADDR + $MACHINE_OFFSET)
                Switch ($PE_IMAGE_FILE_HEADER) {
                    0 {
                        $PEArchitecture = 'Native'
                    } # The contents of this file are assumed to be applicable to any machine type
                    0x014c {
                        $PEArchitecture = '32BIT'
                    } # File for Windows 32-bit systems
                    0x0200 {
                        $PEArchitecture = 'Itanium-x64'
                    } # File for Intel Itanium x64 processor family
                    0x8664 {
                        $PEArchitecture = '64BIT'
                    } # File for Windows 64-bit systems
                    Default {
                        $PEArchitecture = 'Unknown'
                    }
                }
                Write-ADTLogEntry -Message "File [$($Path.FullName)] has a detected file architecture of [$PEArchitecture]."

                If ($PassThru) {
                    #  Get the file object, attach a property indicating the type, and write to pipeline
                    Get-Item -LiteralPath $Path.FullName -Force | Add-Member -MemberType 'NoteProperty' -Name 'BinaryType' -Value $PEArchitecture -Force -PassThru | Write-Output
                }
                Else {
                    Write-Output -InputObject ($PEArchitecture)
                }
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to get the PE file architecture. `r`n$(Resolve-Error)" -Severity 3
                If (-not $ContinueOnError) {
                    Throw "Failed to get the PE file architecture: $($_.Exception.Message)"
                }
                Continue
            }
        }
    }
    End {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

filter Resolve-ADTBoundParameters
{
    <#

    .SYNOPSIS
    Resolve the parameters of a function call to a string.

    .DESCRIPTION
    Resolve the parameters of a function call to a string.

    .PARAMETER Parameter
    The name of the function this function is invoked from.

    .INPUTS
    System.Object

    .OUTPUTS
    System.Object

    .EXAMPLE
    $PSBoundParameters | Resolve-ADTBoundParameters

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Exclude
    )

    # Save off the invocation's command.
    $thisFunc = $MyInvocation.MyCommand

    # Process the piped hashtable.
    $_.GetEnumerator().Where({$Exclude -notcontains $_.Key}).ForEach({
        begin {
            # Establish array to hold return string.
            if (!(Test-Path -LiteralPath 'Variable:paramsArr'))
            {
                $paramsArr = [System.Collections.Generic.List[System.String]]::new()
            }
        }
        process {
            # Recursively expand child hashtables.
            if ($_.Value -isnot [System.Collections.IDictionary])
            {
                # Determine value.
                $val = if ($_.Value -is [System.String])
                {
                    "'$($_.Value.Replace("'", "''"))'"
                }
                elseif ($_.Value -is [System.Collections.IEnumerable])
                {
                    if ($_.Value[0] -is [System.String])
                    {
                        "'$([System.String]::Join("','", $_.Value.Replace("'", "''")))'"
                    }
                    else
                    {
                        [System.String]::Join(',', $_.Value)
                    }
                }
                else
                {
                    $_.Value
                }
                $paramsArr.Add("-$($_.Key):$val")
            }
            else
            {
                $_.Value | & $thisFunc
            }
        }
        end {
            # Join the array and return as a string to the caller.
            if ((Get-PSCallStack).Command.Where({$_.Equals($thisFunc.Name)}).Count.Equals(1))
            {
                return [System.String]::Join(' ', $paramsArr)
            }
        }
    })
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTPowerShellProcessPath
{
    return "$PSHOME\$(if ($PSVersionTable.PSEdition.Equals('Core')) {'pwsh.exe'} else {'powershell.exe'})"
}
