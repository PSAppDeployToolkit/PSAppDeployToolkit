function Invoke-ADTDllFileAction
{
    <#

    .SYNOPSIS
    Register or unregister a DLL file.

    .DESCRIPTION
    Register or unregister a DLL file using regsvr32.exe.

    .PARAMETER FilePath
    Path to the DLL file.

    .PARAMETER DLLAction
    Specify whether to register or unregister the DLL.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return objects.

    .EXAMPLE
    # Register DLL file.
    Invoke-ADTDllFileAction -FilePath "C:\Test\DcTLSFileToDMSComp.dll" -DLLAction 'Register'

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
            if (![System.IO.File]::Exists($_))
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'The specified file does not exist.'))
            }
            return !!$_
        })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Register', 'Unregister')]
        [System.String]$DLLAction
    )

    begin {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction Continue

        # Define parameters to pass to regsrv32.exe.
        $DLLActionParameters = switch ($DLLAction = $Host.CurrentCulture.TextInfo.ToTitleCase($DLLAction.ToLower()))
        {
            Register {
                "/s `"$FilePath`""
            }
            Unregister {
                "/s /u `"$FilePath`""
            }
        }
    }

    process {
        try
        {
            # Determine the bitness of the DLL file.
            Write-ADTLogEntry -Message "$DLLAction DLL file [$FilePath]."
            if ((($DLLFileBitness = Get-ADTPEFileArchitecture -FilePath $FilePath) -ne '64BIT') -and ($DLLFileBitness -ne '32BIT'))
            {
                $naerParams = @{
                    Exception = [System.PlatformNotSupportedException]::new("File [$filePath] has a detected file architecture of [$DLLFileBitness]. Only 32-bit or 64-bit DLL files can be $($DLLAction.ToLower() + 'ed').")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                    ErrorId = 'DllFileArchitectureError'
                    TargetObject = $FilePath
                    RecommendedAction = "Please review the supplied DLL FilePath and try again."
                }
                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
            }

            # Get the correct path to regsrv32.exe for the system and DLL file.
            $RegSvr32Path = if ([System.Environment]::Is64BitOperatingSystem)
            {
                if ($DLLFileBitness -eq '64BIT')
                {
                    if ([System.Environment]::Is64BitProcess)
                    {
                        "$([System.Environment]::SystemDirectory)\regsvr32.exe"
                    }
                    else
                    {
                        "$env:WinDir\Sysnative\regsvr32.exe"
                    }
                }
                elseif ($DLLFileBitness -eq '32BIT')
                {
                    "$env:WinDir\SysWOW64\regsvr32.exe"
                }
            }
            elseif ($DLLFileBitness -eq '32BIT')
            {
               "$([System.Environment]::SystemDirectory)\regsvr32.exe"
            }
            else
            {
                $naerParams = @{
                    Exception = [System.PlatformNotSupportedException]::new("File [$filePath] cannot be $($DLLAction.ToLower()) because it is a 64-bit file on a 32-bit operating system.")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                    ErrorId = 'DllFileArchitectureError'
                    TargetObject = $FilePath
                    RecommendedAction = "Please review the supplied DLL FilePath and try again."
                }
                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
            }

            # Register the DLL file and measure the success.
            if (($ExecuteResult = Start-ADTProcess -Path $RegSvr32Path -Parameters $DLLActionParameters -WindowStyle Hidden -PassThru -NoExitOnProcessFailure).ExitCode -ne 0)
            {
                if ($ExecuteResult.ExitCode -eq 60002)
                {
                    $naerParams = @{
                        Exception = [System.InvalidOperationException]::new("Start-ADTProcess function failed with exit code [$($ExecuteResult.ExitCode)].")
                        Category = [System.Management.Automation.ErrorCategory]::OperationStopped
                        ErrorId = 'ProcessInvocationError'
                        TargetObject = "$FilePath $DLLActionParameters"
                        RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                    }
                    Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
                }
                else
                {
                    $naerParams = @{
                        Exception = [System.InvalidOperationException]::new("regsvr32.exe failed with exit code [$($ExecuteResult.ExitCode)].")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'ProcessInvocationError'
                        TargetObject = "$FilePath $DLLActionParameters"
                        RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                    }
                    Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
                }
            }
        }
        catch
        {
            Write-ADTLogEntry -Message "Failed to $($DLLAction.ToLower()) DLL file.`n$(Resolve-ADTError)" -Severity 3
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
