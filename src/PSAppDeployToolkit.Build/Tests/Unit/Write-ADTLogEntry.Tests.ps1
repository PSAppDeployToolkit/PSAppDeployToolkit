BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Write-ADTLogEntry' {
    BeforeAll {
        # Write-ADTLogEntry is the function under test, so it is deliberately NOT mocked here.
        # All output is redirected to a fresh directory under $TestDrive and asserted against the
        # real on-disk file contents. -LogStyle is always supplied so the function never falls into
        # the lazy Initialize-ADTModule branch and never touches the module's default log location.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'LogDir', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $LogDir = "$TestDrive\Logs"
    }

    BeforeEach {
        # Recreate a clean log directory for every test for full isolation.
        if (Test-Path -LiteralPath $LogDir)
        {
            Remove-Item -LiteralPath $LogDir -Recurse -Force
        }
        New-Item -Path $LogDir -ItemType Directory -Force | Out-Null
    }

    AfterAll {
        if (Test-Path -LiteralPath $LogDir)
        {
            Remove-Item -LiteralPath $LogDir -Recurse -Force
        }
    }

    Context 'Parameter contract' {
        It 'declares Message as a mandatory parameter' {
            (Get-Command Write-ADTLogEntry).Parameters['Message'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'accepts Message from the pipeline' {
            (Get-Command Write-ADTLogEntry).Parameters['Message'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).ValueFromPipeline | Should -Contain $true
        }

        It 'exposes LogType as an alias for LogStyle' {
            (Get-Command Write-ADTLogEntry).Parameters['LogStyle'].Aliases | Should -Contain 'LogType'
        }
    }

    Context 'CMTrace log style' {
        It 'wraps the message in the CMTrace LOG envelope' {
            Write-ADTLogEntry -Message 'Installing patch MS15-031' -Source 'Add-Patch' -LogStyle CMTrace -LogFileDirectory $LogDir -LogFileName 'cm.log'
            $line = Get-Content -LiteralPath "$LogDir\cm.log" -Raw
            $line | Should -Match '<!\[LOG\[Installing patch MS15-031\]LOG\]!>'
        }

        It 'records the source in the component attribute' {
            Write-ADTLogEntry -Message 'component test' -Source 'MyComponent' -LogStyle CMTrace -LogFileDirectory $LogDir -LogFileName 'cm.log'
            (Get-Content -LiteralPath "$LogDir\cm.log" -Raw) | Should -Match 'component="MyComponent"'
        }

        It 'prefixes the script section inside the LOG envelope' {
            Write-ADTLogEntry -Message 'sectioned' -Source 'S' -ScriptSection 'Installation' -LogStyle CMTrace -LogFileDirectory $LogDir -LogFileName 'cm.log'
            (Get-Content -LiteralPath "$LogDir\cm.log" -Raw) | Should -Match '<!\[LOG\[\[Installation\] :: sectioned\]LOG\]!>'
        }

        It 'preserves newlines for a multi-line message within a single LOG envelope' {
            Write-ADTLogEntry -Message "first line`nsecond line" -Source 'S' -LogStyle CMTrace -LogFileDirectory $LogDir -LogFileName 'cm.log'
            $raw = Get-Content -LiteralPath "$LogDir\cm.log" -Raw
            $raw | Should -Match '(?s)<!\[LOG\[first line.*second line.*\]LOG\]!>'
            # The whole multi-line message must remain a single CMTrace record (one envelope).
            ([System.Text.RegularExpressions.Regex]::Matches($raw, '<!\[LOG\[')).Count | Should -Be 1
        }
    }

    Context 'Legacy log style' {
        It 'writes the [date] [source] [severity] :: message shape' {
            Write-ADTLogEntry -Message 'legacy body' -Source 'LegSource' -Severity Warning -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'leg.log'
            $line = Get-Content -LiteralPath "$LogDir\leg.log"
            $line | Should -Match '^\[[^\]]+\] \[LegSource\] \[Warning\] :: legacy body$'
        }

        It 'includes the script section as a bracketed token before the source' {
            Write-ADTLogEntry -Message 'with section' -Source 'S' -ScriptSection 'Prep' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'leg.log'
            (Get-Content -LiteralPath "$LogDir\leg.log") | Should -Match '\[Prep\] \[S\] \[Info\] :: with section'
        }

        It 'omits the script section token when none is supplied' {
            Write-ADTLogEntry -Message 'no section' -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'leg.log'
            (Get-Content -LiteralPath "$LogDir\leg.log") | Should -Match '^\[[^\]]+\] \[S\] \[Info\] :: no section$'
        }
    }

    Context 'Severity mapping' {
        # Legacy style emits the severity enum NAME; CMTrace emits the numeric type token.
        # LogSeverity: Success=0, Info=1, Warning=2, Error=3.
        It 'writes the <Name> severity token in legacy style for <Name>' -ForEach @(
            @{ Name = 'Success' }
            @{ Name = 'Info' }
            @{ Name = 'Warning' }
            @{ Name = 'Error' }
        ) {
            Write-ADTLogEntry -Message "sev $Name" -Source 'S' -Severity $Name -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'sev.log'
            (Get-Content -LiteralPath "$LogDir\sev.log") | Should -Match "\[$Name\] :: sev $Name"
        }

        It 'writes type="<Type>" in CMTrace style for <Name>' -ForEach @(
            @{ Name = 'Success'; Type = 0 }
            @{ Name = 'Info'; Type = 1 }
            @{ Name = 'Warning'; Type = 2 }
            @{ Name = 'Error'; Type = 3 }
        ) {
            Write-ADTLogEntry -Message "sev $Name" -Source 'S' -Severity $Name -LogStyle CMTrace -LogFileDirectory $LogDir -LogFileName 'sev.log'
            (Get-Content -LiteralPath "$LogDir\sev.log") | Should -Match "type=`"$Type`""
        }

        It 'defaults to Info severity when none is specified' {
            Write-ADTLogEntry -Message 'default sev' -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'sev.log'
            (Get-Content -LiteralPath "$LogDir\sev.log") | Should -Match '\[Info\] :: default sev'
        }
    }

    Context 'Message content round-trips' {
        It 'writes one line per element for an array of messages' {
            Write-ADTLogEntry -Message @('lineA', 'lineB', 'lineC') -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'arr.log'
            $lines = @(Get-Content -LiteralPath "$LogDir\arr.log")
            $lines.Count | Should -Be 3
            $lines[0] | Should -Match ':: lineA$'
            $lines[1] | Should -Match ':: lineB$'
            $lines[2] | Should -Match ':: lineC$'
        }

        It 'writes one line per element for messages supplied via the pipeline' {
            'pipeA', 'pipeB' | Write-ADTLogEntry -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'pipe.log'
            $lines = @(Get-Content -LiteralPath "$LogDir\pipe.log")
            $lines.Count | Should -Be 2
            $lines[0] | Should -Match ':: pipeA$'
            $lines[1] | Should -Match ':: pipeB$'
        }

        It 'appends to an existing log file rather than overwriting it' {
            Write-ADTLogEntry -Message 'one' -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'app.log'
            Write-ADTLogEntry -Message 'two' -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'app.log'
            $lines = @(Get-Content -LiteralPath "$LogDir\app.log")
            $lines.Count | Should -Be 2
            $lines[0] | Should -Match ':: one$'
            $lines[1] | Should -Match ':: two$'
        }

        It 'writes the log file with a UTF-8 byte-order mark' {
            Write-ADTLogEntry -Message 'bom check' -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'bom.log'
            $bytes = [System.IO.File]::ReadAllBytes("$LogDir\bom.log")
            ('{0:x2}{1:x2}{2:x2}' -f $bytes[0], $bytes[1], $bytes[2]) | Should -Be 'efbbbf'
        }

        It 'creates the log file directory when it does not already exist' {
            $nested = "$LogDir\Created\Nested"
            Test-Path -LiteralPath $nested | Should -BeFalse
            Write-ADTLogEntry -Message 'made dir' -Source 'S' -LogStyle Legacy -LogFileDirectory $nested -LogFileName 'm.log'
            Test-Path -LiteralPath "$nested\m.log" | Should -BeTrue
        }
    }

    Context 'PassThru behaviour' {
        It 'returns nothing by default' {
            $result = Write-ADTLogEntry -Message 'silent' -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'pt.log'
            $result | Should -BeNullOrEmpty
        }

        It 'returns a LogEntry object when -PassThru is specified' {
            $result = Write-ADTLogEntry -Message 'passed through' -Source 'PtSource' -Severity Warning -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'pt.log' -PassThru
            $result | Should -BeOfType ([PSAppDeployToolkit.Logging.LogEntry])
            $result.Message | Should -Be 'passed through'
            $result.Source | Should -Be 'PtSource'
            $result.Severity | Should -Be ([PSAppDeployToolkit.Logging.LogSeverity]::Warning)
        }
    }

    Context 'Debug message suppression' {
        It 'does not write a debug message to disk when LogDebugMessage is not enabled' {
            Write-ADTLogEntry -Message 'debug only' -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'dbg.log' -DebugMessage
            Test-Path -LiteralPath "$LogDir\dbg.log" | Should -BeFalse
        }
    }

    Context 'Input validation' {
        It 'throws a validation error for a <Label> message' -ForEach @(
            @{ Label = 'null'; Value = $null }
            @{ Label = 'empty'; Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw = $true
                ErrorId = 'ParameterArgumentValidationError,Write-ADTLogEntry'
            }
            { Write-ADTLogEntry -Message $Value -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'v.log' } | Should @shouldParams
        }

        It 'throws a validation error for duplicate messages (ValidateUnique)' {
            $shouldParams = @{
                Throw = $true
                ErrorId = 'ParameterArgumentValidationError,Write-ADTLogEntry'
            }
            { Write-ADTLogEntry -Message @('dup', 'dup') -Source 'S' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'v.log' } | Should @shouldParams
        }

        It 'throws a validation error for a whitespace source' {
            $shouldParams = @{
                Throw = $true
                ErrorId = 'ParameterArgumentValidationError,Write-ADTLogEntry'
            }
            { Write-ADTLogEntry -Message 'm' -Source '   ' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'v.log' } | Should @shouldParams
        }

        It 'throws a transformation error for an invalid severity value' {
            $shouldParams = @{
                Throw = $true
                ErrorId = 'ParameterArgumentTransformationError,Write-ADTLogEntry'
            }
            { Write-ADTLogEntry -Message 'm' -Source 'S' -Severity 'Bogus' -LogStyle Legacy -LogFileDirectory $LogDir -LogFileName 'v.log' } | Should @shouldParams
        }
    }
}
