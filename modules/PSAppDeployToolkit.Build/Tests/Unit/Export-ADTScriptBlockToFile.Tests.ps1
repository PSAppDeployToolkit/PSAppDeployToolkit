BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    function Export-Probe
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [System.Management.Automation.ScriptBlock]$ScriptBlock,

            [Parameter(Mandatory = $true)]
            [System.String]$LiteralPath,

            [Parameter(Mandatory = $false)]
            [System.Management.Automation.SwitchParameter]$Force
        )

        # Splatted because the analyser and the formatter disagree over the casing of -ScriptBlock on a
        # function neither of them can resolve, and each keeps undoing the other.
        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Sb = $ScriptBlock; Path = $LiteralPath; UseForce = $Force } {
            $splat = @{ ScriptBlock = $Sb; LiteralPath = $Path; Force = $UseForce }
            Export-ADTScriptBlockToFile @splat
        }
    }
}

Describe 'Export-ADTScriptBlockToFile' {
    Context 'Functionality' {
        It 'Writes the scriptblock body to the given path' {
            $path = "$TestDrive\basic.ps1"
            Export-Probe -ScriptBlock { Write-Output 'hello' } -LiteralPath $path
            [System.IO.File]::ReadAllText($path).Trim() | Should -BeExactly "Write-Output 'hello'"
        }

        It 'Writes something that parses back to the same commands' {
            # The point of the export: the file has to be runnable, not merely a transcript.
            $path = "$TestDrive\roundtrip.ps1"
            Export-Probe -ScriptBlock { Write-Output 'a'; Write-Output 'b' } -LiteralPath $path
            & $path | Should -Be @('a', 'b')
        }

        It 'Removes the common leading indentation' {
            # A scriptblock written inside a function arrives indented, and the exported file should not
            # inherit that offset.
            # Built from a string rather than written inline, so the deliberately uneven indentation is not
            # something the repository's formatter would straighten out.
            $path = "$TestDrive\indent.ps1"
            Export-Probe -LiteralPath $path -ScriptBlock ([System.Management.Automation.ScriptBlock]::Create(
                    "`n                Write-Output 'a'`n                    Write-Output 'b'`n                Write-Output 'c'`n"
                ))

            $lines = [System.IO.File]::ReadAllLines($path)
            $lines[0] | Should -BeExactly "Write-Output 'a'"
            $lines[1] | Should -BeExactly "    Write-Output 'b'"
            $lines[2] | Should -BeExactly "Write-Output 'c'"
        }

        It 'Trims blank lines from the end' {
            $path = "$TestDrive\trailing.ps1"
            Export-Probe -LiteralPath $path -ScriptBlock ([System.Management.Automation.ScriptBlock]::Create("Write-Output 'a'`n`n`n"))
            [System.IO.File]::ReadAllLines($path)[-1] | Should -BeExactly "Write-Output 'a'"
        }

        It 'Writes UTF-8 with a byte order mark by default' {
            $path = "$TestDrive\encoding.ps1"
            Export-Probe -ScriptBlock { Write-Output 'a' } -LiteralPath $path
            $bytes = [System.IO.File]::ReadAllBytes($path)
            $bytes[0..2] | Should -Be @(0xEF, 0xBB, 0xBF)
        }

        It 'Rejects a scriptblock with nothing in it' {
            { Export-Probe -ScriptBlock { } -LiteralPath "$TestDrive\empty.ps1" } | Should -Throw
        }

        It 'Refuses to write over an existing file without -Force' {
            # The guard tested for a container, so it fired for a directory and never for a file. An
            # existing file was replaced without a word, and -Force had nothing to override.
            $path = "$TestDrive\existing.ps1"
            Export-Probe -ScriptBlock { Write-Output 'first' } -LiteralPath $path
            { Export-Probe -ScriptBlock { Write-Output 'second' } -LiteralPath $path } | Should -Throw -ErrorId 'LiteralPathAlreadyExists,*'
            [System.IO.File]::ReadAllText($path).Trim() | Should -BeExactly "Write-Output 'first'"
        }

        It 'Writes over an existing file when -Force is given' {
            $path = "$TestDrive\forced.ps1"
            Export-Probe -ScriptBlock { Write-Output 'first' } -LiteralPath $path
            Export-Probe -ScriptBlock { Write-Output 'second' } -LiteralPath $path -Force
            [System.IO.File]::ReadAllText($path).Trim() | Should -BeExactly "Write-Output 'second'"
        }

        It 'Still refuses a directory' {
            # The one case the old guard did catch, which the fix must not lose.
            $dir = "$TestDrive\adirectory"
            $null = New-Item -Path $dir -ItemType Directory
            { Export-Probe -ScriptBlock { Write-Output 'x' } -LiteralPath $dir } | Should -Throw -ErrorId 'LiteralPathAlreadyExists,*'
        }
    }
}
