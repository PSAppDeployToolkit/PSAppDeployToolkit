#-----------------------------------------------------------------------------
#
# MARK: Convert-ADTRegistryKeyToHashtable
#
#-----------------------------------------------------------------------------

function Private:ConvertTo-ADTExpression
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false, ValueFromPipeline = $true)]
        [AllowNull()]
        [System.Object]$InputObject
    )

    # Return a string representation of the input object that can be evaluated back to an equivalent object.

    if ($null -eq $InputObject)
    {
        return '$null'
    }
    if ($InputObject -is [System.Boolean] -or $InputObject -is [System.Management.Automation.SwitchParameter])
    {
        if ($InputObject) { return '$true' } else { return '$false' }
    }
    if ($InputObject -is [System.DateTime])
    {
        return "'$($InputObject.ToString('yyyy-MM-dd'))'"
    }
    if ($InputObject -is [System.TimeSpan])
    {
        return $InputObject.TotalSeconds.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    }
    if (($InputObject -is [System.String]) -or ($InputObject -is [System.Char]) -or ($InputObject -is [System.Version]) -or ($InputObject -is [System.Guid]) -or ($InputObject -is [System.IO.FileSystemInfo]) -or ($InputObject.GetType().IsEnum))
    {
        $str = $InputObject.ToString()
        if ($str -match '(?<!`)\$')
        {
            return "`"$($str -replace '(?<!`)"', '`"')`""
        }
        return "'$($str.Replace("'", "''"))'"
    }
    if ($InputObject -is [System.ValueType])
    {
        return $InputObject.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    }
    if ($InputObject -is [System.Collections.IDictionary])
    {
        $pairs = foreach ($entry in $InputObject.GetEnumerator())
        {
            $entryKey = $entry.Key.ToString().Replace("'", "''")
            "'$entryKey' = $(ConvertTo-ADTExpression -InputObject $entry.Value)"
        }
        $dictionaryPrefix = if ($InputObject -is [System.Collections.Specialized.OrderedDictionary]) { '[ordered]@{' } else { '@{' }
        return "$dictionaryPrefix $($pairs -join '; ') }"
    }
    if (($InputObject -is [System.Collections.IEnumerable]) -and !($InputObject -is [System.String]))
    {
        $items = foreach ($item in $InputObject)
        {
            ConvertTo-ADTExpression -InputObject $item
        }
        return "@($($items -join ', '))"
    }

    $naerParams = @{
        Exception = [System.ArgumentException]::new("Session property value of type [$($InputObject.GetType().FullName)] is not supported.")
        Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
        ErrorId = 'UnsupportedSessionPropertyValueType'
        TargetObject = $InputObject
        RecommendedAction = 'Use any System.ValueType, strings, datetimes, timespans, arrays, hashtables, or ordered dictionaries.'
    }
    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
}
