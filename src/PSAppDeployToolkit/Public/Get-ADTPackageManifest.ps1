#-----------------------------------------------------------------------------
#
# MARK: Read-ADTPackageManifest
#
#-----------------------------------------------------------------------------

function Get-ADTPackageManifest
{
    <#
    .SYNOPSIS
        Get all of the properties from a Windows Installer database table or the Summary Information stream and return as a custom object.

    .DESCRIPTION
        Use the Windows Installer object to read all of the properties from a Windows Installer database table or the Summary Information stream.

    .PARAMETER LiteralPath
        The fully qualified path to an appx/msix (bundle) installer, or a xml containing manifest data.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.PackageManifestInfo

        Returns the object containing the data the input represents.

    .EXAMPLE
        Get-ADTPackageManifest -LiteralPath 'C:\Package\Package.appx'

        Retrieve all of the properties from the installer.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTMsiTableProperty
    #>
    [CmdletBinding()]
    [OutputType([PSADT.Types.PackageManifestInfo])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (!(Test-Path -LiteralPath $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                elseif ([System.IO.Path]::GetExtension($_) -notin @('.appx', '.appxbundle', '.msix', '.msixbundle', '.xml'))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The file must be a package installer or manifest file.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $xmlSettings = [System.Xml.XmlReaderSettings]@{
            DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
            XmlResolver = $null
        }
    }

    process
    {
        $item = Get-Item -LiteralPath $LiteralPath
        $xml = [System.Xml.XmlDocument]::new()
        $xml.XmlResolver = $null

        # If the given path is the installer, read the xml from the archive stream
        if ($item.Extension -in @('.appx', '.appxbundle', '.msix', 'msixbundle'))
        {
            $archiveSelector = if ($item.Extension -like '*bundle') { "AppxMetadata/AppxBundleManifest.xml" } else { "AppxManifest.xml" }

            $zipArchive = [System.IO.Compression.ZipFile]::OpenRead($item.FullName)
            try
            {
                if (-not ($manifestEntry = $zipArchive.GetEntry($archiveSelector)))
                {
                    $naerParams = @{
                        Exception = [System.InvalidOperationException]::("The given package file does not contain the expected manifest file.")
                        Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                        ErrorId = 'PackageIsMissingManifest'
                    }
                    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                }

                $xmlReader = [System.Xml.XmlReader]::Create($manifestEntry.Open(), $xmlSettings)
                try
                {
                    $xml.Load($xmlReader)
                }
                finally
                {
                    if ($xmlReader)
                    {
                        $xmlReader.Dispose()
                    }
                }
            }
            finally
            {
                if ($zipArchive)
                {
                    $zipArchive.Dispose()
                }
            }
        }
        # If the file is the document file, use a stream reader
        else
        {
            $streamReader = [System.IO.StreamReader]::new($item.FullName)
            try
            {
                $xmlReader = [System.Xml.XmlReader]::Create($streamReader, $xmlSettings)
                try
                {
                    $xml.Load($xmlReader)
                }
                finally
                {
                    if ($xmlReader)
                    {
                        $xmlReader.Dispose()
                    }
                }
            }
            finally
            {
                if ($streamReader)
                {
                    $streamReader.Dispose()
                }
            }
        }

        # Get the identity node that must be present in both packages and bundles.
        if (!($idNode = $xml.SelectSingleNode("/*[local-name()='Package' or local-name()='Bundle']/*[local-name()='Identity']")))
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The given manifest is missing the required Identity node")
                Category = [System.Management.Automation.ErrorCategory]::InvalidData
                ErrorId = 'DocumentNotPackageManifest'
                TargetObject = $xml
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }

        # Extract all information form the id node.
        $isBundle = $idNode.ParentNode.LocalName -eq 'Bundle'
        $name = $idNode.Attributes["Name"].Value
        $version = $idNode.Attributes["Version"].Value
        $architecture = if ($idNode.Attributes["ProcessorArchitecture"]) { $idNode.Attributes["ProcessorArchitecture"].Value.ToLower() } else { "neutral" }
        $publisherDn = $idNode.Attributes["Publisher"].Value
        $resourceId = if ($isBundle) { "~" } elseif ($idNode.Attributes["ResourceId"]) { $idNode.Attributes["ResourceId"].Value } else { "" }
        $publisherId = [PSADT.PackageManagement.PackageUtilities]::GetPackageFamilyName($name, $publisherDn).Split("_")[1]

        # Set the default values, that will be overwritten by the manifest data, if possible.
        $displayName = $null
        $publisherDisplayName = $null
        $isFramework = $false
        $isResource = $false
        $description = $null
        $bundledApplications = [System.Collections.Generic.List[System.String]]::new()
        $bundledResources = [System.Collections.Generic.List[System.String]]::new()

        if (-not $isBundle)
        {
            # Packages contain a required node Properties, containing additional information such as the DisplayName.
            $propNode = $idNode.ParentNode["Properties"]

            $displayName = $propNode["DisplayName"].InnerText
            $publisherDisplayName = $propNode["PublisherDisplayName"].InnerText
            $isFramework = $propNode["Framework"] -and $propNode["Framework"].InnerText -eq 'true'
            $isResource = $propNode["ResourcePackage"] -and $propNode["ResourcePackage"].InnerText -eq 'true'

            if ($propNode["Description"])
            {
                $description = $propNode["Description"].InnerText
            }
        }
        else
        {
            # Bundles include information about the included packages. Expose the separated by type.
            $idNode.ParentNode["Packages"].ChildNodes | & {
                process
                {
                    $attr = $_.Attributes
                    $arch = if ($attr["Architecture"]) { $attr["Architecture"].Value.ToLower() } else { "neutral" }
                    $fullName = "${name}_$($attr["Version"].Value)_${arch}_$($attr["resourceId"].Value)_${publisherId}"

                    if ($attr["Type"] -and $attr["Type"].Value -eq "application")
                    {
                        $bundledApplications.Add($fullName)
                    }
                    else
                    {
                        $bundledResources.Add($fullName)
                    }
                }
            }
        }

        $PSCmdlet.WriteObject(
            [PSADT.Types.PackageManifestInfo]::new(
                $name,
                $version,
                $architecture,
                $resourceId,
                $publisherId,
                $publisherDn,
                $displayName,
                $publisherDisplayName,
                $description,
                $isFramework,
                $isResource,
                $isBundle,
                $bundledApplications,
                $bundledResources
            )
        )
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
