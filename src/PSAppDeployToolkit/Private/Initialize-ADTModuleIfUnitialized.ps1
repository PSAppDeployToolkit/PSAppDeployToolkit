#-----------------------------------------------------------------------------
#
# MARK: Initialize-ADTModuleIfUnitialized
#
#-----------------------------------------------------------------------------

function Initialize-ADTModuleIfUnitialized
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Initialize the module if there's no session and it hasn't been previously initialized.
    if (!($adtSession = if (& $Script:CommandTable.'Test-ADTSessionActive') { & $Script:CommandTable.'Get-ADTSession' }) -and !(& $Script:CommandTable.'Test-ADTModuleInitialized'))
    {
        try
        {
            & $Script:CommandTable.'Initialize-ADTModule'
        }
        catch
        {
            $Cmdlet.ThrowTerminatingError($_)
        }
    }

    # Return the current session if we happened to get one.
    if ($adtSession)
    {
        return $adtSession
    }
}
