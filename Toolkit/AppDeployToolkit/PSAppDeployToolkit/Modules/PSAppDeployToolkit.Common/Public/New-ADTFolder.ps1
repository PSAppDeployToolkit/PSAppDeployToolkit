function New-ADTFolder
{
    <#

    .SYNOPSIS
    Create a new folder.

    .DESCRIPTION
    Create a new folder if it does not exist.

    .PARAMETER Path
    Path to the new folder to create.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    New-ADTFolder -Path "$envWinDir\System32"

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    begin {
        # Make this function continue on error.
        $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
        if (!$PSBoundParameters.ContainsKey('ErrorAction'))
        {
            $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Continue
        }
        Write-ADTDebugHeader
    }

    process {
        if ([System.IO.Directory]::Exists($Path))
        {
            Write-ADTLogEntry -Message "Folder [$Path] already exists."
            return
        }

        try {
            Write-ADTLogEntry -Message "Creating folder [$Path]."
            [System.Void](New-Item -Path $Path -ItemType Directory -Force)
        }
        catch
        {
            if ($PSBoundParameters.ErrorAction -notmatch '^(Ignore|SilentlyContinue)$')
            {
                Write-ADTLogEntry -Message "Failed to create folder [$Path].`n$(Resolve-ADTError)" -Severity 3
                if ($PSBoundParameters.ErrorAction.Equals([System.Management.Automation.ActionPreference]::Stop))
                {
                    throw
                }
            }
        }
    }

    end {
        Write-ADTDebugFooter
    }
}
