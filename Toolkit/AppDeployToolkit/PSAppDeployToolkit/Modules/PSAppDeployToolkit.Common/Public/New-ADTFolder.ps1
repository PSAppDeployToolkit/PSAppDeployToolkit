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

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    begin {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -Prefix "Failed to create folder [$Path]."
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
