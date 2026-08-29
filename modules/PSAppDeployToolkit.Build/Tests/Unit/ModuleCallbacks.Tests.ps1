BeforeDiscovery {
    Set-Location -Path $PSScriptRoot
    $ModuleName = 'PSAppDeployToolkit'

    # Anchored on $PSScriptRoot because the .NET file APIs below resolve a relative path against the process
    # working directory, which Set-Location does not move.
    $ModuleRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', $ModuleName))
    Get-Module $ModuleName -ErrorAction SilentlyContinue | Remove-Module -Force
    Import-Module ([System.IO.Path]::Combine($ModuleRoot, "$ModuleName.psd1")) -Force

    # Read from the enumeration rather than listed here, so a callback type added later is checked without this
    # file being touched. Each entry is a hashtable because Pester makes a hashtable's keys available as
    # variables inside the test.
    $script:CallbackTypes = foreach ($name in [System.Enum]::GetNames([PSAppDeployToolkit.Foundation.CallbackType]))
    {
        @{ CallbackType = $name }
    }
}

Describe 'Module callback registrations' -Tag Unit {
    <#
        The module builds its callback table by hand, one entry per callback type, and every caller reaches a
        type's callbacks by indexing that table. A type declared in the enumeration but missing from the table
        therefore fails at the point something tries to raise it - during a deployment, on a path that only runs
        when that stage is reached - rather than when the module is imported.

        Neither the compiler nor the C# tests can see this: the enumeration is in one project and the table that
        has to match it is in a script in another. So it is checked here, where both are in scope at once.
    #>

    It 'Finds callback types to check' -ForEach @{ Found = $script:CallbackTypes.Count } {
        # The count is passed in as data because a variable set during discovery is not visible at run time,
        # and without this a run that found no types at all would report every test below as passing.
        $Found | Should -BeGreaterThan 0
    }

    It 'Has somewhere to register a <CallbackType> callback' -ForEach $script:CallbackTypes {
        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Name = $CallbackType } {
            $ADT.Callbacks.ContainsKey([System.Enum]::Parse([PSAppDeployToolkit.Foundation.CallbackType], $Name)) | Should -BeTrue
        }
    }

    It 'Registers nothing the enumeration does not declare' {
        # The other direction. A type removed from the enumeration leaves an entry behind that nothing can ever
        # reach, which is harmless but is a sign the two have drifted apart.
        $registered = InModuleScope -ModuleName PSAppDeployToolkit { $ADT.Callbacks.Keys | ForEach-Object { $_.ToString() } }
        ($registered | Sort-Object) -join ', ' | Should -BeExactly (([System.Enum]::GetNames([PSAppDeployToolkit.Foundation.CallbackType]) | Sort-Object) -join ', ')
    }
}
