#-----------------------------------------------------------------------------
#
# MARK: Import-ADTModuleDataFile
#
#-----------------------------------------------------------------------------

function Private:Import-ADTModuleDataFile
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowNull()][PSAppDeployToolkit.Attributes.AllowNullButNotEmptyOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [System.String[]]$BaseDirectory,

        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$FileName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Globalization.CultureInfo]$UICulture = [System.Globalization.CultureInfo]::CurrentUICulture,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IgnorePolicy
    )

    # Internal function to process the imported data.
    function Update-ADTImportedDataValues
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [AllowEmptyCollection()]
            [System.Collections.Hashtable]$DataFile,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Collections.Hashtable]$NewData,

            [Parameter(Mandatory = $true)]
            [AllowEmptyCollection()]
            [System.Collections.Hashtable]$Defaults
        )

        # Process the provided default data so we can add missing data to the data file.
        foreach ($section in $NewData.GetEnumerator())
        {
            # Recursively process hashtables, otherwise just update the value.
            if ($section.Value -is [System.Collections.Hashtable])
            {
                if (!$DataFile.ContainsKey($section.Key) -or ($DataFile.($section.Key) -isnot [System.Collections.Hashtable]))
                {
                    $DataFile.($section.Key) = @{}
                }
                $sectionDefaults = @{}; if ($Defaults.ContainsKey($section.Key) -and ($Defaults.($section.Key) -is [System.Collections.Hashtable]))
                {
                    $sectionDefaults = $Defaults.($section.Key)
                }
                & $MyInvocation.MyCommand -DataFile $DataFile.($section.Key) -NewData $section.Value -Defaults $sectionDefaults
            }
            elseif (!$DataFile.ContainsKey($section.Key) -or (Out-ADTString -InputObject $section.Value))
            {
                $DataFile.($section.Key) = $section.Value
            }
            elseif ($Defaults.ContainsKey($section.Key) -and ($null -eq $Defaults.($section.Key)))
            {
                $DataFile.($section.Key) = $null
            }
        }
    }

    # Import the default data first and foremost, keeping a pristine copy to tell which settings ship as null.
    $section = [System.Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase([System.IO.Path]::GetFileNameWithoutExtension($FileName))
    $initialUICulture = $UICulture
    $defaultData = while ($true)
    {
        if (($defaultSection = (Get-ADTModuleDefaults).$section).ContainsKey($UICulture.Name))
        {
            $defaultSection.($UICulture.Name).Ast.EndBlock.Statements.PipelineElements.Expression
            $UICulture = $initialUICulture
            break
        }
        $UICulture = $UICulture.Parent
    }
    $importedData = $defaultData.SafeGetValue()
    $defaults = $defaultData.SafeGetValue()

    # Super-impose the caller's data if it's different from default.
    $null = $PSBoundParameters.Remove('IgnorePolicy')
    foreach ($directory in $BaseDirectory)
    {
        $PSBoundParameters.BaseDirectory = [System.Management.Automation.WildcardPattern]::Escape($directory)
        Update-ADTImportedDataValues -DataFile $importedData -NewData (Import-LocalizedData @PSBoundParameters) -Defaults $defaults
    }

    # Return the data we've got if we're not reading values out of the registry.
    if ($IgnorePolicy)
    {
        return $importedData
    }

    # Super-impose registry values if they exist: the shared key first, then each versioned key this module is new enough to read, oldest first.
    $policyRootKey = 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\PSAppDeployToolkit'
    $policyKeys = if ($versionedKeys = Get-ChildItem -LiteralPath $policyRootKey -ErrorAction Ignore)
    {
        try
        {
            $layers = [System.Collections.Generic.SortedDictionary[System.Version, System.String]]::new()
            foreach ($versionedKey in $versionedKeys)
            {
                $version = $null; if ([System.Version]::TryParse($versionedKey.PSChildName, [ref]$version) -and ($version -le $Script:Module.ModuleInfo.Version))
                {
                    $layers[$version] = $versionedKey.PSPath
                }
            }
            $policyRootKey
            $layers.Values
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        finally
        {
            $versionedKeys.Dispose()
        }
    }
    else
    {
        $policyRootKey
    }

    # Walk each key from its un-cultured values up through the culture chain, so the most culture-specific value wins.
    $cultureNames = [System.Collections.Generic.List[System.String]]::new()
    for ($culture = $UICulture; ![System.String]::IsNullOrWhiteSpace($culture.Name); $culture = $culture.Parent)
    {
        $cultureNames.Insert(0, $culture.Name)
    }
    $cultureNames.Insert(0, [System.String]::Empty)
    foreach ($policyKey in $policyKeys)
    {
        foreach ($cultureName in $cultureNames)
        {
            if ($policySettings = Get-ChildItem -LiteralPath "$policyKey\$section$(if ($cultureName.Length) { "\$cultureName" })" -ErrorAction Ignore | Convert-ADTRegistryKeyToHashtable)
            {
                Update-ADTImportedDataValues -DataFile $importedData -NewData $policySettings -Defaults $defaults
            }
        }
    }

    # Return the built out data to the caller.
    return $importedData
}
