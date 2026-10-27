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


#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $script:ModuleSource = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../src/Fluence.Wpf.PowerShell'))
    $script:ShellPath = (Get-Process -Id $PID).Path
    $script:ImportProbe = Join-Path $TestDrive 'probe.ps1'
    $probe = @'
param([string]$Manifest)
$ErrorActionPreference = 'Stop'
try
{
    $module = Import-Module -Name $Manifest -PassThru -Force -ErrorAction Stop
    [pscustomobject]@{
        Imported = $true
        Commands = @($module.ExportedFunctions.Keys)
        Sample = (New-FluencePrompt -Message 'probe').Message
    } | ConvertTo-Json -Compress
}
catch
{
    [pscustomobject]@{ Imported = $false; Error = $_.Exception.Message } | ConvertTo-Json -Compress
}
'@
    [System.IO.File]::WriteAllText($script:ImportProbe, $probe, [System.Text.UTF8Encoding]::new($true))
}

Describe 'Public module import in a fresh process' {
    It 'exports and executes public commands from a path containing brackets and spaces' {
        $package = Join-Path $TestDrive '[release] module'
        Copy-Item -LiteralPath $script:ModuleSource -Destination $package -Recurse
        $manifest = Join-Path $package 'Fluence.Wpf.PowerShell.psd1'
        $result = & $script:ShellPath -NoProfile -NonInteractive -File $script:ImportProbe -Manifest $manifest | ConvertFrom-Json
        $LASTEXITCODE | Should -Be 0
        $result.Imported | Should -BeTrue
        $result.Sample | Should -Be 'probe'
        $expected = Import-PowerShellDataFile -LiteralPath $manifest
        @($result.Commands).Count | Should -Be @($expected.FunctionsToExport).Count
        foreach ($command in $expected.FunctionsToExport)
        {
            $result.Commands | Should -Contain $command
        }
    }

    It 'fails import when the <Folder> script directory is <Condition>' -ForEach @(
        @{ Folder = 'Public'; Condition = 'missing' }
        @{ Folder = 'Private'; Condition = 'missing' }
        @{ Folder = 'Public'; Condition = 'empty' }
        @{ Folder = 'Private'; Condition = 'empty' }
    ) {
        $package = Join-Path $TestDrive ($Folder + '-' + $Condition)
        Copy-Item -LiteralPath $script:ModuleSource -Destination $package -Recurse
        $directory = Join-Path $package $Folder
        Move-Item -LiteralPath $directory -Destination ($package + '-removed')
        if ($Condition -eq 'empty')
        {
            $null = New-Item -ItemType Directory -Path $directory
        }
        $result = & $script:ShellPath -NoProfile -NonInteractive -File $script:ImportProbe -Manifest (Join-Path $package 'Fluence.Wpf.PowerShell.psd1') | ConvertFrom-Json
        $LASTEXITCODE | Should -Be 0
        $result.Imported | Should -BeFalse
        $result.Error | Should -Match $Folder
    }
}
