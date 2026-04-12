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

    .PARAMETER PassThru
        Returns the newly created folder object.

    .INPUTS
        System.String

        Accepts a string value for the folder path.

    .OUTPUTS
        System.IO.DirectoryInfo

        The folder created by this function.

    .EXAMPLE
        New-ADTFolder -LiteralPath "$env:WinDir\System32"

        Creates a new folder at the specified path if it does not already exist.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/New-ADTFolder
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    [OutputType([System.IO.DirectoryInfo])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true, Position = 0)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.Switchparameter]$PassThru
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

            if ($PassThru)
            {
                return (Get-Item -LiteralPath $LiteralPath)
            }

            return
        }

        try
        {
            try
            {
                Write-ADTLogEntry -Message "Creating folder [$LiteralPath]."
                if (!$PSCmdlet.ShouldProcess("Folder [$LiteralPath]", 'Create'))
                {
                    return
                }

                $item = New-Item -Path $LiteralPath -ItemType Directory -Force

                if ($PassThru)
                {
                    return $item
                }
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
