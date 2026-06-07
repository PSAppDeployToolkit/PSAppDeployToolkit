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
        This function is deprecated and will be removed in PSAppDeployToolkit 4.3.0.

        An active ADT session is NOT required to use this function.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTMsiProperty
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.__ComObject]$Database,

        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$PropertyName,

        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$PropertyValue
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated and will be removed in PSAppDeployToolkit 4.3.0." -Severity Warning
        $View = $null
        $Installer = $null
        $SelectRecord = $null
        $WriteRecord = $null
        $FetchedRecord = $null
    }

    process
    {
        Write-ADTLogEntry -Message "Setting the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]."
        if (!$PSCmdlet.ShouldProcess("MSI Property [$PropertyName]", 'Set'))
        {
            return
        }
        try
        {
            try
            {
                # Create a WindowsInstaller.Installer instance for building parameterized MSI records.
                # Using parameterized queries (? placeholders + Record objects) avoids SQL injection
                # and correctly handles property names/values that contain single quotes.
                $Installer = New-Object -ComObject WindowsInstaller.Installer

                # Open a parameterized SELECT to determine whether the property already exists.
                # https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
                $View = $Database.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Database, @('SELECT Value FROM Property WHERE Property = ?'))
                $SelectRecord = $Installer.GetType().InvokeMember('CreateRecord', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Installer, @(1))
                $null = $SelectRecord.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $SelectRecord, @(1, $PropertyName))
                $null = $View.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $View, @($SelectRecord))
                $FetchedRecord = $View.GetType().InvokeMember('Fetch', [System.Reflection.BindingFlags]::InvokeMethod, $null, $View, @())
                $null = $View.GetType().InvokeMember('Close', [System.Reflection.BindingFlags]::InvokeMethod, $null, $View, @())
                $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($View)
                $View = $null
                $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($SelectRecord)
                $SelectRecord = $null

                # Set the MSI property using a parameterized UPDATE or INSERT query as appropriate.
                if ($null -ne $FetchedRecord)
                {
                    # Property exists: UPDATE the existing row.
                    $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($FetchedRecord)
                    $FetchedRecord = $null
                    $View = $Database.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Database, @('UPDATE Property SET Value = ? WHERE Property = ?'))
                    $WriteRecord = $Installer.GetType().InvokeMember('CreateRecord', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Installer, @(2))
                    $null = $WriteRecord.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $WriteRecord, @(1, $PropertyValue))
                    $null = $WriteRecord.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $WriteRecord, @(2, $PropertyName))
                }
                else
                {
                    # Property does not exist: INSERT a new row.
                    $View = $Database.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Database, @('INSERT INTO Property (Property, Value) VALUES (?, ?)'))
                    $WriteRecord = $Installer.GetType().InvokeMember('CreateRecord', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Installer, @(2))
                    $null = $WriteRecord.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $WriteRecord, @(1, $PropertyName))
                    $null = $WriteRecord.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $WriteRecord, @(2, $PropertyValue))
                }
                $null = $View.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $View, @($WriteRecord))
                $null = $View.GetType().InvokeMember('Close', [System.Reflection.BindingFlags]::InvokeMethod, $null, $View, @())
                $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($View)
                $View = $null
                $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($WriteRecord)
                $WriteRecord = $null
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
            if ($null -ne $FetchedRecord) { $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($FetchedRecord) }
            if ($null -ne $WriteRecord) { $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($WriteRecord) }
            if ($null -ne $SelectRecord) { $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($SelectRecord) }
            if ($null -ne $View) { $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($View) }
            if ($null -ne $Installer)
            {
                $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($Installer)
                $Installer = $null
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
