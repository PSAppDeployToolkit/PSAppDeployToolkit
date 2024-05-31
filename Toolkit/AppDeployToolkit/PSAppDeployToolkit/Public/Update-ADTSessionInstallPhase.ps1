function Update-ADTSessionInstallPhase
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Value
    )

    (Get-ADTSession).SetPropertyValue('InstallPhase', $Value)
}
