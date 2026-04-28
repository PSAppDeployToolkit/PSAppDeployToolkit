#-----------------------------------------------------------------------------
#
# MARK: New-ADTTemplate
#
#-----------------------------------------------------------------------------

function New-ADTTemplate
{
    <#
    .SYNOPSIS
        Creates a new folder containing a template front end and module folder, ready to customise.

    .DESCRIPTION
        Specify a destination path where a new folder will be created. You also have the option of creating a template for v3 compatibility mode.

    .PARAMETER Destination
        Path where the new folder should be created. Default is the current working directory.

    .PARAMETER Name
        Name of the newly created folder. Default is PSAppDeployToolkit_Version.

    .PARAMETER Version
        Defaults to 4 for the standard v4 template. Use 3 for the v3 compatibility mode template.

    .PARAMETER SessionProperties
        A dictionary of key-value pairs to inject into the $adtSession hashtable of the generated Invoke-AppDeployToolkit.ps1. Accepts [hashtable], [ordered], or any [System.Collections.IDictionary] type. Only supported when -Version is 4.

    .PARAMETER Config
        A dictionary of key-value pairs to override or add to the generated Config\config.psd1. The dictionary structure must mirror the config file's nested hashtable layout (e.g. @{ MSI = @{ InstallParams = 'REBOOT=ReallySuppress /QB-!' } }). Existing keys are overridden in place. New keys or sections that do not exist in the default config are appended at the end of the relevant hashtable level.

    .PARAMETER PreInstallScriptBlock
        A ScriptBlock whose content will replace the Pre-Install phase of the Install-ADTDeployment function in the generated Invoke-AppDeployToolkit.ps1. Only supported when -Version is 4.

    .PARAMETER InstallScriptBlock
        A ScriptBlock whose content will replace the Install phase of the Install-ADTDeployment function in the generated Invoke-AppDeployToolkit.ps1. Only supported when -Version is 4.

    .PARAMETER PostInstallScriptBlock
        A ScriptBlock whose content will replace the Post-Install phase of the Install-ADTDeployment function in the generated Invoke-AppDeployToolkit.ps1. Only supported when -Version is 4.

    .PARAMETER PreUninstallScriptBlock
        A ScriptBlock whose content will replace the Pre-Uninstall phase of the Uninstall-ADTDeployment function in the generated Invoke-AppDeployToolkit.ps1. Only supported when -Version is 4.

    .PARAMETER UninstallScriptBlock
        A ScriptBlock whose content will replace the Uninstall phase of the Uninstall-ADTDeployment function in the generated Invoke-AppDeployToolkit.ps1. Only supported when -Version is 4.

    .PARAMETER PostUninstallScriptBlock
        A ScriptBlock whose content will replace the Post-Uninstall phase of the Uninstall-ADTDeployment function in the generated Invoke-AppDeployToolkit.ps1. Only supported when -Version is 4.

    .PARAMETER PreRepairScriptBlock
        A ScriptBlock whose content will replace the Pre-Repair phase of the Repair-ADTDeployment function in the generated Invoke-AppDeployToolkit.ps1. Only supported when -Version is 4.

    .PARAMETER RepairScriptBlock
        A ScriptBlock whose content will replace the Repair phase of the Repair-ADTDeployment function in the generated Invoke-AppDeployToolkit.ps1. Only supported when -Version is 4.

    .PARAMETER PostRepairScriptBlock
        A ScriptBlock whose content will replace the Post-Repair phase of the Repair-ADTDeployment function in the generated Invoke-AppDeployToolkit.ps1. Only supported when -Version is 4.

    .PARAMETER Assets
        An array of file or folder paths to copy into the Assets folder of the generated template. Paths are passed to Copy-Item with -Recurse.

    .PARAMETER Files
        An array of file or folder paths to copy into the Files folder of the generated template. Paths are passed to Copy-Item with -Recurse.

    .PARAMETER SupportFiles
        An array of file or folder paths to copy into the SupportFiles folder of the generated template. Paths are passed to Copy-Item with -Recurse.

    .PARAMETER Show
        Opens the newly created folder in Windows Explorer.

    .PARAMETER Force
        If the destination folder already exists, this switch will force the creation of the new folder.

    .PARAMETER PassThru
        Returns the newly created folder object.

    .PARAMETER ZeroConfig
        When specified, injects a default MSI scriptblock into the Install, Uninstall, and Repair phases that executes the detected default MSI. If InstallScriptBlock, UninstallScriptBlock, or RepairScriptBlock are also provided, the zero-config content is prepended to the user-supplied scriptblock. Only supported when -Version is 4.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        By default, this function returns no output.

    .OUTPUTS
        System.IO.DirectoryInfo

        When the `-PassThru` parameter is specified, this function returns a DirectoryInfo object representing the directory containing the new PSADT template.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -Name 'PSAppDeployToolkitv4'

        Creates a new v4 template named PSAppDeployToolkitv4 under C:\Temp.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -Name 'PSAppDeployToolkitv3' -Version 3

        Creates a new v3 compatibility mode template named PSAppDeployToolkitv3 under C:\Temp.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -SessionProperties @{ AppVendor = 'Contoso'; AppName = 'MyApp'; AppVersion = '6.7'; RequireAdmin = $false; AppProcessesToClose = @('notepad', @{ Name = 'calc'; Description = 'Calculator' }) }

        Creates a new v4 template with the specified session properties pre-populated in the $adtSession hashtable.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -Config @{ Toolkit = @{ LogPath = '$env:ProgramData\Microsoft\IntuneManagementExtension\Logs' } }

        Creates a new v4 template with a custom log folder.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -Assets 'C:\Assets\AppIconLight.png', 'C:\Assets\AppIconDark.png' -Config @{ Assets = @{ Logo = '..\Assets\AppIconLight.png'; LogoDark = '..\Assets\AppIconDark.png' } }

        Creates a new v4 template with custom icons copied to Assets and config.psd1 updated configured to use them.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -SessionProperties @{ AppVendor = 'Contoso'; AppName = 'MyApp'; AppVersion = '6.7' } -Files 'C:\Installers\Setup.msi' -InstallScriptBlock { Start-ADTMsiProcess -Action Install -FilePath 'Setup.msi' } -UninstallScriptBlock {  Start-ADTMsiProcess -Action Uninstall -FilePath 'Setup.msi' }

        Creates a new v4 template with session properties defined, an MSI copied to Files and install/uninstall commands added.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/New-ADTTemplate
    #>

    [CmdletBinding(SupportsShouldProcess = $false)]
    [OutputType([System.IO.DirectoryInfo])]
    param
    (
        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Destination = $ExecutionContext.SessionState.Path.CurrentLocation.Path,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [PSDefaultValue(Help = "PSAppDeployToolkit_<ModuleVersion>")]
        [System.String]$Name = "$($MyInvocation.MyCommand.Module.Name)_$($MyInvocation.MyCommand.Module.Version)",

        [Parameter(Mandatory = $false)]
        [ValidateRange(3, 4)]
        [System.Int32]$Version = 4,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Collections.IDictionary]$SessionProperties,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Collections.IDictionary]$Config,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$PreInstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$InstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$PostInstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$PreUninstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$UninstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$PostUninstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$PreRepairScriptBlock,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$RepairScriptBlock,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Management.Automation.ScriptBlock]$PostRepairScriptBlock,

        [Parameter(Mandatory = $false)]
        [SupportsWildcards()]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String[]]$Assets,

        [Parameter(Mandatory = $false)]
        [SupportsWildcards()]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String[]]$Files,

        [Parameter(Mandatory = $false)]
        [SupportsWildcards()]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String[]]$SupportFiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Show,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ZeroConfig,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Initialize the function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Helper to convert objects to their PowerShell expression string representation.
        function ConvertTo-ADTExpression
        {
            [CmdletBinding()]
            [OutputType([System.String])]
            param
            (
                [Parameter(Mandatory = $false)]
                [AllowNull()]
                [System.Object]$InputObject,

                [Parameter(Mandatory = $false)]
                [System.Management.Automation.SwitchParameter]$LiteralString
            )

            if ($null -eq $InputObject)
            {
                return '$null'
            }

            # Handle IEnumerable (non-string, non-dictionary) before the switch to prevent auto-enumeration of arrays.
            if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [System.String] -and $InputObject -isnot [System.Collections.IDictionary])
            {
                $items = foreach ($item in $InputObject)
                {
                    ConvertTo-ADTExpression -InputObject $item -LiteralString:$LiteralString
                }
                return "@($($items -join ', '))"
            }

            switch ($InputObject)
            {
                { $_ -is [System.Boolean] -or $_ -is [System.Management.Automation.SwitchParameter] }
                {
                    if ($_) { return '$true' } else { return '$false' }
                }
                { $_ -is [System.String] -or $_ -is [System.Char] -or $_ -is [System.Version] -or $_ -is [System.Guid] -or $_ -is [System.IO.FileSystemInfo] -or $_.GetType().IsEnum }
                {
                    $str = $_.ToString()
                    if (!$LiteralString -and ($str -match '(?<!`)\$'))
                    {
                        return "`"$($str -replace '(?<!`)"', '`"')`""
                    }
                    return "'$([System.Management.Automation.Language.CodeGeneration]::EscapeSingleQuotedStringContent($str))'"
                }
                { $_ -is [System.DateTime] -or $_ -is [System.DateTimeOffset] }
                {
                    return "(Get-Date '$($_.ToString('o'))')"
                }
                { $_ -is [System.TimeSpan] }
                {
                    return "[System.TimeSpan]'$($_.ToString())'"
                }
                { $_ -is [System.Management.Automation.ScriptBlock] }
                {
                    $scriptBody = (ConvertTo-ADTScriptBody -ScriptBlock $_)
                    # Add extra indentation here for multi-line script blocks, since this output will be injected into a hash table value assignment that is already indented once.
                    if ($scriptBody -match '\n') { return ('{' + [System.Environment]::NewLine + ($scriptBody -replace '(?m)^', '    ') + [System.Environment]::NewLine + '    }') } else { return ('{ ' + $scriptBody.Trim() + ' }') }
                }
                { $_ -is [System.Collections.IDictionary] }
                {
                    $pairs = foreach ($entry in $_.GetEnumerator())
                    {
                        "'$([System.Management.Automation.Language.CodeGeneration]::EscapeSingleQuotedStringContent($entry.Key.ToString()))' = $(ConvertTo-ADTExpression -InputObject $entry.Value -LiteralString:$LiteralString)"
                    }
                    $dictionaryPrefix = if ($_ -is [System.Collections.Specialized.OrderedDictionary]) { '[ordered]@{' } else { '@{' }
                    return "$dictionaryPrefix $($pairs -join '; ') }"
                }
                { $_ -is [System.ValueType] }
                {
                    return $_.ToString([System.Globalization.CultureInfo]::InvariantCulture)
                }
                default
                {
                    $naerParams = @{
                        Exception = [System.ArgumentException]::new("Session property value of type [$($_.GetType().FullName)] is not supported.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                        ErrorId = 'UnsupportedSessionPropertyValueType'
                        TargetObject = $_
                        RecommendedAction = 'Use any System.ValueType, strings, datetimes, timespans, scriptblocks, hashtables, or ordered dictionaries.'
                    }
                    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                }
            }
        }

        # Helper to normalize (and optionally concatenate) scriptblocks. Each input is trimmed, line endings normalized, source indentation stripped, leading tabs replaced with spaces.
        function ConvertTo-ADTScriptBody
        {
            [CmdletBinding()]
            [OutputType([System.String])]
            param
            (
                [Parameter(Mandatory = $true)]
                [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
                [System.Management.Automation.ScriptBlock[]]$ScriptBlock
            )

            $scriptBodies = foreach ($sb in $ScriptBlock)
            {
                # Trim leading empty lines (but not whitespace to preserve indentation) and trailing whitespace, replace tabs with spaces, then split into lines to analyze indentation.
                $lines = ([regex]::Replace($sb.ToString().TrimStart("`r", "`n").TrimEnd(), '(?m)^\t+', { param($m) '    ' * $m.Value.Length })).Split([System.String[]]("`r`n", "`n"), [System.StringSplitOptions]::None)
                $minIndent = ($lines | Where-Object { $_ -match '\S' } | ForEach-Object { [regex]::Match($_, '^ *').Length } | Measure-Object -Minimum).Minimum
                ($lines | ForEach-Object { if ([System.String]::IsNullOrWhiteSpace($_)) { $_ } elseif ($_.Length -ge $minIndent) { '    ' + $_.Substring($minIndent) } else { '    ' + $_ } }) -join [System.Environment]::NewLine
            }
            return ($scriptBodies -join ([System.Environment]::NewLine * 2))
        }

        # Helper to return a list of text replacements for a config.psd1 text content based on a dictionary of settings.
        function Get-ADTConfigReplacements
        {
            [CmdletBinding()]
            [OutputType([PSCustomObject[]])]
            param
            (
                [Parameter(Mandatory = $true)]
                [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
                [System.String]$ConfigText,

                [Parameter(Mandatory = $true)]
                [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
                [System.Collections.IDictionary]$Settings
            )

            $configHashtableAst = [System.Management.Automation.Language.Parser]::ParseInput($ConfigText, [ref]$null, [ref]$null).EndBlock.Statements[0].PipelineElements[0].Expression
            $configInsertions = @{}

            # Recursive scriptblock to walk user dictionary against config AST.
            $collectReplacements = {
                param ([System.Collections.IDictionary]$SettingsDict, $HashtableAst, [System.String]$Path)
                foreach ($kvp in $SettingsDict.GetEnumerator())
                {
                    $currentPath = if ($Path) { "$Path.$($kvp.Key)" } else { $kvp.Key }
                    $matchingKvp = $HashtableAst.KeyValuePairs.Where({ $_.Item1.Value -eq $kvp.Key })
                    if ($matchingKvp.Count -eq 0)
                    {
                        # Key does not exist in the default config; accumulate it for insertion.
                        $serializedValue = ConvertTo-ADTExpression -InputObject $kvp.Value -LiteralString
                        $closingBraceOffset = $ConfigText.LastIndexOf('}', $HashtableAst.Extent.EndOffset - 1)
                        $braceLineStart = $ConfigText.LastIndexOf("`n", $closingBraceOffset - 1) + 1
                        $indent = if ($HashtableAst.KeyValuePairs.Count -gt 0)
                        {
                            ' ' * ($HashtableAst.KeyValuePairs[0].Item1.Extent.StartColumnNumber - 1)
                        }
                        else
                        {
                            '    '
                        }
                        if (!$configInsertions.ContainsKey($braceLineStart))
                        {
                            $configInsertions[$braceLineStart] = [System.Collections.Generic.List[System.String]]::new()
                        }
                        $configInsertions[$braceLineStart].Add("${indent}'$([System.Management.Automation.Language.CodeGeneration]::EscapeSingleQuotedStringContent($kvp.Key))' = $serializedValue")
                        continue
                    }
                    $astValue = $matchingKvp[0].Item2.PipelineElements[0].Expression
                    if ($kvp.Value -is [System.Collections.IDictionary])
                    {
                        if ($astValue -isnot [System.Management.Automation.Language.HashtableAst])
                        {
                            $naerParams = @{
                                Exception = [System.ArgumentException]::new("The config key '$currentPath' is not a hashtable in the default configuration but a hashtable value was provided.")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                                ErrorId = 'ConfigKeyTypeMismatch'
                                TargetObject = $currentPath
                                RecommendedAction = 'Please provide a scalar value for this key and try again.'
                            }
                            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                        }
                        & $collectReplacements -SettingsDict $kvp.Value -HashtableAst $astValue -Path $currentPath
                    }
                    else
                    {
                        [PSCustomObject]@{
                            Start = $matchingKvp[0].Item2.Extent.StartOffset
                            End = $matchingKvp[0].Item2.Extent.EndOffset
                            Value = (ConvertTo-ADTExpression -InputObject $kvp.Value -LiteralString)
                        }
                    }
                }
            }
            & $collectReplacements -SettingsDict $Settings -HashtableAst $configHashtableAst -Path $null

            # Convert accumulated new-key insertions into replacement objects.
            foreach ($offset in $configInsertions.Keys)
            {
                [PSCustomObject]@{
                    Start = $offset
                    End = $offset
                    Value = (($configInsertions[$offset] -join [System.Environment]::NewLine) + [System.Environment]::NewLine)
                }
            }
        }

        # Helper to apply an array of Start/End/Value replacement objects to a string from end to start.
        function Set-ADTTextReplacements
        {
            [CmdletBinding()]
            [OutputType([System.String])]
            param
            (
                [Parameter(Mandatory = $true)]
                [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
                [System.String]$InputText,

                [Parameter(Mandatory = $true)]
                [ValidateScript({
                        if ($null -eq $_.Start -or $null -eq $_.End -or $null -eq $_.Value)
                        {
                            $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName 'Replacements' -ProvidedValue ($_ | Out-String).Trim() -ExceptionMessage 'The specified replacement does not have the required Start/End/Value properties.'))
                        }
                        return $true
                    })]
                [PSCustomObject[]]$Replacements
            )

            foreach ($replacement in ($Replacements | Sort-Object -Property Start -Descending))
            {
                $InputText = $InputText.Substring(0, $replacement.Start) + $replacement.Value + $InputText.Substring($replacement.End)
            }
            return $InputText
        }

        # Some parameters are only supported for v4 templates.
        if ($Version.Equals(3))
        {
            if (($invalidParams = @('SessionProperties', 'PreInstallScriptBlock', 'InstallScriptBlock', 'PostInstallScriptBlock', 'PreUninstallScriptBlock', 'UninstallScriptBlock', 'PostUninstallScriptBlock', 'PreRepairScriptBlock', 'RepairScriptBlock', 'PostRepairScriptBlock', 'ZeroConfig').Where({ $PSBoundParameters.ContainsKey($_) })))
            {
                $naerParams = @{
                    Exception = [System.InvalidOperationException]::new("The following parameters are not supported when -Version is 3: $($invalidParams -join ', ').")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'InvalidParameter'
                    TargetObject = $invalidParams
                    RecommendedAction = "Please use -Version 4 or remove the -$($invalidParams -join ', -') parameter(s) and try again."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
        }

        # Handle -ZeroConfig: inject the default MSI scriptblock into Install/Uninstall/Repair phases.
        if ($ZeroConfig)
        {
            $zeroConfigScriptBlock = {
                ## Handle Zero-Config MSI actions.
                if ($adtSession.UseDefaultMsi)
                {
                    $ExecuteDefaultMSISplat = @{ Action = $adtSession.DeploymentType; FilePath = $adtSession.DefaultMsiFile }
                    if ($adtSession.DefaultMstFile)
                    {
                        $ExecuteDefaultMSISplat.Add('Transforms', $adtSession.DefaultMstFile)
                    }
                    Start-ADTMsiProcess @ExecuteDefaultMSISplat
                }
            }
            foreach ($sbName in 'InstallScriptBlock', 'UninstallScriptBlock', 'RepairScriptBlock')
            {
                if ($PSBoundParameters.ContainsKey($sbName))
                {
                    $PSBoundParameters[$sbName] = [System.Management.Automation.ScriptBlock]::Create((ConvertTo-ADTScriptBody -ScriptBlock $zeroConfigScriptBlock, $PSBoundParameters[$sbName]))
                }
                else
                {
                    $PSBoundParameters.Add($sbName, $zeroConfigScriptBlock)
                }
            }
        }

        # Resolve the path to handle setups like ".\", etc.
        # We can't use things like a DirectoryInfo cast as .NET doesn't
        # track when the current location in PowerShell has been changed.
        if (($resolvedDest = Resolve-Path -LiteralPath $Destination -ErrorAction Ignore))
        {
            $Destination = $resolvedDest.Path
        }

        # Set up remaining variables.
        $moduleName = $MyInvocation.MyCommand.Module.Name
        $templatePath = (Join-Path -Path $Destination -ChildPath $Name).Trim()
        $templateModulePath = if ($Version.Equals(3))
        {
            (Join-Path -Path $templatePath -ChildPath "AppDeployToolkit\$moduleName").Trim()
        }
        else
        {
            (Join-Path -Path $templatePath -ChildPath $moduleName).Trim()
        }
    }

    process
    {
        try
        {
            try
            {
                # If we're running a release module, ensure the psd1 files haven't been tampered with.
                if ($Script:Module.Compiled -and $Script:Module.Signed -and ($badFiles = Get-ChildItem -LiteralPath $Script:PSScriptRoot -Filter *.ps*1 -Recurse | Get-AuthenticodeSignature | & { process { if (!$_.Status.Equals([System.Management.Automation.SignatureStatus]::Valid)) { return $_ } } }))
                {
                    $naerParams = @{
                        Exception = [System.Security.Cryptography.CryptographicException]::new("One or more files within this module have invalid digital signatures.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidData
                        ErrorId = 'ADTDataFileSignatureError'
                        TargetObject = $badFiles
                        RecommendedAction = "Please re-download $($MyInvocation.MyCommand.Module.Name) and try again."
                    }
                    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                }

                # Create directories.
                if ((Test-Path -LiteralPath $templatePath -PathType Container) -and [System.IO.Directory]::GetFileSystemEntries($templatePath))
                {
                    if (!$Force)
                    {
                        $naerParams = @{
                            Exception = [System.IO.IOException]::new("Folders [$templatePath] already exists and is not empty.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                            ErrorId = 'NonEmptySubfolderError'
                            TargetObject = $templatePath
                            RecommendedAction = "Please remove the existing folder, supply a new name, or add the -Force parameter and try again."
                        }
                        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                    }
                    $null = Remove-Item -LiteralPath $templatePath -Recurse -Force
                }
                $null = New-Item -Path "$templatePath\Files" -ItemType Directory -Force
                $null = New-Item -Path "$templatePath\SupportFiles" -ItemType Directory -Force

                # Copy in the frontend files.
                Copy-Item -Path "$([System.Management.Automation.WildcardPattern]::Escape("$Script:PSScriptRoot\opt\Frontend\v$Version"))\*" -Destination $templatePath -Recurse -Force

                # Export default module assets to disk.
                $null = New-Item -Path "$templatePath\Assets" -ItemType Directory -Force
                $defaultAssets = $Script:ADT.ModuleDefaults.Config.([System.String]::Empty).Ast.EndBlock.Statements.PipelineElements.Expression.KeyValuePairs.Where({ $_.Item1.Value.Equals('Assets') }).Item2.PipelineElements.Expression.KeyValuePairs
                [System.IO.File]::WriteAllBytes("$templatePath\Assets\Banner.Classic.png", [System.Convert]::FromBase64String(($banner = $defaultAssets.Where({ $_.Item1.Value.Equals('Banner') }).Item2.PipelineElements.Expression.Value)))
                [System.IO.File]::WriteAllBytes("$templatePath\Assets\AppIcon.ico", [System.Convert]::FromBase64String(($logo = $defaultAssets.Where({ $_.Item1.Value.Equals('Logo') }).Item2.PipelineElements.Expression.Value)))
                $configText = $ADT.ModuleDefaults.Config.([System.String]::Empty).ToString().Replace($banner, '..\Assets\Banner.Classic.png').Replace($logo, '..\Assets\AppIcon.ico')

                # Override config values if specified.
                if ($PSBoundParameters.ContainsKey('Config'))
                {
                    $configReplacements = Get-ADTConfigReplacements -ConfigText $configText -Settings $Config
                    $configText = Set-ADTTextReplacements -InputText $configText -Replacements $configReplacements
                }

                # Export the string data from the module to disk.
                $null = New-Item -Path "$templatePath\Config" -ItemType Directory -Force
                Export-ADTScriptBlockToFile -ScriptBlock ([System.Management.Automation.ScriptBlock]::Create($configText)) -LiteralPath "$templatePath\Config\config.psd1"

                # Export the string data from the module to disk.
                $null = New-Item -Path "$templatePath\Strings" -ItemType Directory -Force
                foreach ($stringData in $ADT.ModuleDefaults.Strings.GetEnumerator())
                {
                    if ([System.String]::IsNullOrWhiteSpace($stringData.Key))
                    {
                        continue
                    }
                    $null = New-Item -Path "$templatePath\Strings\$($stringData.Key)" -ItemType Directory -Force
                    Export-ADTScriptBlockToFile -ScriptBlock $stringData.Value -LiteralPath "$templatePath\Strings\$($stringData.Key)\strings.psd1"
                }
                Export-ADTScriptBlockToFile -ScriptBlock $ADT.ModuleDefaults.Strings.([System.String]::Empty) -LiteralPath "$templatePath\Strings\strings.psd1"

                # Remove any digital signatures from the ps*1 files.
                Get-ChildItem -LiteralPath $templatePath -File -Filter *.ps*1 -Recurse | & {
                    process
                    {
                        if (($sigLine = $(($fileLines = [System.IO.File]::ReadAllLines($_.FullName)) -match '^# SIG # Begin signature block$')))
                        {
                            [System.IO.File]::WriteAllLines($_.FullName, $fileLines[0..($fileLines.IndexOf($sigLine) - 2)])
                        }
                    }
                }

                # Copy in the module files.
                $null = New-Item -Path $templateModulePath -ItemType Directory -Force
                Copy-Item -Path "$([System.Management.Automation.WildcardPattern]::Escape("$Script:PSScriptRoot"))\*" -Destination $templateModulePath -Recurse -Force

                # Make the shipped module and its files read-only.
                $(Get-Item -LiteralPath $templateModulePath; Get-ChildItem -LiteralPath $templateModulePath -Recurse) | & {
                    process
                    {
                        $_.Attributes = 'ReadOnly'
                    }
                }

                # Copy user-supplied content into the template folders.
                foreach ($folder in 'Assets', 'Files', 'SupportFiles')
                {
                    if ($PSBoundParameters.ContainsKey($folder) -and $PSBoundParameters[$folder].Count -gt 0)
                    {
                        Copy-Item -Path $PSBoundParameters[$folder] -Destination "$templatePath\$folder" -Recurse -Force
                    }
                }

                # Process the generated script
                if ($Version.Equals(4))
                {
                    $params = @{
                        LiteralPath = "$templatePath\Invoke-AppDeployToolkit.ps1"
                        Encoding = ('utf8', 'utf8BOM')[$PSVersionTable.PSEdition.Equals('Core')]
                    }
                    $scriptContent = ((Get-Content @params -Raw) -replace '\r?\n', [System.Environment]::NewLine).Replace('..\..\..\..\', [System.Management.Automation.Language.NullString]::Value).Replace('2000-12-31', [System.DateTime]::Now.ToString('yyyy-MM-dd'))

                    # Collect all script content replacements (absolute offsets) across all features,
                    # then apply them in a single pass from end to start to preserve earlier offsets.
                    $scriptReplacements = [System.Collections.Generic.List[PSCustomObject]]::new()
                    $hasSessionProperties = $PSBoundParameters.ContainsKey('SessionProperties') -and $SessionProperties.Count -gt 0
                    $sectionsToProcess = @('PostRepair', 'Repair', 'PreRepair', 'PostUninstall', 'Uninstall', 'PreUninstall', 'PostInstall', 'Install', 'PreInstall' ).Where({ $PSBoundParameters.ContainsKey($_ + 'ScriptBlock') })
                    $scriptAst = [System.Management.Automation.Language.Parser]::ParseInput($scriptContent, [ref]$null, [ref]$null)

                    # Strip all SuppressMessageAttribute decorations from the generated script.
                    foreach ($attributeAst in $scriptAst.FindAll({ param ($ast) $ast -is [System.Management.Automation.Language.AttributeAst] -and $ast.TypeName.FullName -eq 'System.Diagnostics.CodeAnalysis.SuppressMessageAttribute' }, $true))
                    {
                        # Remove the entire line containing the attribute (including the trailing newline).
                        $lineStart = $scriptContent.LastIndexOf("`n", $attributeAst.Extent.StartOffset) + 1
                        $lineEnd = $scriptContent.IndexOf("`n", $attributeAst.Extent.EndOffset)
                        if ($lineEnd -lt 0) { $lineEnd = $scriptContent.Length } else { $lineEnd++ }
                        $scriptReplacements.Add([PSCustomObject]@{
                                Start = $lineStart
                                End = $lineEnd
                                Value = [System.String]::Empty
                            })
                    }

                    # Inject deployment script blocks into their corresponding phase variables if provided.
                    if ($sectionsToProcess.Count -gt 0)
                    {
                        foreach ($section in $sectionsToProcess)
                        {
                            $sbParamName = $section + 'ScriptBlock'

                            # Find the variable assignment in the AST (e.g. $PreInstall = { ... }).
                            $sbAssignment = $scriptAst.Find({
                                    param ($ast)
                                    $ast -is [System.Management.Automation.Language.AssignmentStatementAst] -and
                                    ($ast.Left | Get-Member -Name VariablePath) -and
                                    $ast.Left.VariablePath.UserPath -eq $section
                                }, $true)
                            if (!$sbAssignment)
                            {
                                $naerParams = @{
                                    Exception = [System.InvalidOperationException]::new("Variable '`$$section' not found in template script.")
                                    Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                                    ErrorId = 'TemplateVariableNotFound'
                                    TargetObject = $sbParamName
                                }
                                throw (New-ADTErrorRecord @naerParams)
                            }

                            # Get the scriptblock expression on the right-hand side.
                            $sbAst = $sbAssignment.Right.Expression
                            if ($sbAst -isnot [System.Management.Automation.Language.ScriptBlockExpressionAst])
                            {
                                $naerParams = @{
                                    Exception = [System.InvalidOperationException]::new("Variable '`$$section' is not assigned a scriptblock in the template script.")
                                    Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                                    ErrorId = 'TemplateVariableNotScriptBlock'
                                    TargetObject = $sbParamName
                                }
                                throw (New-ADTErrorRecord @naerParams)
                            }

                            $scriptText = '{' + [System.Environment]::NewLine + (ConvertTo-ADTScriptBody -ScriptBlock $PSBoundParameters[$sbParamName]) + [System.Environment]::NewLine + '}'

                            # Replace the entire scriptblock expression (including braces) with the user's content.
                            $scriptReplacements.Add([PSCustomObject]@{
                                    Start = $sbAst.Extent.StartOffset
                                    End = $sbAst.Extent.EndOffset
                                    Value = $scriptText
                                })
                        }
                    }

                    # Inject SessionProperties into the $adtSession hashtable if provided.
                    if ($hasSessionProperties)
                    {

                        # Find the $adtSession assignment statement.
                        $assignmentAst = $scriptAst.Find({
                                param ($ast)
                                $ast -is [System.Management.Automation.Language.AssignmentStatementAst] -and
                                ($ast.Left | Get-Member -Name VariablePath) -and
                                $ast.Left.VariablePath.UserPath -eq 'adtSession'
                            }, $true)

                        if ($assignmentAst)
                        {
                            $hashtableAst = $assignmentAst.Right.Expression

                            # Build a lookup of existing keys to their value's absolute offsets in $scriptContent.
                            $existingKeys = @{}
                            foreach ($kvp in $hashtableAst.KeyValuePairs)
                            {
                                $existingKeys[$kvp.Item1.Value] = [PSCustomObject]@{
                                    Start = $kvp.Item2.Extent.StartOffset
                                    End = $kvp.Item2.Extent.EndOffset
                                }
                            }

                            # Collect replacements (absolute offsets) for existing keys, and new entries for missing keys.
                            $newEntries = [System.Collections.Generic.List[System.String]]::new()

                            foreach ($key in $SessionProperties.Keys)
                            {
                                $serializedValue = ConvertTo-ADTExpression -InputObject $SessionProperties[$key]
                                if ($existingKeys.ContainsKey($key))
                                {
                                    $scriptReplacements.Add([PSCustomObject]@{
                                            Start = $existingKeys[$key].Start
                                            End = $existingKeys[$key].End
                                            Value = $serializedValue
                                        })
                                }
                                else
                                {
                                    $newEntries.Add("    $key = $serializedValue")
                                }
                            }

                            # Insert new entries before the closing brace of the hashtable.
                            if ($newEntries.Count -gt 0)
                            {
                                $closingBraceOffset = $scriptContent.LastIndexOf('}', $hashtableAst.Extent.EndOffset - 1)
                                $insertion = [System.Environment]::NewLine + ($newEntries -join [System.Environment]::NewLine) + [System.Environment]::NewLine
                                $scriptReplacements.Add([PSCustomObject]@{
                                        Start = $closingBraceOffset
                                        End = $closingBraceOffset
                                        Value = $insertion
                                    })
                            }
                        }
                    }
                    if ($scriptReplacements)
                    {
                        $scriptContent = Set-ADTTextReplacements -InputText $scriptContent -Replacements $scriptReplacements
                    }
                    Out-File -InputObject $scriptContent @params -Width ([System.Int16]::MaxValue) -Force
                }
                else
                {
                    # Copy over Deploy-Application.exe from the v4 template.
                    Copy-Item -LiteralPath $Script:PSScriptRoot\opt\Frontend\v4\Invoke-AppDeployToolkit.exe -Destination "$templatePath\Deploy-Application.exe"
                }

                # Display the newly created folder in Windows Explorer.
                if ($Show)
                {
                    & (Join-Path -Path ([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows)) -ChildPath explorer.exe) $templatePath
                }

                # Return a DirectoryInfo object if passing through.
                if ($PassThru)
                {
                    return (Get-Item -LiteralPath $templatePath)
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

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
