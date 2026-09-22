function New-FluencePrompt
{
    <#
    .SYNOPSIS
        Builds a single input-prompt specification for Show-FluenceDialog.
    .DESCRIPTION
        Returns a Fluence.Prompt object describing one input field: its name (the result key),
        message, input type, default value, and optional validation rules.
    .PARAMETER Name
        The result key under which the captured value is returned. Defaults to an auto name if omitted.
    .PARAMETER Message
        The label shown above (or beside) the input control.
    .PARAMETER InputType
        One of: Text, Multiline, Password, Number, Checkbox, Toggle, Choice, List, Date, Time,
        FileOpen, FileSave, FolderOpen, Link. Choice renders the -ValidateSet as a combo box or radio
        buttons; List renders it as a list view that can also take several selections (-MultiSelect).
    .PARAMETER AsPlainText
        Password prompts only. Return a plain string instead of the default SecureString.
        Assign an explicitly requested plaintext value to a variable to avoid printing it.
    .PARAMETER DefaultValue
        The initial value. For a List prompt with -MultiSelect, one item or an array of items.
        Password defaults must be strings and remain plaintext in the specification; omit the
        default when collecting a secret. SecureString defaults are rejected without decryption.
    .PARAMETER ValidateSet
        For Choice and List prompts, the allowed values. Required when InputType is Choice or List.
    .PARAMETER As
        For Choice prompts, how to render the set: Combo (default) or Radio.
    .PARAMETER MultiSelect
        For List prompts, allow more than one item to be selected. The captured value is then always
        an array, even for one selected item. Not valid for other input types.
    .PARAMETER ValidateNotEmpty
        Require a non-whitespace value (for a List prompt, at least one selected item) before the
        dialog can close on a non-cancel button. Secure passwords require Length greater than zero.
    .PARAMETER ValidatePattern
        A regular expression the value must match. Password prompts require -AsPlainText to use it.
    .PARAMETER ValidateScript
        A scriptblock that receives the value and returns $true when valid. On a separate UI
        runspace it is recreated from text; caller variables, functions and closures are unavailable.
        Keep validators self-contained. On the caller's STA thread the live block is preserved.
        Password validators receive SecureString unless -AsPlainText is specified; use Length to
        check the number of characters. Do not retain or dispose the validator input; the module
        replaces it as the field changes. Required secure passwords must have at least one character.
    .EXAMPLE
        New-FluencePrompt -Name User -Message 'Account name' -ValidateNotEmpty
    .EXAMPLE
        New-FluencePrompt -Name Features -Message 'Features' -InputType List -ValidateSet 'Core', 'Docs', 'Samples' -MultiSelect -DefaultValue 'Core'
    .OUTPUTS
        Fluence.Prompt
    .NOTES
        Does not require a host application; this only builds a specification object.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Prompt')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds an in-memory specification object; changes no system state.')]
    param
    (
        [Parameter()]
        [string]$Name,

        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Message,

        [Parameter()]
        [ValidateSet('Text', 'Multiline', 'Password', 'Number', 'Checkbox', 'Toggle',
            'Choice', 'List', 'Date', 'Time', 'FileOpen', 'FileSave', 'FolderOpen', 'Link')]
        [string]$InputType = 'Text',

        [Parameter()]
        [object]$DefaultValue,

        [Parameter()]
        [switch]$AsPlainText,

        [Parameter()]
        [string[]]$ValidateSet,

        [Parameter()]
        [ValidateSet('Combo', 'Radio')]
        [string]$As = 'Combo',

        [Parameter()]
        [switch]$MultiSelect,

        [Parameter()]
        [switch]$ValidateNotEmpty,

        [Parameter()]
        [string]$ValidatePattern,

        [Parameter()]
        [scriptblock]$ValidateScript
    )

    if (($InputType -eq 'Choice' -or $InputType -eq 'List') -and ($null -eq $ValidateSet -or $ValidateSet.Count -eq 0))
    {
        throw "A $InputType prompt requires -ValidateSet."
    }

    if ($MultiSelect -and $InputType -ne 'List')
    {
        throw '-MultiSelect applies only to List prompts.'
    }

    if ($AsPlainText -and $InputType -ne 'Password')
    {
        throw '-AsPlainText applies only to Password prompts.'
    }

    if ($InputType -eq 'Password')
    {
        if ($null -ne $DefaultValue -and $DefaultValue -isnot [string])
        {
            throw 'A Password -DefaultValue must be a string. Omit it to collect a password without a plaintext default.'
        }
        if (-not $AsPlainText -and -not [string]::IsNullOrWhiteSpace($ValidatePattern))
        {
            throw 'Password -ValidatePattern requires -AsPlainText. Use -ValidateScript with SecureString.Length for secure validation.'
        }
    }

    if ([string]::IsNullOrWhiteSpace($Name))
    {
        $Name = 'Input_' + [guid]::NewGuid().ToString('N').Substring(0, 8)
    }

    # Coerce a provided DefaultValue to the type the input control expects, so a non-coercible value
    # fails fast here (at spec-build time, with a clear message) instead of throwing deep inside the
    # UI thread when New-FluenceInputControl casts it at render time. [Convert]::ToBoolean is used for
    # Checkbox/Toggle so the string 'false' becomes $false (a plain [bool] cast treats any non-empty
    # string as $true).
    if ($PSBoundParameters.ContainsKey('DefaultValue') -and $null -ne $DefaultValue)
    {
        try
        {
            switch ($InputType)
            {
                'Number'   { $DefaultValue = [double]$DefaultValue }
                'Date'     { $DefaultValue = [datetime]$DefaultValue }
                'Time'     { $DefaultValue = [timespan]$DefaultValue }
                'Checkbox' { $DefaultValue = [System.Convert]::ToBoolean($DefaultValue) }
                'Toggle'   { $DefaultValue = [System.Convert]::ToBoolean($DefaultValue) }
            }
        }
        catch
        {
            throw "DefaultValue '$DefaultValue' is not valid for an InputType of '$InputType': $($_.Exception.Message)"
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ValidatePattern))
    {
        try
        {
            $null = [regex]::new($ValidatePattern)
        }
        catch
        {
            throw "-ValidatePattern is not a valid regular expression: $($_.Exception.Message)"
        }
    }

    if (($InputType -eq 'Choice' -or $InputType -eq 'List') -and $null -ne $DefaultValue)
    {
        $defaults = @($DefaultValue)
        if (-not $MultiSelect -and $defaults.Count -ne 1)
        {
            throw "A single-selection $InputType prompt requires one -DefaultValue."
        }
        $canonical = @()
        foreach ($value in $defaults)
        {
            $match = @($ValidateSet | Where-Object { $_ -eq [string]$value })
            if ($match.Count -eq 0)
            {
                throw "DefaultValue '$value' is not in -ValidateSet for '$Name'."
            }
            $canonical += $match[0]
        }
        if ($MultiSelect)
        {
            $DefaultValue = [object[]]$canonical
        }
        else
        {
            $DefaultValue = $canonical[0]
        }
    }

    $prompt = [pscustomobject]@{
        PSTypeName       = 'Fluence.Prompt'
        Name             = $Name
        Message          = $Message
        InputType        = $InputType
        AsPlainText      = [bool]$AsPlainText
        DefaultValue     = $DefaultValue
        ValidateSet      = $ValidateSet
        As               = $As
        MultiSelect      = [bool]$MultiSelect
        ValidateNotEmpty = [bool]$ValidateNotEmpty
        ValidatePattern  = $ValidatePattern
        ValidateScript   = $ValidateScript
    }

    return $prompt
}
