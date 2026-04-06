BeforeAll {
    #-------------------------------------------------------------------------
    Set-Location -Path $PSScriptRoot
    #-------------------------------------------------------------------------
    $ModuleName = 'PSAppDeployToolkit'

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'PathToManifest', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
    $PathToManifest = [System.IO.Path]::Combine('..', '..', $ModuleName, "$ModuleName.psd1")
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'PathToModule', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
    $PathToModule = [System.IO.Path]::Combine('..', '..', $ModuleName, "$ModuleName.psm1")
    #-------------------------------------------------------------------------
}
Describe 'Module Tests' -Tag Unit {
    Context 'Module Tests' {
        $script:manifestEval = $null
        It 'Should pass Test-ModuleManifest' {
            { $script:manifestEval = Test-ModuleManifest -Path $PathToManifest } | Should -Not -Throw
            $? | Should -BeTrue
        } #manifestTest
        It "The root module 'PSAppDeployToolkit.psm1' should exist" {
            $PathToModule | Should -Exist
            $? | Should -BeTrue
        } #psm1Exists
        It "The root module should be 'PSAppDeployToolkit.psm1' in the module manifest" {
            $script:manifestEval.RootModule | Should -BeExactly 'PSAppDeployToolkit.psm1'
        } #validPSM1
        It 'Should have a matching module name in the manifest' {
            $script:manifestEval.Name | Should -BeExactly $ModuleName
        } #name
        It 'Should have a valid description in the manifest' {
            [System.String]::IsNullOrWhiteSpace($script:manifestEval.Description) | Should -BeFalse
        } #description
        It 'Should have a valid author in the manifest' {
            [System.String]::IsNullOrWhiteSpace($script:manifestEval.Author) | Should -BeFalse
        } #author
        It 'Should have a valid guid in the manifest' {
            $script:manifestEval.Guid | Should -Not -Be ([System.Guid]::Empty)
        } #guid
        It 'Should not have any whitespace in the tags' {
            foreach ($tag in $script:manifestEval.Tags)
            {
                $tag | Should -Not -Match '\s'
            }
        } #tagSpaces
        It 'Should have a valid project Uri' {
            [System.String]::IsNullOrWhiteSpace($script:manifestEval.ProjectUri) | Should -BeFalse
        } #uri
    } #context_ModuleTests
} #describe_ModuleTests
