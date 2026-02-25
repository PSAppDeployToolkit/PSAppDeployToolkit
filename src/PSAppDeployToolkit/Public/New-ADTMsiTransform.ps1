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

        Example hashtable: `@{ ALLUSERS = 1 }`

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
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
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
                if (!(Test-Path -LiteralPath $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName MsiPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$MsiPath,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if (!(Test-Path -LiteralPath $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ApplyTransformPath -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$ApplyTransformPath = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'If `-ApplyTransformPath` was specified: `<ApplyTransformPath>.new.mst`; If only `-MsiPath` was specified: `<MsiPath>.mst`')]
        [System.String]$NewTransformPath = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$TransformProperties
    )

    begin
    {
        # Define properties for how the MSI database is opened.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Determine the path for the new transform file that will be generated.
        if (!$NewTransformPath)
        {
            $NewTransformPath = if ($ApplyTransformPath)
            {
                (Join-Path -Path (Get-Item -LiteralPath $MsiPath).DirectoryName -ChildPath ([System.IO.Path]::GetFileNameWithoutExtension($ApplyTransformPath) + '.new' + [System.IO.Path]::GetExtension($ApplyTransformPath))).Trim()
            }
            else
            {
                (Join-Path -Path (Get-Item -LiteralPath $MsiPath).DirectoryName -ChildPath ([System.IO.Path]::GetFileNameWithoutExtension($MsiPath) + '.mst')).Trim()
            }
        }
    }

    process
    {
        Write-ADTLogEntry -Message "Creating a transform file for MSI [$MsiPath]."
        $propsDict = [System.Collections.Generic.Dictionary[System.String, System.String]]::new()
        $TransformProperties.GetEnumerator() | & { process { $propsDict.Add($_.Key, $_.Value) } }
        try
        {
            try
            {
                [PSADT.WindowsInstaller.MsiUtilities]::CreatePropertyTransformFile($MsiPath, $NewTransformPath, $propsDict, $ApplyTransformPath)
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
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
