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
        [System.String]$UICulture,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoAdmxParsing
    )

    # Internal function to process the imported data.
    function Update-ImportedDataValues
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [AllowEmptyCollection()]
            [System.Collections.Hashtable]$DataFile,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Collections.Hashtable]$NewData
        )

        # Process the provided default data so we can add missing data to the data file.
        foreach ($section in $NewData.GetEnumerator())
        {
            # Recursively process hashtables, otherwise just update the value.
            if ($section.Value -is [System.Collections.Hashtable])
            {
                if (!$DataFile.ContainsKey($section.Key) -or ($DataFile.($section.Key) -isnot [System.Collections.Hashtable]))
                {
                    $DataFile.($section.Key) = @{}
                }
                & $MyInvocation.MyCommand -DataFile $DataFile.($section.Key) -NewData $section.Value
            }
            else
            {
                $DataFile.($section.Key) = $section.Value
            }
        }
    }

    # Remove parameters not compatible with Import-LocalizedData from $PSBoundParameters.
    $null = $PSBoundParameters.Remove('NoAdmxParsing')

    # Establish directory paths for the specified input.
    $moduleDirectory = $Script:ADT.Directories.Defaults.([regex]::Replace($BaseDirectory, '^.+\\', [System.String]::Empty))
    $callerDirectory = $BaseDirectory

    # Import the default data first and foremost.
    $PSBoundParameters.BaseDirectory = $moduleDirectory
    $importedData = Import-LocalizedData @PSBoundParameters

    # Validate we imported something from our default location.
    if (!$importedData.Count)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The importation of the module's default $FileName file returned a null or empty result.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTDataFileImportFailure'
            TargetObject = [System.IO.Path]::Combine($PSBoundParameters.BaseDirectory, $FileName)
            RecommendedAction = "Please ensure that this module is not corrupt or missing files, then try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Super-impose the caller's data if it's different from default.
    if (!$callerDirectory.Equals($moduleDirectory))
    {
        $PSBoundParameters.BaseDirectory = $callerDirectory
        Update-ImportedDataValues -DataFile $importedData -NewData (Import-LocalizedData @PSBoundParameters)
    }

    # Super-impose registry values if they exist.
    if (!$NoAdmxParsing -and ($admxSettings = Get-ChildItem -LiteralPath "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\PSAppDeployToolkit\$([System.IO.Path]::GetFileNameWithoutExtension($FileName))" -ErrorAction Ignore | Convert-RegistryKeyToHashtable))
    {
        Update-ImportedDataValues -DataFile $importedData -NewData $admxSettings
    }

    # Return the built out data to the caller.
    return $importedData
}
