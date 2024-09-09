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
                    $PSCmdlet.ThrowTerminatingError((& $Script:CommandTable.'New-ADTValidateScriptErrorRecord' -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified input is not an .msp file.'))
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
        $adtSession = & $Script:CommandTable.'Initialize-ADTModuleIfUnitialized' -Cmdlet $PSCmdlet
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
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
                elseif (& $Script:CommandTable.'Test-Path' -LiteralPath $Path)
                {
                    (& $Script:CommandTable.'Get-Item' -LiteralPath $Path).FullName
                }
                else
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Failed to find MSP file [$Path]." -Severity 3
                    $naerParams = @{
                        Exception = [System.IO.FileNotFoundException]::new("Failed to find MSP file [$Path].")
                        Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                        ErrorId = 'MsiFileNotFound'
                        TargetObject = $Path
                        RecommendedAction = "Please confirm the path of the MSP file and try again."
                    }
                    throw (& $Script:CommandTable.'New-ADTErrorRecord' @naerParams)
                }

                # Create a Windows Installer object and open the database in read-only mode.
                & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Checking MSP file for valid product codes.'
                [__ComObject]$Installer = & $Script:CommandTable.'New-Object' -ComObject WindowsInstaller.Installer
                [__ComObject]$Database = & $Script:CommandTable.'Invoke-ADTObjectMethod' -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($mspFile, 32)

                # Get the SummaryInformation from the windows installer database and store all product codes found.
                [__ComObject]$SummaryInformation = & $Script:CommandTable.'Get-ADTObjectProperty' -InputObject $Database -PropertyName SummaryInformation
                $AllTargetedProductCodes = & $Script:CommandTable.'Get-ADTInstalledApplication' -ProductCode (& $Script:CommandTable.'Get-ADTObjectProperty' -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(7)).Split(';')

                # Free our COM objects.
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($SummaryInformation)
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($Database)
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($Installer)

                # If the application is installed, patch it.
                if ($AllTargetedProductCodes)
                {
                    & $Script:CommandTable.'Start-ADTMsiProcess' -Action Patch @PSBoundParameters
                }
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
