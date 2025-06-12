BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTIniSection' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
    }
    BeforeEach {
        $IniContent = @"
[MySection]
MyKey=MyValue
MyKey2=MyValue2
"@
        $IniPath = "$TestDrive\IniFile.ini"
        Set-Content -Path $IniPath -Value $IniContent -Encoding Ascii -Force
    }

    Context 'Functionality' {
        It 'Should merge content into a section by default' {
            $IniSection = [ordered]@{
                'MyKey' = 'MyNewValue'
            }
            Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content $IniSection
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyKey=MyNewValue\r\nMyKey2=MyValue2\r\n'
        }
        It 'Should overwrite a section when required' {
            $IniSection = [ordered]@{
                'MyKey' = 'MyNewValue'
                'MyOtherKey' = 'MyOtherValue'
            }
            Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content $IniSection -Overwrite
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyKey=MyNewValue\r\nMyOtherKey=MyOtherValue\r\n'
            $IniPath | Should -Not -FileContentMatch 'MyKey2=MyValue2'
        }
        It 'Should leave a section untouched with empty hashtable input by default' {
            Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content @{}
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyKey=MyValue\r\nMyKey2=MyValue2\r\n'
        }
        It 'Should leave a section untouched with null input by default' {
            Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content $null
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyKey=MyValue\r\nMyKey2=MyValue2\r\n'
        }
        It 'Should overwrite a section to be empty with empty hashtable input when required' {
            Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content @{} -Overwrite
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\n$'
            $IniPath | Should -Not -FileContentMatch 'MyKey=MyValue'
        }
        It 'Should overwrite a section to be empty with null input when required' {
            Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content $null -Overwrite
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\n$'
            $IniPath | Should -Not -FileContentMatch 'MyKey=MyValue'
        }
        It 'Should create a new file if it does not exist when Force is specified' {
            $newIniPath = "$TestDrive\NewIniFile.ini"
            Set-ADTIniSection -FilePath $newIniPath -Section 'NewSection' -Content @{'NewKey' = 'NewValue' } -Force
            $newIniPath | Should -FileContentMatchMultiline '\[NewSection\]\r\nNewKey=NewValue'
            Remove-Item -Path $newIniPath -Force
        }
        It 'Should handle string / number / bool / null inputs' {
            $IniSection = [ordered]@{
                'StringKey' = 'StringValue'
                'EmptyKey' = ''
                'IntKey' = 123
                'DoubleKey' = 1.23
                'BoolKey' = $true
                'NullKey' = $null
            }
            Set-ADTIniSection -FilePath $IniPath -Section 'NewSection' -Content $IniSection
            $IniPath | Should -FileContentMatchMultiline '\[NewSection\]\r\nStringKey=StringValue\r\nEmptyKey=\r\nIntKey=123\r\nDoubleKey=1.23\r\nBoolKey=True\r\nNullKey=\r\n'
        }
    }

    Context 'Input Validation' {
        It 'Should verify that FilePath is not null, empty or whitespace' {
            { Set-ADTIniSection -FilePath $null -Section 'Anything' -Content @{} } | Should -Throw
            { Set-ADTIniSection -FilePath '' -Section 'Anything' -Content @{} } | Should -Throw
            { Set-ADTIniSection -FilePath ' ' -Section 'Anything' -Content @{} } | Should -Throw
        }
        It 'Should verify that FilePath exists if Force is not specified' {
            { Set-ADTIniSection -FilePath "$TestDrive\DoesNotExist.ini" -Section 'Anything' -Content @{} } | Should -Throw
        }
        It 'Should verify that Section is not null, empty or whitespace' {
            { Set-ADTIniSection -FilePath $IniPath -Section $null -Content @{} } | Should -Throw
            { Set-ADTIniSection -FilePath $IniPath -Section '' -Content @{} } | Should -Throw
            { Set-ADTIniSection -FilePath $IniPath -Section ' ' -Content @{} } | Should -Throw
        }
        It 'Should accept hashtable input' {
            $IniSection = @{
                'MyKey' = 'MyValue'
            }
            { Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content $IniSection } | Should -Not -Throw
        }
        It 'Should accept ordered dictionary input' {
            $IniSection = [ordered]@{
                'MyKey' = 'MyValue'
            }
            { Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content $IniSection } | Should -Not -Throw
        }
        It 'Should accept typed Dictionary input' {
            $IniSection = New-Object 'System.Collections.Generic.Dictionary[string,string]'
            $IniSection.Add('MyKey', 'MyValue')
            { Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content $IniSection } | Should -Not -Throw
        }
        It 'Should accept empty hashtable as Content' {
            { Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content @{} } | Should -Not -Throw
        }
        It 'Should throw if Content is not IDictionary' {
            { Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content @('Dude',"Where's",'My','Dictionary') } | Should -Throw
        }
        It 'Should accept null Content' {
            { Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content $null } | Should -Not -Throw
        }
        It 'Should throw with unexpected dictionary value type System.Array' {
            $IniContent = @{
                'ArrayKey' = @(1..10)
            }
            { Set-ADTIniSection -FilePath $IniPath -Section 'MySection' -Content $IniContent } | Should -Throw
        }
    }
}
