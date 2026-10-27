<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


Describe 'Pinned build-tool installation (<HostName>)' -ForEach @(
    @{ HostName = 'Windows PowerShell'; ModuleCount = 2 }
    @{ HostName = 'PowerShell 7'; ModuleCount = 3 }
) {
    BeforeAll {
        # PowerShellGet's dynamic parameters can bind before Pester's mock intercepts -Default.
        # A static test shim keeps these tests independent of the machine's repository state.
        function Register-CiTestRepository
        {
            [CmdletBinding()]
            param([switch]$Default)
            $null = $Default
        }
        Set-Alias -Name Register-PSRepository -Value Register-CiTestRepository
        $workflowPath = Join-Path $PSScriptRoot '../../.github/workflows/build.yml'
        $workflow = Get-Content -LiteralPath $workflowPath -Raw
        $stepName = [regex]::Escape("Install pinned build tools ($HostName)")
        $match = [regex]::Match($workflow, "(?ms)      - name: $stepName\r?`n.*?        run: \|\r?`n(?<Script>(?:          [^\r\n]*\r?`n)+)")
        $match.Success | Should -BeTrue
        $script:installTools = [scriptblock]::Create($match.Groups['Script'].Value)
    }

    BeforeEach {
        $script:pesterAttempts = 0
        Mock Get-PackageProvider { [pscustomobject]@{ Version = [version]'2.8.5.208' } }
        Mock Install-PackageProvider {}
        Mock Get-Module {}
        Mock Install-Module {}
        Mock Get-PSRepository { [pscustomobject]@{ Name = 'PSGallery' } }
        Mock Register-PSRepository {}
        Mock Start-Sleep {}
        Mock Write-Warning {}
    }

    It 'installs the pinned versions without delaying a successful attempt' {
        & $script:installTools

        Should -Invoke Install-Module -Times $ModuleCount -Exactly
        Should -Invoke Install-Module -Times 1 -Exactly -ParameterFilter {
            $Name -eq 'Pester' -and $RequiredVersion -eq '5.8.0' -and
            $Repository -eq 'PSGallery' -and $Scope -eq 'CurrentUser' -and
            $SkipPublisherCheck -and $Force -and $ErrorAction -eq 'Stop'
        }
        Should -Invoke Install-Module -Times 1 -Exactly -ParameterFilter {
            $Name -eq 'PSScriptAnalyzer' -and $RequiredVersion -eq '1.25.0' -and $ErrorAction -eq 'Stop'
        }
        if ($HostName -eq 'PowerShell 7')
        {
            Should -Invoke Install-Module -Times 1 -Exactly -ParameterFilter {
                $Name -eq 'Alt3.Docusaurus.Powershell' -and $RequiredVersion -eq '2.0.1' -and $ErrorAction -eq 'Stop'
            }
        }
        Should -Invoke Start-Sleep -Times 0 -Exactly
    }

    It 'registers the default gallery when a module is missing and PSGallery is absent' {
        $script:galleryRegistered = $false
        Mock Get-PSRepository { if ($script:galleryRegistered) { [pscustomobject]@{ Name = 'PSGallery' } } }
        Mock Register-PSRepository { $script:galleryRegistered = $true }

        & $script:installTools

        Should -Invoke Register-PSRepository -Times 1 -Exactly -ParameterFilter { $Default -and $ErrorAction -eq 'Stop' }
        Should -Invoke Install-Module -Times $ModuleCount -Exactly
        Should -Invoke Start-Sleep -Times 0 -Exactly
    }

    It 'retries a failed default gallery registration and then installs the pinned modules' {
        $script:registrationAttempts = 0
        $script:galleryRegistered = $false
        Mock Get-PSRepository { if ($script:galleryRegistered) { [pscustomobject]@{ Name = 'PSGallery' } } }
        Mock Register-PSRepository {
            $script:registrationAttempts++
            if ($script:registrationAttempts -eq 1) { throw 'Gallery registration failed' }
            $script:galleryRegistered = $true
        }

        & $script:installTools

        Should -Invoke Register-PSRepository -Times 2 -Exactly
        Should -Invoke Start-Sleep -Times 1 -Exactly -ParameterFilter { $Seconds -eq 15 }
        Should -Invoke Install-Module -Times $ModuleCount -Exactly
    }

    It 'fails without installing modules when default gallery registration repeatedly fails' {
        Mock Get-PSRepository {}
        Mock Register-PSRepository { throw 'Gallery registration failed' }

        { & $script:installTools } | Should -Throw '*Gallery registration failed*'

        Should -Invoke Register-PSRepository -Times 3 -Exactly
        Should -Invoke Install-Module -Times 0 -Exactly
        Should -Invoke Start-Sleep -Times 2 -Exactly -ParameterFilter { $Seconds -eq 15 }
    }

    It 'does not reinstall cached modules when the gallery needs registration' {
        Mock Get-PSRepository {}
        Mock Get-Module { [pscustomobject]@{ Version = [version]'1.25.0' } } -ParameterFilter { $Name -eq 'PSScriptAnalyzer' }

        & $script:installTools

        Should -Invoke Register-PSRepository -Times 1 -Exactly
        Should -Invoke Install-Module -Times 0 -Exactly -ParameterFilter { $Name -eq 'PSScriptAnalyzer' }
        Should -Invoke Install-Module -Times 1 -Exactly -ParameterFilter { $Name -eq 'Pester' }
    }

    It 'preserves an already registered gallery' {
        & $script:installTools

        Should -Invoke Get-PSRepository -Times 1 -Exactly -ParameterFilter { $Name -eq 'PSGallery' }
        Should -Invoke Register-PSRepository -Times 0 -Exactly
    }

    It 'recovers from two transient Pester download failures' {
        Mock Install-Module {
            $script:pesterAttempts++
            if ($script:pesterAttempts -lt 3)
            {
                throw 'Pester failed to download'
            }
        } -ParameterFilter { $Name -eq 'Pester' }

        & $script:installTools

        Should -Invoke Install-Module -Times 3 -Exactly -ParameterFilter { $Name -eq 'Pester' }
        Should -Invoke Start-Sleep -Times 2 -Exactly -ParameterFilter { $Seconds -eq 15 }
    }

    It 'fails with the original error after the third unsuccessful download' {
        Mock Install-Module { throw 'Pester failed to download' } -ParameterFilter { $Name -eq 'Pester' }

        { & $script:installTools } | Should -Throw '*Pester failed to download*'

        Should -Invoke Install-Module -Times 3 -Exactly -ParameterFilter { $Name -eq 'Pester' }
        Should -Invoke Start-Sleep -Times 2 -Exactly
    }

    It 'skips already installed pinned versions' {
        Mock Get-Module {
            [pscustomobject]@{ Version = [version]'1.25.0' }
            [pscustomobject]@{ Version = [version]'5.8.0' }
            [pscustomobject]@{ Version = [version]'2.0.1' }
        }

        & $script:installTools

        Should -Invoke Install-Module -Times 0 -Exactly
        Should -Invoke Install-PackageProvider -Times 0 -Exactly
        Should -Invoke Get-PSRepository -Times 0 -Exactly
        Should -Invoke Register-PSRepository -Times 0 -Exactly
        Should -Invoke Start-Sleep -Times 0 -Exactly
    }
}
