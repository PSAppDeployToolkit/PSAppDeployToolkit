function New-ADTMsiTransform
{
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
    Hashtable which contains calls to Set-ADTMsiProperty for configuring the desired properties which should be included in new transform file.

    Example hashtable: [Hashtable]$TransformProperties = @{ 'ALLUSERS' = '1' }

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    New-ADTMsiTransform -MsiPath 'C:\Temp\PSADTInstall.msi' -TransformProperties @{
        'ALLUSERS' = '1'
        'AgreeToLicense' = 'Yes'
        'REBOOT' = 'ReallySuppress'
        'RebootYesNo' = 'No'
        'ROOTDRIVE' = 'C:'
    }

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
            if (!(Test-Path -Path $_ -PathType Leaf))
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName MsiPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
            }
            return !!$_
        })]
        [System.String]$MsiPath,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
            if (!(Test-Path -Path $_ -PathType Leaf))
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ApplyTransformPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
            }
            return !!$_
        })]
        [System.String]$ApplyTransformPath,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$NewTransformPath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$TransformProperties
    )

    begin {
        # Define properties for how the MSI database is opened.
        $msiOpenDatabaseModeReadOnly = 0
        $msiOpenDatabaseModeTransact = 1
        $msiViewModifyUpdate = 2
        $msiViewModifyReplace = 4
        $msiViewModifyDelete = 6
        $msiTransformErrorNone = 0
        $msiTransformValidationNone = 0
        $msiSuppressApplyTransformErrors = 63

        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -ErrorAction Continue
    }

    process {
        try
        {
            # Discover the parent folder that the MSI file resides in.
            Write-ADTLogEntry -Message "Creating a transform file for MSI [$MsiPath]."
            $MsiParentFolder = Split-Path -Path $MsiPath -Parent

            # Create a second copy of the MSI database.
            $TempMsiPath = Join-Path -Path $MsiParentFolder -ChildPath ([System.IO.Path]::GetFileName(([System.IO.Path]::GetTempFileName())))
            Write-ADTLogEntry -Message "Copying MSI database in path [$MsiPath] to destination [$TempMsiPath]."
            [System.Void](Copy-Item -LiteralPath $MsiPath -Destination $TempMsiPath -Force)

            # Open both copies of the MSI database.
            Write-ADTLogEntry -Message "Opening the MSI database [$MsiPath] in read only mode."
            $Installer = New-Object -ComObject WindowsInstaller.Installer
            $MsiPathDatabase = Invoke-ADTObjectMethod -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($MsiPath, $msiOpenDatabaseModeReadOnly)
            Write-ADTLogEntry -Message "Opening the MSI database [$TempMsiPath] in view/modify/update mode."
            $TempMsiPathDatabase = Invoke-ADTObjectMethod -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($TempMsiPath, $msiViewModifyUpdate)

            # If a MSI transform file was specified, then apply it to the temporary copy of the MSI database.
            if ($ApplyTransformPath)
            {
                Write-ADTLogEntry -Message "Applying transform file [$ApplyTransformPath] to MSI database [$TempMsiPath]."
                [System.Void](Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName ApplyTransform -ArgumentList @($ApplyTransformPath, $msiSuppressApplyTransformErrors))
            }

            # Determine the path for the new transform file that will be generated.
            if ($NewTransformPath)
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
                Set-ADTMsiProperty -DataBase $TempMsiPathDatabase -PropertyName $property.Key -PropertyValue $property.Value
            }

            # Commit the new properties to the temporary copy of the MSI database
            [System.Void](Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName Commit)

            # Reopen the temporary copy of the MSI database in read only mode.
            Write-ADTLogEntry -Message "Re-opening the MSI database [$TempMsiPath] in read only mode."
            [System.Void][System.Runtime.Interopservices.Marshal]::ReleaseComObject($TempMsiPathDatabase)
            $TempMsiPathDatabase = Invoke-ADTObjectMethod -InputObject $Installer -MethodName OpenDatabase -ArgumentList @($TempMsiPath, $msiOpenDatabaseModeReadOnly)

            # Delete the new transform file path if it already exists.
            if (Test-Path -LiteralPath $NewTransformPath -PathType Leaf)
            {
                Write-ADTLogEntry -Message "A transform file of the same name already exists. Deleting transform file [$NewTransformPath]."
                [System.Void](Remove-Item -LiteralPath $NewTransformPath -Force)
            }

            # Generate the new transform file by taking the difference between the temporary copy of the MSI database and the original MSI database.
            Write-ADTLogEntry -Message "Generating new transform file [$NewTransformPath]."
            [System.Void](Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName GenerateTransform -ArgumentList @($MsiPathDatabase, $NewTransformPath))
            [System.Void](Invoke-ADTObjectMethod -InputObject $TempMsiPathDatabase -MethodName CreateTransformSummaryInfo -ArgumentList @($MsiPathDatabase, $NewTransformPath, $msiTransformErrorNone, $msiTransformValidationNone))

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
        catch {
            Write-ADTLogEntry -Message "Failed to create new transform file in path [$NewTransformPath].`n$(Resolve-ADTError)" -Severity 3
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -ErrorRecord $_
        }
        finally
        {
            # Release all COM objects to prevent file locks.
            $null = foreach ($variable in (Get-Variable -Name TempMsiPathDatabase, MsiPathDatabase, Installer -ValueOnly -ErrorAction Ignore))
            {
                try
                {
                    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($variable)
                }
                catch
                {
                    $null
                }
            }

            # Delete the temporary copy of the MSI database.
            $null = try
            {
                # Delete the temporary copy of the MSI database.
                if (Test-Path -LiteralPath $TempMsiPath -PathType Leaf)
                {
                    Remove-Item -LiteralPath $TempMsiPath -Force
                }
            }
            catch
            {
                $null
            }
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
