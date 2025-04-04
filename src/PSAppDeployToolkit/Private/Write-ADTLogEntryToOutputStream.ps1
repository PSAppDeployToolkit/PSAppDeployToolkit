#-----------------------------------------------------------------------------
#
# MARK: Write-ADTLogEntryToOutputStream
#
#-----------------------------------------------------------------------------

function Write-ADTLogEntryToOutputStream
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Message,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Source,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.ConsoleColor]$ForegroundColor,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.ConsoleColor]$BackgroundColor
    )

    begin
    {
        # Remove parameters that aren't used to generate an InformationRecord object.
        $null = $PSBoundParameters.Remove('Verbose')
        $null = $PSBoundParameters.Remove('Source')

        # Establish the base InformationRecord to write out.
        $infoRecord = [System.Management.Automation.InformationRecord]::new([System.Management.Automation.HostInformationMessage]$PSBoundParameters, $Source)
    }

    process
    {
        # Update the message for piped operations and write out to the InformationStream.
        $infoRecord.MessageData.Message = $Message
        if ($VerbosePreference.Equals([System.Management.Automation.ActionPreference]::Continue))
        {
            $PSCmdlet.WriteVerbose($infoRecord.MessageData.Message)
        }
        else
        {
            $PSCmdlet.WriteInformation($infoRecord)
        }
    }
}
