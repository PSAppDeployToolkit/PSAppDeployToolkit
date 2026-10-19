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

    .PARAMETER LauncherName
        The base name to use for the generated launcher files, without a file extension.

    .PARAMETER ExcludeContent
        Specifies one or more optional content categories to omit from the generated template. Valid values are Assets, Config, Extensions, Files, Module, Strings and SupportFiles. All content is included by default. You cannot exclude Files, SupportFiles, Config or Strings if specifying those items as separate parameters. You can exclude Assets while specifying -Assets to omit the default assets. Excluding Module omits the bundled PSAppDeployToolkit module for deployments that provide it externally.

    .PARAMETER SessionProperties
        A dictionary of key-value pairs to inject into the $adtSession hashtable of the generated Invoke-AppDeployToolkit.ps1. Accepts [hashtable], [ordered], or any [System.Collections.IDictionary] type. Only supported when -Version is 4.

    .PARAMETER Config
        A dictionary of key-value pairs to override or add to the generated Config\config.psd1. The dictionary structure must mirror the config file's nested hashtable layout (e.g. @\{ MSI = @\{ InstallParams = 'REBOOT=ReallySuppress /QB-!' \} \}). Existing keys are overridden in place. New keys or sections that do not exist in the default config are appended at the end of the relevant hashtable level.

    .PARAMETER Strings
        A dictionary of language codes to key-value pairs to override or add to the generated strings.psd1 files. Each top-level key must be a language code (e.g. en, fr, pt-BR), where en targets Strings\strings.psd1 and every other language targets Strings\<language>\strings.psd1. A language that the module does not ship is created from a copy of the English string table with the supplied values applied on top. The value for each language must mirror the string table's nested hashtable layout (e.g. @\{ en = @\{ BalloonTip = @\{ Start = @\{ Install = 'Install has started.' \} \} \} \}). Language codes and keys are matched case-insensitively; specifying the same language or key more than once with differing case is an error. Existing keys are overridden in place. New keys that do not exist in the default string table are appended at the end of the relevant hashtable level.

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
        An array of file or folder paths to copy into the Assets folder of the generated template. Paths are passed to Copy-Item with -Recurse. Custom assets are copied even when Assets is specified in -ExcludeContent, allowing the default assets to be replaced entirely.

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
        New-ADTTemplate -Destination 'C:\Temp' -LauncherName 'Deploy-ContosoApp'

        Creates a new v4 template whose launcher files are named Deploy-ContosoApp.exe and Deploy-ContosoApp.ps1.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -ExcludeContent Extensions, Strings, SupportFiles

        Creates a new v4 template without the PSAppDeployToolkit.Extensions, Strings, or SupportFiles folders.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -SessionProperties @{ AppVendor = 'Contoso'; AppName = 'MyApp'; AppVersion = '6.7'; RequireAdmin = $false; AppProcessesToClose = @('notepad', @{ Name = 'calc'; Description = 'Calculator' }) }

        Creates a new v4 template with the specified session properties pre-populated in the $adtSession hashtable.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -Config @{ Toolkit = @{ LogPath = '$env:ProgramData\Microsoft\IntuneManagementExtension\Logs' } }

        Creates a new v4 template with a custom log folder.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -Assets 'C:\Assets\AppIconLight.png', 'C:\Assets\AppIconDark.png' -Config @{ Assets = @{ Logo = '..\Assets\AppIconLight.png'; LogoDark = '..\Assets\AppIconDark.png' } }

        Creates a new v4 template with custom icons copied to Assets, with config.psd1 updated and configured to use them.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -Strings @{ en = @{ BalloonTip = @{ Start = @{ Install = 'Contoso App installation started.' } } }; fr = @{ BalloonTip = @{ Start = @{ Install = 'Contoso App : installation en cours.' } } } }

        Creates a new v4 template with the English and French balloon tip text for the start of an installation overridden.

    .EXAMPLE
        New-ADTTemplate -Destination 'C:\Temp' -SessionProperties @{ AppVendor = 'Contoso'; AppName = 'MyApp'; AppVersion = '6.7' } -Files 'C:\Installers\Setup.msi' -InstallScriptBlock { Start-ADTMsiProcess -Action Install -FilePath 'Setup.msi' } -UninstallScriptBlock { Start-ADTMsiProcess -Action Uninstall -FilePath 'Setup.msi' }

        Creates a new v4 template with session properties defined, an MSI copied to Files and install/uninstall commands added.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/New-ADTTemplate

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/modules/PSAppDeployToolkit/Public/New-ADTTemplate.ps1
    #>

    [CmdletBinding(SupportsShouldProcess = $false)]
    [OutputType([System.IO.DirectoryInfo])]
    param
    (
        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Destination = $ExecutionContext.SessionState.Path.CurrentLocation.Path,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ($_.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Name -ProvidedValue $_ -ExceptionMessage 'The specified template folder name contains invalid file name characters.'))
                }
                if ($_ -in '.', '..')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Name -ProvidedValue $_ -ExceptionMessage 'The specified template folder name is a relative directory alias.'))
                }
                return $true
            })]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [PSDefaultValue(Help = "PSAppDeployToolkit_<ModuleVersion>")]
        [System.String]$Name = "$($MyInvocation.MyCommand.Module.Name)_$($MyInvocation.MyCommand.Module.Version)",

        [Parameter(Mandatory = $false)]
        [ValidateRange(3, 4)]
        [System.Int32]$Version = 4,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ($_.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LauncherName -ProvidedValue $_ -ExceptionMessage 'The specified launcher base name contains invalid file name characters.'))
                }
                if ($_ -match '\.(?:exe|ps1|pdb)$')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LauncherName -ProvidedValue $_ -ExceptionMessage "The specified launcher base name should not contain a file extension."))
                }
                return $true
            })]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$LauncherName,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Assets', 'Config', 'Extensions', 'Files', 'Module', 'Strings', 'SupportFiles')]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String[]]$ExcludeContent = @(),

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Collections.IDictionary]$SessionProperties,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Collections.IDictionary]$Config,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Collections.IDictionary]$Strings,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$PreInstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$InstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$PostInstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$PreUninstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$UninstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$PostUninstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$PreRepairScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$RepairScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
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

        # Reject content inputs whose destination category has been excluded. Assets is allowed so custom assets can be provided while excluding the defaults.
        if (($invalidParams = @('Config', 'Files', 'Strings', 'SupportFiles').Where({ ($_ -in $ExcludeContent) -and $PSBoundParameters.ContainsKey($_) })))
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The following parameters cannot be supplied when their content is excluded: $($invalidParams -join ', ').")
                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                ErrorId = 'InvalidParameter'
                TargetObject = $invalidParams
                RecommendedAction = "Please remove the -$($invalidParams -join ', -') parameter(s), or remove the corresponding value(s) from -ExcludeContent."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }

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
                        # Escaped in a statement of its own. Nesting this inside the double quoted string
                        # it builds meant the backticks were consumed as that string's own escapes, so the
                        # quotes went out bare and closed the value early.
                        $escaped = [System.Text.RegularExpressions.Regex]::Replace($str, '(?<!`)"', '`"')
                        return '"' + $escaped + '"'
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
                [ValidateNotNullOrEmpty()]
                [System.Management.Automation.ScriptBlock[]]$ScriptBlock
            )

            $scriptBodies = foreach ($sb in $ScriptBlock)
            {
                if ([System.String]::IsNullOrWhiteSpace($sb.ToString()))
                {
                    continue
                }

                # Trim leading empty lines (but not whitespace to preserve indentation) and trailing whitespace, replace tabs with spaces, then split into lines to analyze indentation.
                $lines = ([regex]::Replace($sb.ToString().TrimStart("`r", "`n").TrimEnd(), '(?m)^\t+', { param($m) '    ' * $m.Value.Length })).Split([System.String[]]("`r`n", "`n"), [System.StringSplitOptions]::None)
                $minIndent = ($lines | Where-Object { $_ -match '\S' } | ForEach-Object { [regex]::Match($_, '^ *').Length } | Measure-Object -Minimum).Minimum
                ($lines | ForEach-Object { if ([System.String]::IsNullOrWhiteSpace($_)) { $_ } elseif ($_.Length -ge $minIndent) { '    ' + $_.Substring($minIndent) } else { '    ' + $_ } }) -join [System.Environment]::NewLine
            }
            return ($scriptBodies -join ([System.Environment]::NewLine * 2))
        }

        # Helper to return a list of text replacements for a config.psd1 or strings.psd1 text content based on a dictionary of settings.
        function Get-ADTDataFileReplacement
        {
            [CmdletBinding()]
            [OutputType([PSCustomObject[]])]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateSet('Config', 'Strings')]
                [System.String]$DataName,

                [Parameter(Mandatory = $true)]
                [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
                [System.String]$DataFileText,

                [Parameter(Mandatory = $true)]
                [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
                [System.Collections.IDictionary]$Settings,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.String]$KeyPath
            )

            $dataFileHashtableAst = [System.Management.Automation.Language.Parser]::ParseInput($DataFileText, [ref]$null, [ref]$null).EndBlock.Statements[0].PipelineElements[0].Expression
            $dataFileInsertions = @{}
            $dataLabel = $DataName.ToLowerInvariant()
            $keyTypeMismatchErrorId = "$($DataName)KeyTypeMismatch"

            # Recursive scriptblock to walk user dictionary against data file AST.
            $collectReplacements = {
                param ([System.Collections.IDictionary]$SettingsDict, $HashtableAst, [System.String]$Path)
                foreach ($kvp in $SettingsDict.GetEnumerator())
                {
                    $currentPath = if ($Path) { "$Path.$($kvp.Key)" } else { $kvp.Key }
                    $matchingKvp = $HashtableAst.KeyValuePairs.Where({ $_.Item1.Value -eq $kvp.Key })
                    if ($matchingKvp.Count -eq 0)
                    {
                        # Key does not exist in the default data file; accumulate it for insertion.
                        $serializedValue = ConvertTo-ADTExpression -InputObject $kvp.Value -LiteralString
                        $closingBraceOffset = $DataFileText.LastIndexOf('}', $HashtableAst.Extent.EndOffset - 1)
                        $braceLineStart = $DataFileText.LastIndexOf("`n", $closingBraceOffset - 1) + 1
                        $indent = if ($HashtableAst.KeyValuePairs.Count -gt 0)
                        {
                            ' ' * ($HashtableAst.KeyValuePairs[0].Item1.Extent.StartColumnNumber - 1)
                        }
                        else
                        {
                            '    '
                        }
                        if (!$dataFileInsertions.ContainsKey($braceLineStart))
                        {
                            $dataFileInsertions[$braceLineStart] = [System.Collections.Generic.List[System.String]]::new()
                        }
                        $dataFileInsertions[$braceLineStart].Add("${indent}'$([System.Management.Automation.Language.CodeGeneration]::EscapeSingleQuotedStringContent($kvp.Key))' = $serializedValue")
                        continue
                    }
                    $astValue = $matchingKvp[0].Item2.PipelineElements[0].Expression
                    if ($kvp.Value -is [System.Collections.IDictionary])
                    {
                        if ($astValue -isnot [System.Management.Automation.Language.HashtableAst])
                        {
                            $naerParams = @{
                                Exception = [System.ArgumentException]::new("The $dataLabel key '$currentPath' is not a hashtable in the default $dataLabel.psd1 file but a hashtable value was provided.")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                                ErrorId = $keyTypeMismatchErrorId
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
            & $collectReplacements -SettingsDict $Settings -HashtableAst $dataFileHashtableAst -Path $KeyPath

            # Convert accumulated new-key insertions into replacement objects.
            foreach ($offset in $dataFileInsertions.Keys)
            {
                [PSCustomObject]@{
                    Start = $offset
                    End = $offset
                    Value = (($dataFileInsertions[$offset] -join [System.Environment]::NewLine) + [System.Environment]::NewLine)
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
                            $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName 'Replacements' -ProvidedValue $_ -ExceptionMessage 'The specified replacement does not have the required Start/End/Value properties.'))
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

        # Keys are matched case-insensitively, so reject dictionaries (e.g. generic dictionaries) that supply the same key with differing case.
        $assertUniqueKeys = {
            param ([System.Collections.IDictionary]$Dictionary, [System.String]$DataName, [System.String]$Path)
            $seenKeys = [System.Collections.Generic.HashSet[System.String]]::new([System.StringComparer]::OrdinalIgnoreCase)
            foreach ($kvp in $Dictionary.GetEnumerator())
            {
                $key = $kvp.Key.ToString()
                $currentPath = if ($Path) { "$Path.$key" } else { $key }
                if (!$seenKeys.Add($key))
                {
                    $naerParams = @{
                        Exception = [System.ArgumentException]::new("The $($DataName.ToLowerInvariant()) key '$currentPath' is specified more than once with differing case.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                        ErrorId = "$($DataName)KeyDuplicate"
                        TargetObject = $currentPath
                        RecommendedAction = 'Please specify each key only once (keys are not case-sensitive) and try again.'
                    }
                    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                }
                if ($kvp.Value -is [System.Collections.IDictionary])
                {
                    & $assertUniqueKeys -Dictionary $kvp.Value -DataName $DataName -Path $currentPath
                }
            }
        }
        if ($PSBoundParameters.ContainsKey('Config'))
        {
            & $assertUniqueKeys -Dictionary $Config -DataName Config -Path $null
        }

        # Resolve each -Strings language to its default string table key. English is the root string table, keyed by an empty string.
        $stringsByLanguage = [System.Collections.Generic.Dictionary[System.String, System.Collections.IDictionary]]::new([System.StringComparer]::OrdinalIgnoreCase)
        if ($PSBoundParameters.ContainsKey('Strings'))
        {
            & $assertUniqueKeys -Dictionary $Strings -DataName Strings -Path $null
            $languageLookup = [System.Collections.Generic.Dictionary[System.String, System.String]]::new([System.StringComparer]::OrdinalIgnoreCase)
            foreach ($shippedLanguage in (Get-ADTModuleDefaults).Strings.Keys)
            {
                $languageLookup.Add($(if ($shippedLanguage) { $shippedLanguage } else { 'en' }), $shippedLanguage)
            }
            foreach ($language in $Strings.GetEnumerator())
            {
                $languageName = $language.Key.ToString(); $languageKey = $null
                if (!$languageLookup.TryGetValue($languageName, [ref]$languageKey))
                {
                    # New languages become folder names, so restrict them to language tag characters.
                    if ($languageName -notmatch '\A[A-Za-z]{2,8}(?:-[A-Za-z0-9]{1,8})*\z')
                    {
                        $naerParams = @{
                            Exception = [System.ArgumentException]::new("The language [$languageName] specified in -Strings is not a valid language code.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                            ErrorId = 'StringsLanguageInvalid'
                            TargetObject = $languageName
                            RecommendedAction = 'Please specify a language code such as en, fr or pt-BR and try again.'
                        }
                        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                    }
                    $languageKey = $languageName
                }
                if (($language.Value -isnot [System.Collections.IDictionary]) -or ($language.Value.Count -eq 0))
                {
                    $naerParams = @{
                        Exception = [System.ArgumentException]::new("The value for language [$languageName] specified in -Strings is not a non-empty dictionary.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                        ErrorId = 'StringsLanguageValueInvalid'
                        TargetObject = $language.Value
                        RecommendedAction = 'Please provide a hashtable of string table overrides for this language and try again.'
                    }
                    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                }
                $stringsByLanguage.Add($languageKey, $language.Value)
            }
        }

        # Handle -ZeroConfig: inject the default MSI scriptblock into Install/Uninstall/Repair phases.
        if ($ZeroConfig)
        {
            # Define as a string initially so that this block does not get translated into CommandTable calls by the build system
            $zeroConfigScriptBlock = @'
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
'@
            $zeroConfigScriptBlock = [System.Management.Automation.ScriptBlock]::Create($zeroConfigScriptBlock)
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
                if ((Test-ADTModuleCompiled) -and (Test-ADTModuleSigned) -and ($badFiles = Get-ChildItem -LiteralPath (Get-ADTModuleDirectory) -Filter *.ps*1 -Recurse | Get-AuthenticodeSignature | & { process { if (!$_.Status.Equals([System.Management.Automation.SignatureStatus]::Valid)) { return $_ } } }))
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
                            Exception = [System.IO.IOException]::new("Folder [$templatePath] already exists and is not empty.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                            ErrorId = 'NonEmptySubfolderError'
                            TargetObject = $templatePath
                            RecommendedAction = "Please remove the existing folder, supply a new name, or add the -Force parameter and try again."
                        }
                        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                    }
                    $null = Remove-Item -LiteralPath $templatePath -Recurse -Force
                }
                $null = New-Item -Path $templatePath -ItemType Directory -Force
                foreach ($folder in 'Files', 'SupportFiles')
                {
                    if ($folder -notin $ExcludeContent)
                    {
                        $null = New-Item -Path (Join-Path -Path $templatePath -ChildPath $folder) -ItemType Directory -Force
                    }
                }

                # Copy in the frontend files.
                Get-ChildItem -LiteralPath "$(Get-ADTModuleDirectory)\opt\Frontend\v$Version" -Force | & { process { if (!$_.Name.Equals('PSAppDeployToolkit.Extensions') -or ('Extensions' -notin $ExcludeContent)) { Copy-Item -LiteralPath $_.FullName -Destination $templatePath -Recurse -Force } } }

                if (('Assets' -notin $ExcludeContent) -or ('Config' -notin $ExcludeContent))
                {
                    $defaultAssets = (Get-ADTModuleDefaults).Config.([System.String]::Empty).Ast.EndBlock.Statements.PipelineElements.Expression.KeyValuePairs.Where({ $_.Item1.Value.Equals('Assets') }).Item2.PipelineElements.Expression.KeyValuePairs
                    $banner = $defaultAssets.Where({ $_.Item1.Value.Equals('Banner') }).Item2.PipelineElements.Expression.Value
                    $logo = $defaultAssets.Where({ $_.Item1.Value.Equals('Logo') }).Item2.PipelineElements.Expression.Value
                }

                # Export default module assets to disk.
                if ('Assets' -notin $ExcludeContent -or $PSBoundParameters.ContainsKey('Assets'))
                {
                    $null = New-Item -Path "$templatePath\Assets" -ItemType Directory -Force
                }
                if ('Assets' -notin $ExcludeContent)
                {
                    [System.IO.File]::WriteAllBytes("$templatePath\Assets\Banner.Classic.png", [System.Convert]::FromBase64String($banner))
                    [System.IO.File]::WriteAllBytes("$templatePath\Assets\AppIcon.png", [System.Convert]::FromBase64String($logo))
                }

                # Export the configuration data from the module to disk.
                if ('Config' -notin $ExcludeContent)
                {
                    $configText = (Get-ADTModuleDefaults).Config.([System.String]::Empty).ToString().Replace($banner, '..\Assets\Banner.Classic.png').Replace($logo, '..\Assets\AppIcon.png')
                    if ($PSBoundParameters.ContainsKey('Config'))
                    {
                        $configReplacements = Get-ADTDataFileReplacement -DataName Config -DataFileText $configText -Settings $Config
                        $configText = Set-ADTTextReplacements -InputText $configText -Replacements $configReplacements
                    }
                    $null = New-Item -Path "$templatePath\Config" -ItemType Directory -Force
                    Export-ADTScriptBlockToFile -ScriptBlock ([System.Management.Automation.ScriptBlock]::Create($configText)) -LiteralPath "$templatePath\Config\config.psd1"
                }

                # Export the string data from the module to disk.
                if ('Strings' -notin $ExcludeContent)
                {
                    $defaultStrings = (Get-ADTModuleDefaults).Strings
                    $languages = @($defaultStrings.Keys) + @($stringsByLanguage.Keys.Where({ !$defaultStrings.ContainsKey($_) }))
                    foreach ($language in $languages)
                    {
                        # New languages start from the English string table.
                        $stringsScriptBlock = if ($defaultStrings.ContainsKey($language)) { $defaultStrings[$language] } else { $defaultStrings[[System.String]::Empty] }
                        if ($stringsByLanguage.ContainsKey($language))
                        {
                            $stringsText = $stringsScriptBlock.ToString()
                            if (($stringsReplacements = Get-ADTDataFileReplacement -DataName Strings -DataFileText $stringsText -Settings $stringsByLanguage[$language] -KeyPath $(if ($language) { $language } else { 'en' })))
                            {
                                $stringsScriptBlock = [System.Management.Automation.ScriptBlock]::Create((Set-ADTTextReplacements -InputText $stringsText -Replacements $stringsReplacements))
                            }
                        }
                        $stringsPath = [System.IO.Path]::Combine("$templatePath\Strings", $language)
                        $null = New-Item -Path $stringsPath -ItemType Directory -Force
                        Export-ADTScriptBlockToFile -ScriptBlock $stringsScriptBlock -LiteralPath "$stringsPath\strings.psd1"
                    }
                }

                # Ensure all editable ps*1 files are not read-only and remove any digital signatures.
                Get-ChildItem -LiteralPath $templatePath -File -Filter *.ps*1 -Recurse | & {
                    process
                    {
                        $attributes = [System.IO.File]::GetAttributes($_.FullName)
                        if (($attributes -band [System.IO.FileAttributes]::ReadOnly) -ne 0)
                        {
                            [System.IO.File]::SetAttributes($_.FullName, ($attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly)))
                        }

                        if (($sigLine = $(($fileLines = [System.IO.File]::ReadAllLines($_.FullName)) -match '^# SIG # Begin signature block$')))
                        {
                            [System.IO.File]::WriteAllLines($_.FullName, $fileLines[0..($fileLines.IndexOf($sigLine) - 2)])
                        }
                    }
                }

                if ('Module' -notin $ExcludeContent)
                {
                    # Copy in the module files.
                    $null = New-Item -Path $templateModulePath -ItemType Directory -Force
                    Copy-Item -Path "$([System.Management.Automation.WildcardPattern]::Escape((Get-ADTModuleDirectory)))\*" -Destination $templateModulePath -Recurse -Force

                    # Make the shipped module and its files read-only.
                    $(Get-Item -LiteralPath $templateModulePath; Get-ChildItem -LiteralPath $templateModulePath -Recurse) | & {
                        process
                        {
                            $attributes = [System.IO.File]::GetAttributes($_.FullName)
                            if (($attributes -band [System.IO.FileAttributes]::ReadOnly) -eq 0)
                            {
                                [System.IO.File]::SetAttributes($_.FullName, ($attributes -bor [System.IO.FileAttributes]::ReadOnly))
                            }
                        }
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
                    $scriptPath = "$templatePath\Invoke-AppDeployToolkit.ps1"
                    $scriptEncoding = [System.Text.UTF8Encoding]::new($true, $true)
                    $scriptContent = ([System.IO.File]::ReadAllText($scriptPath, $scriptEncoding) -replace '\r?\n', [System.Environment]::NewLine).Replace('..\..\..\..\', [System.Management.Automation.Language.NullString]::Value).Replace('2000-12-31', [System.DateTime]::Now.ToString('yyyy-MM-dd'))

                    # Collect all script content replacements (absolute offsets) across all features,
                    # then apply them in a single pass from end to start to preserve earlier offsets.
                    $scriptReplacements = [System.Collections.Generic.List[PSCustomObject]]::new()
                    $hasSessionProperties = $PSBoundParameters.ContainsKey('SessionProperties') -and $SessionProperties.Count -gt 0
                    $sectionsToProcess = @('Post-Repair', 'Repair', 'Pre-Repair', 'Post-Uninstall', 'Uninstall', 'Pre-Uninstall', 'Post-Install', 'Install', 'Pre-Install' ).Where({ $PSBoundParameters.ContainsKey($_.Replace('-', [System.Management.Automation.Language.NullString]::Value) + 'ScriptBlock') })
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
                            # Find the variable assignment in the AST (e.g. $PreInstall = { ... }).
                            $sbAst = $scriptAst.Find({
                                    param ($ast)
                                    $ast -is [System.Management.Automation.Language.ScriptBlockExpressionAst] -and
                                    $ast.Parent -is [System.Management.Automation.Language.CommandAst] -and
                                    $ast.Parent.CommandElements[0] -is [System.Management.Automation.Language.StringConstantExpressionAst] -and
                                    $ast.Parent.CommandElements[0].Value.Equals('New-Variable') -and
                                    ((($nameParam = $ast.Parent.CommandElements | & { process { if (($_ -is [System.Management.Automation.Language.CommandParameterAst]) -and ($_.ParameterName.Equals('Name'))) { return $_ } } }) | Measure-Object).Count -eq 1) -and
                                    $ast.Parent.CommandElements[$ast.Parent.CommandElements.IndexOf($nameParam) + 1].Value.Equals($section)
                                }, $true)

                            # Get the scriptblock expression on the right-hand side.
                            $sbParamName = $section.Replace('-', [System.Management.Automation.Language.NullString]::Value) + 'ScriptBlock'
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

                            # Replace the entire scriptblock expression (including braces) with the user's content.
                            $scriptBody = ConvertTo-ADTScriptBody -ScriptBlock $PSBoundParameters[$sbParamName]
                            $scriptReplacements.Add([PSCustomObject]@{
                                    Start = $sbAst.Extent.StartOffset
                                    End = $sbAst.Extent.EndOffset
                                    Value = '{' + [System.Environment]::NewLine + $(if ($scriptBody) { $scriptBody + [System.Environment]::NewLine }) + '}'
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
                    [System.IO.File]::WriteAllText($scriptPath, $scriptContent, $scriptEncoding)

                    # Rename all v4 launcher artifacts to the requested base name.
                    if ($PSBoundParameters.ContainsKey('LauncherName') -and $LauncherName -ne 'Invoke-AppDeployToolkit')
                    {
                        foreach ($extension in 'exe', 'ps1')
                        {
                            Move-Item -LiteralPath "$templatePath\Invoke-AppDeployToolkit.$extension" -Destination "$templatePath\$LauncherName.$extension"
                        }
                        if (Test-Path -LiteralPath "$templatePath\Invoke-AppDeployToolkit.pdb" -PathType Leaf)
                        {
                            Move-Item -LiteralPath "$templatePath\Invoke-AppDeployToolkit.pdb" -Destination "$templatePath\$LauncherName.pdb"
                        }
                    }
                }
                else
                {
                    # Copy over Deploy-Application.exe from the v4 template.
                    $v3LauncherName = if ($PSBoundParameters.ContainsKey('LauncherName')) { $LauncherName } else { 'Deploy-Application' }
                    Copy-Item -LiteralPath "$(Get-ADTModuleDirectory)\opt\Frontend\v4\Invoke-AppDeployToolkit.exe" -Destination "$templatePath\$v3LauncherName.exe"
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
