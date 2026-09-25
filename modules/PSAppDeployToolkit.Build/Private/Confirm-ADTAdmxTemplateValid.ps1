#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTAdmxTemplateValid
#
#-----------------------------------------------------------------------------

function Confirm-ADTAdmxTemplateValid
{
    # A setting whose comment says it requires a version belongs under that version's key. Any other is read by every version from the shared key.
    $policyRoot = 'SOFTWARE\Policies\PSAppDeployToolkit'
    $sharedKeyVersion = [System.Version]'4.0'

    # Internal worker function to read each setting's required version from the comment above it in the config defaults.
    function Get-ADTConfigSettingVersions
    {
        [CmdletBinding()]
        [OutputType([System.Collections.Hashtable])]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Management.Automation.ScriptBlock]$Config
        )

        # Parse the defaults' own text so the comment tokens and the hashtable share line numbers.
        $tokens = $null; $errors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($Config.Ast.Extent.Text, [ref]$tokens, [ref]$errors)
        if ($errors)
        {
            throw "The config defaults could not be parsed: $($errors[0].Message)"
        }
        $comments = $tokens | & { process { if ($_.Kind -eq [System.Management.Automation.Language.TokenKind]::Comment) { return $_ } } }
        $versions = @{}
        foreach ($section in $ast.Find({ $args[0] -is [System.Management.Automation.Language.HashtableAst] }, $true).KeyValuePairs)
        {
            $versions.Add($section.Item1.Value, @{})
            foreach ($setting in $section.Item2.PipelineElements[0].Expression.KeyValuePairs)
            {
                # Gather the comment block sitting directly above the setting, then take any version it says the setting requires.
                $line = $setting.Item1.Extent.StartLineNumber - 1
                $text = while ($comment = $comments | & { process { if ($_.Extent.EndLineNumber -eq $line) { return $_ } } })
                {
                    $comment.Text
                    $line = $comment.Extent.StartLineNumber - 1
                }
                if (($text -join ' ') -match 'Requires PSAppDeployToolkit (\d+)\.(\d+)')
                {
                    $versions.($section.Item1.Value).Add($setting.Item1.Value, [System.Version]"$($Matches[1]).$($Matches[2])")
                }
            }
        }
        return $versions
    }

    # Internal worker function for processing each hashtable.
    function Confirm-ADTAdmxCategoryMatchesConfigSection
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.String]$Category,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Collections.Hashtable]$Section
        )

        # Recursively process subsections that are hashtables.
        $sectionProps = foreach ($kvp in $Section.GetEnumerator())
        {
            if ($kvp.Value -is [System.Collections.Hashtable])
            {
                Confirm-ADTAdmxCategoryMatchesConfigSection -Category $kvp.Key -Section $kvp.Value
            }
            else
            {
                $kvp.Key
            }
        }

        # Test our collected session properties.
        $admxProps = $admxData.policyDefinitions.policies.policy | & { process { if ($_.parentCategory.ref.Equals($Category)) { return $_.Name.Split('_')[0] } } }
        if ($missing = $sectionProps | & { process { if ($admxProps -notcontains $_) { return $_ } } })
        {
            throw "The ADMX category [$Category] is missing the following config options: ['$([System.String]::Join("', '", $missing))']."
        }
        if ($extras = $admxProps | & { process { if ($sectionProps -notcontains $_) { return $_ } } })
        {
            throw "The ADMX category [$Category] has the following extra config options: ['$([System.String]::Join("', '", $extras))']."
        }
    }

    # Internal worker function to confirm a policy writes where its setting belongs, with a supportedOn and elements that agree.
    function Confirm-ADTAdmxPolicyKeyValid
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Xml.XmlElement]$Policy,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Collections.Hashtable]$Section
        )

        # Take the setting and any layer from the name, which is Setting, Setting_Qualifier, or either with a _Major_Minor suffix.
        $category = $Policy.parentCategory.ref
        if ($Policy.name -notmatch '^(?<setting>[A-Za-z][A-Za-z0-9]*)(?:_[A-Za-z][A-Za-z0-9]*)?(?:_(?<major>\d+)_(?<minor>\d+))?$')
        {
            throw "The ADMX policy [$($Policy.name)] is not named in the form Setting, Setting_Qualifier or Setting_Major_Minor."
        }
        $setting = $Matches['setting']
        $nameVersion = if (![System.String]::IsNullOrEmpty($Matches['major']))
        {
            [System.Version]"$($Matches['major']).$($Matches['minor'])"
        }

        # Take the layer from the key the policy writes to.
        if ($Policy.key -notmatch "^$([regex]::Escape($policyRoot))\\(?:(?<version>\d+\.\d+)\\)?Config\\$([regex]::Escape($category))$")
        {
            throw "The ADMX policy [$($Policy.name)] writes to [$($Policy.key)], which is neither the shared nor a versioned key for the category [$category]."
        }
        $keyVersion = if (![System.String]::IsNullOrEmpty($Matches['version']))
        {
            [System.Version]$Matches['version']
        }
        else
        {
            $sharedKeyVersion
        }
        if ($keyVersion -gt $moduleVersion)
        {
            throw "The ADMX policy [$($Policy.name)] writes to a key for version [$keyVersion], which the module shipping it, [$moduleVersion], cannot read."
        }

        # The name, the key and the setting's age must all agree on the layer.
        if ($nameVersion -and ($nameVersion -ne $keyVersion))
        {
            throw "The ADMX policy [$($Policy.name)] is named for version [$nameVersion] but writes to [$($Policy.key)]."
        }
        $settingVersion = $settingVersions.$category[$setting]
        if ($null -eq $settingVersion)
        {
            if (!$nameVersion -and ($keyVersion -ne $sharedKeyVersion))
            {
                throw "The ADMX policy [$($Policy.name)] is for a setting every version reads from the shared key, but writes to [$($Policy.key)]. Keep it on the shared key and add a versioned twin for any newer grammar."
            }
        }
        elseif (!$nameVersion -and ($keyVersion -ne $settingVersion))
        {
            throw "The ADMX policy [$($Policy.name)] is for a setting that requires PSAppDeployToolkit [$settingVersion], but writes to [$($Policy.key)] rather than [$policyRoot\$settingVersion\Config\$category]."
        }

        # The supportedOn names the layer the key belongs to, and its definition must exist.
        $expectedSupportedOn = "SUPPORTED_PSAppDeployToolkit_$($keyVersion.Major)_$($keyVersion.Minor)"
        if (!$supportedOnDefinitions.Contains($expectedSupportedOn))
        {
            throw "The ADMX template has no supportedOn definition [$expectedSupportedOn] for the policy [$($Policy.name)]."
        }
        if (!$Policy.supportedOn.ref.Equals($expectedSupportedOn))
        {
            throw "The ADMX policy [$($Policy.name)] has the supportedOn [$($Policy.supportedOn.ref)] but writes to a key for version [$keyVersion], so it should be [$expectedSupportedOn]."
        }

        # Every element writes to the policy's own key, and may only be optional when the setting ships as null, as the module ignores a blank for anything else.
        foreach ($element in $Policy.SelectNodes('*[local-name()="elements"]/*'))
        {
            if ($element.HasAttribute('key') -and !$element.GetAttribute('key').Equals($Policy.key))
            {
                throw "The ADMX policy [$($Policy.name)] has the element [$($element.GetAttribute('id'))] writing to [$($element.GetAttribute('key'))] rather than the policy's own key."
            }
            if (!$element.GetAttribute('required').Equals('true') -and ($null -ne $Section.$setting))
            {
                throw "The ADMX policy [$($Policy.name)] has the element [$($element.GetAttribute('id'))] as optional, but the setting [$setting] does not ship as null, so the module would ignore a blank."
            }
        }
    }

    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Import config and XML as required.
        Write-ADTBuildLogEntry -Message "Confirming ADMX template matches the module config."
        $adtConfig = & (Get-Module -Name $Script:ModuleConstants.ModuleName) { $Module.Defaults.Config.([System.String]::Empty).Ast.EndBlock.Statements.PipelineElements.Expression.SafeGetValue() }
        $moduleVersion = (Get-Module -Name $Script:ModuleConstants.ModuleName).Version
        $admxData = [System.Xml.XmlDocument]::new()
        $admxData.Load($Script:ModuleConstants.Paths.AdmxTemplate)
        $supportedOnDefinitions = [System.String[]]$admxData.policyDefinitions.supportedOn.definitions.definition.name

        # Process the hashtable. We assume that each initial section is a hashtable.
        foreach ($kvp in $adtConfig.GetEnumerator())
        {
            Confirm-ADTAdmxCategoryMatchesConfigSection -Category $kvp.Key -Section $kvp.Value
        }

        # Confirm each policy writes where its setting belongs, then that the shared key still carries everything every version reads.
        Write-ADTBuildLogEntry -Message "Confirming ADMX policies write to the keys their settings belong under."
        $settingVersions = Get-ADTConfigSettingVersions -Config (& (Get-Module -Name $Script:ModuleConstants.ModuleName) { $Module.Defaults.Config.([System.String]::Empty) })
        $sharedKeyPolicies = @{}
        foreach ($category in $adtConfig.Keys)
        {
            $sharedKeyPolicies.Add($category, [System.Collections.Generic.List[System.String]]::new())
        }
        foreach ($policy in $admxData.policyDefinitions.policies.policy)
        {
            $category = $policy.parentCategory.ref
            if (!$adtConfig.ContainsKey($category))
            {
                throw "The ADMX policy [$($policy.name)] is in the category [$category], which the config does not have."
            }
            Confirm-ADTAdmxPolicyKeyValid -Policy $policy -Section $adtConfig.$category
            if ($policy.key.Equals("$policyRoot\Config\$category"))
            {
                $sharedKeyPolicies.$category.Add($policy.name.Split('_')[0])
            }
        }
        foreach ($category in $adtConfig.Keys)
        {
            if ($missing = $adtConfig.$category.Keys | & { process { if (($null -eq $settingVersions.$category[$_]) -and !$sharedKeyPolicies.$category.Contains($_)) { return $_ } } })
            {
                throw "The ADMX shared key for [$category] has no policy for the following settings every version reads: ['$([System.String]::Join("', '", $missing))']."
            }
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
