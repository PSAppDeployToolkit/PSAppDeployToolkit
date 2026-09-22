<#
.SYNOPSIS
    Generates one Markdown reference page per exported cmdlet from the module's comment-based help.
.DESCRIPTION
    Imports the module, then for every exported function writes docs/powershell/reference/<Name>.md
    with the synopsis, the syntax from Get-Command -Syntax, a parameter table (type, required,
    default, description) built from Get-Help, the outputs, the examples and the notes. The pages
    are generated, not hand-edited: change the comment-based help in the function and re-run.

    The output is UTF-8 with BOM and LF line endings, as the repository text policy requires.
.PARAMETER OutputPath
    The folder that receives the pages. Defaults to <repo>/docs/powershell/reference.
.EXAMPLE
    pwsh -NoProfile -File build/Export-ModuleReference.ps1
.NOTES
    Run from any location after build/Build-Module.ps1 has staged the assemblies (the module
    import needs them). Does not require a host application.
#>
[CmdletBinding()]
param
(
    [Parameter()]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$moduleRoot = Split-Path $PSScriptRoot -Parent
$repo = Split-Path $moduleRoot -Parent
if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    $OutputPath = Join-Path $repo 'docs\powershell\reference'
}
$null = New-Item -ItemType Directory -Path $OutputPath -Force

Import-Module (Join-Path $moduleRoot 'src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1') -Force

function ConvertTo-OneLine
{
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text))
    {
        return ''
    }
    $flat = ($Text -replace '\s+', ' ').Trim()
    return $flat.Replace('|', '\|')
}

function ConvertTo-ParagraphList
{
    param([object[]]$Blocks)
    $lines = @()
    foreach ($block in @($Blocks))
    {
        $text = [string]$block.Text
        if ([string]::IsNullOrWhiteSpace($text))
        {
            continue
        }
        # Comment-based help wraps lines at column ~100; rejoin a paragraph, keep blank-line breaks.
        foreach ($paragraph in ($text -split '(?:\r?\n){2,}'))
        {
            $joined = (($paragraph -split '\r?\n') | ForEach-Object { $_.Trim() }) -join ' '
            if (-not [string]::IsNullOrWhiteSpace($joined))
            {
                $lines += $joined.Trim()
                $lines += ''
            }
        }
    }
    return $lines
}

$commonParameters = @('Verbose', 'Debug', 'ErrorAction', 'WarningAction', 'InformationAction', 'ProgressAction',
    'ErrorVariable', 'WarningVariable', 'InformationVariable', 'OutVariable', 'OutBuffer', 'PipelineVariable',
    'WhatIf', 'Confirm')

$exported = (Get-Module Fluence.Wpf.PowerShell).ExportedFunctions.Keys | Sort-Object
$encoding = [System.Text.UTF8Encoding]::new($true)

