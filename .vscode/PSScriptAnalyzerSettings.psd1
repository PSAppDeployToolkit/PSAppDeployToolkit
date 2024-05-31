# PSAppDeployToolkit default rules for PSScriptAnalyser, to ensure compatibility with PowerSHell 5.1
@{
    Severity     = @(
        'Error',
        'Warning'
    );
    ExcludeRules = @(
        'PSUseDeclaredVarsMoreThanAssigments'
    );
    Rules        = @{
        'PSUseCompatibleCmdlets' = @{
            'compatibility' = @('desktop-5.1.14393.206-windows')
        }
    };
}
