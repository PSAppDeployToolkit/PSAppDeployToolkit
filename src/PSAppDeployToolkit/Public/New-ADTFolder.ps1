#-----------------------------------------------------------------------------
#
# MARK: New-ADTFolder
#
#-----------------------------------------------------------------------------

function New-ADTFolder
{
    <#
    .SYNOPSIS
        Create a new folder.

    .DESCRIPTION
        Create a new folder if it does not exist. This function checks if the specified path already exists and creates the folder if it does not. It logs the creation process and handles any errors that may occur during the folder creation.

    .PARAMETER LiteralPath
        Path to the new folder to create.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        New-ADTFolder -LiteralPath "$env:WinDir\System32"

        Creates a new folder at the specified path if it does not already exist.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/New-ADTFolder
    #>

    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        if (Test-Path -LiteralPath $LiteralPath -PathType Container)
        {
            Write-ADTLogEntry -Message "Folder [$LiteralPath] already exists."
            return
        }

        try
        {
            try
            {
                Write-ADTLogEntry -Message "Creating folder [$LiteralPath]."
                $null = New-Item -Path $LiteralPath -ItemType Directory -Force
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to create folder [$LiteralPath]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
