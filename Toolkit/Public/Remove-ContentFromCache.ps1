Function Remove-ContentFromCache {
    <#
.SYNOPSIS
    Removes the toolkit content from the cache folder on the local machine and reverts the $dirFiles and $supportFiles directory
.DESCRIPTION
    Removes the toolkit content from the cache folder on the local machine and reverts the $dirFiles and $supportFiles directory
.PARAMETER Path
    The path to the software cache folder
.EXAMPLE
    Remove-ContentFromCache -Path 'C:\Windows\Temp\PSAppDeployToolkit'
.NOTES

.LINK
    https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false, Position = 0, HelpMessage = 'The path to the software cache folder')]
        [ValidateNotNullorEmpty()]
        [String]$Path = "$configToolkitCachePath\$installName"
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            If (Test-Path -LiteralPath $Path -PathType 'Container') {
                Try {
                    Write-Log -Message "Removing cache folder [$Path]." -Source ${CmdletName}
                    $null = Remove-Item -Path $Path -Recurse -ErrorAction 'Stop'
                    [String]$dirFiles = Join-Path -Path $scriptParentPath -ChildPath 'Files'
                    [String]$dirSupportFiles = Join-Path -Path $scriptParentPath -ChildPath 'SupportFiles'
                }
                Catch {
                    Write-Log -Message "Failed to remove cache folder [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                    Throw "Failed to remove cache folder [$Path]: $($_.Exception.Message)"
                }
            }
            Else {
                Write-Log -Message "Cache folder [$Path] does not exist." -Source ${CmdletName}
            }
        }
        Catch {
            Write-Log -Message "Failed to remove cache folder [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            Throw "Failed to remove cache folder [$Path]: $($_.Exception.Message)"
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
