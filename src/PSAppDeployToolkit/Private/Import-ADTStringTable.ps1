#-----------------------------------------------------------------------------
#
# MARK: Import-ADTStringTable
#
#-----------------------------------------------------------------------------

function Import-ADTStringTable
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BaseDirectory,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$UICulture
    )

    # Process the incoming $BaseDirectory value.
    if (!$BaseDirectory.Equals($Script:PSScriptRoot) -and ![System.IO.File]::Exists([System.IO.Path]::Combine($BaseDirectory, 'Strings', 'strings.psd1')))
    {
        $BaseDirectory = $Script:PSScriptRoot
    }

    # Store the chosen language within this session.
    Import-LocalizedData -BaseDirectory ([System.IO.Path]::Combine($BaseDirectory, 'Strings')) -FileName strings.psd1 -UICulture $UICulture
}
