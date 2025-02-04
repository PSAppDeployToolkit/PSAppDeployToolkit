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

    .PARAMETER Path
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
        Get-ADTMsiTableProperty -Path 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst'

        Retrieve all of the properties from the default 'Property' table.

    .EXAMPLE
        (Get-ADTMsiTableProperty -Path 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst' -Table 'Property').ProductCode

        Retrieve all of the properties from the 'Property' table, then retrieves just the 'ProductCode' member.

    .EXAMPLE
        Get-ADTMsiTableProperty -Path 'C:\Package\AppDeploy.msi' -GetSummaryInformation

        Retrieve the Summary Information for the Windows Installer database.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding(DefaultParameterSetName = 'TableInfo')]
    [OutputType([System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]])]
    [OutputType([PSADT.Types.MsiSummaryInfo])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (!(Test-Path -Path $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if (!(Test-Path -Path $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName TransformPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String[]]$TransformPath,

        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'MSI file: "Property"; MSP file: "MsiPatchMetadata"')]
        [System.String]$Table,

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
        if (!$PSBoundParameters.ContainsKey('Table'))
        {
            $Table = ('MsiPatchMetadata', 'Property')[[System.IO.Path]::GetExtension($Path) -eq '.msi']
        }
        if (!$PSBoundParameters.ContainsKey('TablePropertyNameColumnNum'))
        {
            $TablePropertyNameColumnNum = 2 - ([System.IO.Path]::GetExtension($Path) -eq '.msi')
        }
        if (!$PSBoundParameters.ContainsKey('TablePropertyValueColumnNum'))
        {
            $TablePropertyValueColumnNum = 3 - ([System.IO.Path]::GetExtension($Path) -eq '.msi')
        }

        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        if ($PSCmdlet.ParameterSetName -eq 'TableInfo')
        {
            Write-ADTLogEntry -Message "Reading data from Windows Installer database file [$Path] in table [$Table]."
        }
        else
        {
            Write-ADTLogEntry -Message "Reading the Summary Information from the Windows Installer database file [$Path]."
        }
        try
        {
            try
            {
                # Create a Windows Installer object and define properties for how the MSI database is opened
                $Installer = New-Object -ComObject WindowsInstaller.Installer
                $msiOpenDatabaseModeReadOnly = 0
                $msiSuppressApplyTransformErrors = 63
                $msiOpenDatabaseModePatchFile = 32
                $msiOpenDatabaseMode = if (($IsMspFile = [IO.Path]::GetExtension($Path) -eq '.msp'))
                {
                    $msiOpenDatabaseModePatchFile
                }
                else
                {
                    $msiOpenDatabaseModeReadOnly
                }

                # Open database in read only mode and apply a list of transform(s).
                $Database = Invoke-ADTObjectMethod -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($Path, $msiOpenDatabaseMode)
                if ($TransformPath -and !$IsMspFile)
                {
                    $null = foreach ($Transform in $TransformPath)
                    {
                        Invoke-ADTObjectMethod -InputObject $Database -MethodName ApplyTransform -ArgumentList @($Transform, $msiSuppressApplyTransformErrors)
                    }
                }

                # Get either the requested windows database table information or summary information.
                if ($GetSummaryInformation)
                {
                    # Get the SummaryInformation from the windows installer database.
                    # Summary property descriptions: https://msdn.microsoft.com/en-us/library/aa372049(v=vs.85).aspx
                    $SummaryInformation = Get-ADTObjectProperty -InputObject $Database -PropertyName SummaryInformation
                    return [PSADT.Types.MsiSummaryInfo]::new(
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(1)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(2)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(3)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(4)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(5)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(6)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(7)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(8)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(9)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(11)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(12)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(13)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(14)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(15)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(16)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(18)),
                        (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName Property -ArgumentList @(19))
                    )
                }

                # Open the requested table view from the database.
                $TableProperties = [System.Collections.Generic.Dictionary[System.String, System.Object]]::new()
                $View = Invoke-ADTObjectMethod -InputObject $Database -MethodName OpenView -ArgumentList @("SELECT * FROM $Table")
                $null = Invoke-ADTObjectMethod -InputObject $View -MethodName Execute

                # Retrieve the first row from the requested table. If the first row was successfully retrieved, then save data and loop through the entire table.
                # https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
                while (($Record = Invoke-ADTObjectMethod -InputObject $View -MethodName Fetch))
                {
                    $TableProperties.Add((Get-ADTObjectProperty -InputObject $Record -PropertyName StringData -ArgumentList @($TablePropertyNameColumnNum)), (Get-ADTObjectProperty -InputObject $Record -PropertyName StringData -ArgumentList @($TablePropertyValueColumnNum)))
                }

                # Return the accumulated results. We can't use a custom object for this as we have no idea what's going to be in the properties of a given MSI.
                # We also can't use a pscustomobject accelerator here as the MSI may have the same keys with different casing, necessitating the use of a dictionary for storage.
                if ($TableProperties.Count)
                {
                    return [System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]][System.Collections.ObjectModel.ReadOnlyDictionary[System.String, System.Object]]$TableProperties
                }
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
        finally
        {
            # Release all COM objects to prevent file locks.
            $null = foreach ($variable in (Get-Variable -Name View, SummaryInformation, Database, Installer -ValueOnly -ErrorAction Ignore))
            {
                try
                {
                    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($variable)
                }
                catch
                {
                    $null
                }
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
