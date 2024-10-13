#-----------------------------------------------------------------------------
#
# MARK: Get-ADTRedirectedUri
#
#-----------------------------------------------------------------------------

function Get-ADTRedirectedUri
{
    <#
    .SYNOPSIS
        Returns the resolved URI from the provided permalink.

    .DESCRIPTION
        This function gets the resolved/redirected URI from the provided input and returns it to the caller.

    .PARAMETER Uri
        The URL that requires redirection resolution.

    .PARAMETER Headers
        Any headers that need to be provided for URI redirection resolution.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Uri

        Get-ADTRedirectedUri returns a Uri of the resolved/redirected URI.

    .EXAMPLE
        Get-ADTRedirectedUri -Uri https://aka.ms/getwinget

        Returns the absolute URI for the specified short link, e.g. https://github.com/microsoft/winget-cli/releases/download/v1.8.1911/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle

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
    [OutputType([System.Uri])]
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

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$Headers = @{ Accept = 'text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7' }
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
                # Create web request.
                Write-ADTLogEntry -Message "Retrieving the redirected URI for [$Uri]."
                $webReq = [System.Net.WebRequest]::Create($Uri)
                $webReq.AllowAutoRedirect = $false
                $Headers.GetEnumerator() | & { process { $webReq.($_.Key) = $_.Value } }

                # Get a response and close it out.
                $reqRes = $webReq.GetResponse()
                $resLoc = $reqRes.GetResponseHeader('Location')
                $reqRes.Close()

                # If $resLoc is empty, return the provided URI so something is returned to the caller.
                $Uri = if (![System.String]::IsNullOrWhiteSpace($resLoc))
                {
                    $resLoc
                }

                # Return the redirected URI to the caller.
                Write-ADTLogEntry -Message "Retrieved redireted URI [$Uri] from the provided input."
                return $Uri
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to determine the redirected URI for [$Uri]."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
