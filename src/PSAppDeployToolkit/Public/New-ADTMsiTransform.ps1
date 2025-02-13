#-----------------------------------------------------------------------------
#
# MARK: New-ADTMsiTransform
#
#-----------------------------------------------------------------------------

function New-ADTMsiTransform
{
    <#
    .SYNOPSIS
        Create a transform file for an MSI database.

    .DESCRIPTION
        Create a transform file for an MSI database and create/modify properties in the Properties table. This function allows you to specify an existing transform to apply before making changes and to define the path for the new transform file. If the new transform file already exists, it will be deleted before creating a new one.

    .PARAMETER MsiPath
        Specify the path to an MSI file.

    .PARAMETER ApplyTransformPath
        Specify the path to a transform which should be applied to the MSI database before any new properties are created or modified.

    .PARAMETER NewTransformPath
        Specify the path where the new transform file with the desired properties will be created. If a transform file of the same name already exists, it will be deleted before a new one is created.

    .PARAMETER TransformProperties
        Hashtable which contains calls to `Set-ADTMsiProperty` for configuring the desired properties which should be included in the new transform file.

        Example hashtable: @{ ALLUSERS = 1 }

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        New-ADTMsiTransform -MsiPath 'C:\Temp\PSADTInstall.msi' -TransformProperties @{
            ALLUSERS = 1
            AgreeToLicense = 'Yes'
            REBOOT = 'ReallySuppress'
            RebootYesNo = 'No'
            ROOTDRIVE = 'C:'
        }

        Creates a new transform file for the specified MSI with the given properties.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/New-ADTMsiTransform
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = "This function does not change system state.")]
    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (!(Test-Path -Path $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName MsiPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$MsiPath,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if (!(Test-Path -Path $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ApplyTransformPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$ApplyTransformPath,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'If `-ApplyTransformPath` was specified: `<ApplyTransformPath>.new.mst`; If only `-MsiPath` was specified: `<MsiPath>.mst`')]
        [System.String]$NewTransformPath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$TransformProperties
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Define properties for how the MSI database is opened.
        $msiOpenDatabaseTypes = @{
            OpenDatabaseModeReadOnly = 0
            OpenDatabaseModeTransact = 1
            ViewModifyUpdate = 2
            ViewModifyReplace = 4
            ViewModifyDelete = 6
            TransformErrorNone = 0
            TransformValidationNone = 0
            SuppressApplyTransformErrors = 63
        }
    }

    process
    {
        Write-ADTLogEntry -Message "Creating a transform file for MSI [$MsiPath]."
        try
        {
            try
            {
                # Create a second copy of the MSI database.
                $MsiParentFolder = Split-Path -Path $MsiPath -Parent
                $TempMsiPath = Join-Path -Path $MsiParentFolder -ChildPath ([System.IO.Path]::GetRandomFileName())
                Write-ADTLogEntry -Message "Copying MSI database in path [$MsiPath] to destination [$TempMsiPath]."
                $null = Copy-Item -LiteralPath $MsiPath -Destination $TempMsiPath -Force

                # Open both copies of the MSI database.
                Write-ADTLogEntry -Message "Opening the MSI database [$MsiPath] in read only mode."
                $Installer = New-Object -ComObject WindowsInstaller.Installer
                $MsiPathDatabase = Invoke-ADTObjectMethod -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($MsiPath, $msiOpenDatabaseTypes.OpenDatabaseModeReadOnly)
                Write-ADTLogEntry -Message "Opening the MSI database [$TempMsiPath] in view/modify/update mode."
                $TempMsiPathDatabase = Invoke-ADTObjectMethod -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($TempMsiPath, $msiOpenDatabaseTypes.ViewModifyUpdate)

                # If a MSI transform file was specified, then apply it to the temporary copy of the MSI database.
                if ($ApplyTransformPath)
                {
                    Write-ADTLogEntry -Message "Applying transform file [$ApplyTransformPath] to MSI database [$TempMsiPath]."
                    $null = Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName ApplyTransform -ArgumentList @($ApplyTransformPath, $msiOpenDatabaseTypes.SuppressApplyTransformErrors)
                }

                # Determine the path for the new transform file that will be generated.
                if (!$NewTransformPath)
                {
                    $NewTransformFileName = if ($ApplyTransformPath)
                    {
                        [System.IO.Path]::GetFileNameWithoutExtension($ApplyTransformPath) + '.new' + [System.IO.Path]::GetExtension($ApplyTransformPath)
                    }
                    else
                    {
                        [System.IO.Path]::GetFileNameWithoutExtension($MsiPath) + '.mst'
                    }
                    $NewTransformPath = Join-Path -Path $MsiParentFolder -ChildPath $NewTransformFileName
                }

                # Set the MSI properties in the temporary copy of the MSI database.
                foreach ($property in $TransformProperties.GetEnumerator())
                {
                    Set-ADTMsiProperty -Database $TempMsiPathDatabase -PropertyName $property.Key -PropertyValue $property.Value
                }

                # Commit the new properties to the temporary copy of the MSI database
                $null = Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName Commit

                # Reopen the temporary copy of the MSI database in read only mode.
                Write-ADTLogEntry -Message "Re-opening the MSI database [$TempMsiPath] in read only mode."
                $null = [System.Runtime.InteropServices.Marshal]::ReleaseComObject($TempMsiPathDatabase)
                $TempMsiPathDatabase = Invoke-ADTObjectMethod -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($TempMsiPath, $msiOpenDatabaseTypes.OpenDatabaseModeReadOnly)

                # Delete the new transform file path if it already exists.
                if (Test-Path -LiteralPath $NewTransformPath -PathType Leaf)
                {
                    Write-ADTLogEntry -Message "A transform file of the same name already exists. Deleting transform file [$NewTransformPath]."
                    $null = Remove-Item -LiteralPath $NewTransformPath -Force
                }

                # Generate the new transform file by taking the difference between the temporary copy of the MSI database and the original MSI database.
                Write-ADTLogEntry -Message "Generating new transform file [$NewTransformPath]."
                $null = Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName GenerateTransform -ArgumentList @($MsiPathDatabase, $NewTransformPath)
                $null = Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName CreateTransformSummaryInfo -ArgumentList @($MsiPathDatabase, $NewTransformPath, $msiOpenDatabaseTypes.TransformErrorNone, $msiOpenDatabaseTypes.TransformValidationNone)

                if (!(Test-Path -LiteralPath $NewTransformPath -PathType Leaf))
                {
                    $naerParams = @{
                        Exception = [System.IO.IOException]::new("Failed to generate transform file in path [$NewTransformPath].")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'MsiTransformFileMissing'
                        TargetObject = $NewTransformPath
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }
                Write-ADTLogEntry -Message "Successfully created new transform file in path [$NewTransformPath]."
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to create new transform file in path [$NewTransformPath]."
        }
        finally
        {
            # Release all COM objects to prevent file locks.
            $null = foreach ($variable in (Get-Variable -Name TempMsiPathDatabase, MsiPathDatabase, Installer -ValueOnly -ErrorAction Ignore))
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

            # Delete the temporary copy of the MSI database.
            $null = Remove-Item -LiteralPath $TempMsiPath -Force -ErrorAction Ignore
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
