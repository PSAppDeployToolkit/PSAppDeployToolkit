function Find-RegexMatch
{
<#
.SYNOPSIS

    Performs regex match or replace operations on a given text using a specified pattern.

.DESCRIPTION

    The Find-RegexMatch function uses regular expressions to either find matches or perform replacements
    on a given text. It supports various regex options and allows for detailed match information output.

.PARAMETER Pattern

    The regex pattern to match.

.PARAMETER Text

    The text to search or replace within.

.PARAMETER Replacement

    The string to use for replacements. Only used in the 'Replace' parameter set.

.PARAMETER MatchEvaluator

    A script block that processes each match and returns the replacement string. Only used in the 'Replace' parameter set.

.PARAMETER EscapePattern

    Escapes special characters in the regex pattern by replacing them with their escape codes.

    Escapes a minimal set of characters: \, *, +, ?, |, {, [, (,), ^, $, ., #, and white space.

    This instructs the regular expression engine to interpret these characters literally rather than as metacharacters.

    While the Escape method escapes the straight opening bracket ([) and opening brace ({) characters, it does not escape
    their corresponding closing characters (] and }). In most cases, escaping these is not necessary. If a closing bracket
    or brace is not preceded by its corresponding opening character, the regular expression engine interprets it literally.

.PARAMETER UnescapePattern

    Unescapes any escaped characters in the regex pattern by performing one of the following two transformations:

    1. It reverses the transformation performed by the Escape method by removing the escape character ("\") from each character escaped by the method.
       These include the \, *, +, ?, |, {, [, (,), ^, $, ., #, and white space characters.
       In addition, the Unescape method unescapes the closing bracket (]) and closing brace (}) characters.

    2. It replaces the hexadecimal values in verbatim string literals with the actual printable characters.
       For example, it replaces @"\x07" with "\a", or @"\x0A" with "\n".
       It converts to supported escape characters such as \a, \b, \e, \n, \r, \f, \t, \v, and alphanumeric characters.

.PARAMETER FirstMatch

    Searches an input string for a substring that matches a regular expression pattern and returns the first occurrence.

.PARAMETER StartingPosition

    Searches the specified input string for all occurrences of a regular expression, beginning at the specified starting position in the string. Default is 0.

.PARAMETER ReplaceLimit

    Maximum number of times the replacement can occur. Default is [int]::MaxValue.

.PARAMETER Timeout

    The maximum amount of time (in milliseconds) to execute a matching operation before it times out. Default is Infinite.

.PARAMETER IgnoreCase

    Specifies case-insensitive matching.

.PARAMETER Multiline

    Multiline mode. Changes the meaning of ^ and $ so they match at the beginning and end, respectively, of any line, and not just the beginning and end of the entire string.

.PARAMETER SingleLine

    Specifies single-line mode. Changes the meaning of the dot (.) so it matches every character (instead of every character except \n).

.PARAMETER IgnorePatternWhitespace

    Eliminates unescaped white space from the pattern and enables comments marked with #.
    However, this value does not affect or eliminate white space in character classes, numeric quantifiers,
    or tokens that mark the beginning of individual regular expression language elements.

.PARAMETER ExplicitCapture

    Specifies that the only valid captures are explicitly named or numbered groups of the form (?<name>...).
    This allows unnamed parentheses to act as noncapturing groups without the syntactic clumsiness of the expression (?:...).

.PARAMETER CaptureGroups

    An array containing named and numbered capturing groups to include in the match information.

.PARAMETER Compiled

    Specifies that the regular expression is compiled to MSIL code, instead of being interpreted.
    Compiled regular expressions maximize run-time performance at the expense of initialization time.

.PARAMETER RightToLeft

    Specifies that the search will be from right to left instead of from left to right.

.PARAMETER CultureInvariant

    Specifies that cultural differences in language is ignored.

.PARAMETER NonBacktracking

    Enable matching using an approach that avoids backtracking and guarantees linear-time processing in the length of the input.

.PARAMETER Detailed

    If set, the function will return detailed match information.

.EXAMPLE

    Find-RegexMatch -Pattern '\d+' -Text 'Sample text with numbers 123 and 456'

    Performs a regex match to find all digit sequences in the given text.

.EXAMPLE

    Find-RegexMatch -Pattern '\d+' -Text 'Sample text with numbers 123 and 456' -Replacement 'NUM'

    Performs a regex replace operation, replacing all digit sequences with 'NUM'.

.EXAMPLE

    Find-RegexMatch -Pattern '\d+' -Text 'Sample text with numbers 123 and 456' -MatchEvaluator {
        param ($match)
        "[$($match.Value)]"
    }

    Performs a regex replace operation using a MatchEvaluator script block to process each match.

.EXAMPLE

    Find-RegexMatch -Pattern '(?<Number>\d+)' -Text 'Sample text with numbers 123 and 456' -Detailed

    Performs a regex match operation and returns detailed match information.

.EXAMPLE

    # Using Capture Groups
    $pattern = '(?<Year>\d{4})-(?<Month>\d{2})-(?<Day>\d{2})'
    $text = '2023-07-15 and 2024-08-16'

    Find-RegexMatch -Pattern $pattern -Text $text -Detailed -CaptureGroups @('Year', 'Month', 'Day')

    Returns detailed match information including the named capture groups for Year, Month, and Day.

.EXAMPLE

    # Using MatchEvaluator and ExplicitCapture
    $pattern = '(?<Word>\w+)\s(?<Digit>\d+)'
    $text = 'Sample 123 text 456 with 789 numbers'

    Find-RegexMatch -Pattern $pattern -Text $text -ExplicitCapture -MatchEvaluator {
        param ($match)
        "$($match.Groups['Word'].Value)-$($match.Groups['Digit'].Value)"
    }

    Performs a regex replace operation using a MatchEvaluator script block to concatenate the 'Word' and 'Digit' named groups,
    only capturing explicitly named groups.

.EXAMPLE

    # Using RightToLeft option
    $pattern = '\d+'
    $text = 'Sample text with numbers 123 and 456'

    Find-RegexMatch -Pattern $pattern -Text $text -RightToLeft

    Performs a regex match to find all digit sequences in the given text starting from the end and moving to the start.

.EXAMPLE

    # Using CultureInvariant option
    $pattern = '\w+'
    $text = 'Some text with special characters: ä, ö, ü'

    Find-RegexMatch -Pattern $pattern -Text $text -CultureInvariant

    Performs a regex match that ignores cultural differences in language.

.EXAMPLE

    # Using Timeout option
    Find-RegexMatch -Pattern '\d+' -Text 'Sample text with numbers 123 and 456' -Timeout 500

    Performs a regex match operation with a timeout of 500 milliseconds.

.NOTES
#>
    [CmdletBinding(DefaultParameterSetName = 'Match')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [string]$Pattern,

        [Parameter(Mandatory = $true, Position = 1, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [string]$Text,

        [Parameter(ParameterSetName = 'Replace')]
        [string]$Replacement,

        [Parameter(ParameterSetName = 'Replace')]
        [scriptblock]$MatchEvaluator,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [Parameter(ParameterSetName = 'Escape')]
        [switch]$EscapePattern,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [Parameter(ParameterSetName = 'Unescape')]
        [switch]$UnescapePattern,

        [Parameter(ParameterSetName = 'Match')]
        [switch]$FirstMatch,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [int]$StartingPosition = 0,

        [Parameter(ParameterSetName = 'Replace')]
        [int]$ReplaceLimit = [int]::MaxValue,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [int]$Timeout = ([System.Text.RegularExpressions.Regex]::InfiniteMatchTimeout).TotalMilliseconds,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [switch]$IgnoreCase,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [switch]$Multiline,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [switch]$SingleLine,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [switch]$IgnorePatternWhitespace,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [switch]$ExplicitCapture,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [array]$CaptureGroups = @(),

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [switch]$Compiled,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [switch]$RightToLeft,

        [Parameter(ParameterSetName = 'Match')]
        [Parameter(ParameterSetName = 'Replace')]
        [switch]$CultureInvariant,

        [Parameter(ParameterSetName = 'Match')]
        [switch]$Detailed
    )

    begin
    {
        # Build regex options
        [System.Text.RegularExpressions.RegexOptions]$options = [System.Text.RegularExpressions.RegexOptions]::None

        if ($IgnoreCase) { $options += [System.Text.RegularExpressions.RegexOptions]::IgnoreCase }
        if ($Multiline) { $options += [System.Text.RegularExpressions.RegexOptions]::Multiline }
        if ($SingleLine) { $options += [System.Text.RegularExpressions.RegexOptions]::Singleline }
        if ($IgnorePatternWhitespace) { $options += [System.Text.RegularExpressions.RegexOptions]::IgnorePatternWhitespace }
        if ($ExplicitCapture) { $options += [System.Text.RegularExpressions.RegexOptions]::ExplicitCapture }
        if ($Compiled) { $options += [System.Text.RegularExpressions.RegexOptions]::Compiled }
        if ($RightToLeft) { $options += [System.Text.RegularExpressions.RegexOptions]::RightToLeft }
        if ($CultureInvariant) { $options += [System.Text.RegularExpressions.RegexOptions]::CultureInvariant }

        # Escape or Unescape pattern if required
        switch ($true)
        {
            $EscapePattern { $Pattern = [System.Text.RegularExpressions.Regex]::Escape($Pattern) }
            $UnescapePattern { $Pattern = [System.Text.RegularExpressions.Regex]::Unescape($Pattern) }
        }

        # Warning for IgnorePatternWhitespace
        [string]$unescapedNumberSignPattern = '(?<!\\)#'
        [string]$unescapedWhiteSpacePattern = '(?<!\\)\s'
        if ($IgnorePatternWhitespace -and (($Pattern -match $unescapedNumberSignPattern) -or ($Pattern -match $unescapedWhiteSpacePattern)))
        {
            Write-Warning -Message 'If a regular expression pattern includes either the number sign (#) or literal white-space characters, it must be escaped if input text is parsed with the [-IgnorePatternWhitespace] option enabled.'
        }

        # Compile the regex pattern with timeout
        [regex]$regex = [regex]::new($Pattern, $options, [timespan]::FromMilliseconds($Timeout))
    }
    
    process
    {
        if ($PSCmdlet.ParameterSetName -eq 'Replace')
        {
            # Perform replace operation
            if ($null -ne $MatchEvaluator)
            {
                # Use MatchEvaluator delegate
                $regex.Replace($Text, [System.Text.RegularExpressions.MatchEvaluator]{
                    param ([System.Text.RegularExpressions.Match]$match)

                    & $MatchEvaluator $match
                }, $ReplaceLimit, $StartingPosition)
            }
            else
            {
                # Use replacement string
                $regex.Replace($Text, $Replacement, $ReplaceLimit, $StartingPosition)
            }
        }
        elseif ($FirstMatch)
        {
            # Perform first match operation
            [System.Text.RegularExpressions.Match]$match = $regex.Match($Text, $StartingPosition)
            
            if ($Detailed)
            {
                [PSCustomObject]@{
                    Value = [string]$match.Value
                    Index = [int]$match.Index
                    Length = [int]$match.Length
                    Groups = @(
                        if ($CaptureGroups.Count -eq 0)
                        {
                            foreach ($group in $match.Groups)
                            {
                                [PSCustomObject]@{
                                    Name = [string]$group.Name
                                    Value = [string]$group.Value
                                    Index = [int]$group.Index
                                    Length = [int]$group.Length
                                }
                            }
                        }
                        else
                        {
                            foreach ($group in $CaptureGroups)
                            {
                                if ($match.Groups[$group])
                                {
                                    [PSCustomObject]@{
                                        Name = [string]($match.Groups[$group]).Name
                                        Value = [string]($match.Groups[$group]).Value
                                        Index = [int]($match.Groups[$group]).Index
                                        Length = [int]($match.Groups[$group]).Length
                                    }
                                }
                            }
                        }
                    )
                }
            }
            else
            {
                if ($CaptureGroups.Count -eq 0)
                {
                    $match.Value
                }
                else
                {
                    foreach ($group in $CaptureGroups)
                    {
                        if ($match.Groups[$group])
                        {
                            $match.Groups[$group].Value
                        }
                    }
                }
            }
        }
        else
        {
            # Perform matches operation
            [System.Text.RegularExpressions.MatchCollection]$patternMatches = $regex.Matches($Text, $StartingPosition)
            
            if ($Detailed)
            {
                foreach ($match in $patternMatches)
                {
                    [PSCustomObject]@{
                        Value = [string]$match.Value
                        Index = [int]$match.Index
                        Length = [int]$match.Length
                        Groups = @(
                            if ($CaptureGroups.Count -eq 0)
                            {
                                foreach ($group in $match.Groups)
                                {
                                    [PSCustomObject]@{
                                        Name = [string]$group.Name
                                        Value = [string]$group.Value
                                        Index = [int]$group.Index
                                        Length = [int]$group.Length
                                    }
                                }
                            }
                            else
                            {
                                foreach ($group in $CaptureGroups)
                                {
                                    if ($match.Groups[$group])
                                    {
                                        [PSCustomObject]@{
                                            Name = [string]($match.Groups[$group]).Name
                                            Value = [string]($match.Groups[$group]).Value
                                            Index = [int]($match.Groups[$group]).Index
                                            Length = [int]($match.Groups[$group]).Length
                                        }
                                    }
                                }
                            }
                        )
                    }
                }
            }
            else
            {
                foreach ($match in $patternMatches)
                {
                    if ($CaptureGroups.Count -eq 0)
                    {
                        $match.Value
                    }
                    else
                    {
                        foreach ($group in $CaptureGroups)
                        {
                            if ($match.Groups[$group])
                            {
                                $match.Groups[$group].Value
                            }
                        }
                    }
                }
            }
        }
    }
}