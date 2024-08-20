@{
    #________________________________________
    #IncludeDefaultRules
    IncludeDefaultRules = $true
    #________________________________________
    #Severity
    #Specify Severity when you want to limit generated diagnostic records to a specific subset: [ Error | Warning | Information ]
    Severity            = @('Error', 'Warning')
    #________________________________________
    #CustomRulePath
    #Specify CustomRulePath when you have a large set of custom rules you'd like to reference
    #CustomRulePath = "Module\InjectionHunter\1.0.0\InjectionHunter.psd1"
    #________________________________________
    #IncludeRules
    #Specify IncludeRules when you only want to run specific subset of rules instead of the default rule set.
    #IncludeRules = @('PSShouldProcess',
    #                 'PSUseApprovedVerbs')
    #________________________________________
    #ExcludeRules
    #Specify ExcludeRules when you want to exclude a certain rule from the the default set of rules.
    #ExcludeRules = @(
    #    'PSUseDeclaredVarsMoreThanAssignments'
    #)
    #________________________________________
    #Rules
    #Here you can specify customizations for particular rules. Several examples are included below:
    #Rules = @{
    #    PSUseCompatibleCmdlets = @{
    #        compatibility = @('core-6.1.0-windows', 'desktop-4.0-windows')
    #    }
    #    PSUseCompatibleSyntax = @{
    #        Enable = $true
    #        TargetVersions = @(
    #            '3.0',
    #            '5.1',
    #            '6.2'
    #        )
    #    }
    #    PSUseCompatibleCommands = @{
    #        Enable = $true
    #        TargetProfiles = @(
    #            'win-8_x64_10.0.14393.0_6.1.3_x64_4.0.30319.42000_core', # PS 6.1 on WinServer-2019
    #            'win-8_x64_10.0.17763.0_5.1.17763.316_x64_4.0.30319.42000_framework', # PS 5.1 on WinServer-2019
    #            'win-8_x64_6.2.9200.0_3.0_x64_4.0.30319.42000_framework' # PS 3 on WinServer-2012
    #        )
    #    }
    #    PSUseCompatibleTypes = @{
    #        Enable = $true
    #        TargetProfiles = @(
    #            'ubuntu_x64_18.04_6.1.3_x64_4.0.30319.42000_core',
    #            'win-48_x64_10.0.17763.0_5.1.17763.316_x64_4.0.30319.42000_framework'
    #        )
    #        # You can specify types to not check like this, which will also ignore methods and members on it:
    #        IgnoreTypes = @(
    #            'System.IO.Compression.ZipFile'
    #        )
    #    }
    #}
    #________________________________________
}
