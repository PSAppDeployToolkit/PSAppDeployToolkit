BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    function Get-LogText
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [System.String]$Path
        )

        return [System.IO.File]::ReadAllText($Path)
    }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Write-ADTLogEntry' {
    Context 'Without a session' {
        It 'Writes to the file it is given' {
            # The toolkit's own functions log before a session exists, and so do callers using the module
            # purely for its logging, so this path has to stand on its own.
            Write-ADTLogEntry -Message 'plain message' -LogFileDirectory "$TestDrive\NoSession" -LogFileName 'plain.log'
            Get-LogText -Path "$TestDrive\NoSession\plain.log" | Should -BeLike '*plain message*'
        }

        It 'Creates the directory it was pointed at' {
            Write-ADTLogEntry -Message 'creates directory' -LogFileDirectory "$TestDrive\Created\Deeper" -LogFileName 'made.log'
            Test-Path -LiteralPath "$TestDrive\Created\Deeper\made.log" -PathType Leaf | Should -BeTrue
        }

        It 'Appends rather than replacing' {
            Write-ADTLogEntry -Message 'first line' -LogFileDirectory "$TestDrive\Append" -LogFileName 'append.log'
            Write-ADTLogEntry -Message 'second line' -LogFileDirectory "$TestDrive\Append" -LogFileName 'append.log'
            $text = Get-LogText -Path "$TestDrive\Append\append.log"
            $text | Should -BeLike '*first line*'
            $text | Should -BeLike '*second line*'
        }

        It 'Writes each of several messages' {
            Write-ADTLogEntry -Message 'one', 'two', 'three' -LogFileDirectory "$TestDrive\Many" -LogFileName 'many.log'
            @([System.IO.File]::ReadAllLines("$TestDrive\Many\many.log")).Count | Should -Be 3
        }

        It 'Accepts messages from the pipeline' {
            'piped one', 'piped two' | Write-ADTLogEntry -LogFileDirectory "$TestDrive\Piped" -LogFileName 'piped.log'
            @([System.IO.File]::ReadAllLines("$TestDrive\Piped\piped.log")).Count | Should -Be 2
        }
    }

    Context 'Formatting' {
        It 'Writes CMTrace format by default' {
            # CMTrace is the configured default, and its shape is what the log viewer parses.
            # Matched as an escaped regex rather than a wildcard, because the CMTrace prefix is made of the
            # very brackets -BeLike treats as a character class.
            Write-ADTLogEntry -Message 'default style' -LogFileDirectory "$TestDrive\Fmt" -LogFileName 'default.log'
            Get-LogText -Path "$TestDrive\Fmt\default.log" | Should -Match ([System.Text.RegularExpressions.Regex]::Escape('<![LOG[default style]LOG]!>'))
        }

        It 'Writes the legacy format when asked' {
            Write-ADTLogEntry -Message 'legacy style' -LogFileDirectory "$TestDrive\Fmt" -LogFileName 'legacy.log' -LogStyle Legacy
            Get-LogText -Path "$TestDrive\Fmt\legacy.log" | Should -BeLike '*] :: legacy style*'
        }

        It 'Records <Severity> as type <CMTraceType>' -ForEach @(
            @{ Severity = 'Info'; CMTraceType = 1 }
            @{ Severity = 'Warning'; CMTraceType = 2 }
            @{ Severity = 'Error'; CMTraceType = 3 }
        ) {
            # CMTrace colours a line by this number, so a severity mapped to the wrong one would show an
            # error as ordinary output.
            Write-ADTLogEntry -Message "severity $Severity" -Severity $Severity -LogFileDirectory "$TestDrive\Sev" -LogFileName "$Severity.log"
            Get-LogText -Path "$TestDrive\Sev\$Severity.log" | Should -BeLike "*type=`"$CMTraceType`"*"
        }

        It 'Names the source it is told to' {
            Write-ADTLogEntry -Message 'sourced' -Source 'MyOwnSource' -LogFileDirectory "$TestDrive\Src" -LogFileName 'src.log'
            Get-LogText -Path "$TestDrive\Src\src.log" | Should -BeLike '*MyOwnSource*'
        }

        It 'Includes the script section it is told to' {
            Write-ADTLogEntry -Message 'sectioned' -ScriptSection 'MySection' -LogStyle Legacy -LogFileDirectory "$TestDrive\Sec" -LogFileName 'sec.log'
            Get-LogText -Path "$TestDrive\Sec\sec.log" | Should -BeLike '*MySection*'
        }
    }

    Context 'Pass through' {
        It 'Returns nothing unless asked' {
            Write-ADTLogEntry -Message 'quiet' -LogFileDirectory "$TestDrive\Pt" -LogFileName 'quiet.log' | Should -BeNullOrEmpty
        }

        It 'Returns a log entry with -PassThru' {
            $entry = Write-ADTLogEntry -Message 'passed through' -LogFileDirectory "$TestDrive\Pt" -LogFileName 'pt.log' -PassThru
            $entry | Should -BeOfType ([PSAppDeployToolkit.Logging.LogEntry])
            $entry.Message | Should -BeExactly 'passed through'
        }

        It 'Carries both rendered forms on the entry' {
            # Callers hooking OnLogEntry re-emit the line elsewhere, so both renderings travel with it
            # rather than having to be rebuilt.
            $entry = Write-ADTLogEntry -Message 'both forms' -LogFileDirectory "$TestDrive\Pt" -LogFileName 'both.log' -PassThru
            $entry.CMTraceLogLine | Should -Match ([System.Text.RegularExpressions.Regex]::Escape('<![LOG[both forms]LOG]!>'))
            $entry.LegacyLogLine | Should -BeLike '*:: both forms'
        }

        It 'Returns one entry per message' {
            @(Write-ADTLogEntry -Message 'a', 'b' -LogFileDirectory "$TestDrive\Pt" -LogFileName 'multi.log' -PassThru).Count | Should -Be 2
        }
    }

    Context 'With a session' {
        BeforeAll {
            $script:Session = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'WriteLogProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            $script:SessionLog = (Get-ChildItem -LiteralPath (Get-ADTConfig).Toolkit.LogPath -Recurse -File -Filter '*WriteLogProbe*' | Select-Object -First 1).FullName
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Writes into the session log without being told where' {
            Write-ADTLogEntry -Message 'session routed'
            Get-LogText -Path $script:SessionLog | Should -BeLike '*session routed*'
        }

        It 'Tags the line with the session install phase' {
            Write-ADTLogEntry -Message 'phase tagged'
            Get-LogText -Path $script:SessionLog | Should -Match ([System.Text.RegularExpressions.Regex]::Escape("[$($script:Session.InstallPhase)] :: phase tagged"))
        }

        It 'Fires the log entry callbacks' {
            # An extension watching OnLogEntry mirrors the deployment's log somewhere else, so every entry
            # has to reach it.
            $script:Seen = [System.Collections.Generic.List[System.String]]::new()
            function Test-LogCallback
            {
                param
                (
                    [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
                    [PSAppDeployToolkit.Logging.LogEntry]$LogEntry
                )

                process
                {
                    $script:Seen.Add($LogEntry.Message)
                }
            }
            Add-ADTModuleCallback -Hookpoint OnLogEntry -Callback (Get-Command Test-LogCallback)
            try
            {
                Write-ADTLogEntry -Message 'callback watched'
                $script:Seen | Should -Contain 'callback watched'
            }
            finally
            {
                Clear-ADTModuleCallback -Hookpoint OnLogEntry
            }
        }

        It 'Keeps a debug message out of the log unless debugging is on' {
            Write-ADTLogEntry -Message 'hidden debug line' -DebugMessage
            Get-LogText -Path $script:SessionLog | Should -Not -BeLike '*hidden debug line*'
        }
    }

    Context 'Input Validation' {
        It 'Rejects being given no messages' {
            { Write-ADTLogEntry -Message @() -LogFileDirectory "$TestDrive\None" -LogFileName 'none.log' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Write-ADTLogEntry'
        }

        It 'Rejects a repeated message' {
            # ValidateUnique on the parameter, so the same line twice in one call is refused rather than
            # written out twice.
            { Write-ADTLogEntry -Message 'same', 'same' -LogFileDirectory "$TestDrive\None" -LogFileName 'dupe.log' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Rejects a severity it does not know' {
            { Write-ADTLogEntry -Message 'bad severity' -Severity 'Catastrophic' -LogFileDirectory "$TestDrive\None" -LogFileName 'sev.log' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
