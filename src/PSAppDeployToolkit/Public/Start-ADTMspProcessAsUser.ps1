#-----------------------------------------------------------------------------
#
# MARK: Start-ADTMspProcessAsUser
#
#-----------------------------------------------------------------------------

function Start-ADTMspProcessAsUser
{
    <#
    .SYNOPSIS
        Executes an MSP file using the same logic as Start-ADTMsiProcess.

    .DESCRIPTION
        Reads SummaryInfo targeted product codes in MSP file and determines if the MSP file applies to any installed products. If a valid installed product is found, triggers the Start-ADTMsiProcess function to patch the installation.

        Uses default config MSI parameters. You can use -AdditionalArgumentList to add additional parameters.

    .PARAMETER FilePath
        Path to the MSP file.

    .PARAMETER AdditionalArgumentList
        Additional parameters.

    .PARAMETER Username
        A username to invoke the process as. Only supported while running as the SYSTEM account.

    .PARAMETER UseLinkedAdminToken
        Use a user's linked administrative token while running the process under their context.

    .PARAMETER UseHighestAvailableToken
        Use a user's linked administrative token if it's available while running the process under their context.

    .PARAMETER InheritEnvironmentVariables
        Specifies whether the process running as a user should inherit the SYSTEM account's environment variables.

    .PARAMETER ExpandEnvironmentVariables
        Specifies whether to expand any Windows/DOS-style environment variables in the specified FilePath/ArgumentList.

    .PARAMETER DenyUserTermination
        Specifies that users cannot terminate the process started in their context. The user will still be able to terminate the process if they're an administrator, though.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Start-ADTMspProcessAsUser -FilePath 'Adobe_Reader_11.0.3_EN.msp'

        Executes the specified MSP file for Adobe Reader 11.0.3.

    .EXAMPLE
        Start-ADTMspProcessAsUser -FilePath 'AcroRdr2017Upd1701130143_MUI.msp' -AdditionalArgumentList 'ALLUSERS=1'

        Executes the specified MSP file for Acrobat Reader 2017 with additional parameters.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTMspProcessAsUser
    #>

    [CmdletBinding()]
    [OutputType([System.Int32])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = '$RunAsActiveUser.UserName')]
        [System.Security.Principal.NTAccount]$Username,

        [Parameter(Mandatory = $true, HelpMessage = 'Please supply the path to the MSP file to process.')]
        [ValidateScript({
                if ([System.IO.Path]::GetExtension($_) -notmatch '^\.msp$')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'The specified input has an invalid file extension.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$AdditionalArgumentList,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$UseHighestAvailableToken,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExpandEnvironmentVariables,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DenyUserTermination
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Convert the Username field into a RunAsActiveUser object as required by the subsystem.
        $gacsuParams = @{}; if ($PSBoundParameters.ContainsKey('Username'))
        {
            $gacsuParams.Add('Username', $Username)
            $gacsuParams.Add('AllowAnyValidSession', $true)
        }
        if (!($PSBoundParameters.RunAsActiveUser = Get-ADTClientServerUser @gacsuParams))
        {
            try
            {
                $naerParams = @{
                    Exception = [System.ArgumentNullException]::new("Could not find a valid logged on user session$(if ($PSBoundParameters.ContainsKey('Username')) { " for [$Username]" }).", $null)
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'NoActiveUserError'
                    TargetObject = $Username
                    RecommendedAction = "Please re-run this command while a user is logged onto the device and try again."
                }
                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
                return
            }
        }
        $null = $PSBoundParameters.Remove('Username')

        # Just farm it out to Start-ADTMspProcessAsUser as it can do it all.
        try
        {
            return Start-ADTMspProcessAsUser @PSBoundParameters
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
