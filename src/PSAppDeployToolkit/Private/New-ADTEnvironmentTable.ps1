#-----------------------------------------------------------------------------
#
# MARK: New-ADTEnvironmentTable
#
#-----------------------------------------------------------------------------

function Private:New-ADTEnvironmentTable
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = "This function does not change system state.")]
    [CmdletBinding()]
    [OutputType([PSAppDeployToolkit.Foundation.EnvironmentTable])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$AdditionalEnvironmentVariables
    )

    # Perform initial setup.
    $adtEnv = [PSAppDeployToolkit.Foundation.EnvironmentTable]::new($PSCmdlet, $PSVersionTable, $PSVersionTable.PSVersion)

    # Add in additional environment variables from the caller if they've provided any.
    if ($PSBoundParameters.ContainsKey('AdditionalEnvironmentVariables'))
    {
        $AdditionalEnvironmentVariables.GetEnumerator() | & {
            begin
            {
                $adtEnvProps = $adtEnv.PSObject.get_Properties()
            }
            process
            {
                $adtEnvProps.Add([System.Management.Automation.PSNoteProperty]::new($_.get_Key(), $_.get_Value()))
            }
        }
    }

    # Return variables for use within the module.
    return $adtEnv
}
