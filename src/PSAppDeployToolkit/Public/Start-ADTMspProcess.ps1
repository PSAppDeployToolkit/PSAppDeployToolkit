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

    [CmdletBinding()]
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
        [System.String[]]$AdditionalArgumentList
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
                $mspFile = if ($adtSession -and [System.IO.File]::Exists(($dirFilesPath = [System.IO.Path]::Combine($adtSession.DirFiles, $FilePath))))
                {
                    $dirFilesPath
                }
                elseif (Test-Path -LiteralPath $FilePath)
                {
                    (Get-Item -LiteralPath $FilePath).FullName
                }
                else
                {
                    Write-ADTLogEntry -Message "Failed to find MSP file [$FilePath]." -Severity 3
                    $naerParams = @{
                        Exception = [System.IO.FileNotFoundException]::new("Failed to find MSP file [$FilePath].")
                        Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                        ErrorId = 'MsiFileNotFound'
                        TargetObject = $FilePath
                        RecommendedAction = "Please confirm the path of the MSP file and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Create a Windows Installer object and open the database in read-only mode.
                Write-ADTLogEntry -Message 'Checking MSP file for valid product codes.'
                [__ComObject]$Installer = New-Object -ComObject WindowsInstaller.Installer
                [__ComObject]$Database = Invoke-ADTObjectMethod -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($mspFile, 32)

                # Get the SummaryInformation from the Windows Installer database and store all product codes found.
                [__ComObject]$SummaryInformation = Get-ADTObjectProperty -InputObject $Database -PropertyName SummaryInformation
                $AllTargetedProductCodes = Get-ADTApplication -ProductCode (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(7)).Split(';')

                # Free our COM objects.
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($SummaryInformation)
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($Database)
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($Installer)

                # If the application is installed, patch it.
                if ($AllTargetedProductCodes)
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
