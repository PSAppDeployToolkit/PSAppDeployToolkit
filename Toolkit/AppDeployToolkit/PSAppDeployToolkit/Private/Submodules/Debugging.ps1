#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Write-FunctionHeaderOrFooter {
    <#
.SYNOPSIS

Write the function header or footer to the log upon first entering or exiting a function.

.DESCRIPTION

Write the "Function Start" message, the bound parameters the function was invoked with, or the "Function End" message when entering or exiting a function.

Messages are debug messages so will only be logged if LogDebugMessage option is enabled in XML config file.

.PARAMETER CmdletName

The name of the function this function is invoked from.

.PARAMETER CmdletBoundParameters

The bound parameters of the function this function is invoked from.

.PARAMETER Header

Write the function header.

.PARAMETER Footer

Write the function footer.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

.EXAMPLE

Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$CmdletName,
        [Parameter(Mandatory = $true, ParameterSetName = 'Header')]
        [AllowEmptyCollection()]
        [Hashtable]$CmdletBoundParameters,
        [Parameter(Mandatory = $true, ParameterSetName = 'Header')]
        [Switch]$Header,
        [Parameter(Mandatory = $true, ParameterSetName = 'Footer')]
        [Switch]$Footer
    )

    If ($Header) {
        Write-ADTLogEntry -Message 'Function Start' -Source ${CmdletName} -DebugMessage

        ## Get the parameters that the calling function was invoked with
        [String]$CmdletBoundParameters = $CmdletBoundParameters | Format-Table -Property @{ Label = 'Parameter'; Expression = { "[-$($_.Key)]" } }, @{ Label = 'Value'; Expression = { $_.Value }; Alignment = 'Left' }, @{ Label = 'Type'; Expression = { $_.Value.GetType().Name }; Alignment = 'Left' } -AutoSize -Wrap | Out-String
        If ($CmdletBoundParameters) {
            Write-ADTLogEntry -Message "Function invoked with bound parameter(s): `r`n$CmdletBoundParameters" -Source ${CmdletName} -DebugMessage
        }
        Else {
            Write-ADTLogEntry -Message 'Function invoked without any bound parameters.' -Source ${CmdletName} -DebugMessage
        }
    }
    ElseIf ($Footer) {
        Write-ADTLogEntry -Message 'Function End' -Source ${CmdletName} -DebugMessage
    }
}
