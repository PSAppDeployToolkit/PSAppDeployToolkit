BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

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
            # The host running this test is guaranteed to be there, which a shell or explorer is not.
            $self = [System.IO.Path]::GetFileNameWithoutExtension([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)
            $running = @(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $self))
            $running.Count | Should -BeGreaterThan 0
            $running[0] | Should -BeOfType ([PSADT.ProcessManagement.RunningProcessInfo])
        }

        It 'Reports the process it found' {
            $self = [System.IO.Path]::GetFileNameWithoutExtension([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)
            $running = @(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $self))
            $running.Process.Id | Should -Contain ([System.Diagnostics.Process]::GetCurrentProcess().Id)
        }

        It 'Fills in a description for what it found' {
            # The description is what a close-applications prompt shows the user, so an empty one would
            # leave them looking at a blank row.
            $self = [System.IO.Path]::GetFileNameWithoutExtension([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)
            (@(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $self))[0]).Description | Should -Not -BeNullOrEmpty
        }

        It 'Returns nothing when no such process is running' {
            Get-ADTRunningProcesses -ProcessObject (New-Definition -Name 'ADTNoSuchProcessIsRunning12345') | Should -BeNullOrEmpty
        }

        It 'Takes more than one definition at a time' {
            $self = [System.IO.Path]::GetFileNameWithoutExtension([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)
            $running = @(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $self), (New-Definition -Name 'ADTNoSuchProcessIsRunning12345'))
            $running.Count | Should -BeGreaterThan 0
        }

        It 'Takes the definition through its -ProcessObject alias' {
            # The parameter is named ProcessDefinition; the toolkit's own callers use the alias.
            $self = [System.IO.Path]::GetFileNameWithoutExtension([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)
            @(Get-ADTRunningProcesses -ProcessObject (New-Definition -Name $self)).Count | Should -BeGreaterThan 0
        }
    }
}
