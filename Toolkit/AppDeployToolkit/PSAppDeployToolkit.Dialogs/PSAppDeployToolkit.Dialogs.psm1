#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3

# Dot-source our imports and perform exports.
Export-ModuleMember -Function (Get-ChildItem -Path $PSScriptRoot\Private\*.ps1, $PSScriptRoot\Public\*.ps1).ForEach({
    # As we declare all functions read-only, attempt removal before dot-sourcing the function again.
    Remove-Item -LiteralPath "Function:$($_.BaseName)" -Force -ErrorAction Ignore

    # Dot source in the function code.
    . $_.FullName

    # Mark the dot-sourced function as read-only.
    Set-Item -LiteralPath "Function:$($_.BaseName)" -Options ReadOnly

    # Echo out the public functions.
    if ($_.DirectoryName.EndsWith('Public'))
    {
        return $_.BaseName
    }
})
