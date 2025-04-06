#-----------------------------------------------------------------------------
#
# MARK: Initialize-ADTModuleIfUnitialized
#
#-----------------------------------------------------------------------------

function Private:Initialize-ADTModuleIfUnitialized
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Initialize the module if there's no session and it hasn't been previously initialized.
    if (!($adtSession = if (Test-ADTSessionActive) { Get-ADTSession }) -and !(Test-ADTModuleInitialized))
    {
        try
        {
            Initialize-ADTModule
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
