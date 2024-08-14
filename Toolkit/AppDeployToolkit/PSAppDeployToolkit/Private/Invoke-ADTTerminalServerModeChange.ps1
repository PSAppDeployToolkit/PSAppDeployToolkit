#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Invoke-ADTTerminalServerModeChange
{
    <#

    .SYNOPSIS
    Changes the mode for Remote Desktop Session Host/Citrix servers.

    .DESCRIPTION
    Changes the mode for Remote Desktop Session Host/Citrix servers.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Install', 'Execute')]
        [System.String]$Mode
    )

    Write-ADTLogEntry -Message "$(($msg = "Changing terminal server into user $($Mode.ToLower()) mode"))."
    $terminalServerResult = & "$([System.Environment]::SystemDirectory)\change.exe" User /$Mode 2>&1
    if (!$LASTEXITCODE.Equals(1))
    {
        Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$LASTEXITCODE]: $terminalServerResult") -Severity 3
        $naerParams = @{
            Exception = [System.ApplicationException]::new($msg)
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'RdsChangeUtilityFailure'
            TargetObject = $terminalServerResult
            RecommendedAction = "Please review the result in this error's TargetObject property and try again."
        }
        throw (New-ADTErrorRecord @naerParams)
    }
}
