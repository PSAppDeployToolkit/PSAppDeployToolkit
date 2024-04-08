#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-DeferHistory {
    <#
.SYNOPSIS

Get the history of deferrals from the registry for the current application, if it exists.

.DESCRIPTION

Get the history of deferrals from the registry for the current application, if it exists.

.PARAMETER DeferTimesRemaining

Specify the number of deferrals remaining.

.PARAMETER DeferDeadline

Specify the deadline for the deferral.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the history of deferrals from the registry for the current application, if it exists.

.EXAMPLE

Get-DeferHistory

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Write-Log -Message 'Getting deferral history...' -Source ${CmdletName}
        Get-RegistryKey -Key $regKeyDeferHistory -ContinueOnError $true
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Set-DeferHistory {
    <#
.SYNOPSIS

Set the history of deferrals in the registry for the current application.

.DESCRIPTION

Set the history of deferrals in the registry for the current application.

.PARAMETER DeferTimesRemaining

Specify the number of deferrals remaining.

.PARAMETER DeferDeadline

Specify the deadline for the deferral.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None. This function does not return any objects.

.EXAMPLE

Set-DeferHistory

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [String]$deferTimesRemaining,
        [Parameter(Mandatory = $false)]
        [String]$deferDeadline
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        If ($deferTimesRemaining -and ($deferTimesRemaining -ge 0)) {
            Write-Log -Message "Setting deferral history: [DeferTimesRemaining = $deferTimesRemaining]." -Source ${CmdletName}
            Set-RegistryKey -Key $regKeyDeferHistory -Name 'DeferTimesRemaining' -Value $deferTimesRemaining -ContinueOnError $true
        }
        If ($deferDeadline) {
            Write-Log -Message "Setting deferral history: [DeferDeadline = $deferDeadline]." -Source ${CmdletName}
            Set-RegistryKey -Key $regKeyDeferHistory -Name 'DeferDeadline' -Value $deferDeadline -ContinueOnError $true
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
