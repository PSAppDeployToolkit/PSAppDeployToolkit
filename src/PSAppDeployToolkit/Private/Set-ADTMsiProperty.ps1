#-----------------------------------------------------------------------------
#
# MARK: Set-ADTMsiProperty
#
#-----------------------------------------------------------------------------

function Set-ADTMsiProperty
{
    <#

    .SYNOPSIS
    Set a property in the MSI property table.

    .DESCRIPTION
    Set a property in the MSI property table.

    .PARAMETER Database
    Specify a ComObject representing an MSI database opened in view/modify/update mode.

    .PARAMETER PropertyName
    The name of the property to be set/modified.

    .PARAMETER PropertyValue
    The value of the property to be set/modified.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Set-ADTMsiProperty -Database $TempMsiPathDatabase -PropertyName 'ALLUSERS' -PropertyValue '1'

    .NOTES
    This is an internal script function and should typically not be called directly.

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.__ComObject]$Database,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$PropertyName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$PropertyValue
    )

    begin
    {
        # Make this function continue on error.
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Setting the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]."
        try
        {
            try
            {
                # Open the requested table view from the database.
                $View = & $Script:CommandTable.'Invoke-ADTObjectMethod' -InputObject $Database -MethodName OpenView -ArgumentList @("SELECT * FROM Property WHERE Property='$PropertyName'")
                $null = & $Script:CommandTable.'Invoke-ADTObjectMethod' -InputObject $View -MethodName Execute

                # Retrieve the requested property from the requested table and close off the view.
                # https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
                $Record = & $Script:CommandTable.'Invoke-ADTObjectMethod' -InputObject $View -MethodName Fetch
                $null = & $Script:CommandTable.'Invoke-ADTObjectMethod' -InputObject $View -MethodName Close -ArgumentList @()
                $null = [System.Runtime.InteropServices.Marshal]::ReleaseComObject($View)

                # Set the MSI property.
                $View = if ($Record)
                {
                    # If the property already exists, then create the view for updating the property.
                    & $Script:CommandTable.'Invoke-ADTObjectMethod' -InputObject $Database -MethodName OpenView -ArgumentList @("UPDATE Property SET Value='$PropertyValue' WHERE Property='$PropertyName'")
                }
                else
                {
                    # If property does not exist, then create view for inserting the property.
                    & $Script:CommandTable.'Invoke-ADTObjectMethod' -InputObject $Database -MethodName OpenView -ArgumentList @("INSERT INTO Property (Property, Value) VALUES ('$PropertyName','$PropertyValue')")
                }
                $null = & $Script:CommandTable.'Invoke-ADTObjectMethod' -InputObject $View -MethodName Execute
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to set the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]."
        }
        finally
        {
            $null = try
            {
                if (& $Script:CommandTable.'Test-Path' -LiteralPath Microsoft.PowerShell.Core\Variable::View)
                {
                    & $Script:CommandTable.'Invoke-ADTObjectMethod' -InputObject $View -MethodName Close -ArgumentList @()
                    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($View)
                }
            }
            catch
            {
                $null
            }
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
