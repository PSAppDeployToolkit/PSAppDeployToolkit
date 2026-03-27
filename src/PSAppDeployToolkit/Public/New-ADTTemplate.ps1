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

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

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
        [System.Collections.IDictionary]$SessionProperties,

        [Parameter(Mandatory = $false)]
        [System.Collections.IDictionary]$Config,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.ScriptBlock]$PreInstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.ScriptBlock]$InstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.ScriptBlock]$PostInstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.ScriptBlock]$PreUninstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.ScriptBlock]$UninstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.ScriptBlock]$PostUninstallScriptBlock,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.ScriptBlock]$PreRepairScriptBlock,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.ScriptBlock]$RepairScriptBlock,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.ScriptBlock]$PostRepairScriptBlock,

        [Parameter(Mandatory = $false)]
        [SupportsWildcards()]
        [System.String[]]$Assets,

        [Parameter(Mandatory = $false)]
        [SupportsWildcards()]
        [System.String[]]$Files,

        [Parameter(Mandatory = $false)]
        [SupportsWildcards()]
        [System.String[]]$SupportFiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Show,

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
            if ($InputObject -is [System.Boolean] -or $InputObject -is [System.Management.Automation.SwitchParameter])
            {
                if ($InputObject) { return '$true' } else { return '$false' }
            }
            if ($InputObject -is [System.DateTime])
            {
                return "'$($InputObject.ToString('yyyy-MM-dd'))'"
            }
            if ($InputObject -is [System.TimeSpan])
            {
                return $InputObject.TotalSeconds.ToString([System.Globalization.CultureInfo]::InvariantCulture)
            }
            if (($InputObject -is [System.String]) -or ($InputObject -is [System.Char]) -or ($InputObject -is [System.Version]) -or ($InputObject -is [System.Guid]) -or ($InputObject -is [System.IO.FileSystemInfo]) -or ($InputObject.GetType().IsEnum))
            {
                $str = $InputObject.ToString()
                if (!$LiteralString -and ($str -match '(?<!`)\$'))
                {
                    return "`"$($str -replace '(?<!`)"', '`"')`""
                }
                return "'$($str.Replace("'", "''"))'"
            }
            if ($InputObject -is [System.ValueType])
            {
                return $InputObject.ToString([System.Globalization.CultureInfo]::InvariantCulture)
            }
            if ($InputObject -is [System.Collections.IDictionary])
            {
                $pairs = foreach ($entry in $InputObject.GetEnumerator())
                {
                    $entryKey = $entry.Key.ToString().Replace("'", "''")
                    "'$entryKey' = $(ConvertTo-ADTExpression -InputObject $entry.Value -LiteralString:$LiteralString)"
                }
                $dictionaryPrefix = if ($InputObject -is [System.Collections.Specialized.OrderedDictionary]) { '[ordered]@{' } else { '@{' }
                return "$dictionaryPrefix $($pairs -join '; ') }"
            }
            if (($InputObject -is [System.Collections.IEnumerable]) -and !($InputObject -is [System.String]))
            {
                $items = foreach ($item in $InputObject)
                {
                    ConvertTo-ADTExpression -InputObject $item -LiteralString:$LiteralString
                }
                return "@($($items -join ', '))"
            }

            $naerParams = @{
                Exception = [System.ArgumentException]::new("Session property value of type [$($InputObject.GetType().FullName)] is not supported.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                ErrorId = 'UnsupportedSessionPropertyValueType'
                TargetObject = $InputObject
                RecommendedAction = 'Use any System.ValueType, strings, datetimes, timespans, arrays, hashtables, or ordered dictionaries.'
            }
            throw (New-ADTErrorRecord @naerParams)
        }

        # Some parameters are only supported for v4 templates.
        if ($Version.Equals(3))
        {
            if (($invalidParams = @('SessionProperties', 'PreInstallScriptBlock', 'InstallScriptBlock', 'PostInstallScriptBlock', 'PreUninstallScriptBlock', 'UninstallScriptBlock', 'PostUninstallScriptBlock', 'PreRepairScriptBlock', 'RepairScriptBlock', 'PostRepairScriptBlock').Where({ $PSBoundParameters.ContainsKey($_) })))
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
                    throw (New-ADTErrorRecord @naerParams)
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
                        throw (New-ADTErrorRecord @naerParams)
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
                $configBlock = [System.Management.Automation.ScriptBlock]::Create($ADT.ModuleDefaults.Config.([System.String]::Empty).ToString().Replace($banner, '..\Assets\Banner.Classic.png').Replace($logo, '..\Assets\AppIcon.ico'))

                # Override config values if specified.
                if ($PSBoundParameters.ContainsKey('Config') -and $Config.Count -gt 0)
                {
                    $configText = $configBlock.ToString()
                    $configAst = [System.Management.Automation.Language.Parser]::ParseInput($configText, [ref]$null, [ref]$null)
                    $configHashtableAst = $configAst.EndBlock.Statements[0].PipelineElements[0].Expression
                    $configReplacements = [System.Collections.Generic.List[PSCustomObject]]::new()
                    $configInsertions = @{}

                    # Recursive scriptblock to walk user dictionary against config AST.
                    $collectReplacements = {
                        param ([System.Collections.IDictionary]$UserDict, $HashtableAst, [System.String]$Path)
                        foreach ($key in $UserDict.Keys)
                        {
                            $currentPath = if ($Path) { "$Path.$key" } else { $key }
                            $matchingKvp = $HashtableAst.KeyValuePairs.Where({ $_.Item1.Value -eq $key })
                            if ($matchingKvp.Count -eq 0)
                            {
                                # Key does not exist in the default config; accumulate it for insertion.
                                $serializedValue = ConvertTo-ADTExpression -InputObject $UserDict[$key] -LiteralString
                                $closingBraceOffset = $configText.LastIndexOf('}', $HashtableAst.Extent.EndOffset - 1)
                                $braceLineStart = $configText.LastIndexOf("`n", $closingBraceOffset - 1) + 1
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
                                $configInsertions[$braceLineStart].Add("${indent}$($key.Replace("'", "''")) = $serializedValue")
                                continue
                            }
                            $astValue = $matchingKvp[0].Item2.PipelineElements[0].Expression
                            if ($UserDict[$key] -is [System.Collections.IDictionary])
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
                                    throw (New-ADTErrorRecord @naerParams)
                                }
                                & $collectReplacements -UserDict $UserDict[$key] -HashtableAst $astValue -Path $currentPath
                            }
                            else
                            {
                                $configReplacements.Add([PSCustomObject]@{
                                        Start = $matchingKvp[0].Item2.Extent.StartOffset
                                        End = $matchingKvp[0].Item2.Extent.EndOffset
                                        Value = (ConvertTo-ADTExpression -InputObject $UserDict[$key] -LiteralString)
                                    })
                            }
                        }
                    }
                    & $collectReplacements -UserDict $Config -HashtableAst $configHashtableAst -Path $null

                    # Convert accumulated new-key insertions into replacement objects.
                    foreach ($offset in $configInsertions.Keys)
                    {
                        $configReplacements.Add([PSCustomObject]@{
                                Start = $offset
                                End = $offset
                                Value = (($configInsertions[$offset] -join "`n") + "`n")
                            })
                    }

                    # Apply all replacements from end to start to preserve earlier offsets.
                    foreach ($replacement in ($configReplacements | Sort-Object -Property Start -Descending))
                    {
                        $configText = $configText.Substring(0, $replacement.Start) + $replacement.Value + $configText.Substring($replacement.End)
                    }
                    $configBlock = [System.Management.Automation.ScriptBlock]::Create($configText)
                }

                # Export the string data from the module to disk.
                $null = New-Item -Path "$templatePath\Config" -ItemType Directory -Force
                Export-ADTScriptBlockToFile -ScriptBlock $configBlock -LiteralPath "$templatePath\Config\config.psd1"

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
                    $scriptContent = (Get-Content @params -Raw).Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n").Replace('..\..\..\..\', [System.Management.Automation.Language.NullString]::Value).Replace('2000-12-31', [System.DateTime]::Now.ToString('yyyy-MM-dd'))

                    # Collect all script content replacements (absolute offsets) across both features,
                    # then apply them in a single pass from end to start to preserve earlier offsets.
                    $scriptReplacements = [System.Collections.Generic.List[PSCustomObject]]::new()
                    $hasSessionProperties = $PSBoundParameters.ContainsKey('SessionProperties') -and $SessionProperties.Count -gt 0
                    $deploymentScriptMap = @{
                        PreInstallScriptBlock = @{ Function = 'Install-ADTDeployment'; PhaseIndex = 0 }
                        InstallScriptBlock = @{ Function = 'Install-ADTDeployment'; PhaseIndex = 1 }
                        PostInstallScriptBlock = @{ Function = 'Install-ADTDeployment'; PhaseIndex = 2 }
                        PreUninstallScriptBlock = @{ Function = 'Uninstall-ADTDeployment'; PhaseIndex = 0 }
                        UninstallScriptBlock = @{ Function = 'Uninstall-ADTDeployment'; PhaseIndex = 1 }
                        PostUninstallScriptBlock = @{ Function = 'Uninstall-ADTDeployment'; PhaseIndex = 2 }
                        PreRepairScriptBlock = @{ Function = 'Repair-ADTDeployment'; PhaseIndex = 0 }
                        RepairScriptBlock = @{ Function = 'Repair-ADTDeployment'; PhaseIndex = 1 }
                        PostRepairScriptBlock = @{ Function = 'Repair-ADTDeployment'; PhaseIndex = 2 }
                    }
                    $boundDeployScripts = $deploymentScriptMap.Keys.Where({ $PSBoundParameters.ContainsKey($_) })

                    if ($hasSessionProperties -or $boundDeployScripts.Count -gt 0)
                    {
                        $scriptAst = [System.Management.Automation.Language.Parser]::ParseInput($scriptContent, [ref]$null, [ref]$null)
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

                    # Inject deployment script blocks into their corresponding phases if provided.
                    if ($boundDeployScripts.Count -gt 0)
                    {
                        foreach ($paramName in $boundDeployScripts)
                        {
                            $mapping = $deploymentScriptMap[$paramName]

                            # Find the target function in the AST.
                            $funcAst = $scriptAst.Find({
                                    param ($ast)
                                    $ast -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
                                    $ast.Name -eq $mapping.Function
                                }, $true)
                            if (!$funcAst)
                            {
                                $naerParams = @{
                                    Exception = [System.InvalidOperationException]::new("Function '$($mapping.Function)' not found in template script.")
                                    Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                                    ErrorId = 'TemplateFunctionNotFound'
                                    TargetObject = $mapping.Function
                                }
                                throw (New-ADTErrorRecord @naerParams)
                            }

                            # Find all $adtSession.InstallPhase assignments within the function.
                            $phaseAssignments = @($funcAst.FindAll({
                                        param ($ast)
                                        $ast -is [System.Management.Automation.Language.AssignmentStatementAst] -and
                                        $ast.Left -is [System.Management.Automation.Language.MemberExpressionAst] -and
                                        $ast.Left.Member.Value -eq 'InstallPhase'
                                    }, $true))

                            $targetAssignment = $phaseAssignments[$mapping.PhaseIndex]
                            if (!$targetAssignment)
                            {
                                $naerParams = @{
                                    Exception = [System.InvalidOperationException]::new("InstallPhase assignment at index $($mapping.PhaseIndex) not found in function '$($mapping.Function)'.")
                                    Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                                    ErrorId = 'TemplatePhaseAssignmentNotFound'
                                    TargetObject = $mapping
                                }
                                throw (New-ADTErrorRecord @naerParams)
                            }

                            # Determine the start of the replacement region (line after the InstallPhase assignment).
                            $replaceStart = $scriptContent.IndexOf("`n", $targetAssignment.Extent.EndOffset) + 1

                            # Determine the end of the replacement region.
                            if ($mapping.PhaseIndex -eq 2)
                            {
                                # Post phase: replace up to the last line before the function's closing brace.
                                $replaceEnd = $scriptContent.LastIndexOf("`n", $funcAst.Extent.EndOffset - 1) + 1
                            }
                            else
                            {
                                # Pre/Main phase: replace up to the next MARK separator.
                                $separatorPattern = "    ##================================================`r`n    ## MARK:"
                                $replaceEnd = $scriptContent.IndexOf($separatorPattern, $replaceStart)
                                if ($replaceEnd -eq -1)
                                {
                                    $naerParams = @{
                                        Exception = [System.InvalidOperationException]::new("MARK separator not found after phase index $($mapping.PhaseIndex) in function '$($mapping.Function)'.")
                                        Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                                        ErrorId = 'TemplateSeparatorNotFound'
                                        TargetObject = $mapping
                                    }
                                    throw (New-ADTErrorRecord @naerParams)
                                }
                            }

                            # Normalize the user's script block to 4-space indentation.
                            $userLines = $PSBoundParameters[$paramName].ToString().Split("`n").Where({ $_.Trim().Length -gt 0 })
                            $minIndent = [System.Int32]::MaxValue
                            foreach ($line in $userLines)
                            {
                                $trimmed = $line.TrimStart()
                                if ($trimmed.Length -gt 0)
                                {
                                    $indent = $line.Length - $trimmed.Length
                                    if ($indent -lt $minIndent)
                                    {
                                        $minIndent = $indent
                                    }
                                }
                            }
                            if ($minIndent -eq [System.Int32]::MaxValue) { $minIndent = 0 }
                            $normalizedLines = foreach ($line in $userLines)
                            {
                                $stripped = if ($line.Length -gt $minIndent) { $line.Substring($minIndent) } else { $line.TrimStart() }
                                "    $($stripped.TrimEnd())"
                            }
                            $normalizedText = $normalizedLines -join "`r`n"

                            # Build the replacement text.
                            if ($mapping.PhaseIndex -eq 2)
                            {
                                # Post phase: content + newline before closing brace.
                                $replacementText = "`r`n$normalizedText`r`n"
                            }
                            else
                            {
                                # Pre/Main phase: content + blank line before the next MARK separator.
                                $replacementText = "`r`n$normalizedText`r`n`r`n"
                            }

                            $scriptReplacements.Add([PSCustomObject]@{
                                    Start = $replaceStart
                                    End = $replaceEnd
                                    Value = $replacementText
                                })
                        }
                    }

                    # Apply all replacements from end to start to preserve earlier offsets.
                    foreach ($replacement in ($scriptReplacements | Sort-Object -Property Start -Descending))
                    {
                        $scriptContent = $scriptContent.Substring(0, $replacement.Start) + $replacement.Value + $scriptContent.Substring($replacement.End)
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
