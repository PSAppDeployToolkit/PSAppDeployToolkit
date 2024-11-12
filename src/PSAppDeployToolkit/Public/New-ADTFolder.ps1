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

    .PARAMETER Path
        Path to the new folder to create.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        New-ADTFolder -Path "$env:WinDir\System32"

        Creates a new folder at the specified path if it does not already exist.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        if ([System.IO.Directory]::Exists($Path))
        {
            Write-ADTLogEntry -Message "Folder [$Path] already exists."
            return
        }

        try
        {
            try
            {
                Write-ADTLogEntry -Message "Creating folder [$Path]."
                $null = New-Item -Path $Path -ItemType Directory -Force
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to create folder [$Path]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
