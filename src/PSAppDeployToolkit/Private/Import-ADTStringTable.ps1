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
        [AllowNull()][PSAppDeployToolkit.Foundation.AllowNullButNotEmptyOrWhiteSpace()]
        [System.String[]]$BaseDirectory,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Globalization.CultureInfo]$UICulture
    )

    # Internal filter to expand variables.
    function Expand-ADTConfigValuesInStringTable
    {
        begin
        {
            $substitutions = [System.Text.RegularExpressions.Regex]::new('\{([^\d]+)\}', [System.Text.RegularExpressions.RegexOptions]::Compiled)
            $config = Get-ADTConfig
        }

        process
        {
            foreach ($section in $($_.GetEnumerator()))
            {
                if ($section.Value -is [System.String])
                {
                    $_.($section.Key) = $substitutions.Replace($section.Value,
                        {
                            return $args[0].Groups[1].Value.Split('\', [System.StringSplitOptions]::RemoveEmptyEntries) | & {
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

    # Import string table, perform value substitutions, then return it to the caller.
    $strings = Import-ADTModuleDataFile @PSBoundParameters -FileName strings.psd1 -IgnorePolicy
    $strings | Expand-ADTConfigValuesInStringTable
    return $strings
}
