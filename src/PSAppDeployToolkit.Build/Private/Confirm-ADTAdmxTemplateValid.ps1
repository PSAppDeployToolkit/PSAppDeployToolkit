#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTAdmxTemplateValid
#
#-----------------------------------------------------------------------------

function Confirm-ADTAdmxTemplateValid
{
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

    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Import config and XML as required.
        Write-ADTBuildLogEntry -Message "Confirming ADMX template matches the module config."
        $adtConfig = Import-LocalizedData -BaseDirectory $Script:ModuleConstants.Paths.ModuleConfig -FileName config.psd1
        $admxData = [System.Xml.XmlDocument]::new()
        $admxData.Load($Script:ModuleConstants.Paths.AdmxTemplate)

        # Process the hashtable. We assume that each initial section is a hashtable.
        foreach ($kvp in $adtConfig.GetEnumerator())
        {
            Confirm-ADTAdmxCategoryMatchesConfigSection -Category $kvp.Key -Section $kvp.Value
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
