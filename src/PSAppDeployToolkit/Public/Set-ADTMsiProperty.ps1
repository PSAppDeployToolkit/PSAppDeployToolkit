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
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Set-ADTMsiProperty -Database $TempMsiPathDatabase -PropertyName 'ALLUSERS' -PropertyValue '1'

    .NOTES
        An active ADT session is NOT required to use this function.

        Original Author: Julian DA CUNHA - dacunha.julian@gmail.com, used with permission.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTMsiProperty
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
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
        $View = $null
    }

    process
    {
        Write-ADTLogEntry -Message "Setting the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]."
        try
        {
            try
            {
                # Open the requested table view from the database.
                $View = Invoke-ADTObjectMethod -InputObject $Database -MethodName OpenView -ArgumentList @("SELECT * FROM Property WHERE Property='$PropertyName'")
                $null = Invoke-ADTObjectMethod -InputObject $View -MethodName Execute

                # Retrieve the requested property from the requested table and close off the view.
                # https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
                $Record = Invoke-ADTObjectMethod -InputObject $View -MethodName Fetch
                $null = Invoke-ADTObjectMethod -InputObject $View -MethodName Close
                $null = [System.Runtime.InteropServices.Marshal]::ReleaseComObject($View)

                # Set the MSI property.
                $View = if ($Record)
                {
                    # If the property already exists, then create the view for updating the property.
                    Invoke-ADTObjectMethod -InputObject $Database -MethodName OpenView -ArgumentList @("UPDATE Property SET Value='$PropertyValue' WHERE Property='$PropertyName'")
                }
                else
                {
                    # If property does not exist, then create view for inserting the property.
                    Invoke-ADTObjectMethod -InputObject $Database -MethodName OpenView -ArgumentList @("INSERT INTO Property (Property, Value) VALUES ('$PropertyName','$PropertyValue')")
                }
                $null = Invoke-ADTObjectMethod -InputObject $View -MethodName Execute
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to set the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]."
        }
        finally
        {
            $null = try
            {
                if ($View)
                {
                    Invoke-ADTObjectMethod -InputObject $View -MethodName Close
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
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
