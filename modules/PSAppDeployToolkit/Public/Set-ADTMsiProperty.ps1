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

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Set-ADTMsiProperty.ps1
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

        # Internal worker to run a statement against the database with its values supplied out of band.
        # The Windows Installer query engine has no escape sequence for a quote inside a literal, so the
        # values are bound to markers in the statement rather than written into it.
        function Invoke-ADTMsiDatabaseStatement
        {
            [CmdletBinding()]
            [OutputType([System.Object])]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.__ComObject]$Database,

                [Parameter(Mandatory = $true)]
                [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
                [System.String]$Statement,

                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String[]]$Values,

                [Parameter(Mandatory = $false)]
                [System.Management.Automation.SwitchParameter]$Fetch
            )

            $record = $installer.GetType().InvokeMember('CreateRecord', [System.Reflection.BindingFlags]::InvokeMethod, $null, $installer, @($Values.Length))
            try
            {
                for ($i = 0; $i -lt $Values.Length; $i++)
                {
                    $null = $record.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $record, @(($i + 1), $Values[$i]))
                }
                $view = $Database.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Database, @($Statement))
                try
                {
                    $null = $view.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $view, @($record))
                    if ($Fetch)
                    {
                        return $view.GetType().InvokeMember('Fetch', [System.Reflection.BindingFlags]::InvokeMethod, $null, $view, $null)
                    }
                }
                finally
                {
                    $null = $view.GetType().InvokeMember('Close', [System.Reflection.BindingFlags]::InvokeMethod, $null, $view, $null)
                    $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($view)
                }
            }
            finally
            {
                $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($record)
            }
        }
        $installer = New-Object -ComObject WindowsInstaller.Installer
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
                # Retrieve the requested property from the requested table to find out whether it is there.
                # https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
                if (Invoke-ADTMsiDatabaseStatement -Database $Database -Statement 'SELECT * FROM Property WHERE Property=?' -Values $PropertyName -Fetch)
                {
                    # If the property already exists, update it in place.
                    $null = Invoke-ADTMsiDatabaseStatement -Database $Database -Statement 'UPDATE Property SET Value=? WHERE Property=?' -Values $PropertyValue, $PropertyName
                }
                else
                {
                    # If the property does not exist, add it to the table.
                    $null = Invoke-ADTMsiDatabaseStatement -Database $Database -Statement 'INSERT INTO Property (Property, Value) VALUES (?, ?)' -Values $PropertyName, $PropertyValue
                }
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
    }

    end
    {
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