foreach ($name in $exported)
{
    $command = Get-Command -Name $name -Module Fluence.Wpf.PowerShell
    $help = Get-Help -Name $name -Full
    $syntax = ((Get-Command -Name $name -Syntax) -split '\r?\n' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join "`n"

    $out = [System.Collections.Generic.List[string]]::new()
    $out.Add("# $name")
    $out.Add('')
    $out.Add((ConvertTo-OneLine ([string]$help.Synopsis)))
    $out.Add('')
    $out.Add('## Syntax')
    $out.Add('')
    $out.Add('```text')
    foreach ($line in ($syntax -split "`n"))
    {
        $out.Add($line.Trim())
    }
    $out.Add('```')
    $out.Add('')

    $description = ConvertTo-ParagraphList -Blocks $help.description
    if ($description.Count -gt 0)
    {
        $out.Add('## Description')
        $out.Add('')
        foreach ($line in $description)
        {
            $out.Add($line)
        }
    }

    $out.Add('## Parameters')
    $out.Add('')
    $out.Add('| Name | Type | Required | Default | Description |')
    $out.Add('| --- | --- | --- | --- | --- |')
    $helpParameters = @{}
    foreach ($p in @($help.parameters.parameter))
    {
        $helpParameters[[string]$p.name] = $p
    }

    # Defaults come from the parameter block itself: Get-Help reports the type's zero value (0, False)
    # for a parameter that declares no default, which would read as a real default in the table.
    $declaredDefaults = @{}
    foreach ($astParameter in @($command.ScriptBlock.Ast.Body.ParamBlock.Parameters))
    {
        if ($null -ne $astParameter.DefaultValue)
        {
            $declaredDefaults[$astParameter.Name.VariablePath.UserPath] = $astParameter.DefaultValue.Extent.Text.Trim("'", '"')
        }
    }
    foreach ($parameterName in $command.Parameters.Keys)
    {
        if ($commonParameters -contains $parameterName)
        {
            continue
        }
        $metadata = $command.Parameters[$parameterName]
        $typeName = $metadata.ParameterType.Name
        if ($metadata.ParameterType -eq [System.Management.Automation.SwitchParameter])
        {
            $typeName = 'switch'
        }
        $required = 'No'
        foreach ($set in $metadata.ParameterSets.Values)
        {
            if ($set.IsMandatory)
            {
                $required = 'Yes'
            }
        }
        if ($metadata.ParameterSets.Count -gt 1 -and $required -eq 'Yes')
        {
            $required = 'In some sets'
        }
        $default = ''
        if ($declaredDefaults.ContainsKey($parameterName))
        {
            $default = ConvertTo-OneLine ([string]$declaredDefaults[$parameterName])
        }
        $description = ''
        if ($helpParameters.ContainsKey($parameterName))
        {
            $hp = $helpParameters[$parameterName]
            $description = ConvertTo-OneLine (([string[]]($hp.description | ForEach-Object { $_.Text })) -join ' ')
        }
        $validValues = @()
        foreach ($attribute in $metadata.Attributes)
        {
            if ($attribute -is [System.Management.Automation.ValidateSetAttribute])
            {
                $validValues = @($attribute.ValidValues)
            }
        }
        if ($validValues.Count -gt 0)
        {
            $description = ($description + ' Values: ' + ($validValues -join ', ') + '.').Trim()
        }
        $out.Add("| ``-$parameterName`` | $typeName | $required | $default | $description |")
    }
    $out.Add('')

    $outputs = @()
    foreach ($returnValue in @($help.returnValues.returnValue))
    {
        $typeText = ConvertTo-OneLine ([string]$returnValue.type.name)
        if (-not [string]::IsNullOrWhiteSpace($typeText))
        {
            $outputs += $typeText
        }
    }
    if ($outputs.Count -gt 0)
    {
        $out.Add('## Outputs')
        $out.Add('')
        foreach ($o in $outputs)
        {
            $out.Add("- $o")
        }
        $out.Add('')
    }

    $examples = @($help.examples.example)
    if ($examples.Count -gt 0)
    {
        $out.Add('## Examples')
        $out.Add('')
        $index = 1
        foreach ($example in $examples)
        {
            $out.Add("### Example $index")
            $out.Add('')
            $out.Add('```powershell')
            $code = ([string]$example.code).TrimEnd()
            foreach ($line in ($code -split '\r?\n'))
            {
                $out.Add($line)
            }
            $out.Add('```')
            $out.Add('')
            foreach ($line in (ConvertTo-ParagraphList -Blocks $example.remarks))
            {
                $out.Add($line)
            }
            $index++
        }
    }

    $notes = ConvertTo-ParagraphList -Blocks $help.alertSet.alert
    if ($notes.Count -gt 0)
    {
        $out.Add('## Notes')
        $out.Add('')
        foreach ($line in $notes)
        {
            $out.Add($line)
        }
    }

    $out.Add('## See also')
    $out.Add('')
    $out.Add('- [Reference index](README.md)')
    $out.Add('- [Result objects](result-objects.md)')
    $out.Add('- [Input types](input-types.md)')
    $out.Add('')

    $text = ($out -join "`n").TrimEnd() + "`n"
    $text = $text.Replace([string][char]0x2014, ', ').Replace([string][char]0x2013, '-')
    [System.IO.File]::WriteAllText((Join-Path $OutputPath "$name.md"), $text, $encoding)
    Write-Output "Wrote $name.md"
}
