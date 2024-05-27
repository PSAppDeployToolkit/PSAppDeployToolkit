#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function ConvertTo-ADTNTAccountOrSID
{
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
    System.String. Accepts a string containing the NT Account name or SID.

    .OUTPUTS
    System.String. Returns the NT Account name or SID.

    .EXAMPLE
    ConvertTo-ADTNTAccountOrSID -AccountName 'CONTOSO\User1'

    Converts a Windows NT Account name to the corresponding SID.

    .EXAMPLE
    ConvertTo-ADTNTAccountOrSID -SID 'S-1-5-21-1220945662-2111687655-725345543-14012660'

    Converts a Windows NT Account SID to the corresponding NT Account Name.

    .EXAMPLE
    ConvertTo-ADTNTAccountOrSID -WellKnownSIDName 'NetworkServiceSid'

    Converts a Well Known SID name to a SID.

    .NOTES
    This is an internal script function and should typically not be called directly.

    The conversion can return an empty result if the user account does not exist anymore or if translation fails.

    http://blogs.technet.com/b/askds/archive/2011/07/28/troubleshooting-sid-translation-failures-from-the-obvious-to-the-not-so-obvious.aspx

    .LINK
    https://psappdeploytoolkit.com

    .LINK
    http://msdn.microsoft.com/en-us/library/system.security.principal.wellknownsidtype(v=vs.110).aspx

    #>

    param (
        [Parameter(Mandatory = $true, ParameterSetName = 'NTAccountToSID', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$AccountName,

        [Parameter(Mandatory = $true, ParameterSetName = 'SIDToNTAccount', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SID,

        [Parameter(Mandatory = $true, ParameterSetName = 'WellKnownName', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WellKnownSIDName,

        [Parameter(Mandatory = $false, ParameterSetName = 'WellKnownName')]
        [System.Management.Automation.SwitchParameter]$WellKnownToNTAccount
    )

    begin {
        Write-DebugHeader
    }

    process {
        switch ($PSCmdlet.ParameterSetName)
        {
            'SIDToNTAccount' {
                $msg = "the SID [$SID] to an NT Account name"
                Write-ADTLogEntry -Message "Converting $msg."

                try
                {
                    return [System.Security.Principal.SecurityIdentifier]::new($SID).Translate([System.Security.Principal.NTAccount])
                }
                catch
                {
                    Write-ADTLogEntry -Message "Unable to convert $msg. It may not be a valid account anymore or there is some other problem. `r`n$(Resolve-Error)" -Severity 2
                }
            }
            'NTAccountToSID' {
                $msg = "the NT Account [$AccountName] to a SID"
                Write-ADTLogEntry -Message "Converting $msg."

                try
                {
                    return [System.Security.Principal.NTAccount]::new($AccountName).Translate([System.Security.Principal.SecurityIdentifier])
                }
                catch
                {
                    Write-ADTLogEntry -Message "Unable to convert $msg. It may not be a valid account anymore or there is some other problem. `r`n$(Resolve-Error)" -Severity 2
                }
            }
            'WellKnownName' {
                [String]$ConversionType = if ($WellKnownToNTAccount)
                {
                    'NTAccount'
                }
                else
                {
                    'SID'
                }
                [String]$msg = "the Well Known SID Name [$WellKnownSIDName] to a $ConversionType"
                Write-ADTLogEntry -Message "Converting $msg."

                # Get the SID for the root domain.
                $DomainSid = try
                {
                    [System.Security.Principal.SecurityIdentifier]::new([System.DirectoryServices.DirectoryEntry]::new("LDAP://$((Get-CimInstance -ClassName Win32_ComputerSystem).Domain.ToLower())").ObjectSid[0], 0)
                }
                catch
                {
                    Write-ADTLogEntry -Message 'Unable to get Domain SID from Active Directory. Setting Domain SID to $null.' -Severity 2
                }

                # Get the SID for the well known SID name.
                try
                {
                    $NTAccountSID = [System.Security.Principal.SecurityIdentifier]::new([System.Security.Principal.WellKnownSidType]::$WellKnownSIDName, $DomainSid)
                    if ($WellKnownToNTAccount)
                    {
                        return $NTAccountSID.Translate([System.Security.Principal.NTAccount])
                    }
                    return $NTAccountSID
                }
                catch
                {
                    Write-ADTLogEntry -Message "Failed to convert $msg. It may not be a valid account anymore or there is some other problem.`n$(Resolve-Error)" -Severity 3
                }
            }
        }
    }

    end {
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

    .INPUTS
    System.IO.FileInfo. Accepts a FileInfo object from the pipeline.

    .OUTPUTS
    System.String. Returns a string indicating the file binary type.

    .EXAMPLE
    Get-ADTPEFileArchitecture -FilePath "$env:windir\notepad.exe"

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateScript({if ($_.Where({![System.IO.File]::Exists($_) -or (('.exe', '.dll', '.ocx', '.drv', '.sys', '.scr', '.efi', '.cpl', '.fon') -notcontains [System.IO.Path]::GetExtension($_))})) {throw "One or more files either does not exist or has an invalid extension."}; $true})]
        [System.IO.FileInfo[]]$FilePath
    )

    begin {
        [System.Int32]$MACHINE_OFFSET = 4
        [System.Int32]$PE_POINTER_OFFSET = 60
        [System.Byte[]]$data = [System.Byte[]]::new(4096)
        Write-DebugHeader
    }

    process {
        foreach ($Path in $filePath)
        {
            # Read the first 4096 bytes of the file.
            $stream = [System.IO.FileStream]::new($Path.FullName, 'Open', 'Read')
            [System.Void]$stream.Read($data, 0, $data.Count)
            $stream.Flush()
            $stream.Close()

            # Get the file header from the header's address, factoring in any offsets.
            $PEArchitecture = switch ($PE_IMAGE_FILE_HEADER = [System.BitConverter]::ToUInt16($data, [System.BitConverter]::ToInt32($data, $PE_POINTER_OFFSET) + $MACHINE_OFFSET))
            {
                0 {
                    # The contents of this file are assumed to be applicable to any machine type
                    'Native'
                }
                0x014c {
                    # File for Windows 32-bit systems
                    '32BIT'
                }
                0x0200 {
                    # File for Intel Itanium x64 processor family
                    'Itanium-x64'
                }
                0x8664 {
                    # File for Windows 64-bit systems
                    '64BIT'
                }
                default {
                    'Unknown'
                }
            }
            Write-ADTLogEntry -Message "File [$($Path.FullName)] has a detected file architecture of [$PEArchitecture]."

            # Output the string to the pipeline.
            $PEArchitecture
        }
    }

    end {
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
