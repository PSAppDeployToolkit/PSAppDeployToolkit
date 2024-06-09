Function Invoke-RegisterOrUnregisterDLL {
    <#
.SYNOPSIS

Register or unregister a DLL file.

.DESCRIPTION

Register or unregister a DLL file using regsvr32.exe. Function can be invoked using alias: 'Register-DLL' or 'Unregister-DLL'.

.PARAMETER FilePath

Path to the DLL file.

.PARAMETER DLLAction

Specify whether to register or unregister the DLL. Optional if function is invoked using 'Register-DLL' or 'Unregister-DLL' alias.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return objects.

.EXAMPLE

Register-DLL -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

Register DLL file using the "Register-DLL" alias for this function

.EXAMPLE

UnRegister-DLL -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

Unregister DLL file using the "Unregister-DLL" alias for this function

.EXAMPLE

Invoke-RegisterOrUnregisterDLL -FilePath "C:\Test\DcTLSFileToDMSComp.dll" -DLLAction 'Register'

Register DLL file using the actual name of this function

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$FilePath,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Register', 'Unregister')]
        [String]$DLLAction,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        $adtEnv = Get-ADTEnvironment
        Write-ADTDebugHeader

        ## Get name used to invoke this function in case the 'Register-DLL' or 'Unregister-DLL' alias was used and set the correct DLL action
        [String]${InvokedCmdletName} = $MyInvocation.InvocationName
        #  Set the correct register/unregister action based on the alias used to invoke this function
        If (${InvokedCmdletName} -ne ${CmdletName}) {
            Switch (${InvokedCmdletName}) {
                'Register-DLL' {
                    [String]$DLLAction = 'Register'
                }
                'Unregister-DLL' {
                    [String]$DLLAction = 'Unregister'
                }
            }
        }
        #  Set the correct DLL register/unregister action parameters
        If (-not $DLLAction) {
            Throw 'Parameter validation failed. Please specify the [-DLLAction] parameter to determine whether to register or unregister the DLL.'
        }
        [String]$DLLAction = $adtEnv.culture.TextInfo.ToTitleCase($DLLAction.ToLower())
        Switch ($DLLAction) {
            'Register' {
                [String]$DLLActionParameters = "/s `"$FilePath`""
            }
            'Unregister' {
                [String]$DLLActionParameters = "/s /u `"$FilePath`""
            }
        }
    }
    Process {
        Try {
            Write-ADTLogEntry -Message "$DLLAction DLL file [$filePath]."
            If (-not (Test-Path -LiteralPath $FilePath -PathType 'Leaf')) {
                Throw "File [$filePath] could not be found."
            }

            [String]$DLLFileBitness = Get-ADTPEFileArchitecture -FilePath $filePath
            If (($DLLFileBitness -ne '64BIT') -and ($DLLFileBitness -ne '32BIT')) {
                Throw "File [$filePath] has a detected file architecture of [$DLLFileBitness]. Only 32-bit or 64-bit DLL files can be $($DLLAction.ToLower() + 'ed')."
            }

            If ($adtEnv.Is64Bit) {
                If ($DLLFileBitness -eq '64BIT') {
                    If ($adtEnv.Is64BitProcess) {
                        [String]$RegSvr32Path = "$env:WinDir\System32\regsvr32.exe"
                    }
                    Else {
                        [String]$RegSvr32Path = "$env:WinDir\Sysnative\regsvr32.exe"
                    }
                }
                ElseIf ($DLLFileBitness -eq '32BIT') {
                    [String]$RegSvr32Path = "$env:WinDir\SysWOW64\regsvr32.exe"
                }
            }
            Else {
                If ($DLLFileBitness -eq '64BIT') {
                    Throw "File [$filePath] cannot be $($DLLAction.ToLower()) because it is a 64-bit file on a 32-bit operating system."
                }
                ElseIf ($DLLFileBitness -eq '32BIT') {
                    [String]$RegSvr32Path = "$env:WinDir\System32\regsvr32.exe"
                }
            }

            [PSObject]$ExecuteResult = Execute-Process -Path $RegSvr32Path -Parameters $DLLActionParameters -WindowStyle 'Hidden' -PassThru -ExitOnProcessFailure $false

            If ($ExecuteResult.ExitCode -ne 0) {
                If ($ExecuteResult.ExitCode -eq 60002) {
                    Throw "Execute-Process function failed with exit code [$($ExecuteResult.ExitCode)]."
                }
                Else {
                    Throw "regsvr32.exe failed with exit code [$($ExecuteResult.ExitCode)]."
                }
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to $($DLLAction.ToLower()) DLL file. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to $($DLLAction.ToLower()) DLL file: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
