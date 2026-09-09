BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # The host running this test is guaranteed to be there, which a shell or explorer is not. Read once
    # rather than in each test, since every read is a Process object that has to be closed again.
    $script:SelfName = [System.IO.Path]::GetFileNameWithoutExtension((Get-ADTCallerProcessPath))

    function New-Definition
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [System.String]$Name
        )

        return [PSADT.ProcessManagement.ProcessDefinition]::new($Name)
    }
}

Describe 'Get-ADTRunningProcesses' {
    Context 'Functionality' {
        It 'Finds a process that is running' {
            $running = @(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $script:SelfName))
            $running.Count | Should -BeGreaterThan 0
            $running[0] | Should -BeOfType ([PSADT.ProcessManagement.RunningProcessInfo])
        }

        It 'Reports the process it found' {
            $running = @(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $script:SelfName))
            $running.Process.Id | Should -Contain $PID
        }

        It 'Fills in a description for what it found' {
            # The description is what a close-applications prompt shows the user, so an empty one would
            # leave them looking at a blank row.
            (@(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $script:SelfName))[0]).Description | Should -Not -BeNullOrEmpty
        }

        It 'Returns nothing when no such process is running' {
            Get-ADTRunningProcesses -ProcessObject (New-Definition -Name 'ADTNoSuchProcessIsRunning12345') | Should -BeNullOrEmpty
        }

        It 'Takes more than one definition at a time' {
            $running = @(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $script:SelfName), (New-Definition -Name 'ADTNoSuchProcessIsRunning12345'))
            $running.Count | Should -BeGreaterThan 0
        }

        It 'Takes the definition through its -ProcessObject alias' {
            # The parameter is named ProcessDefinition; the toolkit's own callers use the alias.
            @(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $script:SelfName)).Count | Should -BeGreaterThan 0
        }
    }
}
