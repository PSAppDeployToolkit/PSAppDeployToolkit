#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Mount-ADTWimFile
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo]$ImagePath,

        [Parameter(Mandatory = $true, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateScript({
                if (& $Script:CommandTable.'Get-WindowsImage' -Mounted | & $Script:CommandTable.'Where-Object' -Property Path -EQ -Value ($_ -replace '\\$'))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path has a pre-existing WIM mounted.'))
                }
                if (& $Script:CommandTable.'Get-ChildItem' -LiteralPath $_ -ErrorAction Ignore)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path is not empty.'))
                }
                return !!$_
            })]
        [System.IO.DirectoryInfo]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'Index')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Index,

        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Name')]
        [System.Management.Automation.SwitchParameter]$Force
    )

    begin
    {
        # Attempt to get specified WIM image before initialising.
        $null = try
        {
            $PSBoundParameters.Remove('PassThru')
            $PSBoundParameters.Remove('Force')
            $PSBoundParameters.Remove('Path')
            & $Script:CommandTable.'Get-WindowsImage' @PSBoundParameters
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Announce commencement.
        Write-ADTLogEntry -Message "Mounting WIM file [$ImagePath] to [$Path]."
        try
        {
            try
            {
                # If we're using the force, forcibly remove the existing directory.
                if ([System.IO.Directory]::Exists($Path) -and $Force)
                {
                    Remove-Item -LiteralPath $Path -Force -Confirm:$false
                }

                # If the path doesn't exist, create it.
                if (![System.IO.Directory]::Exists($Path))
                {
                    Write-ADTLogEntry -Message "Creating path [$Path] as it does not exist."
                    $Path = [System.IO.Directory]::CreateDirectory($Path).FullName
                }

                # Mount the WIM file.
                $res = & $Script:CommandTable.'Mount-WindowsImage' @PSBoundParameters -Path $Path -ReadOnly -CheckIntegrity
                Write-ADTLogEntry -Message "Successfully mounted WIM file [$ImagePath]."

                # Store the result within the user's ADTSession if there's an active one.
                if (Test-ADTSessionActive)
                {
                    (Get-ADTSession).MountedWimFiles.Add($res)
                }

                # Return the result if we're passing through.
                if ($PassThru)
                {
                    return $res
                }
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
