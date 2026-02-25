#-----------------------------------------------------------------------------
#
# MARK: Get-ADTMsiTableProperty
#
#-----------------------------------------------------------------------------

function Get-ADTMsiTableProperty
{
    <#
    .SYNOPSIS
        Get all of the properties from a Windows Installer database table or the Summary Information stream and return as a custom object.

    .DESCRIPTION
        Use the Windows Installer object to read all of the properties from a Windows Installer database table or the Summary Information stream.

    .PARAMETER LiteralPath
        The fully qualified path to an database file. Supports .msi and .msp files.

    .PARAMETER TransformPath
        The fully qualified path to a list of MST file(s) which should be applied to the MSI file.

    .PARAMETER Table
        The name of the the MSI table from which all of the properties must be retrieved.

    .PARAMETER TablePropertyNameColumnNum
        Specify the table column number which contains the name of the properties.

    .PARAMETER TablePropertyValueColumnNum
        Specify the table column number which contains the value of the properties.

    .PARAMETER GetSummaryInformation
        Retrieves the Summary Information for the Windows Installer database.

        Summary Information property descriptions: https://msdn.microsoft.com/en-us/library/aa372049(v=vs.85).aspx

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]

        Returns a readonly dictionary with the properties as key/value pairs.

    .EXAMPLE
        Get-ADTMsiTableProperty -LiteralPath 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst'

        Retrieve all of the properties from the default 'Property' table.

    .EXAMPLE
        (Get-ADTMsiTableProperty -LiteralPath 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst' -Table 'Property').ProductCode

        Retrieve all of the properties from the 'Property' table, then retrieves just the 'ProductCode' member.

    .EXAMPLE
        Get-ADTMsiTableProperty -LiteralPath 'C:\Package\AppDeploy.msi' -GetSummaryInformation

        Retrieve the Summary Information for the Windows Installer database.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTMsiTableProperty
    #>

    [CmdletBinding(DefaultParameterSetName = 'TableInfo')]
    [OutputType([System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]])]
    [OutputType([PSADT.WindowsInstaller.MsiSummaryInfo])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (!(Test-Path -LiteralPath $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if (!(Test-Path -LiteralPath $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName TransformPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String[]]$TransformPath,

        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'MSI file: "Property"; MSP file: "MsiPatchMetadata"')]
        [System.String]$Table = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'MSI file: 1; MSP file: 2')]
        [System.Int32]$TablePropertyNameColumnNum,

        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'MSI file: 2; MSP file: 3')]
        [System.Int32]$TablePropertyValueColumnNum,

        [Parameter(Mandatory = $true, ParameterSetName = 'SummaryInfo')]
        [System.Management.Automation.SwitchParameter]$GetSummaryInformation
    )

    begin
    {
        # Set default values.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        if (!$PSBoundParameters.ContainsKey('Table'))
        {
            $Table = ('MsiPatchMetadata', 'Property')[[System.IO.Path]::GetExtension($LiteralPath) -eq '.msi']
        }
        if (!$PSBoundParameters.ContainsKey('TablePropertyNameColumnNum'))
        {
            $TablePropertyNameColumnNum = 2 - ([System.IO.Path]::GetExtension($LiteralPath) -eq '.msi')
        }
        if (!$PSBoundParameters.ContainsKey('TablePropertyValueColumnNum'))
        {
            $TablePropertyValueColumnNum = 3 - ([System.IO.Path]::GetExtension($LiteralPath) -eq '.msi')
        }
    }

    process
    {
        try
        {
            try
            {
                # Get either the requested windows database table information or summary information.
                if ($GetSummaryInformation)
                {
                    Write-ADTLogEntry -Message "Reading the Summary Information from the Windows Installer database file [$LiteralPath]."
                    return [PSADT.WindowsInstaller.MsiSummaryInfo]::Get($LiteralPath, $TransformPath)
                }
                Write-ADTLogEntry -Message "Reading data from Windows Installer database file [$LiteralPath] in table [$Table]."
                return [PSADT.WindowsInstaller.MsiUtilities]::GetMsiTableDictionary($LiteralPath, $Table, $TablePropertyNameColumnNum, $TablePropertyValueColumnNum, $TransformPath)
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to get the MSI table [$Table]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
