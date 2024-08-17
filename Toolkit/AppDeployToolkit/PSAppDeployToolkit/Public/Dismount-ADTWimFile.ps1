#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Dismount-ADTWimFile
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (!(& $Script:CommandTable.'Get-WindowsImage' -Mounted | & $Script:CommandTable.'Where-Object' -Property Path -EQ -Value ($_ -replace '\\$')))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path is not a WIM mount point.'))
                }
                return !!$_
            })]
        [System.IO.DirectoryInfo]$Path
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Announce commencement.
        Write-ADTLogEntry -Message "Dismounting WIM file at path [$Path]."
        try
        {
            try
            {
                # Perform the dismount and discard all changes.
                $null = & $Script:CommandTable.'Dismount-WindowsImage' -Path $Path -Discard
                Write-ADTLogEntry -Message "Successfully dismounted WIM file."
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage 'Error occurred while attemping to mount WIM file.'
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
