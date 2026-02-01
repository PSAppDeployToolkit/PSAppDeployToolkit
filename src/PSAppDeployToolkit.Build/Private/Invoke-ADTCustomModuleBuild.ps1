#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTCustomModuleBuild
#
#-----------------------------------------------------------------------------

function Invoke-ADTCustomModuleBuild
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Actions
    )

    # Go through the motions.
    Initialize-ADTModuleBuild
    try
    {
        foreach ($action in $Actions)
        {
            & $action
        }
        Complete-ADTModuleBuild
    }
    catch
    {
        Complete-ADTModuleBuild -ErrorRecord $_
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
