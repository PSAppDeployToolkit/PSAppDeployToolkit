function Set-ADTActiveSetup
{
    <#

    .SYNOPSIS
    Creates an Active Setup entry in the registry to execute a file for each user upon login.

    .DESCRIPTION
    Active Setup allows handling of per-user changes registry/file changes upon login.

    A registry key is created in the HKLM registry hive which gets replicated to the HKCU hive when a user logs in.

    If the "Version" value of the Active Setup entry in HKLM is higher than the version value in HKCU, the file referenced in "StubPath" is executed.

    This Function:
        - Creates the registry entries in HKLM:SOFTWARE\Microsoft\Active Setup\Installed Components\$installName.
        - Creates StubPath value depending on the file extension of the $StubExePath parameter.
        - Handles Version value with YYYYMMDDHHMMSS granularity to permit re-installs on the same day and still trigger Active Setup after Version increase.
        - Copies/overwrites the StubPath file to $StubExePath destination path if file exists in 'Files' subdirectory of script directory.
        - Executes the StubPath file for the current user based on $ExecuteForCurrentUser (no need to logout/login to trigger Active Setup).

    .PARAMETER StubExePath
    Use this parameter to specify the destination path of the file that will be executed upon user login.

    Note: Place the file you want users to execute in the '\Files' subdirectory of the script directory and the toolkit will install it to the path specificed in this parameter.

    .PARAMETER Arguments
    Arguments to pass to the file being executed.

    .PARAMETER Description
    Description for the Active Setup. Users will see "Setting up personalized settings for: $Description" at logon. Default is: $installName.

    .PARAMETER Key
    Name of the registry key for the Active Setup entry. Default is: $installName.

    .PARAMETER Wow6432Node
    Specify this switch to use Active Setup entry under Wow6432Node on a 64-bit OS. Default is: $false.

    .PARAMETER Version
    Optional. Specify version for Active setup entry. Active Setup is not triggered if Version value has more than 8 consecutive digits. Use commas to get around this limitation. Default: YYYYMMDDHHMMSS

    Note:
        - Do not use this parameter if it is not necessary. PSADT will handle this parameter automatically using the time of the installation as the version number.
        - In Windows 10, Scripts and EXEs might be blocked by AppLocker. Ensure that the path given to -StubExePath will permit end users to run Scripts and EXEs unelevated.

    .PARAMETER Locale
    Optional. Arbitrary string used to specify the installation language of the file being executed. Not replicated to HKCU.

    .PARAMETER PurgeActiveSetupKey
    Remove Active Setup entry from HKLM registry hive. Will also load each logon user's HKCU registry hive to remove Active Setup entry. Function returns after purging.

    .PARAMETER DisableActiveSetup
    Disables the Active Setup entry so that the StubPath file will not be executed. This also disables -ExecuteForCurrentUser

    .PARAMETER NoExecuteForCurrentUser
    Specifies whether the StubExePath should be executed for the current user. Since this user is already logged in, the user won't have the application started without logging out and logging back in.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.Boolean. Returns $true if Active Setup entry was created or updated, $false if Active Setup entry was not created or updated.

    .EXAMPLE
    Set-ADTActiveSetup -StubExePath 'C:\Users\Public\Company\ProgramUserConfig.vbs' -Arguments '/Silent' -Description 'Program User Config' -Key 'ProgramUserConfig' -Locale 'en'

    .EXAMPLE
    Set-ADTActiveSetup -StubExePath "$envWinDir\regedit.exe" -Arguments "/S `"%SystemDrive%\Program Files (x86)\PS App Deploy\PSAppDeployHKCUSettings.reg`"" -Description 'PS App Deploy Config' -Key 'PS_App_Deploy_Config' -ContinueOnError $true

    .EXAMPLE
    # Delete "ProgramUserConfig" active setup entry from all registry hives.
    Set-ADTActiveSetup -Key 'ProgramUserConfig' -PurgeActiveSetupKey

    .NOTES
    Original code borrowed from: Denis St-Pierre (Ottawa, Canada), Todd MacNaught (Ottawa, Canada)

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, ParameterSetName = 'Create')]
        [ValidateScript({
            if (('.exe', '.vbs', '.cmd', '.ps1', '.js') -notcontains ($StubExeExt = [System.IO.Path]::GetExtension($_)))
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName StubExePath -ProvidedValue $_ -ExceptionMessage "Unsupported Active Setup StubPath file extension [$StubExeExt]."))
            }
            return !!$_
        })]

        [System.String]$StubExePath,

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Arguments,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node,

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Version = ((Get-Date -Format 'yyMM,ddHH,mmss').ToString()), # Ex: 1405,1515,0522

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Locale,

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$DisableActiveSetup,

        [Parameter(Mandatory = $true, ParameterSetName = 'Purge')]
        [System.Management.Automation.SwitchParameter]$PurgeActiveSetupKey,

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$NoExecuteForCurrentUser
    )

    dynamicparam {
        # Attempt to get the most recent ADTSession object.
        $adtSession = try {Get-ADTSession} catch {[System.Void]$null}

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Description', [System.Management.Automation.RuntimeDefinedParameter]::new(
            'Description', [System.String], [System.Collections.Generic.List[System.Attribute]]@(
                [System.Management.Automation.ParameterAttribute]@{Mandatory = !$adtSession; ParameterSetName = 'Create'}
                [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
            )
        ))
        $paramDictionary.Add('Key', [System.Management.Automation.RuntimeDefinedParameter]::new(
            'Key', [System.String], [System.Collections.Generic.List[System.Attribute]]@(
                [System.Management.Automation.ParameterAttribute]@{Mandatory = !$adtSession}
                [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
            )
        ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin {
        # Set defaults for when there's an active ADTSession and overriding values haven't been specified.
        if ($adtSession)
        {
            ('Description', 'Key').Where({!$PSBoundParameters.ContainsKey($_)}).ForEach({Set-Variable -Name $_ -Value $adtSession.GetPropertyValue('InstallName')})
        }

        # Define initial variables.
        $runAsActiveUser = Get-ADTRunAsActiveUser
        $CUStubExePath = $null
        $CUArguments = $null
        $StubPath = $null

        # Define internal function to test current ActiveSetup stuff.
        function Test-ADTActiveSetup
        {
            [CmdletBinding()]
            param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$HKLMKey,

                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$HKCUKey,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.String]$UserSID
            )

            $HKCUProps = if ($UserSID)
            {
                Get-ADTRegistryKey -Key $HKCUKey -SID $UserSID
            }
            else
            {
                Get-ADTRegistryKey -Key $HKCUKey
            }
            $HKLMProps = Get-ADTRegistryKey -Key $HKLMKey
            [String]$HKCUVer = $HKCUProps.Version
            [String]$HKLMVer = $HKLMProps.Version
            [Int32]$HKLMInst = $HKLMProps.IsInstalled

            # HKLM entry not present. Nothing to run.
            if (!$HKLMProps)
            {
                Write-ADTLogEntry 'HKLM active setup entry is not present.'
                return $false
            }

            # HKLM entry present, but disabled. Nothing to run.
            if ($HKLMInst -eq 0)
            {
                Write-ADTLogEntry 'HKLM active setup entry is present, but it is disabled (IsInstalled set to 0).'
                return $false
            }

            # HKLM entry present and HKCU entry is not. Run the StubPath.
            if (!$HKCUProps)
            {
                Write-ADTLogEntry 'HKLM active setup entry is present. HKCU active setup entry is not present.'
                return $true
            }

            # Both entries present. HKLM entry does not have Version property. Nothing to run.
            if (!$HKLMVer)
            {
                Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKLM Version property is missing.'
                return $false
            }

            # Both entries present. HKLM entry has Version property, but HKCU entry does not. Run the StubPath.
            if (!$HKCUVer)
            {
                Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKCU Version property is missing.'
                return $true
            }

            # After cleanup, the HKLM Version property is empty. Considering it missing. HKCU is present so nothing to run.
            if (!($HKLMValidVer = [System.String]::Join($null, $HKLMVer.GetEnumerator().Where({[System.Char]::IsDigit($_) -or ($_ -eq ',')}))))
            {
                Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKLM Version property is invalid.'
                return $false
            }

            # After cleanup, the HKCU Version property is empty while HKLM Version property is not. Run the StubPath.
            if (!($HKCUValidVer = [System.String]::Join($null, $HKCUVer.GetEnumerator().Where({[System.Char]::IsDigit($_) -or ($_ -eq ',')}))))
            {
                Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKCU Version property is invalid.'
                return $true
            }

            # Both entries present, with a Version property. Compare the Versions.
            try {
                # Convert the version property to Version type and compare.
                if (([System.Version]$HKLMValidVer.Replace(',','.')) -gt ([System.Version]$HKCUValidVer.Replace(',','.')))
                {
                    # HKLM is greater, run the StubPath.
                    Write-ADTLogEntry "HKLM and HKCU active setup entries are present. Both contain Version properties, and the HKLM Version is greater."
                    return $true
                }
                else
                {
                    # The HKCU version is equal or higher than HKLM version, Nothing to run.
                    Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. Both contain Version properties. However, they are either the same or the HKCU Version property is higher.'
                    return $false
                }
            }
            catch {
                # Failed to convert version property to Version type.
                [System.Void]$null
            }

            # Check whether the Versions were split into the same number of strings. Split the version by commas.
            if (($SplitHKLMValidVer = $HKLMValidVer.Split(',')).Count -ne ($SplitHKCUValidVer = $HKCUValidVer.Split(',')).Count)
            {
                # The versions are different length - more commas
                if ($SplitHKLMValidVer.Count -gt $SplitHKCUValidVer.Count)
                {
                    # HKLM is longer, Run the StubPath.
                    Write-ADTLogEntry "HKLM and HKCU active setup entries are present. Both contain Version properties. However, the HKLM Version has more version fields."
                    return $true
                }
                else
                {
                    # HKCU is longer, Nothing to run.
                    Write-ADTLogEntry "HKLM and HKCU active setup entries are present. Both contain Version properties. However, the HKCU Version has more version fields."
                    return $false
                }
            }

            # The Versions have the same number of strings. Compare them
            try
            {
                for ($i = 0; $i -lt $SplitHKLMValidVer.Count; $i++)
                {
                    # Parse the version is UINT64.
                    if ([UInt64]::Parse($SplitHKCUValidVer[$i]) -lt [UInt64]::Parse($SplitHKLMValidVer[$i]))
                    {
                        # The HKCU ver is lower, Run the StubPath.
                        Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid. However, HKCU Version property is lower.'
                        return $true
                    }
                }
                # The HKCU version is equal or higher than HKLM version, Nothing to run.
                Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid. However, they are either the same or HKCU Version property is higher.'
                return $false
            }
            catch
            {
                # Failed to parse strings as UInt64, Run the StubPath.
                Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid. However, parsing string numerics to 64-bit integers failed.' -Severity 2
                return $true
            }
        }

        # Define internal function to the required ActiveSetup registry keys.
        function Set-ADTActiveSetupRegKeys
        {
            [CmdletBinding()]
            param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$ActiveSetupRegKey,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.String]$SID
            )

            $srkParams = if ($SID) {@{SID = $SID}} else {@{}}
            Set-ADTRegistryKey -Key $ActiveSetupRegKey -Name '(Default)' -Value $Description @srkParams
            Set-ADTRegistryKey -Key $ActiveSetupRegKey -Name 'Version' -Value $Version @srkParams
            Set-ADTRegistryKey -Key $ActiveSetupRegKey -Name 'StubPath' -Value $StubPath -Type 'String' @srkParams
            if ($Locale)
            {
                Set-ADTRegistryKey -Key $ActiveSetupRegKey -Name 'Locale' -Value $Locale @srkParams
            }

            # Only Add IsInstalled to HKLM.
            if ($ActiveSetupRegKey.Contains('HKEY_LOCAL_MACHINE'))
            {
                Set-ADTRegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value ([System.UInt32]!$DisableActiveSetup) -Type 'DWord' @srkParams
            }
        }

        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process {
        try
        {
            # Set up the relevant keys, factoring in bitness and architecture.
            if ($Wow6432Node -and [System.Environment]::Is64BitOperatingSystem)
            {
                $ActiveSetupKey = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\$Key"
                $HKCUActiveSetupKey = "Registry::HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\$Key"
            }
            else
            {
                $ActiveSetupKey = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\$Key"
                $HKCUActiveSetupKey = "Registry::HKEY_CURRENT_USER\Software\Microsoft\Active Setup\Installed Components\$Key"
            }

            # Delete Active Setup registry entry from the HKLM hive and for all logon user registry hives on the system.
            if ($PurgeActiveSetupKey)
            {
                Write-ADTLogEntry -Message "Removing Active Setup entry [$ActiveSetupKey]."
                Remove-ADTRegistryKey -Key $ActiveSetupKey -Recurse

                if ($runAsActiveUser)
                {
                    Write-ADTLogEntry -Message "Removing Active Setup entry [$HKCUActiveSetupKey] for all logged on user registry hives on the system."
                    Invoke-ADTAllUsersRegistryChange -UserProfiles (Get-ADTUserProfiles -ExcludeDefaultUser | Where-Object {$_.SID -eq $runAsActiveUser.SID}) -RegistrySettings {
                        if (Get-ADTRegistryKey -Key $HKCUActiveSetupKey -SID $_.SID)
                        {
                            Remove-ADTRegistryKey -Key $HKCUActiveSetupKey -SID $_.SID -Recurse
                        }
                    }
                }
                return
            }

            # Copy file to $StubExePath from the 'Files' subdirectory of the script directory (if it exists there).
            $StubExePath = [System.Environment]::ExpandEnvironmentVariables($StubExePath)
            if ($adtSession)
            {
                $StubExeFile = Join-Path -Path $adtSession.GetPropertyValue('DirFiles') -ChildPath ($ActiveSetupFileName = [System.IO.Path]::GetFileName($StubExePath))
                if (Test-Path -LiteralPath $StubExeFile -PathType Leaf)
                {
                    # This will overwrite the StubPath file if $StubExePath already exists on target.
                    Copy-File -Path $StubExeFile -Destination $StubExePath -ContinueOnError $false
                }
            }

            # Check if the $StubExePath file exists.
            if (!(Test-Path -LiteralPath $StubExePath -PathType Leaf))
            {
                $naerParams = @{
                    Exception = [System.IO.FileNotFoundException]::new("Active Setup StubPath file [$ActiveSetupFileName] is missing.")
                    Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                    ErrorId = 'ActiveSetupFileNotFound'
                    TargetObject = $ActiveSetupFileName
                    RecommendedAction = "Please confirm the provided value and try again."
                }
                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
            }

            # Define Active Setup StubPath according to file extension of $StubExePath.
            switch ($StubExeExt)
            {
                '.exe' {
                    [String]$CUStubExePath = "$StubExePath"
                    [String]$CUArguments = $Arguments
                    [String]$StubPath = "`"$CUStubExePath`""
                }
                '.js' {
                    [String]$CUStubExePath = "$([System.Environment]::SystemDirectory)\cscript.exe"
                    [String]$CUArguments = "//nologo `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.vbs' {
                    [String]$CUStubExePath = "$([System.Environment]::SystemDirectory)\cscript.exe"
                    [String]$CUArguments = "//nologo `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.cmd' {
                    [String]$CUStubExePath = "$([System.Environment]::SystemDirectory)\cmd.exe"
                    [String]$CUArguments = "/C `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.ps1' {
                    [String]$CUStubExePath = Get-ADTPowerShellProcessPath
                    [String]$CUArguments = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `"& {& `\`"$StubExePath`\`"}`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
            }
            if ($Arguments)
            {
                $StubPath = "$StubPath $Arguments"
                if ($StubExeExt -ne '.exe')
                {
                    $CUArguments = "$CUArguments $Arguments"
                }
            }

            # Create the Active Setup entry in the registry.
            Write-ADTLogEntry -Message "Adding Active Setup Key for local machine: [$ActiveSetupKey]."
            Set-ADTActiveSetupRegKeys -ActiveSetupRegKey $ActiveSetupKey

            # Execute the StubPath file for the current user as long as not in Session 0.
            if (!$ExecuteForCurrentUser)
            {
                return
            }

            if (![System.Diagnostics.Process]::GetCurrentProcess().SessionId)
            {
                if (!$runAsActiveUser)
                {
                    Write-ADTLogEntry -Message 'Session 0 detected: No logged in users detected. Active Setup StubPath file will execute when users first log into their account.'
                    return
                }

                # Skip if Active Setup reg key is present and Version is equal or higher
                if (!($InstallNeeded = Test-ADTActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey -UserSID $runAsActiveUser.SID))
                {
                    Write-ADTLogEntry -Message "Session 0 detected: Skipping executing Active Setup StubPath file for currently logged in user [$($runAsActiveUser.NTAccount)]." -Severity 2
                    return
                }

                Write-ADTLogEntry -Message "Session 0 detected: Executing Active Setup StubPath file for currently logged in user [$($runAsActiveUser.NTAccount)]."
                if ($CUArguments)
                {
                    Execute-ProcessAsUser -Path $CUStubExePath -Parameters $CUArguments -Wait -ContinueOnError $true
                }
                else
                {
                    Execute-ProcessAsUser -Path $CUStubExePath -Wait -ContinueOnError $true
                }

                Write-ADTLogEntry -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]."
                Set-ADTActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey -SID $runAsActiveUser.SID
            }
            else
            {
                # Skip if Active Setup reg key is present and Version is equal or higher
                if (!($InstallNeeded = Test-ADTActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey))
                {
                    Write-ADTLogEntry -Message 'Skipping executing Active Setup StubPath file for current user.' -Severity 2
                    return
                }

                Write-ADTLogEntry -Message 'Executing Active Setup StubPath file for the current user.'
                if ($CUArguments)
                {
                    Start-ADTProcess -FilePath $CUStubExePath -Parameters $CUArguments -NoExitOnProcessFailure
                }
                else
                {
                    Start-ADTProcess -FilePath $CUStubExePath -NoExitOnProcessFailure
                }

                Write-ADTLogEntry -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]."
                Set-ADTActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey
            }
        }
        catch {
            Write-ADTLogEntry -Message "Failed to set Active Setup registry entry.`n$(Resolve-ADTError)" -Severity 3
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
