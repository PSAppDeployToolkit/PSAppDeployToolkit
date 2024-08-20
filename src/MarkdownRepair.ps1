<#
.SYNOPSIS
    Repair PlatyPS generated markdown files.
.NOTES
    This file is temporarily required to handle platyPS help generation.
        https://github.com/PowerShell/platyPS/issues/595
    This is a result of a breaking change introduced in PowerShell 7.4.0:
        https://learn.microsoft.com/en-us/powershell/scripting/whats-new/what-s-new-in-powershell-74?view=powershell-7.4
        Breaking Changes: Added the ProgressAction parameter to the Common Parameters
    modified from source: https://github.com/PowerShell/platyPS/issues/595#issuecomment-1820971702
#>

function Remove-CommonParameterFromMarkdown {
    <#
    .SYNOPSIS
        Remove a PlatyPS generated parameter block.
    .DESCRIPTION
        Removes parameter block for the provided parameter name from the markdown file provided.
    #>
    param(
        [Parameter(Mandatory)]
        [string[]]
        $Path,

        [Parameter(Mandatory = $false)]
        [string[]]
        $ParameterName = @('ProgressAction')
    )
    $ErrorActionPreference = 'Stop'
    foreach ($p in $Path) {
        $content = (Get-Content -Path $p -Raw).TrimEnd()
        $updateFile = $false
        foreach ($param in $ParameterName) {
            if (-not ($Param.StartsWith('-'))) {
                $param = "-$($param)"
            }
            # Remove the parameter block
            $pattern = "(?m)^### $param\r?\n[\S\s]*?(?=#{2,3}?)"
            $newContent = $content -replace $pattern, ''
            # Remove the parameter from the syntax block
            $pattern = " \[$param\s?.*?]"
            $newContent = $newContent -replace $pattern, ''
            if ($null -ne (Compare-Object -ReferenceObject $content -DifferenceObject $newContent)) {
                Write-Verbose "Added $param to $p"
                # Update file content
                $content = $newContent
                $updateFile = $true
            }
        }
        # Save file if content has changed
        if ($updateFile) {
            $newContent | Out-File -Encoding utf8 -FilePath $p
            Write-Verbose "Updated file: $p"
        }
    }
    return
}

function Add-MissingCommonParameterToMarkdown {
    param(
        [Parameter(Mandatory)]
        [string[]]
        $Path,

        [Parameter(Mandatory = $false)]
        [string[]]
        $ParameterName = @('ProgressAction')
    )
    $ErrorActionPreference = 'Stop'
    foreach ($p in $Path) {
        $content = (Get-Content -Path $p -Raw).TrimEnd()
        $updateFile = $false
        foreach ($NewParameter in $ParameterName) {
            if (-not ($NewParameter.StartsWith('-'))) {
                $NewParameter = "-$($NewParameter)"
            }
            $pattern = '(?m)^This cmdlet supports the common parameters:(.+?)\.'
            $replacement = {
                $Params = $_.Groups[1].Captures[0].ToString() -split ' '
                $CommonParameters = @()
                foreach ($CommonParameter in $Params) {
                    if ($CommonParameter.StartsWith('-')) {
                        if ($CommonParameter.EndsWith(',')) {
                            $CleanParam = $CommonParameter.Substring(0, $CommonParameter.Length - 1)
                        }
                        elseif ($p.EndsWith('.')) {
                            $CleanParam = $CommonParameter.Substring(0, $CommonParameter.Length - 1)
                        }
                        else {
                            $CleanParam = $CommonParameter
                        }
                        $CommonParameters += $CleanParam
                    }
                }
                if ($NewParameter -notin $CommonParameters) {
                    $CommonParameters += $NewParameter
                }
                $CommonParameters[-1] = "and $($CommonParameters[-1]). "
                return "This cmdlet supports the common parameters: " + (($CommonParameters | Sort-Object) -join ', ')
            }
            $newContent = $content -replace $pattern, $replacement
            if ($null -ne (Compare-Object -ReferenceObject $content -DifferenceObject $newContent)) {
                Write-Verbose "Added $NewParameter to $p"
                $updateFile = $true
                $content = $newContent
            }
        }
        # Save file if content has changed
        if ($updateFile) {
            $newContent | Out-File -Encoding utf8 -FilePath $p
            Write-Verbose "Updated file: $p"
        }
    }
    return
}

function Repair-PlatyPSMarkdown {
    param(
        [Parameter(Mandatory)]
        [string[]]
        $Path,

        [Parameter()]
        [string[]]
        $ParameterName = @('ProgressAction')
    )
    $ErrorActionPreference = 'Stop'
    $Parameters = @{
        Path          = $Path
        ParameterName = $ParameterName
    }
    $null = Remove-CommonParameterFromMarkdown @Parameters
    $null = Add-MissingCommonParameterToMarkdown @Parameters
    return
}
