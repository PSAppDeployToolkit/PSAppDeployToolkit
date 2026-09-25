#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTAdmlTemplateValid
#
#-----------------------------------------------------------------------------

function Confirm-ADTAdmlTemplateValid
{
    # The policy element types each presentation control is allowed to present.
    $controlElements = @{
        textBox = 'text'
        comboBox = 'text'
        decimalTextBox = 'decimal', 'text'
        longDecimalTextBox = 'longDecimal', 'text'
        checkBox = 'boolean'
        dropdownList = 'enum'
        listBox = 'list'
        multiTextBox = 'multiText'
    }

    # Internal worker function to confirm one language's ADML resolves every reference the ADMX makes, and makes none the ADMX doesn't.
    function Confirm-ADTAdmlResourcesValid
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.IO.FileInfo]$Path
        )

        # Load the ADML and collect its identifiers, refusing duplicates as the editor does.
        $admlData = [System.Xml.XmlDocument]::new()
        $admlData.Load($Path.FullName)
        $strings = @{}
        foreach ($string in $admlData.SelectNodes('//*[local-name()="stringTable"]/*[local-name()="string"]'))
        {
            $strings[$string.GetAttribute('id')] = $string.InnerText
        }
        $stringIds = [System.String[]]@($admlData.SelectNodes('//*[local-name()="stringTable"]/*[local-name()="string"]') | & { process { $_.GetAttribute('id') } })
        $presentations = @($admlData.SelectNodes('//*[local-name()="presentationTable"]/*[local-name()="presentation"]'))
        $presentationIds = [System.String[]]@($presentations | & { process { $_.GetAttribute('id') } })
        foreach ($identifiers in ($stringIds, $presentationIds))
        {
            if ($duplicates = $identifiers | Group-Object | & { process { if ($_.Count -gt 1) { return $_.Name } } })
            {
                throw "The ADML file [$($Path.Name)] defines the following identifiers more than once: ['$([System.String]::Join("', '", $duplicates))']."
            }
        }

        # The ADML must be at least the revision the ADMX asks for, or the editor refuses the pair.
        $minRequiredRevision = [System.Version]$admxData.policyDefinitions.resources.GetAttribute('minRequiredRevision')
        $revision = [System.Version]$admlData.DocumentElement.GetAttribute('revision')
        if ($revision -lt $minRequiredRevision)
        {
            throw "The ADML file [$($Path.Name)] is revision [$revision] but the ADMX requires at least [$minRequiredRevision]."
        }

        # Every string and presentation the ADMX references must exist here.
        if ($missing = $stringReferences | & { process { if (!$stringIds.Contains($_)) { return $_ } } })
        {
            throw "The ADML file [$($Path.Name)] is missing the following strings the ADMX references: ['$([System.String]::Join("', '", $missing))']."
        }
        if ($missing = $presentationReferences | & { process { if (!$presentationIds.Contains($_)) { return $_ } } })
        {
            throw "The ADML file [$($Path.Name)] is missing the following presentations the ADMX references: ['$([System.String]::Join("', '", $missing))']."
        }

        # Each policy's explain text must state the version its key requires in the words the config uses, and a shared-key policy must claim none.
        # Then each presentation's controls must pair one-to-one with the elements of the policy using it, by identifier and by type.
        foreach ($policy in $admxData.policyDefinitions.policies.policy)
        {
            $null = $policy.GetAttribute('explainText') -match '^\$\(string\.([A-Za-z0-9_]+)\)$'
            $explainText = $strings[$Matches[1]]
            if ($policy.GetAttribute('key') -match '\\(\d+\.\d+)\\Config\\')
            {
                if (!$explainText.Contains("Requires PSAppDeployToolkit $($Matches[1])"))
                {
                    throw "The ADML file [$($Path.Name)] explains the policy [$($policy.name)] without stating that it requires PSAppDeployToolkit $($Matches[1]), which its key does."
                }
            }
            elseif ($explainText.Contains('Requires PSAppDeployToolkit'))
            {
                throw "The ADML file [$($Path.Name)] explains the policy [$($policy.name)] as requiring a version, but it writes to the shared key every version reads."
            }
            $elements = @($policy.SelectNodes('*[local-name()="elements"]/*'))
            if ($policy.GetAttribute('presentation') -notmatch '^\$\(presentation\.([A-Za-z0-9_]+)\)$')
            {
                if ($elements.Count)
                {
                    throw "The ADMX policy [$($policy.name)] has elements but no presentation to show them with."
                }
                continue
            }
            $presentationId = $Matches[1]
            if (!$elements.Count)
            {
                throw "The ADMX policy [$($policy.name)] has a presentation but no elements for it to show."
            }
            $controls = @(($presentations | & { process { if ($_.GetAttribute('id').Equals($presentationId)) { return $_ } } }).ChildNodes | & { process { if (($_.NodeType -eq [System.Xml.XmlNodeType]::Element) -and !$_.LocalName.Equals('text')) { return $_ } } })
            $controlRefIds = [System.String[]]@($controls | & { process { $_.GetAttribute('refId') } })
            foreach ($control in $controls)
            {
                $refId = $control.GetAttribute('refId')
                if (!($element = $elements | & { process { if ($_.GetAttribute('id').Equals($refId)) { return $_ } } }))
                {
                    throw "The ADML presentation [$presentationId] in [$($Path.Name)] refers to the element [$refId], which the policy [$($policy.name)] does not define."
                }
                if (!$controlElements.ContainsKey($control.LocalName) -or ($controlElements.($control.LocalName) -notcontains $element.LocalName))
                {
                    throw "The ADML presentation [$presentationId] in [$($Path.Name)] uses a [$($control.LocalName)] for the element [$refId], which is a [$($element.LocalName)] and cannot be presented that way."
                }
            }
            foreach ($element in $elements)
            {
                if (!$controlRefIds.Contains($element.GetAttribute('id')))
                {
                    throw "The ADMX policy [$($policy.name)] has the element [$($element.GetAttribute('id'))], which the presentation [$presentationId] in [$($Path.Name)] has no control for."
                }
            }
        }

        # Everything defined here must be referenced by the ADMX, or it is dead text.
        if ($extras = $stringIds | & { process { if (!$stringReferences.Contains($_)) { return $_ } } })
        {
            throw "The ADML file [$($Path.Name)] defines the following strings the ADMX never references: ['$([System.String]::Join("', '", $extras))']."
        }
        if ($extras = $presentationIds | & { process { if (!$presentationReferences.Contains($_)) { return $_ } } })
        {
            throw "The ADML file [$($Path.Name)] defines the following presentations the ADMX never references: ['$([System.String]::Join("', '", $extras))']."
        }
    }

    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Load the ADMX and collect every string and presentation it references, wherever the reference sits.
        Write-ADTBuildLogEntry -Message "Confirming ADML resource files resolve every reference the ADMX template makes."
        $admxData = [System.Xml.XmlDocument]::new()
        $admxData.Load($Script:ModuleConstants.Paths.AdmxTemplate)
        $admxText = [System.IO.File]::ReadAllText($Script:ModuleConstants.Paths.AdmxTemplate)
        $stringReferences = [System.String[]]@([System.Text.RegularExpressions.Regex]::Matches($admxText, '\$\(string\.([A-Za-z0-9_]+)\)') | & { process { $_.Groups[1].Value } } | Sort-Object -Unique)
        $presentationReferences = [System.String[]]@([System.Text.RegularExpressions.Regex]::Matches($admxText, '\$\(presentation\.([A-Za-z0-9_]+)\)') | & { process { $_.Groups[1].Value } } | Sort-Object -Unique)

        # Every display, explain and presentation attribute must be a reference, or the editor shows the raw text or nothing at all.
        foreach ($attribute in $admxData.SelectNodes('//@displayName | //@explainText'))
        {
            if ($attribute.Value -notmatch '^\$\(string\.[A-Za-z0-9_]+\)$')
            {
                throw "The ADMX attribute [$($attribute.Name)] on [$($attribute.OwnerElement.GetAttribute('name'))] is [$($attribute.Value)] rather than a string reference."
            }
        }
        foreach ($attribute in $admxData.SelectNodes('//@presentation'))
        {
            if ($attribute.Value -notmatch '^\$\(presentation\.[A-Za-z0-9_]+\)$')
            {
                throw "The ADMX attribute [presentation] on [$($attribute.OwnerElement.GetAttribute('name'))] is [$($attribute.Value)] rather than a presentation reference."
            }
        }

        # Check every language's ADML beneath the template's folder.
        if (!($admlFiles = Get-ChildItem -LiteralPath ([System.IO.Path]::GetDirectoryName($Script:ModuleConstants.Paths.AdmxTemplate)) -Filter *.adml -Recurse -File))
        {
            throw "The ADMX template has no ADML resource files beside it."
        }
        foreach ($admlFile in $admlFiles)
        {
            Write-ADTBuildLogEntry -Message "Testing ADML resource file [$($admlFile.FullName)]."
            Confirm-ADTAdmlResourcesValid -Path $admlFile
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
