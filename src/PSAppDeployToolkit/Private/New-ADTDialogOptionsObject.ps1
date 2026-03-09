#-----------------------------------------------------------------------------
#
# MARK: New-ADTDialogOptionsObject
#
#-----------------------------------------------------------------------------

function Private:New-ADTDialogOptionsObject
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Type]$Type,

        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Foundation.ValidateNotNullOrWhiteSpace()]
        [System.Collections.Hashtable]$Data,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSAppDeployToolkit.Foundation.DeploymentType]$DeploymentType
    )

    # Map the values out of the dialog system to config values.
    $dialogAssetMap = @{
        AppIconImage = 'Logo'
        AppIconDarkImage = 'LogoDark'
        AppBannerImage = 'Banner'
        AppTaskbarIconImage = 'TaskbarIcon'
        TrayIcon = 'Logo'
    }

    # Spin until this works.
    while ($true)
    {
        try
        {
            if ($PSBoundParameters.ContainsKey('DeploymentType'))
            {
                return $Type::new($DeploymentType, $Data)
            }
            else
            {
                return $Type::new($Data)
            }
        }
        catch
        {
            if (($_.Exception.InnerException -isnot [System.BadImageFormatException]) -or !($dialogAssetKey = $dialogAssetMap[($dialogAssetName = $_.Exception.InnerException.FileName)]))
            {
                $PSCmdlet.ThrowTerminatingError($_)
            }
            if ($null -ne ($dialogAssetValue = $Script:ADT.ModuleDefaults.Config.([System.String]::Empty).Ast.EndBlock.Statements.PipelineElements.Expression.KeyValuePairs.Where({ $_.Item1.Value.Equals('Assets') }).Item2.PipelineElements.Expression.KeyValuePairs.Where({ $_.Item1.Value.Equals($dialogAssetKey) }).Item2.PipelineElements.Expression | Select-Object -ExpandProperty Value -ErrorAction Ignore))
            {
                Write-ADTLogEntry -Message "$($_.Exception.InnerException.Message.Replace($dialogAssetName, $dialogAssetKey).TrimEnd('.')): $($_.Exception.InnerException.InnerException.Message.TrimEnd('.')). Substituting with default asset." -Severity Warning
                $Data.$dialogAssetName = $dialogAssetValue.PSObject.BaseObject
            }
            else
            {
                Write-ADTLogEntry -Message "$($_.Exception.InnerException.Message.Replace($dialogAssetName, $dialogAssetKey).TrimEnd('.')): $($_.Exception.InnerException.InnerException.Message.TrimEnd('.')). Removing non-essential asset." -Severity Warning
                $Data.Remove($dialogAssetName)
            }
        }
    }
}
