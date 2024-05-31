Function Get-MsiTableProperty {
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

The name of the the MSI table from which all of the properties must be retrieved. Default is: 'Property'.

.PARAMETER TablePropertyNameColumnNum

Specify the table column number which contains the name of the properties. Default is: 1 for MSIs and 2 for MSPs.

.PARAMETER TablePropertyValueColumnNum

Specify the table column number which contains the value of the properties. Default is: 2 for MSIs and 3 for MSPs.

.PARAMETER GetSummaryInformation

Retrieves the Summary Information for the Windows Installer database.

Summary Information property descriptions: https://msdn.microsoft.com/en-us/library/aa372049(v=vs.85).aspx

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Management.Automation.PSObject

Returns a custom object with the following properties: 'Name' and 'Value'.

.EXAMPLE

Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst'

Retrieve all of the properties from the default 'Property' table.

.EXAMPLE

Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst' -Table 'Property' | Select-Object -ExpandProperty ProductCode

Retrieve all of the properties from the 'Property' table and then pipe to Select-Object to select the ProductCode property.

.EXAMPLE

Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -GetSummaryInformation

Retrieves the Summary Information for the Windows Installer database.

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding(DefaultParameterSetName = 'TableInfo')]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String[]]$TransformPath,
        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullOrEmpty()]
        [String]$Table = $(If ([IO.Path]::GetExtension($Path) -eq '.msi') {
                'Property'
            }
            Else {
                'MsiPatchMetadata'
            }),
        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullorEmpty()]
        [Int32]$TablePropertyNameColumnNum = $(If ([IO.Path]::GetExtension($Path) -eq '.msi') {
                1
            }
            Else {
                2
            }),
        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullorEmpty()]
        [Int32]$TablePropertyValueColumnNum = $(If ([IO.Path]::GetExtension($Path) -eq '.msi') {
                2
            }
            Else {
                3
            }),
        [Parameter(Mandatory = $true, ParameterSetName = 'SummaryInfo')]
        [ValidateNotNullorEmpty()]
        [Switch]$GetSummaryInformation = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        Write-ADTDebugHeader
    }
    Process {
        Try {
            If ($PSCmdlet.ParameterSetName -eq 'TableInfo') {
                Write-ADTLogEntry -Message "Reading data from Windows Installer database file [$Path] in table [$Table]."
            }
            Else {
                Write-ADTLogEntry -Message "Reading the Summary Information from the Windows Installer database file [$Path]."
            }

            ## Create a Windows Installer object
            [__ComObject]$Installer = New-Object -ComObject 'WindowsInstaller.Installer' -ErrorAction 'Stop'
            ## Determine if the database file is a patch (.msp) or not
            [Boolean]$IsMspFile = [IO.Path]::GetExtension($Path) -eq '.msp'
            ## Define properties for how the MSI database is opened
            [Int32]$msiOpenDatabaseModeReadOnly = 0
            [Int32]$msiSuppressApplyTransformErrors = 63
            [Int32]$msiOpenDatabaseMode = $msiOpenDatabaseModeReadOnly
            [Int32]$msiOpenDatabaseModePatchFile = 32
            If ($IsMspFile) {
                [Int32]$msiOpenDatabaseMode = $msiOpenDatabaseModePatchFile
            }
            ## Open database in read only mode
            [__ComObject]$Database = Invoke-ADTObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($Path, $msiOpenDatabaseMode)
            ## Apply a list of transform(s) to the database
            If (($TransformPath) -and (-not $IsMspFile)) {
                ForEach ($Transform in $TransformPath) {
                    $null = Invoke-ADTObjectMethod -InputObject $Database -MethodName 'ApplyTransform' -ArgumentList @($Transform, $msiSuppressApplyTransformErrors)
                }
            }

            ## Get either the requested windows database table information or summary information
            If ($PSCmdlet.ParameterSetName -eq 'TableInfo') {
                ## Open the requested table view from the database
                [__ComObject]$View = Invoke-ADTObjectMethod -InputObject $Database -MethodName 'OpenView' -ArgumentList @("SELECT * FROM $Table")
                $null = Invoke-ADTObjectMethod -InputObject $View -MethodName 'Execute'

                ## Create an empty object to store properties in
                [PSObject]$TableProperties = New-Object -TypeName 'PSObject'

                ## Retrieve the first row from the requested table. If the first row was successfully retrieved, then save data and loop through the entire table.
                #  https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
                [__ComObject]$Record = Invoke-ADTObjectMethod -InputObject $View -MethodName 'Fetch'
                While ($Record) {
                    #  Read string data from record and add property/value pair to custom object
                    $TableProperties | Add-Member -MemberType 'NoteProperty' -Name (Get-ADTObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @($TablePropertyNameColumnNum)) -Value (Get-ADTObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @($TablePropertyValueColumnNum)) -Force
                    #  Retrieve the next row in the table
                    [__ComObject]$Record = Invoke-ADTObjectMethod -InputObject $View -MethodName 'Fetch'
                }
                Write-Output -InputObject ($TableProperties)
            }
            Else {
                ## Get the SummaryInformation from the windows installer database
                [__ComObject]$SummaryInformation = Get-ADTObjectProperty -InputObject $Database -PropertyName 'SummaryInformation'
                [Hashtable]$SummaryInfoProperty = @{}
                ## Summary property descriptions: https://msdn.microsoft.com/en-us/library/aa372049(v=vs.85).aspx
                $SummaryInfoProperty.Add('CodePage', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(1)))
                $SummaryInfoProperty.Add('Title', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(2)))
                $SummaryInfoProperty.Add('Subject', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(3)))
                $SummaryInfoProperty.Add('Author', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(4)))
                $SummaryInfoProperty.Add('Keywords', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(5)))
                $SummaryInfoProperty.Add('Comments', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(6)))
                $SummaryInfoProperty.Add('Template', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(7)))
                $SummaryInfoProperty.Add('LastSavedBy', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(8)))
                $SummaryInfoProperty.Add('RevisionNumber', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(9)))
                $SummaryInfoProperty.Add('LastPrinted', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(11)))
                $SummaryInfoProperty.Add('CreateTimeDate', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(12)))
                $SummaryInfoProperty.Add('LastSaveTimeDate', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(13)))
                $SummaryInfoProperty.Add('PageCount', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(14)))
                $SummaryInfoProperty.Add('WordCount', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(15)))
                $SummaryInfoProperty.Add('CharacterCount', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(16)))
                $SummaryInfoProperty.Add('CreatingApplication', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(18)))
                $SummaryInfoProperty.Add('Security', (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(19)))
                [PSObject]$SummaryInfoProperties = New-Object -TypeName 'PSObject' -Property $SummaryInfoProperty
                Write-Output -InputObject ($SummaryInfoProperties)
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to get the MSI table [$Table]. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to get the MSI table [$Table]: $($_.Exception.Message)"
            }
        }
        Finally {
            Try {
                If ($View) {
                    $null = Invoke-ADTObjectMethod -InputObject $View -MethodName 'Close' -ArgumentList @()
                    Try {
                        $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($View)
                    }
                    Catch {
                    }
                }
                ElseIf ($SummaryInformation) {
                    Try {
                        $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($SummaryInformation)
                    }
                    Catch {
                    }
                }
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($DataBase)
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer)
            }
            Catch {
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
