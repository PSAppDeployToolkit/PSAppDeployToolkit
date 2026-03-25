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
        [AllowNull()][PSAppDeployToolkit.Attributes.AllowNullButNotEmptyOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [System.String[]]$BaseDirectory,

        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$FileName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Globalization.CultureInfo]$UICulture = [System.Globalization.CultureInfo]::CurrentUICulture,

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

    # Import the default data first and foremost.
    $section = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
    $initialUICulture = $UICulture
    $importedData = while ($true)
    {
        if ($Script:ADT.ModuleDefaults.$section.Contains($UICulture.Name))
        {
            $Script:ADT.ModuleDefaults.$section.($UICulture.Name).Ast.EndBlock.Statements.PipelineElements.Expression.SafeGetValue()
            $UICulture = $initialUICulture
            break
        }
        $UICulture = $UICulture.Parent
    }

    # Super-impose the caller's data if it's different from default.
    $null = $PSBoundParameters.Remove('IgnorePolicy')
    foreach ($directory in $BaseDirectory)
    {
        $PSBoundParameters.BaseDirectory = [System.Management.Automation.WildcardPattern]::Escape($directory)
        Update-ADTImportedDataValues -DataFile $importedData -NewData (Import-LocalizedData @PSBoundParameters)
    }

    # Super-impose registry values if they exist.
    if (!$IgnorePolicy)
    {
        $initialUICulture = $UICulture
        while ($true)
        {
            if ($policySettings = Get-ChildItem -LiteralPath "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\PSAppDeployToolkit\$section$(if (![System.String]::IsNullOrWhiteSpace($UICulture.Name)) { "\$($UICulture.Name)" })" -ErrorAction Ignore | Convert-ADTRegistryKeyToHashtable)
            {
                Update-ADTImportedDataValues -DataFile $importedData -NewData $policySettings
                $UICulture = $initialUICulture
                break
            }
            if ([System.String]::IsNullOrWhiteSpace($UICulture.Name))
            {
                $UICulture = $initialUICulture
                break
            }
            $UICulture = $UICulture.Parent
        }
    }

    # Return the built out data to the caller.
    return $importedData
}
