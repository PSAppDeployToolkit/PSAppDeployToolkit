#-----------------------------------------------------------------------------
#
# MARK: Get-ADTDefaultConfig
#
#-----------------------------------------------------------------------------

function Private:Get-ADTDefaultConfig
{
    # Internal filter to report whether any value names a variable the environment table would have to supply.
    filter Test-ADTConfigNamesEnvironmentValue
    {
        foreach ($section in $($_.GetEnumerator()))
        {
            if ($section.Value -is [System.Collections.Hashtable])
            {
                $section.Value | & $MyInvocation.MyCommand; continue
            }
            if (($section.Value -is [System.String]) -and ($section.Value -match '\$(?!env:|\{env:)[A-Za-z_{]'))
            {
                $true
            }
        }
    }

    # If we're here but the module is in any way/shape/form initialised, throw loudly.
    if (Test-ADTModuleInitialized)
    {
        $naerParams = @{
            Exception = [System.InvalidProgramException]::new("Cannot retrieve the default config while the module is already initialized.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ModuleAlreadyInitialized'
            RecommendedAction = "Please report this issue to the PSAppDeployToolkit team."
        }
        throw (New-ADTErrorRecord @naerParams)
    }

    # Import without a base directory so only the module's defaults are used, with any policy super-imposed on top.
    $config = Import-ADTModuleDataFile -BaseDirectory $null -FileName config.psd1
    Update-ADTConfigTempVariables -Config $config

    # Expand any variables in the config, loading a new environment table if required.
    if ($config | Test-ADTConfigNamesEnvironmentValue)
    {
        (New-ADTEnvironmentTable).PSObject.Properties | & { process { New-Variable -Name $_.Name -Value $_.Value -Option Constant } end { Expand-ADTVariablesInHashtable -Hashtable $config -SessionState $ExecutionContext.SessionState } }
    }
    else
    {
        Expand-ADTVariablesInHashtable -Hashtable $config -SessionState $ExecutionContext.SessionState
    }

    # Perform config post-processing before returning it to the caller.
    Update-ADTConfigAccessiblePaths -Config $config
    return $config
}
