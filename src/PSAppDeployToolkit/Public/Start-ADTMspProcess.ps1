#-----------------------------------------------------------------------------
#
# MARK: Start-ADTMspProcess
#
#-----------------------------------------------------------------------------

function Start-ADTMspProcess
{
    <#
    .SYNOPSIS
        Executes an MSP file using the same logic as Start-ADTMsiProcess.

    .DESCRIPTION
        Reads SummaryInfo targeted product codes in MSP file and determines if the MSP file applies to any installed products. If a valid installed product is found, triggers the Start-ADTMsiProcess function to patch the installation.

        Uses default config MSI parameters. You can use -AdditionalArgumentList to add additional parameters.

    .PARAMETER FilePath
        Path to the MSP file.

    .PARAMETER AdditionalArgumentList
        Additional parameters.

    .PARAMETER RunAsActiveUser
        A RunAsActiveUser object to invoke the process as.

    .PARAMETER UseLinkedAdminToken
        Use a user's linked administrative token while running the process under their context.

    .PARAMETER UseHighestAvailableToken
        Use a user's linked administrative token if it's available while running the process under their context.

    .PARAMETER InheritEnvironmentVariables
        Specifies whether the process running as a user should inherit the SYSTEM account's environment variables.

    .PARAMETER DenyUserTermination
        Specifies that users cannot terminate the process started in their context. The user will still be able to terminate the process if they're an administrator, though.

    .PARAMETER UseUnelevatedToken
        If the current process is elevated, starts the new process unelevated using the user's unelevated linked token.

    .PARAMETER ExpandEnvironmentVariables
        Specifies whether to expand any Windows/DOS-style environment variables in the specified FilePath/ArgumentList.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Start-ADTMspProcess -FilePath 'Adobe_Reader_11.0.3_EN.msp'

        Executes the specified MSP file for Adobe Reader 11.0.3.

    .EXAMPLE
        Start-ADTMspProcess -FilePath 'AcroRdr2017Upd1701130143_MUI.msp' -AdditionalArgumentList 'ALLUSERS=1'

        Executes the specified MSP file for Acrobat Reader 2017 with additional parameters.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTMspProcess
    #>

    [CmdletBinding(DefaultParameterSetName = 'None')]
    [OutputType([System.Int32])]
    param
    (
        [Parameter(Mandatory = $true, HelpMessage = 'Please supply the path to the MSP file to process.')]
        [ValidateScript({
                if ([System.IO.Path]::GetExtension($_) -notmatch '^\.msp$')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'The specified input has an invalid file extension.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$AdditionalArgumentList,

        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser')]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.RunAsActiveUser]$RunAsActiveUser,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser')]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser')]
        [System.Management.Automation.SwitchParameter]$UseHighestAvailableToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser')]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser')]
        [System.Management.Automation.SwitchParameter]$DenyUserTermination,

        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken')]
        [System.Management.Automation.SwitchParameter]$UseUnelevatedToken,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExpandEnvironmentVariables
    )

    begin
    {
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # If the MSP is in the Files directory, set the full path to the MSP.
                $mspFile = if ($adtSession -and ![System.String]::IsNullOrWhiteSpace($adtSession.DirFiles) -and (Test-Path -LiteralPath ($dirFilesPath = (Join-Path -Path $adtSession.DirFiles -ChildPath $FilePath).Trim()) -PathType Leaf))
                {
                    $dirFilesPath
                }
                elseif (Test-Path -LiteralPath $FilePath)
                {
                    (Get-Item -LiteralPath $FilePath).FullName
                }
                else
                {
                    $naerParams = @{
                        Exception = [System.IO.FileNotFoundException]::new("Failed to find MSP file [$FilePath].")
                        Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                        ErrorId = 'MspFileNotFound'
                        TargetObject = $FilePath
                        RecommendedAction = "Please confirm the path of the MSP file and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Check the underlying product is installed before proceeding.
                Write-ADTLogEntry -Message 'Checking MSP file for valid product codes.'
                if (Get-ADTApplication -ProductCode ([PSADT.Utilities.MsiUtilities]::GetMspSupportedProductCodes($mspFile)))
                {
                    Start-ADTMsiProcess -Action Patch @PSBoundParameters
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
