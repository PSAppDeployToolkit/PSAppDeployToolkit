BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Remove-ADTHashtableNullOrEmptyValues' {
    BeforeAll {
        function Compare-ADTHashtable
        {
            [CmdletBinding()]
            [OutputType([System.Boolean])]
            param (
                [Parameter(Mandatory = $true, ValueFromPipeline = $true, Position = 0)]
                [System.Collections.Hashtable]$Left,

                [Parameter(Mandatory = $true, Position = 1)]
                [System.Collections.Hashtable]$Right
            )

            process
            {
                if ($Left.Keys.Count -eq 0)
                {
                    if ($Right.Keys.Count -eq 0)
                    {
                        return $true
                    }

                    return $false
                }

                foreach ($section in $Left.GetEnumerator())
                {
                    if (-not $right.ContainsKey($section.Key))
                    {
                        return $false
                    }

                    if ($section.Value -is [System.Collections.Hashtable])
                    {

                        return (& $MyInvocation.MyCommand -Left $section.Value -Right $Right.($section.Key))
                    }

                    if ($section.Value -ne $Right.($section.Key))
                    {
                        return $false
                    }

                    return $true
                }
            }
        }

        $TestData = @{
            Empty = ''
            EmptyCollection = @()
            Null = $null
            NullString = [System.Management.Automation.Language.NullString]::Value
            Whitespace = " `f`n`r`t`v"

            Layer1 = @{
                Empty = ''
                EmptyCollection = @()
                Null = $null
                NullString = [System.Management.Automation.Language.NullString]::Value
                Whitespace = " `f`n`r`t`v"

                Layer2 = @{
                    Layer3 = @{
                        Layer4 = @{
                            Empty = ''
                            EmptyCollection = @()
                            Null = $null
                            NullString = [System.Management.Automation.Language.NullString]::Value
                            Whitespace = " `f`n`r`t`v"

                            Layer5 = @{
                                Empty = ''
                                EmptyCollection = @()
                                Null = $null
                                NullString = [System.Management.Automation.Language.NullString]::Value
                                Whitespace = " `f`n`r`t`v"
                            }
                        }
                    }
                }
            }
        }

        $NoRecurse = $TestData.Clone()
        $NoRecurse.Remove('Empty')
        $NoRecurse.Remove('EmptyCollection')
        $NoRecurse.Remove('Null')
        $NoRecurse.Remove('NullString')
        $NoRecurse.Remove('Whitespace')

        $Recurse5 = $NoRecurse.Clone()
        $Recurse5.Layer1.Layer2.Layer3.Layer4.Remove('Empty')
        $Recurse5.Layer1.Layer2.Layer3.Layer4.Remove('EmptyCollection')
        $Recurse5.Layer1.Layer2.Layer3.Layer4.Remove('Null')
        $Recurse5.Layer1.Layer2.Layer3.Layer4.Remove('NullString')
        $Recurse5.Layer1.Layer2.Layer3.Layer4.Remove('Whitespace')
    }

    Context 'Functionality' {
        It 'Should return remove null values' {
            Remove-ADTHashtableNullOrEmptyValues -Hashtable $TestData | Compare-ADTHashtable -Right $NoRecurse | Should -BeTrue
            Remove-ADTHashtableNullOrEmptyValues -Hashtable $TestData -Recurse -Depth 1 | Compare-ADTHashtable -Right $NoRecurse | Should -BeTrue
        }
        It 'Should return remove null values recursively' {
            Remove-ADTHashtableNullOrEmptyValues -Hashtable $TestData -Recurse -Depth 5 | Compare-ADTHashtable -Right $Recurse5 | Should -BeTrue

            Remove-ADTHashtableNullOrEmptyValues -Hashtable $TestData -Recurse -Depth 6 | Compare-ADTHashtable -Right @{ } | Should -BeTrue
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Depth is a positive Int32' {
            { Remove-ADTHashtableNullOrEmptyValues -Hashtable @{ } -Recurse -Depth -1 } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,Remove-ADTHashtableNullOrEmptyValues'
            { Remove-ADTHashtableNullOrEmptyValues -Hashtable @{ } -Recurse -Depth ([System.Int32]::MaxValue + 1) } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentTransformationError,Remove-ADTHashtableNullOrEmptyValues'
        }
    }
}
