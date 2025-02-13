#-----------------------------------------------------------------------------
#
# MARK: Add-ADTEdgeExtension
#
#-----------------------------------------------------------------------------

function Add-ADTEdgeExtension
{
    <#
    .SYNOPSIS
        Adds an extension for Microsoft Edge using the ExtensionSettings policy.

    .DESCRIPTION
        This function adds an extension for Microsoft Edge using the ExtensionSettings policy: https://learn.microsoft.com/en-us/deployedge/microsoft-edge-manage-extensions-ref-guide.

        This enables Edge Extensions to be installed and managed like applications, enabling extensions to be pushed to specific devices or users alongside existing GPO/Intune extension policies.

        This should not be used in conjunction with Edge Management Service which leverages the same registry key to configure Edge extensions.

    .PARAMETER ExtensionID
        The ID of the extension to add.

    .PARAMETER UpdateUrl
        The update URL of the extension. This is the URL where the extension will check for updates.

    .PARAMETER InstallationMode
        The installation mode of the extension. Allowed values: blocked, allowed, removed, force_installed, normal_installed.

    .PARAMETER MinimumVersionRequired
        The minimum version of the extension required for installation.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Add-ADTEdgeExtension -ExtensionID "extensionID" -InstallationMode "force_installed" -UpdateUrl "https://edge.microsoft.com/extensionwebstorebase/v1/crx"

        This example adds the specified extension to be force installed in Microsoft Edge.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Add-ADTEdgeExtension
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ExtensionID,

        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (![System.Uri]::IsWellFormedUriString($_, [System.UriKind]::Absolute))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName UpdateUrl -ProvidedValue $_ -ExceptionMessage 'The specified input is not a valid URL.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$UpdateUrl,

        [Parameter(Mandatory = $true)]
        [ValidateSet('blocked', 'allowed', 'removed', 'force_installed', 'normal_installed')]
        [System.String]$InstallationMode,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$MinimumVersionRequired
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message "Adding extension with ID [$ExtensionID] using installation mode [$InstallationMode] and update URL [$UpdateUrl]$(if ($MinimumVersionRequired) {" with minimum version required [$MinimumVersionRequired]"})."
        try
        {
            try
            {
                # Set up the additional extension.
                $additionalExtension = @{
                    installation_mode = $InstallationMode
                    update_url = $UpdateUrl
                }

                # Add in the minimum version if specified.
                if ($MinimumVersionRequired)
                {
                    $additionalExtension.Add('minimum_version_required', $MinimumVersionRequired)
                }

                # Get the current extensions from the registry, add our additional one, then convert the result back to JSON.
                $extensionsSettings = Get-ADTEdgeExtensions |
                    Add-Member -Name $ExtensionID -Value $additionalExtension -MemberType NoteProperty -Force -PassThru |
                    ConvertTo-Json -Compress

                # Add the additional extension to the current values, then re-write the definition in the registry.
                $null = Set-ADTRegistryKey -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings -Value $extensionsSettings
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
