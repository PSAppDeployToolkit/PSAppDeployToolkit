#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTSubstOperation
#
#-----------------------------------------------------------------------------

function Invoke-ADTSubstOperation
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Create')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Delete')]
        [ValidateScript({
                if ($_ -notmatch '^[A-Z]:$')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Drive -ProvidedValue $_ -ExceptionMessage 'The specified drive is not valid. Please specify a drive in the following format: [A:, B:, etc].'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$Drive,

        [Parameter(Mandatory = $true, ParameterSetName = 'Create')]
        [ValidateScript({
                if ($null -eq $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified input is null.'))
                }
                if (!$_.Exists)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified image path cannot be found.'))
                }
                if ([System.Uri]::new($_).IsUnc)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified image path cannot be a network share.'))
                }
                return !!$_
            })]
        [System.IO.DirectoryInfo]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'Delete')]
        [System.Management.Automation.SwitchParameter]$Delete
    )

    # Perform the subst operation. An exit code of 0 is considered successful.
    $substPath = "$([System.Environment]::SystemDirectory)\subst.exe"
    $substResult = if ($Path)
    {
        # Throw if the specified drive letter is in use.
        if ((Get-PSDrive -PSProvider FileSystem).Name -contains $Drive.Substring(0, 1))
        {
            $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Drive -ProvidedValue $Drive -ExceptionMessage 'The specified drive is currently in use. Please try again with an unused drive letter.'))
        }
        Write-ADTLogEntry -Message "$(($msg = "Creating substitution drive [$Drive] for [$Path]"))."
        & $substPath $Drive $Path.FullName
    }
    elseif ($Delete)
    {
        Write-ADTLogEntry -Message "$(($msg = "Deleting substitution drive [$Drive]"))."
        & $substPath $Drive /D
    }
    else
    {
        # If we're here, the caller probably did something silly like -Delete:$false.
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("Unable to determine the required mode of operation.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'SubstModeIndeterminate'
            TargetObject = $PSBoundParameters
            RecommendedAction = "Please review the result in this error's TargetObject property and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    if ($Global:LASTEXITCODE.Equals(0))
    {
        return
    }

    # If we're here, we had a bad exit code.
    Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$Global:LASTEXITCODE]: $substResult") -Severity 3
    $naerParams = @{
        Exception = [System.Runtime.InteropServices.ExternalException]::new($msg, $Global:LASTEXITCODE)
        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
        ErrorId = 'SubstUtilityFailure'
        TargetObject = $substResult
        RecommendedAction = "Please review the result in this error's TargetObject property and try again."
    }
    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
}
