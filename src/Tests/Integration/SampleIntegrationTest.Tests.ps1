# BeforeAll {
#     Set-Location -Path $PSScriptRoot
#     $ModuleName = 'PSAppDeployToolkit'
#     $PathToManifest = [System.IO.Path]::Combine('..', '..', 'Artifacts', "$ModuleName.psd1")
#     #if the module is already in memory, remove it
#     Get-Module $ModuleName -ErrorAction SilentlyContinue | Remove-Module -Force
#     Import-Module $PathToManifest -Force
# }

# Describe 'Integration Tests' -Tag Integration {
#     Context 'First Integration Tests' {
#         It 'should pass the first integration test' {
#             # test logic
#         } #it
#     }
# }

