#-----------------------------------------------------------------------------
#
# MARK: Set-ADTActiveSetup
#
#-----------------------------------------------------------------------------

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

        - Creates the registry entries in "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\$($adtSession.InstallName)".
        - Creates StubPath value depending on the file extension of the $StubExePath parameter.
        - Handles Version value with YYYYMMDDHHMMSS granularity to permit re-installs on the same day and still trigger Active Setup after Version increase.
        - Copies/overwrites the StubPath file to $StubExePath destination path if file exists in 'Files' subdirectory of script directory.
        - Executes the StubPath file for the current user based on $NoExecuteForCurrentUser (no need to logout/login to trigger Active Setup).

    .PARAMETER StubExePath
        Use this parameter to specify the destination path of the file that will be executed upon user login.

        Note: Place the file you want users to execute in the '\Files' subdirectory of the script directory and the toolkit will install it to the path specificed in this parameter.

    .PARAMETER Arguments
        Arguments to pass to the file being executed.

    .PARAMETER Wow6432Node
        Specify this switch to use Active Setup entry under Wow6432Node on a 64-bit OS.

    .PARAMETER ExecutionPolicy
        Specifies the ExecutionPolicy to set when StubExePath is a PowerShell script..

    .PARAMETER Version
        Optional. Specify version for Active setup entry. Active Setup is not triggered if Version value has more than 8 consecutive digits. Use commas to get around this limitation.

        Note:
            - Do not use this parameter if it is not necessary. PSADT will handle this parameter automatically using the time of the installation as the version number.
            - In Windows 10, scripts and executables might be blocked by AppLocker. Ensure that the path given to -StubExePath will permit end users to run scripts and executables unelevated.

    .PARAMETER Locale
        Optional. Arbitrary string used to specify the installation language of the file being executed. Not replicated to HKCU.

    .PARAMETER PurgeActiveSetupKey
        Remove Active Setup entry from HKLM registry hive. Will also load each logon user's HKCU registry hive to remove Active Setup entry. Function returns after purging.

    .PARAMETER DisableActiveSetup
        Disables the Active Setup entry so that the StubPath file will not be executed. This also enables -NoExecuteForCurrentUser.

    .PARAMETER NoExecuteForCurrentUser
        Specifies whether the StubExePath should be executed for the current user. Since this user is already logged in, the user won't have the application started without logging out and logging back in.

    .PARAMETER PassThru
        Returns a ProcessResult from the execution of the ActiveSetup configuration for the current user if `-PassThru` is provided.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Execution.ProcessResult

        This function returns a ProcessResult from the execution of the ActiveSetup configuration for the current user if `-PassThru` is provided.

    .EXAMPLE
        Set-ADTActiveSetup -StubExePath 'C:\Users\Public\Company\ProgramUserConfig.vbs' -Arguments '/Silent' -Description 'Program User Config' -Key 'ProgramUserConfig' -Locale 'en'

    .EXAMPLE
        Set-ADTActiveSetup -StubExePath "$envWinDir\regedit.exe" -Arguments "/S `"%SystemDrive%\Program Files (x86)\PS App Deploy\PSAppDeployHKCUSettings.reg`"" -Description 'PS App Deploy Config' -Key 'PS_App_Deploy_Config'

    .EXAMPLE
        Set-ADTActiveSetup -Key 'ProgramUserConfig' -PurgeActiveSetupKey

        Delete "ProgramUserConfig" active setup entry from all registry hives.

    .NOTES
        An active ADT session is NOT required to use this function.

        Original code borrowed from: Denis St-Pierre (Ottawa, Canada), Todd MacNaught (Ottawa, Canada)

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTActiveSetup
    #>

    [CmdletBinding(DefaultParameterSetName = 'Create')]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Create')]
        [ValidateScript({
                if (('.exe', '.vbs', '.cmd', '.bat', '.ps1', '.js') -notcontains ($StubExeExt = [System.IO.Path]::GetExtension($_)))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName StubExePath -ProvidedValue $_ -ExceptionMessage "Unsupported Active Setup StubPath file extension [$StubExeExt]."))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$StubExePath,

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [Parameter(Mandatory = $false, ParameterSetName = 'CreateNoExecute')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Arguments,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Wow6432Node,

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [Parameter(Mandatory = $false, ParameterSetName = 'CreateNoExecute')]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = '(Get-ExecutionPolicy)')]
        [Microsoft.PowerShell.ExecutionPolicy]$ExecutionPolicy,

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [Parameter(Mandatory = $false, ParameterSetName = 'CreateNoExecute')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Version = [System.DateTime]::Now.ToString('yyMM,ddHH,mmss'), # Ex: 1405,1515,0522

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [Parameter(Mandatory = $false, ParameterSetName = 'CreateNoExecute')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Locale,

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [Parameter(Mandatory = $false, ParameterSetName = 'CreateNoExecute')]
        [System.Management.Automation.SwitchParameter]$DisableActiveSetup,

        [Parameter(Mandatory = $true, ParameterSetName = 'Purge')]
        [System.Management.Automation.SwitchParameter]$PurgeActiveSetupKey,

        [Parameter(Mandatory = $true, ParameterSetName = 'CreateNoExecute')]
        [System.Management.Automation.SwitchParameter]$NoExecuteForCurrentUser,

        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    dynamicparam
    {
        # Attempt to get the most recent ADTSession object.
        $adtSession = if (Test-ADTSessionActive)
        {
            Get-ADTSession
        }

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Key', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Key', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Name of the registry key for the Active Setup entry. Defaults to active session InstallName.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('Description', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Description', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Description for the Active Setup. Users will see "Setting up personalized settings for: $Description" at logon. Defaults to active session InstallName.'; ParameterSetName = 'Create' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Set defaults for when there's an active ADTSession and overriding values haven't been specified.
        $Description = if ($PSCmdlet.ParameterSetName.Equals('Create'))
        {
            if (!$PSBoundParameters.ContainsKey('Description'))
            {
                $adtSession.InstallName
            }
            else
            {
                $PSBoundParameters.Description
            }
        }
        $Key = if (!$PSBoundParameters.ContainsKey('Key'))
        {
            $adtSession.InstallName
        }
        else
        {
            $PSBoundParameters.Key
        }

        # Define initial variables.
        $ActiveSetupFileName = [System.IO.Path]::GetFileName($StubExePath)
        $runAsActiveUser = Get-ADTClientServerUser
        $CUStubExePath = $null
        $CUArguments = $null
        $StubExeExt = [System.IO.Path]::GetExtension($StubExePath)
        $StubPath = $null

        # Define internal function to test current ActiveSetup stuff.
        function Test-ADTActiveSetup
        {
            [CmdletBinding()]
            [OutputType([System.Boolean])]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$HKLMKey,

                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$HKCUKey,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.String]$SID
            )

            # Set up initial variables.
            $HKCUProps = if ($SID)
            {
                Get-ADTRegistryKey -Key $HKCUKey -SID $SID
            }
            else
            {
                Get-ADTRegistryKey -Key $HKCUKey
            }
            $HKLMProps = Get-ADTRegistryKey -Key $HKLMKey
            $HKCUVer = $HKCUProps | Select-Object -ExpandProperty Version -ErrorAction Ignore
            $HKLMVer = $HKLMProps | Select-Object -ExpandProperty Version -ErrorAction Ignore
            $HKLMInst = $HKLMProps | Select-Object -ExpandProperty IsInstalled -ErrorAction Ignore

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
            if (!([System.Object]$HKLMValidVer = [System.String]::Join([System.String]::Empty, ($HKLMVer.GetEnumerator() | & { process { if ([System.Char]::IsDigit($_)) { return $_ } elseif ($_ -eq ',') { return '.' } } }))) -or ![System.Version]::TryParse($HKLMValidVer, [ref]$HKLMValidVer))
            {
                Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKLM Version property is invalid.'
                return $false
            }

            # After cleanup, the HKCU Version property is empty while HKLM Version property is not. Run the StubPath.
            if (!([System.Object]$HKCUValidVer = [System.String]::Join([System.String]::Empty, ($HKCUVer.GetEnumerator() | & { process { if ([System.Char]::IsDigit($_)) { return $_ } elseif ($_ -eq ',') { return '.' } } }))) -or ![System.Version]::TryParse($HKCUValidVer, [ref]$HKCUValidVer))
            {
                Write-ADTLogEntry 'HKLM and HKCU active setup entries are present. HKCU Version property is invalid.'
                return $true
            }

            # Both entries present, with a Version property. Compare the Versions.
            if ($HKLMValidVer -gt $HKCUValidVer)
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

        # Define internal function to the required ActiveSetup registry keys.
        function Set-ADTActiveSetupRegistryEntry
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is an internal worker function that requires no end user confirmation.')]
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$RegPath,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.String]$SID,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.String]$Version,

                [Parameter(Mandatory = $false)]
                [AllowEmptyString()]
                [System.String]$Locale,

                [Parameter(Mandatory = $false)]
                [System.Management.Automation.SwitchParameter]$DisableActiveSetup
            )

            $srkParams = if ($SID) { @{ SID = $SID } } else { @{} }
            Set-ADTRegistryKey -Key $RegPath -Name '(Default)' -Value $Description @srkParams
            Set-ADTRegistryKey -Key $RegPath -Name 'Version' -Value $Version @srkParams
            Set-ADTRegistryKey -Key $RegPath -Name 'StubPath' -Value $StubPath -Type ExpandString @srkParams
            if (![System.String]::IsNullOrWhiteSpace($Locale))
            {
                Set-ADTRegistryKey -Key $RegPath -Name 'Locale' -Value $Locale @srkParams
            }

            # Only Add IsInstalled to HKLM.
            if ($RegPath.Contains('HKEY_LOCAL_MACHINE'))
            {
                Set-ADTRegistryKey -Key $RegPath -Name 'IsInstalled' -Value ([System.UInt32]!$DisableActiveSetup) -Type 'DWord' @srkParams
            }
        }
    }

    process
    {
        try
        {
            try
            {
                # Set up the relevant keys, factoring in bitness and architecture.
                if ($Wow6432Node -and [System.Environment]::Is64BitOperatingSystem)
                {
                    $HKLMRegKey = "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\$Key"
                    $HKCURegKey = "Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\$Key"
                }
                else
                {
                    $HKLMRegKey = "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\$Key"
                    $HKCURegKey = "Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Microsoft\Active Setup\Installed Components\$Key"
                }

                # Delete Active Setup registry entry from the HKLM hive and for all logon user registry hives on the system.
                if ($PurgeActiveSetupKey)
                {
                    # HLKM first.
                    Write-ADTLogEntry -Message "Removing Active Setup entry [$HKLMRegKey]."
                    Remove-ADTRegistryKey -Key $HKLMRegKey -Recurse

                    # All remaining users thereafter.
                    Write-ADTLogEntry -Message "Removing Active Setup entry [$HKCURegKey] for all logged on user registry hives on the system."
                    Invoke-ADTAllUsersRegistryAction -UserProfiles (Get-ADTUserProfiles -ExcludeDefaultUser | & { process { if ($_.SID -eq $runAsActiveUser.SID) { return $_ } } } | Select-Object -First 1) -ScriptBlock {
                        if (Get-ADTRegistryKey -Key $HKCURegKey -SID $_.SID)
                        {
                            Remove-ADTRegistryKey -Key $HKCURegKey -SID $_.SID -Recurse
                        }
                    }
                    return
                }

                # Copy file to $StubExePath from the 'Files' subdirectory of the script directory (if it exists there).
                if ($adtSession -and $adtSession.DirFiles)
                {
                    $StubExeFile = Join-Path -Path $adtSession.DirFiles -ChildPath $ActiveSetupFileName
                    if (Test-Path -LiteralPath $StubExeFile -PathType Leaf)
                    {
                        # This will overwrite the StubPath file if $StubExePath already exists on target.
                        Copy-ADTFile -Path $StubExeFile -Destination $StubExePath -ErrorAction Stop
                    }
                }

                # Check if the $StubExePath file exists.
                if (($StubExePath -notmatch '%\w+%') -and !(Test-Path -LiteralPath $StubExePath -PathType Leaf))
                {
                    $naerParams = @{
                        Exception = [System.IO.FileNotFoundException]::new("Active Setup StubPath file [$ActiveSetupFileName] is missing.")
                        Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                        ErrorId = 'ActiveSetupFileNotFound'
                        TargetObject = $ActiveSetupFileName
                        RecommendedAction = "Please confirm the provided value and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Define Active Setup StubPath according to file extension of $StubExePath.
                switch ($StubExeExt)
                {
                    '.exe'
                    {
                        $CUStubExePath = $StubExePath
                        $CUArguments = $Arguments
                        $StubPath = if ([System.String]::IsNullOrWhiteSpace($Arguments))
                        {
                            "`"$CUStubExePath`""
                        }
                        else
                        {
                            "`"$CUStubExePath`" $CUArguments"
                        }
                        break
                    }
                    { $_ -in '.js', '.vbs' }
                    {
                        $CUStubExePath = "$([System.Environment]::SystemDirectory)\wscript.exe"
                        $CUArguments = if ([System.String]::IsNullOrWhiteSpace($Arguments))
                        {
                            "//nologo `"$StubExePath`""
                        }
                        else
                        {
                            "//nologo `"$StubExePath`"  $Arguments"
                        }
                        $StubPath = "`"$CUStubExePath`" $CUArguments"
                        break
                    }
                    { $_ -in '.cmd', '.bat' }
                    {
                        $CUStubExePath = "$([System.Environment]::SystemDirectory)\cmd.exe"
                        # Prefix any CMD.exe metacharacters ^ or & with ^ to escape them - parentheses only require escaping when there's no space in the path!
                        $StubExePath = if ($StubExePath.Trim() -match '\s')
                        {
                            $StubExePath -replace '([&^])', '^$1'
                        }
                        else
                        {
                            $StubExePath -replace '([()&^])', '^$1'
                        }
                        $CUArguments = if ([System.String]::IsNullOrWhiteSpace($Arguments))
                        {
                            "/C `"$StubExePath`""
                        }
                        else
                        {
                            "/C `"`"$StubExePath`" $Arguments`""
                        }
                        $StubPath = "`"$CUStubExePath`" $CUArguments"
                        break
                    }
                    '.ps1'
                    {
                        $CUStubExePath = Get-ADTPowerShellProcessPath
                        $CUArguments = if ([System.String]::IsNullOrWhiteSpace($Arguments))
                        {
                            "$(if ($PSBoundParameters.ContainsKey('ExecutionPolicy')) { "-ExecutionPolicy $ExecutionPolicy " })-NoProfile -NoLogo -WindowStyle Hidden -File `"$StubExePath`""
                        }
                        else
                        {
                            "$(if ($PSBoundParameters.ContainsKey('ExecutionPolicy')) { "-ExecutionPolicy $ExecutionPolicy " })-NoProfile -NoLogo -WindowStyle Hidden -File `"$StubExePath`" $Arguments"
                        }
                        $StubPath = "`"$CUStubExePath`" $CUArguments"
                        break
                    }
                }

                # Define common parameters split for Set-ADTActiveSetupRegistryEntry.
                $sasreParams = @{
                    Version = $Version
                    Locale = $Locale
                    DisableActiveSetup = $DisableActiveSetup
                }

                # Create the Active Setup entry in the registry.
                Write-ADTLogEntry -Message "Adding Active Setup Key for local machine: [$HKLMRegKey]."
                Set-ADTActiveSetupRegistryEntry @sasreParams -RegPath $HKLMRegKey

                # Execute the StubPath file for the current user as long as not in Session 0.
                if ($NoExecuteForCurrentUser)
                {
                    return
                }

                $processResult = $null
                if ([System.Security.Principal.WindowsIdentity]::GetCurrent().User.IsWellKnown([System.Security.Principal.WellKnownSidType]::LocalSystemSid))
                {
                    if (!$runAsActiveUser)
                    {
                        Write-ADTLogEntry -Message 'Session 0 detected: No logged in users detected. Active Setup StubPath file will execute when users first log into their account.'
                        return
                    }

                    # Skip if Active Setup reg key is present and Version is equal or higher
                    if (!(Test-ADTActiveSetup -HKLMKey $HKLMRegKey -HKCUKey $HKCURegKey -SID $runAsActiveUser.SID))
                    {
                        Write-ADTLogEntry -Message "Session 0 detected: Skipping executing Active Setup StubPath file for currently logged in user [$($runAsActiveUser.NTAccount)]." -Severity 2
                        return
                    }

                    Write-ADTLogEntry -Message "Session 0 detected: Executing Active Setup StubPath file for currently logged in user [$($runAsActiveUser.NTAccount)]."
                    $processResult = if ($CUArguments)
                    {
                        Start-ADTProcessAsUser -FilePath $CUStubExePath -ArgumentList $CUArguments -CreateNoWindow -PassThru:$PassThru
                    }
                    else
                    {
                        Start-ADTProcessAsUser -FilePath $CUStubExePath -CreateNoWindow -PassThru:$PassThru
                    }

                    Write-ADTLogEntry -Message "Adding Active Setup Key for the current user: [$HKCURegKey]."
                    Set-ADTActiveSetupRegistryEntry @sasreParams -RegPath $HKCURegKey -SID $runAsActiveUser.SID
                }
                else
                {
                    # Skip if Active Setup reg key is present and Version is equal or higher
                    if (!(Test-ADTActiveSetup -HKLMKey $HKLMRegKey -HKCUKey $HKCURegKey))
                    {
                        Write-ADTLogEntry -Message 'Skipping executing Active Setup StubPath file for current user.' -Severity 2
                        return
                    }

                    Write-ADTLogEntry -Message 'Executing Active Setup StubPath file for the current user.'
                    $processResult = if ($CUArguments)
                    {
                        if ($StubExeExt -eq '.ps1')
                        {
                            $CUArguments = $CUArguments.Replace("-WindowStyle Hidden ", $null)
                        }
                        Start-ADTProcess -FilePath $CUStubExePath -ArgumentList $CUArguments -CreateNoWindow -PassThru:$PassThru
                    }
                    else
                    {
                        Start-ADTProcess -FilePath $CUStubExePath -CreateNoWindow -PassThru:$PassThru
                    }

                    Write-ADTLogEntry -Message "Adding Active Setup Key for the current user: [$HKCURegKey]."
                    Set-ADTActiveSetupRegistryEntry @sasreParams -RegPath $HKCURegKey
                }

                # Return the process result if its available and requested.
                if ($processResult -and $PassThru)
                {
                    return $processResult
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to set Active Setup registry entry."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
