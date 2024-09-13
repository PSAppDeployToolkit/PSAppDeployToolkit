#-----------------------------------------------------------------------------
#
# MARK: Write-ADTLogEntryToInformationStream
#
#-----------------------------------------------------------------------------

function Write-ADTLogEntryToInformationStream
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
        [System.Management.Automation.SwitchParameter]$NoNewLine,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.ConsoleColor]$ForegroundColor,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.ConsoleColor]$BackgroundColor
    )

    begin
    {
        # Reset NoNewLine to be a proper bool within $PSBoundParameters.
        if ($PSBoundParameters.ContainsKey('NoNewLine'))
        {
            $PSBoundParameters.NoNewLine = $NoNewLine.IsPresent
        }
        $null = $PSBoundParameters.Remove('Source')

        # Establish the base InformationRecord to write out.
        $infoRecord = [System.Management.Automation.InformationRecord]::new([System.Management.Automation.HostInformationMessage]$PSBoundParameters, $Source)
    }

    process
    {
        # Update the message for piped operations and write out to the InformationStream.
        $infoRecord.MessageData.Message = $Message
        $PSCmdlet.WriteInformation($infoRecord)
    }
}
