Function New-MsiTransform {
    <#
.SYNOPSIS

Create a transform file for an MSI database.

.DESCRIPTION

Create a transform file for an MSI database and create/modify properties in the Properties table.

.PARAMETER MsiPath

Specify the path to an MSI file.

.PARAMETER ApplyTransformPath

Specify the path to a transform which should be applied to the MSI database before any new properties are created or modified.

.PARAMETER NewTransformPath

Specify the path where the new transform file with the desired properties will be created. If a transform file of the same name already exists, it will be deleted before a new one is created.

Default is: a) If -ApplyTransformPath was specified but not -NewTransformPath, then <ApplyTransformPath>.new.mst
                b) If only -MsiPath was specified, then <MsiPath>.mst

.PARAMETER TransformProperties

Hashtable which contains calls to Set-MsiProperty for configuring the desired properties which should be included in new transform file.

Example hashtable: [Hashtable]$TransformProperties = @{ 'ALLUSERS' = '1' }

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE
    [Hashtable]$TransformProperties = {
        'ALLUSERS' = '1'
        'AgreeToLicense' = 'Yes'
        'REBOOT' = 'ReallySuppress'
        'RebootYesNo' = 'No'
        'ROOTDRIVE' = 'C:'
    }
    New-MsiTransform -MsiPath 'C:\Temp\PSADTInstall.msi' -TransformProperties $TransformProperties

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String]$MsiPath,
        [Parameter(Mandatory = $false)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String]$ApplyTransformPath,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$NewTransformPath,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [Hashtable]$TransformProperties,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        Write-ADTDebugHeader

        ## Define properties for how the MSI database is opened
        [Int32]$msiOpenDatabaseModeReadOnly = 0
        [Int32]$msiOpenDatabaseModeTransact = 1
        [Int32]$msiViewModifyUpdate = 2
        [Int32]$msiViewModifyReplace = 4
        [Int32]$msiViewModifyDelete = 6
        [Int32]$msiTransformErrorNone = 0
        [Int32]$msiTransformValidationNone = 0
        [Int32]$msiSuppressApplyTransformErrors = 63
    }
    Process {
        Try {
            Write-ADTLogEntry -Message "Creating a transform file for MSI [$MsiPath]."

            ## Discover the parent folder that the MSI file resides in
            [String]$MsiParentFolder = Split-Path -Path $MsiPath -Parent -ErrorAction 'Stop'

            ## Create a temporary file name for storing a second copy of the MSI database
            [String]$TempMsiPath = Join-Path -Path $MsiParentFolder -ChildPath ([IO.Path]::GetFileName(([IO.Path]::GetTempFileName()))) -ErrorAction 'Stop'

            ## Create a second copy of the MSI database
            Write-ADTLogEntry -Message "Copying MSI database in path [$MsiPath] to destination [$TempMsiPath]."
            $null = Copy-Item -LiteralPath $MsiPath -Destination $TempMsiPath -Force -ErrorAction 'Stop'

            ## Create a Windows Installer object
            [__ComObject]$Installer = New-Object -ComObject 'WindowsInstaller.Installer' -ErrorAction 'Stop'

            ## Open both copies of the MSI database
            #  Open the original MSI database in read only mode
            Write-ADTLogEntry -Message "Opening the MSI database [$MsiPath] in read only mode."
            [__ComObject]$MsiPathDatabase = Invoke-ADTObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($MsiPath, $msiOpenDatabaseModeReadOnly)
            #  Open the temporary copy of the MSI database in view/modify/update mode
            Write-ADTLogEntry -Message "Opening the MSI database [$TempMsiPath] in view/modify/update mode."
            [__ComObject]$TempMsiPathDatabase = Invoke-ADTObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($TempMsiPath, $msiViewModifyUpdate)

            ## If a MSI transform file was specified, then apply it to the temporary copy of the MSI database
            If ($ApplyTransformPath) {
                Write-ADTLogEntry -Message "Applying transform file [$ApplyTransformPath] to MSI database [$TempMsiPath]."
                $null = Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'ApplyTransform' -ArgumentList @($ApplyTransformPath, $msiSuppressApplyTransformErrors)
            }

            ## Determine the path for the new transform file that will be generated
            If (-not $NewTransformPath) {
                If ($ApplyTransformPath) {
                    [String]$NewTransformFileName = [IO.Path]::GetFileNameWithoutExtension($ApplyTransformPath) + '.new' + [IO.Path]::GetExtension($ApplyTransformPath)
                }
                Else {
                    [String]$NewTransformFileName = [IO.Path]::GetFileNameWithoutExtension($MsiPath) + '.mst'
                }
                [String]$NewTransformPath = Join-Path -Path $MsiParentFolder -ChildPath $NewTransformFileName -ErrorAction 'Stop'
            }

            ## Set the MSI properties in the temporary copy of the MSI database
            $TransformProperties.GetEnumerator() | ForEach-Object { Set-MsiProperty -DataBase $TempMsiPathDatabase -PropertyName $_.Key -PropertyValue $_.Value }

            ## Commit the new properties to the temporary copy of the MSI database
            $null = Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'Commit'

            ## Reopen the temporary copy of the MSI database in read only mode
            #  Release the database object for the temporary copy of the MSI database
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($TempMsiPathDatabase)
            #  Open the temporary copy of the MSI database in read only mode
            Write-ADTLogEntry -Message "Re-opening the MSI database [$TempMsiPath] in read only mode."
            [__ComObject]$TempMsiPathDatabase = Invoke-ADTObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($TempMsiPath, $msiOpenDatabaseModeReadOnly)

            ## Delete the new transform file path if it already exists
            If (Test-Path -LiteralPath $NewTransformPath -PathType 'Leaf' -ErrorAction 'Stop') {
                Write-ADTLogEntry -Message "A transform file of the same name already exists. Deleting transform file [$NewTransformPath]."
                $null = Remove-Item -LiteralPath $NewTransformPath -Force -ErrorAction 'Stop'
            }

            ## Generate the new transform file by taking the difference between the temporary copy of the MSI database and the original MSI database
            Write-ADTLogEntry -Message "Generating new transform file [$NewTransformPath]."
            $null = Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'GenerateTransform' -ArgumentList @($MsiPathDatabase, $NewTransformPath)
            $null = Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'CreateTransformSummaryInfo' -ArgumentList @($MsiPathDatabase, $NewTransformPath, $msiTransformErrorNone, $msiTransformValidationNone)

            If (Test-Path -LiteralPath $NewTransformPath -PathType 'Leaf' -ErrorAction 'Stop') {
                Write-ADTLogEntry -Message "Successfully created new transform file in path [$NewTransformPath]."
            }
            Else {
                Throw "Failed to generate transform file in path [$NewTransformPath]."
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to create new transform file in path [$NewTransformPath].`n$(Resolve-ADTError)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to create new transform file in path [$NewTransformPath]: $($_.Exception.Message)"
            }
        }
        Finally {
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($TempMsiPathDatabase)
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($MsiPathDatabase)
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer)
            }
            Catch {
            }
            Try {
                ## Delete the temporary copy of the MSI database
                If (Test-Path -LiteralPath $TempMsiPath -PathType 'Leaf' -ErrorAction 'Stop') {
                    $null = Remove-Item -LiteralPath $TempMsiPath -Force -ErrorAction 'Stop'
                }
            }
            Catch {
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
