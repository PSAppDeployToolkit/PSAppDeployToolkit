#-----------------------------------------------------------------------------
#
# MARK: Import-ADTModuleDataFile
#
#-----------------------------------------------------------------------------

function Import-ADTModuleDataFile
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName BaseDirectory -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (![System.IO.Directory]::Exists($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName BaseDirectory -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [System.String]$BaseDirectory,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$FileName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$UICulture
    )

    # Internal function to process the imported data.
    function Add-ModuleDefaultsToImportedData
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Collections.Hashtable]$DataFile,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Collections.Hashtable]$DefaultData
        )

        # Process the provided default data so we can add missing data to the data file.
        foreach ($section in $DefaultData.GetEnumerator())
        {
            # Add the section in wholesale if it doesn't exist, otherwise process it again if its a hashtable.
            if (!$DataFile.ContainsKey($section.Key))
            {
                $DataFile.Add($section.Key, $section.Value)
            }
            elseif ($section.Value -is [System.Collections.Hashtable])
            {
                & $MyInvocation.MyCommand -DataFile $DataFile.($section.Key) -DefaultData $section.Value
            }
        }
    }

    # Import the requested data file as-is.
    $importedData = Import-LocalizedData @PSBoundParameters

    # Return early if the BaseDirectory is that of the module.
    if ($BaseDirectory.Equals((Get-ADTModuleData).Directories.Defaults.([regex]::Replace($BaseDirectory, '^.+\\', [System.String]::Empty))))
    {
        return $importedData
    }

    # The base directory isn't the module's, therefore bring in the module's config so we can fill in any blanks.
    $PSBoundParameters.BaseDirectory = $PSBoundParameters.BaseDirectory -replace '^.+\\', "$Script:PSScriptRoot\"
    Add-ModuleDefaultsToImportedData -DataFile $importedData -DefaultData (Import-LocalizedData @PSBoundParameters)

    # Return the amended caller data to the caller.
    return $importedData
}
