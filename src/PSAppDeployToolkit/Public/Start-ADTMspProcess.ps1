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

        Uses default config MSI parameters. You can use -AddParameters to add additional parameters.

    .PARAMETER Path
        Path to the MSP file.

    .PARAMETER AddParameters
        Additional parameters.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Start-ADTMspProcess -Path 'Adobe_Reader_11.0.3_EN.msp'

        Executes the specified MSP file for Adobe Reader 11.0.3.

    .EXAMPLE
        Start-ADTMspProcess -Path 'AcroRdr2017Upd1701130143_MUI.msp' -AddParameters 'ALLUSERS=1'

        Executes the specified MSP file for Acrobat Reader 2017 with additional parameters.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.Int32])]
    param
    (
        [Parameter(Mandatory = $true, HelpMessage = 'Please enter the path to the MSP file')]
        [ValidateScript({
                if (('.msp' -contains [System.IO.Path]::GetExtension($_)))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified input is not an .msp file.'))
                }
                return !!$_
            })]
        [Alias('FilePath')]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$AddParameters
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
                $mspFile = if ($adtSession -and [System.IO.File]::Exists(($dirFilesPath = [System.IO.Path]::Combine($adtSession.GetPropertyValue('DirFiles'), $Path))))
                {
                    $dirFilesPath
                }
                elseif (Test-Path -LiteralPath $Path)
                {
                    (Get-Item -LiteralPath $Path).FullName
                }
                else
                {
                    Write-ADTLogEntry -Message "Failed to find MSP file [$Path]." -Severity 3
                    $naerParams = @{
                        Exception = [System.IO.FileNotFoundException]::new("Failed to find MSP file [$Path].")
                        Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                        ErrorId = 'MsiFileNotFound'
                        TargetObject = $Path
                        RecommendedAction = "Please confirm the path of the MSP file and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Create a Windows Installer object and open the database in read-only mode.
                Write-ADTLogEntry -Message 'Checking MSP file for valid product codes.'
                [__ComObject]$Installer = New-Object -ComObject WindowsInstaller.Installer
                [__ComObject]$Database = Invoke-ADTObjectMethod -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($mspFile, 32)

                # Get the SummaryInformation from the windows installer database and store all product codes found.
                [__ComObject]$SummaryInformation = Get-ADTObjectProperty -InputObject $Database -PropertyName SummaryInformation
                $msiProductCode = (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(7)).Split(';')
                $AllTargetedProductCodes = Get-ADTApplication -FilterScript { $_.ProductCode -eq $msiProductCode }

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
