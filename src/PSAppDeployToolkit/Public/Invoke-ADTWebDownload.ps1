#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTWebDownload
#
#-----------------------------------------------------------------------------

function Invoke-ADTWebDownload
{
    <#
    .SYNOPSIS
        Wraps around Invoke-WebRequest to provide logging and retry support.

    .DESCRIPTION
        This function allows callers to download files as part of a deployment with logging and retry support.

    .PARAMETER Uri
        The URL that to retrieve the file from.

    .PARAMETER OutFile
        The path of where to save the file to.

    .PARAMETER Headers
        Any headers that need to be provided for file transfer.

    .PARAMETER Sha256Hash
        An optional SHA256 reference file hash for download verification.

    .PARAMETER PassThru
        Returns the WebResponseObject object from Invoke-WebRequest.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        Microsoft.PowerShell.Commands.WebResponseObject

        Invoke-ADTWebDownload returns the results from Invoke-WebRequest if PassThru is specified.

    .EXAMPLE
        Invoke-ADTWebDownload -Uri https://aka.ms/getwinget -OutFile "$($adtSession.DirSupportFiles)\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"

        Downloads the latest WinGet installer to the SupportFiles directory.

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
    [OutputType([Microsoft.PowerShell.Commands.WebResponseObject])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (![System.Uri]::IsWellFormedUriString($_.AbsoluteUri, [System.UriKind]::Absolute))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Uri -ProvidedValue $_ -ExceptionMessage 'The specified input is not a valid Uri.'))
                }
                return !!$_
            })]
        [System.Uri]$Uri,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$OutFile,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$Headers = @{ Accept = 'text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7' },

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Sha256Hash,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Commence download and return the result if passing through.
                Write-ADTLogEntry -Message "Downloading $Uri."
                $iwrParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Exclude Sha256Hash
                $iwrResult = Invoke-ADTCommandWithRetries -Command $Script:CommandTable.'Invoke-WebRequest' -UseBasicParsing @iwrParams -Verbose:$false

                # Validate the hash if one was provided.
                if ($PSBoundParameters.ContainsKey('Sha256Hash') -and (($fileHash = Get-FileHash -LiteralPath $OutFile).Hash -ne $Sha256Hash))
                {
                    $naerParams = @{
                        Exception = [System.BadImageFormatException]::new("The downloaded file has an invalid file hash of [$($fileHash.Hash)].", $OutFile)
                        Category = [System.Management.Automation.ErrorCategory]::InvalidData
                        ErrorId = 'DownloadedFileInvalid'
                        TargetObject = $fileHash
                        RecommendedAction = "Please compare the downloaded file's hash against the provided value and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Return any results from Invoke-WebRequest if we have any and we're passing through.
                if ($PassThru -and $iwrResult)
                {
                    return $iwrResult
                }
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Object ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Error downloading setup file(s) from the provided URL of [$Uri]."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
