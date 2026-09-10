BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # The name of somebody actually signed in, which is what -Username is resolved against. Not
    # $env:USERNAME: a process running as LocalSystem carries the machine account in that variable, and no
    # session belongs to the machine account, so the resolution finds nothing and the test fails against
    # its own premise rather than against the code.
    $script:Sessions = @(Get-ADTLoggedOnUser)
    $script:LoggedOnUserName = $script:Sessions | & { process { if ($_.IsCurrentSession) { return $_ } } } | Select-Object -First 1 -ExpandProperty UserName
    if ([System.String]::IsNullOrWhiteSpace($script:LoggedOnUserName))
    {
        $script:LoggedOnUserName = $script:Sessions | Select-Object -First 1 -ExpandProperty UserName
    }

    function Invoke-Probe
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [System.Collections.Hashtable]$BoundParameters,

            [Parameter(Mandatory = $false)]
            [System.Management.Automation.SwitchParameter]$NoUser
        )

        $dictionary = [System.Collections.Generic.Dictionary[System.String, System.Object]]::new()
        foreach ($pair in $BoundParameters.GetEnumerator())
        {
            $dictionary.Add($pair.Key, $pair.Value)
        }

        $result = InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dict = $dictionary; Cmdlet = $PSCmdlet; Empty = $NoUser.IsPresent } {
            if ($Empty)
            {
                # Stands in for a machine with nobody logged on, which is otherwise not reproducible here.
                Mock Get-ADTClientServerUser { }
            }
            Update-ADTProcessAsUserBoundParameters -Cmdlet $Cmdlet -BoundParameters $Dict
        }
        return [PSCustomObject]@{ Result = $result; BoundParameters = $dictionary }
    }
}

Describe 'Update-ADTProcessAsUserBoundParameters' {
    Context 'Functionality' {
        It 'Adds the resolved user to the bound parameters' {
            $probe = Invoke-Probe -BoundParameters @{ FilePath = 'cmd.exe' }
            $probe.Result | Should -BeTrue
            $probe.BoundParameters['RunAsActiveUser'] | Should -Not -BeNullOrEmpty
        }

        It 'Removes the parameters the subsystem does not take' {
            # -Username and -ContinueWhenNoUserLoggedOn are translated away, because what the subsystem
            # wants is the resolved user object rather than either of them.
            $probe = Invoke-Probe -BoundParameters @{ FilePath = 'cmd.exe'; Username = $script:LoggedOnUserName; ContinueWhenNoUserLoggedOn = $true }
            $probe.BoundParameters.ContainsKey('Username') | Should -BeFalse
            $probe.BoundParameters.ContainsKey('ContinueWhenNoUserLoggedOn') | Should -BeFalse
        }

        It 'Leaves the caller''s other parameters alone' {
            $probe = Invoke-Probe -BoundParameters @{ FilePath = 'cmd.exe'; ArgumentList = '/c exit 0' }
            $probe.BoundParameters['FilePath'] | Should -BeExactly 'cmd.exe'
            $probe.BoundParameters['ArgumentList'] | Should -BeExactly '/c exit 0'
        }

        It 'Resolves the named user when -Username is supplied' {
            $probe = Invoke-Probe -BoundParameters @{ FilePath = 'cmd.exe'; Username = $script:LoggedOnUserName }
            $probe.Result | Should -BeTrue
            $probe.BoundParameters['RunAsActiveUser'].UserName | Should -BeExactly $script:LoggedOnUserName
        }

        It 'Returns false without erroring when nobody is logged on and the caller allows it' {
            $probe = Invoke-Probe -BoundParameters @{ FilePath = 'cmd.exe'; ContinueWhenNoUserLoggedOn = $true } -NoUser
            $probe.Result | Should -BeFalse
        }

        It 'Errors when nobody is logged on and the caller did not allow it' {
            { Invoke-Probe -BoundParameters @{ FilePath = 'cmd.exe' } -NoUser -ErrorAction Stop } | Should -Throw
        }
    }
}
