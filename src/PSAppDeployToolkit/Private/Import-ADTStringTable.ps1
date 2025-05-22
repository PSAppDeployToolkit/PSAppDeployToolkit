#-----------------------------------------------------------------------------
#
# MARK: Import-ADTStringTable
#
#-----------------------------------------------------------------------------

function Private:Import-ADTStringTable
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName BaseDirectory -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (![System.IO.Directory]::Exists($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName BaseDirectory -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [System.String[]]$BaseDirectory,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$UICulture
    )

    # Internal filter to expand variables.
    filter Expand-ADTConfigValuesInStringTable
    {
        process
        {
            # Go recursive if we've received a hashtable, otherwise just update the values.
            foreach ($section in $($_.GetEnumerator()))
            {
                if ($section.Value -is [System.String])
                {
                    $_.($section.Key) = [System.Text.RegularExpressions.Regex]::Replace($section.Value, '\{[^\d]+\}',
                        {
                            return $args[0].Value.Replace('{', $null).Replace('}', $null).Split('\') | & {
                                begin
                                {
                                    $result = $config
                                }
                                process
                                {
                                    $result = $result.$_
                                }
                                end
                                {
                                    return $result
                                }
                            }
                        })
                }
                elseif ($section.Value -is [System.Collections.Hashtable])
                {
                    $section.Value | & $MyInvocation.MyCommand
                }
            }
        }
    }

    # Get the current config so we can read its values.
    $config = Get-ADTConfig

    # Import string table, perform value substitutions, then return it to the caller.
    $strings = Import-ADTModuleDataFile @PSBoundParameters -FileName strings.psd1 -IgnorePolicy
    $strings | Expand-ADTConfigValuesInStringTable
    return $strings
}
