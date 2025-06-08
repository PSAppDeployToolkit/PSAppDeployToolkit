#-----------------------------------------------------------------------------
#
# MARK: Import-ADTModuleDataFile
#
#-----------------------------------------------------------------------------

function Private:Import-ADTModuleDataFile
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
        [System.String[]]$BaseDirectory,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$FileName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Globalization.CultureInfo]$UICulture,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IgnorePolicy
    )

    # Internal function to process the imported data.
    function Update-ADTImportedDataValues
    {
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
            elseif (!$DataFile.ContainsKey($section.Key) -or ![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $section.Value)))
            {
                $DataFile.($section.Key) = $section.Value
            }
        }
    }

    # Establish directory paths for the specified input.
    $moduleDirectory = $Script:ADT.Directories.Defaults.([System.IO.Path]::GetFileNameWithoutExtension($FileName))
    $callerDirectory = $BaseDirectory

    # If we're running a release module, ensure the psd1 files haven't been tampered with.
    if (($badFiles = Test-ADTReleaseBuildFileValidity -LiteralPath $moduleDirectory))
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The module's default $FileName file has been modified from its released state.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidData
            ErrorId = 'ADTDataFileSignatureError'
            TargetObject = $badFiles
            RecommendedAction = "Please re-download $($MyInvocation.MyCommand.Module.Name) and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Import the default data first and foremost.
    $null = $PSBoundParameters.Remove('IgnorePolicy')
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
        foreach ($directory in $callerDirectory)
        {
            $PSBoundParameters.BaseDirectory = $directory
            Update-ADTImportedDataValues -DataFile $importedData -NewData (Import-LocalizedData @PSBoundParameters)
        }
    }

    # Super-impose registry values if they exist.
    if (!$IgnorePolicy -and ($policySettings = Get-ChildItem -LiteralPath "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\PSAppDeployToolkit\$([System.IO.Path]::GetFileNameWithoutExtension($FileName))" -ErrorAction Ignore | Convert-ADTRegistryKeyToHashtable))
    {
        Update-ADTImportedDataValues -DataFile $importedData -NewData $policySettings
    }

    # Return the built out data to the caller.
    return $importedData
}
