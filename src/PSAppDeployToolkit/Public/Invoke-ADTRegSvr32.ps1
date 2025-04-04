#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTRegSvr32
#
#-----------------------------------------------------------------------------

function Invoke-ADTRegSvr32
{
    <#
    .SYNOPSIS
        Register or unregister a DLL file.

    .DESCRIPTION
        Register or unregister a DLL file using regsvr32.exe. This function determines the bitness of the DLL file and uses the appropriate version of regsvr32.exe to perform the action. It supports both 32-bit and 64-bit DLL files on corresponding operating systems.

    .PARAMETER FilePath
        Path to the DLL file.

    .PARAMETER Action
        Specify whether to register or unregister the DLL.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return objects.

    .EXAMPLE
        Invoke-ADTRegSvr32 -FilePath "C:\Test\DcTLSFileToDMSComp.dll" -Action 'Register'

        Registers the specified DLL file.

    .EXAMPLE
        Invoke-ADTRegSvr32 -FilePath "C:\Test\DcTLSFileToDMSComp.dll" -Action 'Unregister'

        Unregisters the specified DLL file.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Invoke-ADTRegSvr32
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (![System.IO.File]::Exists($_) -and ([System.IO.Path]::GetExtension($_) -ne '.dll'))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'The specified file does not exist or is not a DLL file.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Register', 'Unregister')]
        [System.String]$Action
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Define parameters to pass to regsrv32.exe.
        $ActionParameters = switch ($Action = $Host.CurrentCulture.TextInfo.ToTitleCase($Action.ToLower()))
        {
            Register
            {
                "/s `"$FilePath`""
                break
            }
            Unregister
            {
                "/s /u `"$FilePath`""
                break
            }
        }
    }

    process
    {
        Write-ADTLogEntry -Message "$Action DLL file [$FilePath]."
        try
        {
            try
            {
                # Determine the bitness of the DLL file.
                if ((($DLLFileBitness = Get-ADTPEFileArchitecture -FilePath $FilePath) -ne [PSADT.Types.SystemArchitecture]::AMD64) -and ($DLLFileBitness -ne [PSADT.Types.SystemArchitecture]::i386))
                {
                    $naerParams = @{
                        Exception = [System.PlatformNotSupportedException]::new("File [$filePath] has a detected file architecture of [$DLLFileBitness]. Only 32-bit or 64-bit DLL files can be $($Action.ToLower() + 'ed').")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                        ErrorId = 'DllFileArchitectureError'
                        TargetObject = $FilePath
                        RecommendedAction = "Please review the supplied DLL FilePath and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Get the correct path to regsrv32.exe for the system and DLL file.
                $RegSvr32Path = if ([System.Environment]::Is64BitOperatingSystem)
                {
                    if ($DLLFileBitness -eq [PSADT.Types.SystemArchitecture]::AMD64)
                    {
                        if ([System.Environment]::Is64BitProcess)
                        {
                            "$([System.Environment]::SystemDirectory)\regsvr32.exe"
                        }
                        else
                        {
                            "$([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows))\sysnative\regsvr32.exe"
                        }
                    }
                    elseif ($DLLFileBitness -eq [PSADT.Types.SystemArchitecture]::i386)
                    {
                        "$([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::SystemX86))\regsvr32.exe"
                    }
                }
                elseif ($DLLFileBitness -eq [PSADT.Types.SystemArchitecture]::i386)
                {
                    "$([System.Environment]::SystemDirectory)\regsvr32.exe"
                }
                else
                {
                    $naerParams = @{
                        Exception = [System.PlatformNotSupportedException]::new("File [$filePath] cannot be $($Action.ToLower()) because it is a 64-bit file on a 32-bit operating system.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                        ErrorId = 'DllFileArchitectureError'
                        TargetObject = $FilePath
                        RecommendedAction = "Please review the supplied DLL FilePath and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Register the DLL file and measure the success.
                if (($ExecuteResult = Start-ADTProcess -FilePath $RegSvr32Path -ArgumentList $ActionParameters -WindowStyle Hidden -PassThru).ExitCode -ne 0)
                {
                    if ($ExecuteResult.ExitCode -eq 60002)
                    {
                        $naerParams = @{
                            Exception = [System.InvalidOperationException]::new("Start-ADTProcess function failed with exit code [$($ExecuteResult.ExitCode)].")
                            Category = [System.Management.Automation.ErrorCategory]::OperationStopped
                            ErrorId = 'ProcessInvocationError'
                            TargetObject = "$FilePath $ActionParameters"
                            RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    else
                    {
                        $naerParams = @{
                            Exception = [System.InvalidOperationException]::new("regsvr32.exe failed with exit code [$($ExecuteResult.ExitCode)].")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'ProcessInvocationError'
                            TargetObject = "$FilePath $ActionParameters"
                            RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to $($Action.ToLower()) DLL file."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
