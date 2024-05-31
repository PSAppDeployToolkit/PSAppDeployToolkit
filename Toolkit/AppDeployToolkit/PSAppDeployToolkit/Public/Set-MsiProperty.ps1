Function Set-MsiProperty {
    <#
.SYNOPSIS

Set a property in the MSI property table.

.DESCRIPTION

Set a property in the MSI property table.

.PARAMETER DataBase

Specify a ComObject representing an MSI database opened in view/modify/update mode.

.PARAMETER PropertyName

The name of the property to be set/modified.

.PARAMETER PropertyValue

The value of the property to be set/modified.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-MsiProperty -DataBase $TempMsiPathDatabase -PropertyName 'ALLUSERS' -PropertyValue '1'

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [__ComObject]$DataBase,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$PropertyName,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$PropertyValue,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        Write-ADTDebugHeader
    }
    Process {
        Try {
            Write-ADTLogEntry -Message "Setting the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]."

            ## Open the requested table view from the database
            [__ComObject]$View = Invoke-ADTObjectMethod -InputObject $DataBase -MethodName 'OpenView' -ArgumentList @("SELECT * FROM Property WHERE Property='$PropertyName'")
            $null = Invoke-ADTObjectMethod -InputObject $View -MethodName 'Execute'

            ## Retrieve the requested property from the requested table.
            #  https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
            [__ComObject]$Record = Invoke-ADTObjectMethod -InputObject $View -MethodName 'Fetch'

            ## Close the previous view on the MSI database
            $null = Invoke-ADTObjectMethod -InputObject $View -MethodName 'Close' -ArgumentList @()
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($View)

            ## Set the MSI property
            If ($Record) {
                #  If the property already exists, then create the view for updating the property
                [__ComObject]$View = Invoke-ADTObjectMethod -InputObject $DataBase -MethodName 'OpenView' -ArgumentList @("UPDATE Property SET Value='$PropertyValue' WHERE Property='$PropertyName'")
            }
            Else {
                #  If property does not exist, then create view for inserting the property
                [__ComObject]$View = Invoke-ADTObjectMethod -InputObject $DataBase -MethodName 'OpenView' -ArgumentList @("INSERT INTO Property (Property, Value) VALUES ('$PropertyName','$PropertyValue')")
            }
            #  Execute the view to set the MSI property
            $null = Invoke-ADTObjectMethod -InputObject $View -MethodName 'Execute'
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to set the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to set the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]: $($_.Exception.Message)"
            }
        }
        Finally {
            Try {
                If ($View) {
                    $null = Invoke-ADTObjectMethod -InputObject $View -MethodName 'Close' -ArgumentList @()
                    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($View)
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
