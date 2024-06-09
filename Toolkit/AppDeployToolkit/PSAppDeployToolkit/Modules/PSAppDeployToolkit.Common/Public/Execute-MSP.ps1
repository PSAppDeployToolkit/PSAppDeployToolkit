Function Execute-MSP {
    <#
.SYNOPSIS

Executes an MSP file using the same logic as Execute-MSI.

.DESCRIPTION

Reads SummaryInfo targeted product codes in MSP file and determines if the MSP file applies to any installed products
If a valid installed product is found, triggers the Execute-MSI function to patch the installation.
Uses default config MSI parameters. You can use -AddParameters to add additional parameters.

.PARAMETER Path

Path to the msp file

.PARAMETER AddParameters

Additional parameters

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Execute-MSP -Path 'Adobe_Reader_11.0.3_EN.msp'

.EXAMPLE

Execute-MSP -Path 'AcroRdr2017Upd1701130143_MUI.msp' -AddParameters 'ALLUSERS=1'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, HelpMessage = 'Please enter the path to the MSP file')]
        [ValidateScript({ ('.msp' -contains [IO.Path]::GetExtension($_)) })]
        [Alias('FilePath')]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$AddParameters
    )

    Begin {
        $adtSession = Get-ADTSession
        Write-ADTDebugHeader
    }
    Process {
        ## If the MSP is in the Files directory, set the full path to the MSP
        If (Test-Path -LiteralPath (Join-Path -Path $adtSession.GetPropertyValue('dirFiles') -ChildPath $path -ErrorAction 'Ignore') -PathType 'Leaf' -ErrorAction 'Ignore') {
            [String]$mspFile = Join-Path -Path $adtSession.GetPropertyValue('dirFiles') -ChildPath $path
        }
        ElseIf (Test-Path -LiteralPath $Path -ErrorAction 'Ignore') {
            [String]$mspFile = (Get-Item -LiteralPath $Path).FullName
        }
        Else {
            Write-ADTLogEntry -Message "Failed to find MSP file [$path]." -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to find MSP file [$path]."
            }
            Continue
        }
        Write-ADTLogEntry -Message 'Checking MSP file for valid product codes.'

        [Boolean]$IsMSPNeeded = $false

        ## Create a Windows Installer object
        [__ComObject]$Installer = New-Object -ComObject 'WindowsInstaller.Installer' -ErrorAction 'Stop'

        ## Define properties for how the MSI database is opened
        [Int32]$msiOpenDatabaseModePatchFile = 32
        [Int32]$msiOpenDatabaseMode = $msiOpenDatabaseModePatchFile
        ## Open database in read only mode
        [__ComObject]$Database = Invoke-ADTObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($mspFile, $msiOpenDatabaseMode)
        ## Get the SummaryInformation from the windows installer database
        [__ComObject]$SummaryInformation = Get-ADTObjectProperty -InputObject $Database -PropertyName 'SummaryInformation'
        [Hashtable]$SummaryInfoProperty = @{}
        $AllTargetedProductCodes = (Get-ADTObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(7)).Split(';')
        ForEach ($FormattedProductCode in $AllTargetedProductCodes) {
            [PSObject]$MSIInstalled = Get-ADTInstalledApplication -ProductCode $FormattedProductCode
            If ($MSIInstalled) {
                [Boolean]$IsMSPNeeded = $true
            }
        }
        Try {
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($SummaryInformation)
        }
        Catch {
        }
        Try {
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Database)
        }
        Catch {
        }
        Try {
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer)
        }
        Catch {
        }
        If ($IsMSPNeeded) {
            If ($AddParameters) {
                Execute-MSI -Action 'Patch' -Path $Path -AddParameters $AddParameters
            }
            Else {
                Execute-MSI -Action 'Patch' -Path $Path
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
