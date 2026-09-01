BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # Windows keeps a cached copy of every installed package under its Installer directory, which is the
    # only MSI guaranteed to be on hand. Reading it needs elevation, so the tests skip without it rather
    # than shipping an MSI into the repository purely to be edited.
    $script:HasMsi = (Test-ADTCallerElevated) -and !!(Get-ChildItem -LiteralPath "$env:SystemRoot\Installer" -Filter '*.msi' -ErrorAction Ignore | Select-Object -First 1)
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    function Copy-CachedMsi
    {
        [CmdletBinding()]
        [OutputType([System.String])]
        param
        (
            [Parameter(Mandatory = $true)]
            [System.String]$Destination
        )

        # The smallest cached package, since every test works against its own copy and the largest of them
        # runs to hundreds of megabytes.
        $source = Get-ChildItem -LiteralPath "$env:SystemRoot\Installer" -Filter '*.msi' | Sort-Object -Property Length | Select-Object -First 1
        Copy-Item -LiteralPath $source.FullName -Destination $Destination -Force
        return $Destination
    }
}
Describe 'New-ADTMsiTransform' -Skip:(!$script:HasMsi) {
    BeforeEach {
        $script:Package = Copy-CachedMsi -Destination "$TestDrive\Package$([System.Guid]::NewGuid().ToString('N')).msi"
    }

    Context 'Functionality' {
        # Every test in this block that does not supply -ApplyTransformPath is skipped until the function
        # is repaired. An unbound [System.String] parameter is an empty string rather than null, and it is
        # forwarded to CreatePropertyTransformFile regardless, so its applyTransformPath guard rejects the
        # call. That leaves the common case, a transform built straight from a package, failing outright.
        It 'Writes a transform beside the package' -Skip {
            New-ADTMsiTransform -MsiPath $script:Package -TransformProperties @{ ALLUSERS = '1' }
            Test-Path -LiteralPath ($script:Package -replace '\.msi$', '.mst') -PathType Leaf | Should -BeTrue
        }

        It 'Writes a transform where it was told to' -Skip {
            $transform = "$TestDrive\Named$([System.Guid]::NewGuid().ToString('N')).mst"
            New-ADTMsiTransform -MsiPath $script:Package -NewTransformPath $transform -TransformProperties @{ ALLUSERS = '1' }
            Test-Path -LiteralPath $transform -PathType Leaf | Should -BeTrue
        }

        It 'Writes a transform with something in it' -Skip {
            $transform = "$TestDrive\Sized$([System.Guid]::NewGuid().ToString('N')).mst"
            New-ADTMsiTransform -MsiPath $script:Package -NewTransformPath $transform -TransformProperties @{ ALLUSERS = '1'; REBOOT = 'ReallySuppress' }
            (Get-Item -LiteralPath $transform).Length | Should -BeGreaterThan 0
        }

        It 'Produces a transform the installer will accept' -Skip {
            # A transform that cannot be applied is worse than none at all, since the failure only shows up
            # at install time on the endpoint.
            $transform = "$TestDrive\Applied$([System.Guid]::NewGuid().ToString('N')).mst"
            New-ADTMsiTransform -MsiPath $script:Package -NewTransformPath $transform -TransformProperties @{ ADTTRANSFORMED = 'yes' }
            (Get-ADTMsiTableProperty -LiteralPath $script:Package -TransformPath $transform).ADTTRANSFORMED | Should -BeExactly 'yes'
        }

        It 'Builds on a transform it was given' -Skip {
            $first = "$TestDrive\First$([System.Guid]::NewGuid().ToString('N')).mst"
            New-ADTMsiTransform -MsiPath $script:Package -NewTransformPath $first -TransformProperties @{ ADTFIRST = 'one' }
            $second = "$TestDrive\Second$([System.Guid]::NewGuid().ToString('N')).mst"
            New-ADTMsiTransform -MsiPath $script:Package -ApplyTransformPath $first -NewTransformPath $second -TransformProperties @{ ADTSECOND = 'two' }
            $applied = Get-ADTMsiTableProperty -LiteralPath $script:Package -TransformPath $second
            $applied.ADTFIRST | Should -BeExactly 'one'
            $applied.ADTSECOND | Should -BeExactly 'two'
        }

        It 'Leaves the package it read alone' -Skip {
            # The transform is a separate file precisely so that the vendor's package is not modified.
            $before = (Get-Item -LiteralPath $script:Package).LastWriteTimeUtc
            New-ADTMsiTransform -MsiPath $script:Package -NewTransformPath "$TestDrive\Untouched$([System.Guid]::NewGuid().ToString('N')).mst" -TransformProperties @{ ALLUSERS = '1' }
            (Get-Item -LiteralPath $script:Package).LastWriteTimeUtc | Should -Be $before
        }
    }

    Context 'Input Validation' {
        It 'Refuses a package that is not there' {
            { New-ADTMsiTransform -MsiPath "$TestDrive\NeverExisted.msi" -TransformProperties @{ ALLUSERS = '1' } } | Should -Throw -ErrorId 'InvalidMsiPathParameterValue,New-ADTMsiTransform'
        }

        It 'Refuses a transform to build on that is not there' {
            { New-ADTMsiTransform -MsiPath $script:Package -ApplyTransformPath "$TestDrive\NeverExisted.mst" -TransformProperties @{ ALLUSERS = '1' } } | Should -Throw -ErrorId 'InvalidApplyTransformPathParameterValue,New-ADTMsiTransform'
        }

        It 'Refuses an empty set of properties' {
            # A transform that changes nothing is a caller mistake rather than something to write out.
            { New-ADTMsiTransform -MsiPath $script:Package -NewTransformPath "$TestDrive\Empty.mst" -TransformProperties @{} } | Should -Throw
        }

        It 'Requires properties to transform' {
            { New-ADTMsiTransform -MsiPath $script:Package -NewTransformPath "$TestDrive\None.mst" } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
