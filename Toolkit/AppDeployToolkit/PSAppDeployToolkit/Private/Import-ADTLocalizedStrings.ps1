#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Import-ADTLocalizedStrings
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$UICulture
    )

    # Store the chosen language within this session.
    & $Script:CommandTable.'Import-LocalizedData' -BaseDirectory $Script:PSScriptRoot\Strings -FileName strings.psd1 @PSBoundParameters
}
