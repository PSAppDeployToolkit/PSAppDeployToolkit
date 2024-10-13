#-----------------------------------------------------------------------------
#
# MARK: Get-ADTUriFileName
#
#-----------------------------------------------------------------------------

function Get-ADTUriFileName
{
    <#
    .SYNOPSIS
        Returns the filename of the provided URI.

    .DESCRIPTION
        This function gets the filename of the provided URI from the provided input and returns it to the caller.

    .PARAMETER Uri
        The URL that to retrieve the filename from.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Get-ADTUriFileName returns a string value of the URI's filename.

    .EXAMPLE
        Get-ADTUriFileName -Uri https://aka.ms/getwinget

        Returns the filename for the specified URI, redirected or otherwise. e.g. Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle

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
    [OutputType([System.String])]
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
        [System.Uri]$Uri
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
                # Re-write the URI to factor in any redirections.
                Write-ADTLogEntry -Message "Retrieving the file name for URI [$Uri]."
                $Uri = Get-ADTRedirectedUri -Uri $Uri

                # Create web request.
                $webReq = [System.Net.WebRequest]::Create($Uri)
                $webReq.AllowAutoRedirect = $false
                $webReq.Accept = 'text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7'

                # Get a response and close it out.
                $reqRes = $webReq.GetResponse()
                $resCnt = $reqRes.GetResponseHeader('Content-Disposition')
                $reqRes.Close()

                # If $resCnt is empty, the provided URI likely has the filename in it.
                $filename = if (!$resCnt.Contains('filename'))
                {
                    Remove-ADTInvalidFileNameChars -Name $Uri.ToString().Split('/')[-1]
                }
                else
                {
                    Remove-ADTInvalidFileNameChars -Name $resCnt.Split(';').Trim().Where({ $_.StartsWith('filename=') }).Split('=')[-1]
                }

                # Return the determined filename to the caller.
                Write-ADTLogEntry -Message "Resolved filename [$filename] from the provided URI."
                return $filename
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to determine the filename for URI [$Uri]."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
