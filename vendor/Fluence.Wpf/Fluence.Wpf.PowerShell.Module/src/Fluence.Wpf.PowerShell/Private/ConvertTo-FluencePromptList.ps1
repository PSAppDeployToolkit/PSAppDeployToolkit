function ConvertTo-FluencePromptList
{
    <#
    .SYNOPSIS
        Normalizes a mixed array of strings and Fluence.Prompt objects into a Fluence.Prompt list.
    .DESCRIPTION
        Strings are converted via New-FluencePrompt -Message. Fluence.Prompt objects pass through
        unchanged. Any other type raises a terminating error naming the bad item.
    .PARAMETER InputObject
        An array of strings or Fluence.Prompt objects.
    .OUTPUTS
        Fluence.Prompt
    .NOTES
        Private helper. Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Prompt')]
    param
    (
        [Parameter(Mandatory = $true)]
        [object[]]$InputObject
    )

    $result = @()
    foreach ($item in $InputObject)
    {
        if ($item -is [string])
        {
            $result += New-FluencePrompt -Message $item
        }
        elseif ($item.PSObject.TypeNames -contains 'Fluence.Prompt')
        {
            $result += $item
        }
        elseif ($null -eq $item)
        {
            # Without this branch the null falls through to GetType() in the throw below and the
            # caller sees 'You cannot call a method on a null-valued expression' instead of the
            # diagnostic that names the parameter.
            throw "ConvertTo-FluencePromptList: a $null item is not a valid prompt specification."
        }
        else
        {
            throw "ConvertTo-FluencePromptList: unsupported item type '$($item.GetType().FullName)'. Expected [string] or [Fluence.Prompt]."
        }
    }
    return $result
}
