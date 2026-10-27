<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


param(
    [ValidateSet('Write', 'Check')]
    [string]$Mode = 'Write',
    [switch]$Check,
    [string]$Configuration = 'Debug',
    [string]$OutputRoot,
    [string]$CoveragePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($Check) { $Mode = 'Check' }

$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if ([string]::IsNullOrWhiteSpace($OutputRoot)) { $OutputRoot = Join-Path $repository 'artifacts/docs/api' }
if ([string]::IsNullOrWhiteSpace($CoveragePath)) { $CoveragePath = Join-Path $repository 'artifacts/docs/coverage.md' }
$outputRoot = [System.IO.Path]::GetFullPath($OutputRoot, $repository)
$coveragePath = [System.IO.Path]::GetFullPath($CoveragePath, $repository)
$framework = 'net10.0-windows10.0.26100.0'
$assemblyPath = Join-Path $repository "Fluence.Wpf/bin/$Configuration/$framework/Fluence.Wpf.dll"
$xmlPath = Join-Path $repository "Fluence.Wpf/bin/$Configuration/$framework/Fluence.Wpf.xml"
if (-not (Test-Path $assemblyPath) -or -not (Test-Path $xmlPath)) {
    throw "Build Fluence.Wpf for $framework in $Configuration before generating API docs."
}

$assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$commentsXml = [System.Xml.Linq.XDocument]::Load($xmlPath)
$comments = @{}
foreach ($member in $commentsXml.Descendants('member')) {
    $comments[$member.Attribute('name').Value] = $member
}

# An implicit parameterless constructor appears in reflection, but has no source
# declaration or compiler XML entry. Search all library sources so a constructor
# declared in another part of a partial type is not mistaken for an implicit one.
$sourceText = ((Get-ChildItem (Join-Path $repository 'Fluence.Wpf') -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' } |
    ForEach-Object { [System.IO.File]::ReadAllText($_.FullName) }) -join "`n")

function Test-ImplicitConstructor([System.Reflection.ConstructorInfo]$constructor) {
    if ($constructor.GetParameters().Count -ne 0) { return $false }
    $name = [regex]::Escape(($constructor.DeclaringType.Name -replace '`\d+$', ''))
    if ($sourceText -match ('\b(?:class|struct|record(?:\s+(?:class|struct))?)\s+' + $name + '(?:\s*<[^>]+>)?\s*\(')) { return $false }
    # Attributes may precede the declaration or share its line. Match through
    # the final bracket on that line so nested array syntax in an attribute
    # cannot make an explicit constructor look compiler-generated.
    $pattern = '(?m)^\s*(?:\[[^\r\n]*\]\s*)*(?<modifiers>(?:(?:public|protected|internal|private|static|extern|unsafe)\s+)*)' + $name + '\s*\('
    foreach ($match in [regex]::Matches($sourceText, $pattern)) {
        if ($match.Groups['modifiers'].Value -notmatch '\bstatic\b') { return $false }
    }
    return $true
}

$flags = [System.Reflection.BindingFlags]'DeclaredOnly,Public,NonPublic,Instance,Static'
$nullability = [System.Reflection.NullabilityInfoContext]::new()
$allTypes = @($assembly.GetExportedTypes() | Where-Object { $_.Namespace -like 'Fluence.Wpf*' } | Sort-Object FullName)
$typePaths = @{}
foreach ($type in $allTypes) {
    $namespace = $type.Namespace
    $filename = ($type.FullName.Substring($namespace.Length + 1) -replace '\+', '.') + '.md'
    $typePaths[$type.FullName] = "$namespace/$filename"
}
$memberPaths = @{}
$script:currentPage = ''

function Get-Anchor([string]$id) {
    $digest = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($id))
    return 'api-' + [Convert]::ToHexString($digest).Substring(0, 12).ToLowerInvariant()
}

function Get-RelativeDocLink([string]$targetPath, [string]$anchor = '') {
    $sourceDirectory = [System.IO.Path]::GetDirectoryName($script:currentPage)
    $relative = [System.IO.Path]::GetRelativePath((Join-Path $outputRoot $sourceDirectory), (Join-Path $outputRoot $targetPath)).Replace('\', '/')
    if ($targetPath -eq $script:currentPage) { $relative = [System.IO.Path]::GetFileName($targetPath) }
    if ($anchor) { return $relative + '#' + $anchor }
    if (-not $relative) { return './' }
    return $relative
}

function Get-CrefLink([string]$cref) {
    $target = $cref -replace '^[A-Z]:', ''
    $name = (($target -split '\(')[0] -split '\.')[-1]
    if ($memberPaths.ContainsKey($cref)) {
        $entry = $memberPaths[$cref]
        return '[' + $name + '](' + (Get-RelativeDocLink $entry.Path $entry.Anchor) + ')'
    }
    if ($typePaths.ContainsKey($target)) { return '[' + $name + '](' + (Get-RelativeDocLink $typePaths[$target]) + ')' }
    return '`' + $name + '`'
}

function Format-Code([string]$raw, [string]$language) {
    $codeLines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($raw -split '\r?\n')) { $codeLines.Add($line.TrimEnd()) }
    while ($codeLines.Count -gt 0 -and -not $codeLines[0].Trim()) { $codeLines.RemoveAt(0) }
    while ($codeLines.Count -gt 0 -and -not $codeLines[$codeLines.Count - 1].Trim()) { $codeLines.RemoveAt($codeLines.Count - 1) }
    $indents = @($codeLines | Where-Object { $_.Trim() } | ForEach-Object { ([regex]::Match($_, '^\s*')).Length })
    $indent = if ($indents.Count -gt 0) { ($indents | Measure-Object -Minimum).Minimum } else { 0 }
    $normalized = @($codeLines | ForEach-Object { if ($_.Length -ge $indent) { $_.Substring($indent) } else { $_ } })
    return "`n`n~~~$language`n" + ($normalized -join "`n") + "`n~~~`n`n"
}

function Get-LearnUrl([type]$type) {
    $definition = if ($type.IsGenericType) { $type.GetGenericTypeDefinition() } else { $type }
    $name = $definition.FullName.Replace('+', '.') -replace '`(\d+)', '-$1'
    return 'https://learn.microsoft.com/dotnet/api/' + $name.ToLowerInvariant()
}

function Get-TypeId([type]$type) {
    if ($type.IsByRef) { return (Get-TypeId $type.GetElementType()) + '@' }
    if ($type.IsPointer) { return (Get-TypeId $type.GetElementType()) + '*' }
    if ($type.IsArray) {
        $element = Get-TypeId $type.GetElementType()
        if ($type.GetArrayRank() -eq 1) { return "$element[]" }
        return $element + '[' + ((@('0:') * $type.GetArrayRank()) -join ',') + ']'
    }
    if ($type.IsGenericParameter) {
        if ($type.DeclaringMethod) { return '``' + $type.GenericParameterPosition }
        return '`' + $type.GenericParameterPosition
    }
    if ($type.IsGenericType -and -not $type.IsGenericTypeDefinition) {
        $name = $type.GetGenericTypeDefinition().FullName.Replace('+', '.') -replace '`\d+', ''
        return $name + '{' + (($type.GetGenericArguments() | ForEach-Object { Get-TypeId $_ }) -join ',') + '}'
    }
    return $type.FullName.Replace('+', '.')
}

function Get-MemberId([System.Reflection.MemberInfo]$member) {
    $prefix = switch ($member.MemberType) {
        'TypeInfo' { 'T:' }
        'NestedType' { 'T:' }
        'Constructor' { 'M:' }
        'Method' { 'M:' }
        'Property' { 'P:' }
        'Event' { 'E:' }
        'Field' { 'F:' }
        default { throw "Unknown member type $($member.MemberType)" }
    }
    if ($member -is [type]) { return $prefix + $member.FullName.Replace('+', '.') }
    $name = if ($member -is [System.Reflection.ConstructorInfo]) { '#ctor' } else { $member.Name.Replace('.', '#') }
    if ($member -is [System.Reflection.MethodInfo] -and $member.IsGenericMethod) {
        $name += '``' + $member.GetGenericArguments().Count
    }
    $id = $prefix + $member.DeclaringType.FullName.Replace('+', '.') + '.' + $name
    $parameters = @()
    if ($member -is [System.Reflection.MethodBase]) { $parameters = @($member.GetParameters()) }
    elseif ($member -is [System.Reflection.PropertyInfo]) { $parameters = @($member.GetIndexParameters()) }
    if ($parameters.Count -gt 0) { $id += '(' + (($parameters | ForEach-Object { Get-TypeId $_.ParameterType }) -join ',') + ')' }
    if ($member -is [System.Reflection.MethodInfo] -and $member.Name -eq 'op_Implicit') {
        $id += '~' + (Get-TypeId $member.ReturnType)
    }
    if ($member -is [System.Reflection.MethodInfo] -and $member.Name -eq 'op_Explicit') {
        $id += '~' + (Get-TypeId $member.ReturnType)
    }
    return $id
}

function Format-Type([type]$type, $nullable = $null) {
    if ($type.IsByRef) { return Format-Type $type.GetElementType() $nullable.ElementType }
    if ($type.IsArray) {
        $elementNullable = if ($null -ne $nullable) { $nullable.ElementType } else { $null }
        $suffix = '[' + (',' * ($type.GetArrayRank() - 1)) + ']'
        return (Format-Type $type.GetElementType() $elementNullable) + $suffix
    }
    $aliases = @{ 'System.Void'='void'; 'System.Boolean'='bool'; 'System.Byte'='byte'; 'System.SByte'='sbyte'; 'System.Int16'='short'; 'System.UInt16'='ushort'; 'System.Int32'='int'; 'System.UInt32'='uint'; 'System.Int64'='long'; 'System.UInt64'='ulong'; 'System.Single'='float'; 'System.Double'='double'; 'System.Decimal'='decimal'; 'System.Char'='char'; 'System.String'='string'; 'System.Object'='object' }
    $underlying = [System.Nullable]::GetUnderlyingType($type)
    if ($null -ne $underlying) { return (Format-Type $underlying) + '?' }
    if ($type.IsGenericParameter) { return $type.Name }
    $name = if ($aliases.ContainsKey($type.FullName)) { $aliases[$type.FullName] } else { $type.Name -replace '`\d+$', '' }
    if ($type.IsNested) { $name = (Format-Type $type.DeclaringType) + '.' + $name }
    if ($type.IsGenericType) {
        $args = @($type.GetGenericArguments())
        if ($type.IsNested) { $args = @($args | Select-Object -Skip $type.DeclaringType.GetGenericArguments().Count) }
        $formatted = @(for ($i = 0; $i -lt $args.Count; $i++) {
            $argNullable = if ($null -ne $nullable -and $nullable.GenericTypeArguments.Length -gt $i) { $nullable.GenericTypeArguments[$i] } else { $null }
            Format-Type $args[$i] $argNullable
        })
        if ($formatted.Count -gt 0) { $name += '<' + ($formatted -join ', ') + '>' }
    }
    if ($null -ne $nullable -and $nullable.ReadState -eq 'Nullable' -and -not $type.IsValueType) { $name += '?' }
    return $name
}

function Format-Parameter([System.Reflection.ParameterInfo]$parameter, [bool]$extensionFirst = $false) {
    $prefix = if ($parameter.IsOut) { 'out ' } elseif ($parameter.IsIn) { 'in ' } elseif ($parameter.ParameterType.IsByRef) { 'ref ' } elseif ($extensionFirst) { 'this ' } else { '' }
    if ($parameter.GetCustomAttributes([System.ParamArrayAttribute], $false).Count -gt 0) { $prefix = 'params ' }
    $typeName = Format-Type $parameter.ParameterType ($nullability.Create($parameter))
    $result = $prefix + $typeName + ' ' + $parameter.Name
    if ($parameter.IsOptional) {
        $value = $parameter.DefaultValue
        $formatted = if ($null -eq $value) { 'null' } elseif ($value -is [string]) { '"' + $value.Replace('"', '\"') + '"' } elseif ($value -is [bool]) { $value.ToString().ToLowerInvariant() } elseif ($value -is [char]) { "'$value'" } elseif ($value -is [System.DBNull] -or $value -is [System.Reflection.Missing]) { 'default' } elseif ($parameter.ParameterType.IsEnum) { (Format-Type $parameter.ParameterType) + '.' + $value } else { [string]$value }
        $result += ' = ' + $formatted
    }
    return $result
}

function Get-Visibility([System.Reflection.MemberInfo]$member) {
    if ($member -is [System.Reflection.MethodBase]) { $method = $member }
    elseif ($member -is [System.Reflection.PropertyInfo]) { $method = $member.GetAccessors($true) | Sort-Object { if ($_.IsPublic) { 0 } elseif ($_.IsFamily -or $_.IsFamilyOrAssembly) { 1 } else { 2 } } | Select-Object -First 1 }
    elseif ($member -is [System.Reflection.EventInfo]) { $method = $member.GetAddMethod($true) }
    elseif ($member -is [System.Reflection.FieldInfo]) { $method = $member }
    else { return 'public' }
    if ($method.IsPublic) { return 'public' }
    if ($method.IsFamilyOrAssembly) { return 'protected internal' }
    if ($method.IsFamily) { return 'protected' }
    return $null
}

function Get-VirtualModifier([System.Reflection.MethodInfo]$method) {
    if (-not $method.IsVirtual) { return '' }
    $overrides = $method.GetBaseDefinition().DeclaringType -ne $method.DeclaringType
    if ($overrides) {
        if ($method.IsAbstract) { return ' abstract override' }
        if ($method.IsFinal) { return ' sealed override' }
        return ' override'
    }
    if ($method.IsAbstract) { return ' abstract' }
    if ($method.IsFinal) { return '' }
    return ' virtual'
}

function Format-Signature([System.Reflection.MemberInfo]$member) {
    $visibility = Get-Visibility $member
    if ($member -is [System.Reflection.ConstructorInfo]) {
        return "$visibility $($member.DeclaringType.Name -replace '`\d+$','')(" + ((@($member.GetParameters()) | ForEach-Object { Format-Parameter $_ }) -join ', ') + ')'
    }
    if ($member -is [System.Reflection.MethodInfo]) {
        $modifiers = if ($member.IsStatic) { ' static' } else { Get-VirtualModifier $member }
        $return = Format-Type $member.ReturnType ($nullability.Create($member.ReturnParameter))
        $generic = if ($member.IsGenericMethod) { '<' + (($member.GetGenericArguments() | ForEach-Object Name) -join ', ') + '>' } else { '' }
        $isExtension = $member.IsDefined([System.Runtime.CompilerServices.ExtensionAttribute], $false)
        $parameterIndex = 0
        $parameters = @(foreach ($parameter in $member.GetParameters()) {
            Format-Parameter $parameter ($isExtension -and $parameterIndex -eq 0)
            $parameterIndex++
        })
        return "$visibility$modifiers $return $($member.Name)$generic(" + ($parameters -join ', ') + ')'
    }
    if ($member -is [System.Reflection.PropertyInfo]) {
        $accessors = @($member.GetAccessors($true) | Where-Object { Get-Visibility $_ })
        $access = @($accessors | ForEach-Object { if ($_.Name.StartsWith('get_')) { 'get;' } elseif ($_.Name.StartsWith('set_')) { 'set;' } }) -join ' '
        $modifiers = if ($accessors.Count -gt 0 -and $accessors[0].IsStatic) { ' static' } elseif ($accessors.Count -gt 0) { Get-VirtualModifier $accessors[0] } else { '' }
        $name = if ($member.GetIndexParameters().Count -gt 0) { 'this[' + ((@($member.GetIndexParameters()) | ForEach-Object { Format-Parameter $_ }) -join ', ') + ']' } else { $member.Name }
        return "$visibility$modifiers $(Format-Type $member.PropertyType ($nullability.Create($member))) $name { $access }"
    }
    if ($member -is [System.Reflection.EventInfo]) {
        $static = if ($member.GetAddMethod($true).IsStatic) { ' static' } else { '' }
        return "$visibility$static event $(Format-Type $member.EventHandlerType ($nullability.Create($member))) $($member.Name)"
    }
    if ($member -is [System.Reflection.FieldInfo]) {
        if ($member.DeclaringType.IsEnum) { return $member.Name + ' = ' + $member.GetRawConstantValue() }
        $static = if ($member.IsLiteral) { ' const' } elseif ($member.IsStatic) { ' static' } else { '' }
        $readonly = if ($member.IsInitOnly) { ' readonly' } else { '' }
        $value = if ($member.IsLiteral) { ' = ' + $member.GetRawConstantValue() } else { '' }
        return "$visibility$static$readonly $(Format-Type $member.FieldType ($nullability.Create($member))) $($member.Name)$value"
    }
    throw "Unsupported signature: $($member.MemberType)"
}

function Get-PublicMembers([type]$type, [string]$kind) {
    $members = switch ($kind) {
        'Constructors' { $type.GetConstructors($flags) }
        'Properties' { $type.GetProperties($flags) }
        'Methods' { $type.GetMethods($flags) | Where-Object { -not $_.IsSpecialName } }
        'Events' { $type.GetEvents($flags) }
        'Fields' { $type.GetFields($flags) | Where-Object { $_.Name -ne 'value__' } }
    }
    return @($members | Where-Object { Get-Visibility $_ } | Sort-Object Name, { Get-MemberId $_ })
}

foreach ($type in $allTypes) {
    foreach ($section in @('Constructors','Properties','Methods','Events','Fields')) {
        foreach ($member in @(Get-PublicMembers $type $section)) {
            $id = Get-MemberId $member
            $memberPaths[$id] = @{ Path = $typePaths[$type.FullName]; Anchor = Get-Anchor $id }
        }
    }
}

function Add-RelatedType([System.Collections.Generic.HashSet[string]]$related, [type]$candidate) {
    if ($null -eq $candidate) { return }
    if ($candidate.HasElementType) { Add-RelatedType $related $candidate.GetElementType(); return }
    if ($candidate.IsGenericParameter) { return }
    if ($candidate.IsGenericType) {
        foreach ($arg in $candidate.GetGenericArguments()) { Add-RelatedType $related $arg }
        $candidate = $candidate.GetGenericTypeDefinition()
    }
    if ($typePaths.ContainsKey($candidate.FullName)) { [void]$related.Add($candidate.FullName) }
}

function Get-DocText($element) {
    if ($null -eq $element) { return '' }
    $content = foreach ($node in $element.Nodes()) {
        if ($node -is [System.Xml.Linq.XText]) { ([string]$node.Value -replace '\s+', ' ' -replace '&', '&amp;' -replace '<', '&lt;' -replace '>', '&gt;') }
        elseif ($node -is [System.Xml.Linq.XElement]) {
            $inner = Get-DocText $node
            switch ($node.Name.LocalName) {
                'see' { if ($node.Attribute('langword')) { '`' + $node.Attribute('langword').Value + '`' } elseif ($node.Attribute('cref')) { Get-CrefLink $node.Attribute('cref').Value } else { $inner } }
                'paramref' { '`' + $node.Attribute('name').Value + '`' }
                'typeparamref' { '`' + $node.Attribute('name').Value + '`' }
                'c' { '`' + $inner.Trim() + '`' }
                'para' { "`n`n" + $inner + "`n`n" }
                'br' { "`n" }
                'code' { Format-Code $node.Value $(if ($node.Attribute('language')) { $node.Attribute('language').Value } else { '' }) }
                default { $inner }
            }
        }
    }
    return ($content -join '').Trim()
}

function Get-Comment([string]$id) {
    if ($comments.ContainsKey($id)) { return $comments[$id] }
    return $null
}

function Add-Documentation([System.Collections.Generic.List[string]]$lines, $comment, [System.Reflection.MemberInfo]$member) {
    if ($null -eq $comment) { return }
    $summary = Get-DocText $comment.Element('summary')
    if ($summary) { $lines.Add($summary); $lines.Add('') }
    if ($comment.Element('inheritdoc') -and -not $summary) {
        $baseType = if ($member -is [type]) { $member.BaseType } else { $member.DeclaringType.BaseType }
        if ($null -ne $baseType -and $typePaths.ContainsKey($baseType.FullName)) {
            $link = '../' + $typePaths[$baseType.FullName]
            $lines.Add('Documentation inherited from the [' + $baseType.Name + ' API](' + $link + ').'); $lines.Add('')
        } elseif ($null -ne $baseType) {
            $lines.Add('Documentation inherited from the [`' + (Format-Type $baseType) + '` API](' + (Get-LearnUrl $baseType) + ').'); $lines.Add('')
        }
    }
    $remarks = Get-DocText $comment.Element('remarks')
    if ($remarks) { $lines.Add('**Remarks:** ' + $remarks); $lines.Add('') }
    $example = Get-DocText $comment.Element('example')
    if ($example) { $lines.Add('**Example**'); $lines.Add(''); $lines.Add($example); $lines.Add('') }
    foreach ($tag in @('typeparam','param','returns','value','exception')) {
        foreach ($item in $comment.Elements($tag)) {
            $body = Get-DocText $item
            if (-not $body) { continue }
            $label = switch ($tag) { 'param' { 'Parameter' }; 'typeparam' { 'Type parameter' }; 'returns' { 'Returns' }; 'value' { 'Value' }; 'exception' { 'Exception' } }
            $name = if ($item.Attribute('name')) { ' `' + $item.Attribute('name').Value + '`' } elseif ($item.Attribute('cref')) { ' `' + ($item.Attribute('cref').Value -replace '^[A-Z]:','') + '`' } else { '' }
            $lines.Add("**${label}${name}:** $body")
            $lines.Add('')
        }
    }
}

function New-Lines { return ,([System.Collections.Generic.List[string]]::new()) }
function Save-Page([string]$relative, [System.Collections.Generic.List[string]]$lines) {
    $content = (($lines -join "`n").TrimEnd() + "`n")
    $script:pages[$relative] = $content
}

$pages = @{}
$coverage = [ordered]@{ types = 0; constructors = 0; properties = 0; methods = 0; events = 0; fields = 0; documented = 0; implicitConstructors = [System.Collections.Generic.List[string]]::new(); missingComments = [System.Collections.Generic.List[string]]::new() }
$namespaces = @($allTypes | Group-Object Namespace | Sort-Object Name)
$landingLines = New-Lines
$landingLines.Add('# C# API reference'); $landingLines.Add('')
$landingLines.Add('Public and protected members of the Fluence.Wpf assembly, generated from the compiled .NET 10 API and its XML documentation. Protected members are labeled for control authors. Framework members inherited from WPF are linked through the base type; they are not repeated on every page.'); $landingLines.Add('')
$landingLines.Add('For task-oriented examples, see the [control catalog](../controls.md), [Basic walkthrough](../csharp/usage.md), and [theme resources](../theming.md).'); $landingLines.Add('')
$landingLines.Add('## Common starting points'); $landingLines.Add('')
$landingLines.Add('| Task | API | Usage guide |')
$landingLines.Add('| --- | --- | --- |')
$landingLines.Add('| Create a window | [FluenceWindow](Fluence.Wpf.Controls/FluenceWindow.md), [TitleBar](Fluence.Wpf.Controls/TitleBar.md) | [Window and title bar](../how-to/window-and-title-bar.md) |')
$landingLines.Add('| Build a first window | [Button](Fluence.Wpf.Controls/Button.md), [StackPanel](Fluence.Wpf.Controls/StackPanel.md) | [Basic walkthrough](../csharp/usage.md) |')
$landingLines.Add('| Set a theme | [ApplicationThemeManager](Fluence.Wpf/ApplicationThemeManager.md), [SystemThemeWatcher](Fluence.Wpf/SystemThemeWatcher.md) | [Theme and accent](../how-to/theme-and-accent.md) |')
$landingLines.Add('| Choose an accent | [ApplicationAccentColorManager](Fluence.Wpf/ApplicationAccentColorManager.md) | [Theme resources](../theming.md) |')
$landingLines.Add('| Show a dialog | [ContentDialog](Fluence.Wpf.Controls/ContentDialog.md) | [Dialogs and feedback](../how-to/dialogs-and-feedback.md) |')
$landingLines.Add('| Navigate between pages | [NavigationView](Fluence.Wpf.Controls/NavigationView.md), [TabView](Fluence.Wpf.Controls/TabView.md) | [Navigation and tabs](../how-to/navigation-and-tabs.md) |'); $landingLines.Add('')
$landingLines.Add('## Member scope'); $landingLines.Add('')
$landingLines.Add('API pages document members declared by each Fluence type and identify its base type. Inherited WPF members remain part of the API. Protected members are intended for derived controls. For dependency properties, the CLR property is usually the value an application sets; the corresponding static `...Property` field identifies the dependency property. Attached properties use their documented `Get...` and `Set...` methods.'); $landingLines.Add('')
$landingLines.Add('## Template and resource contracts'); $landingLines.Add('')
$landingLines.Add('Default control templates live in `Themes/Controls/` and are merged by `Themes/Generic.xaml`. Named parts and visual states are documented on their CLR type and in the template. The [theme resource reference](../theming.md) describes supported application-wide color, brush, typography, and metric keys. The [PowerShell command reference](../powershell/reference/README.md) is separate from the CLR API.'); $landingLines.Add('')
$landingLines.Add('## Namespaces'); $landingLines.Add('')
foreach ($group in $namespaces) { $landingLines.Add('- [' + $group.Name + '](' + $group.Name + '/index.md) (' + $group.Count + ' types)') }
Save-Page 'index.md' $landingLines

foreach ($group in $namespaces) {
    $namespace = $group.Name
    $index = New-Lines
    $index.Add('# ' + $namespace); $index.Add('')
    $index.Add('## Types'); $index.Add('')
    foreach ($type in $group.Group) {
        $file = $typePaths[$type.FullName].Substring($namespace.Length + 1)
        $index.Add('- [' + ($type.Name -replace '`\d+$','') + '](' + $file + ')')
    }
    Save-Page "$namespace/index.md" $index

    foreach ($type in $group.Group) {
        $coverage.types++
        $script:currentPage = $typePaths[$type.FullName]
        $lines = New-Lines
        $typeName = $type.FullName.Substring($namespace.Length + 1).Replace('+','.') -replace '`\d+',''
        $lines.Add('# ' + $typeName); $lines.Add('')
        $lines.Add('[C# API](../index.md) / [' + $namespace + '](index.md)'); $lines.Add('')
        $lines.Add('- **Assembly:** `Fluence.Wpf`')
        $lines.Add('- **Namespace:** `' + $namespace + '`'); $lines.Add('')
        $kind = if ($type.IsEnum) { 'enum' } elseif ($type.IsInterface) { 'interface' } elseif ($type.IsValueType) { 'struct' } elseif ([System.MulticastDelegate].IsAssignableFrom($type.BaseType)) { 'delegate' } else { 'class' }
        $declaration = 'public ' + $(if ($type.IsAbstract -and $type.IsSealed) { 'static ' } elseif ($type.IsAbstract -and -not $type.IsInterface) { 'abstract ' } elseif ($type.IsSealed -and -not $type.IsValueType -and -not $type.IsEnum) { 'sealed ' } else { '' }) + $kind + ' ' + $typeName
        $bases = @()
        if (-not $type.IsEnum -and $null -ne $type.BaseType -and $type.BaseType -ne [object] -and $type.BaseType -ne [System.ValueType]) {
            $baseDisplay = if ($type.BaseType.Name -eq $type.Name) { $type.BaseType.FullName.Replace('+', '.') } else { Format-Type $type.BaseType }
            $bases += $baseDisplay
        }
        $bases += @($type.GetInterfaces() | Where-Object { $null -eq $type.BaseType -or -not ($type.BaseType.GetInterfaces() -contains $_) } | ForEach-Object { Format-Type $_ })
        if ($bases.Count -gt 0) { $declaration += ' : ' + ($bases -join ', ') }
        $lines.Add('```csharp'); $lines.Add($declaration); $lines.Add('```'); $lines.Add('')
        $typeId = Get-MemberId $type
        $typeComment = Get-Comment $typeId
        if ($null -ne $typeComment) { $coverage.documented++ } else { $coverage.missingComments.Add($typeId) }
        Add-Documentation $lines $typeComment $type
        if ($null -ne $type.BaseType -and $typePaths.ContainsKey($type.BaseType.FullName)) {
            $baseFile = $typePaths[$type.BaseType.FullName]
            $baseRelative = if ($baseFile.StartsWith($namespace + '/')) { $baseFile.Substring($namespace.Length + 1) } else { '../' + $baseFile }
            $lines.Add('**Base type:** [' + $type.BaseType.Name + '](' + $baseRelative + ')'); $lines.Add('')
        } elseif ($null -ne $type.BaseType -and $type.BaseType -ne [object] -and -not $type.IsEnum) {
            $baseLabel = if ($type.BaseType.Name -eq $type.Name) { $type.BaseType.FullName.Replace('+', '.') } else { Format-Type $type.BaseType }
            $lines.Add('**Base type:** [`' + $baseLabel + '`](' + (Get-LearnUrl $type.BaseType) + ') (including inherited WPF and .NET members)'); $lines.Add('')
        }
        $guide = Join-Path $repository ('docs/controls/' + ($type.Name -creplace '([a-z0-9])([A-Z])','$1-$2').ToLowerInvariant() + '.md')
        if (Test-Path $guide) { $lines.Add('[Control guide](../../controls/' + [System.IO.Path]::GetFileName($guide) + ')'); $lines.Add('') }
        $sourceName = ($type.Name -replace '`\d+$', '') + '.cs'
        $source = @(Get-ChildItem (Join-Path $repository 'Fluence.Wpf') -Recurse -File -Filter $sourceName -ErrorAction SilentlyContinue | Select-Object -First 1)
        if ($source.Count -gt 0) {
            $sourceRelative = [System.IO.Path]::GetRelativePath($repository, $source[0].FullName).Replace('\','/')
            $lines.Add('[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/' + $sourceRelative + ')'); $lines.Add('')
        }

        $related = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        Add-RelatedType $related $type.BaseType
        foreach ($interface in $type.GetInterfaces()) { Add-RelatedType $related $interface }

        foreach ($section in @('Constructors','Properties','Methods','Events','Fields')) {
            $members = @(Get-PublicMembers $type $section)
            if ($type.IsEnum -and $section -ne 'Fields') { continue }
            if ($section -eq 'Fields' -and -not $type.IsEnum) {
                $dpFields = @($members | Where-Object { $_.Name -like '*Property' -and $_.FieldType.FullName -eq 'System.Windows.DependencyProperty' })
                $members = @($members | Where-Object { $_ -notin $dpFields })
                if ($dpFields.Count -gt 0) {
                    $lines.Add('## Dependency property identifiers'); $lines.Add('')
                    $lines.Add('Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.'); $lines.Add('')
                    foreach ($member in $dpFields) {
                        $id = Get-MemberId $member
                        $lines.Add('<a id="' + (Get-Anchor $id) + '"></a>'); $lines.Add('')
                        $lines.Add('### ' + $member.Name); $lines.Add('')
                        $lines.Add('```csharp'); $lines.Add((Format-Signature $member)); $lines.Add('```'); $lines.Add('')
                        $comment = Get-Comment $id
                        if ($null -ne $comment) { $coverage.documented++ } else { $coverage.missingComments.Add($id) }
                        $coverage.fields++
                        Add-Documentation $lines $comment $member
                    }
                }
            }
            if ($members.Count -eq 0) { continue }
            $heading = if ($type.IsEnum -and $section -eq 'Fields') { 'Values' } else { $section }
            $lines.Add('## ' + $heading); $lines.Add('')
            foreach ($member in $members) {
                $id = Get-MemberId $member
                $name = if ($member -is [System.Reflection.ConstructorInfo]) { $typeName } else { $member.Name }
                $anchor = Get-Anchor $id
                $lines.Add('<a id="' + $anchor + '"></a>'); $lines.Add('')
                $lines.Add('### ' + $name); $lines.Add('')
                $lines.Add('```csharp'); $lines.Add((Format-Signature $member)); $lines.Add('```'); $lines.Add('')
                if ($member -is [System.Reflection.MethodInfo]) { Add-RelatedType $related $member.ReturnType }
                if ($member -is [System.Reflection.MethodBase]) { foreach ($parameter in $member.GetParameters()) { Add-RelatedType $related $parameter.ParameterType } }
                if ($member -is [System.Reflection.PropertyInfo]) { Add-RelatedType $related $member.PropertyType }
                if ($member -is [System.Reflection.EventInfo]) { Add-RelatedType $related $member.EventHandlerType }
                if ($member -is [System.Reflection.FieldInfo]) { Add-RelatedType $related $member.FieldType }
                $comment = Get-Comment $id
                if ($null -ne $comment) { $coverage.documented++ }
                elseif ($member -is [System.Reflection.ConstructorInfo] -and (Test-ImplicitConstructor $member)) {
                    $coverage.implicitConstructors.Add($id)
                    $lines.Add('Creates a new `' + $typeName + '` instance.'); $lines.Add('')
                }
                else { $coverage.missingComments.Add($id) }
                $key = $section.ToLowerInvariant()
                $coverage[$key]++
                Add-Documentation $lines $comment $member
            }
        }
        [void]$related.Remove($type.FullName)
        if ($related.Count -gt 0) {
            $lines.Add('## Related types'); $lines.Add('')
            foreach ($relatedName in @($related | Sort-Object)) {
                $path = Get-RelativeDocLink $typePaths[$relatedName]
                $lines.Add('- [' + $relatedName + '](' + $path + ')')
            }
            $lines.Add('')
        }
        Save-Page $typePaths[$type.FullName] $lines
    }
}

$manifest = New-Lines
$manifest.Add('# C# API generation coverage'); $manifest.Add('')
$manifest.Add('Generated from the .NET 10 Fluence.Wpf assembly and its compiler XML documentation. This file is updated by the same command as the reference pages.'); $manifest.Add('')
$manifest.Add('| Item | Count |'); $manifest.Add('| --- | ---: |')
foreach ($key in @('types','constructors','properties','methods','events','fields','documented')) { $manifest.Add("| $key | $($coverage[$key]) |") }
$manifest.Add('| Implicit constructors | ' + $coverage.implicitConstructors.Count + ' |')
$manifest.Add('| Members without XML comments | ' + $coverage.missingComments.Count + ' |'); $manifest.Add('')
if ($coverage.implicitConstructors.Count -gt 0) {
    $manifest.Add('## Implicit constructors'); $manifest.Add('')
    $manifest.Add('These parameterless constructors are emitted by the C# compiler and have no source declaration or XML comment. Their reference pages use a generic description.'); $manifest.Add('')
    foreach ($id in $coverage.implicitConstructors) { $manifest.Add('- `' + $id + '`') }
    $manifest.Add('')
}
if ($coverage.missingComments.Count -gt 0) {
    $manifest.Add('## Members without XML comments'); $manifest.Add('')
    foreach ($id in $coverage.missingComments) { $manifest.Add('- `' + $id + '`') }
}
$coverageContent = (($manifest -join "`n").TrimEnd() + "`n")

# Validate the complete render before touching either destination.
if ($coverage.types -eq 0 -or $pages.Count -ne ($coverage.types + $namespaces.Count + 1) -or
    -not $pages.ContainsKey('index.md')) {
    throw 'API documentation generation produced an incomplete page set.'
}
foreach ($relative in $pages.Keys) {
    if ($relative -notmatch '^(?:[A-Za-z0-9_.-]+/)*[A-Za-z0-9_.-]+\.md$' -or
        $relative -split '/' -contains '..' -or
        [string]::IsNullOrWhiteSpace($pages[$relative])) {
        throw "Invalid generated API page: $relative"
    }
}

$encoding = [System.Text.UTF8Encoding]::new($true)
$pageBytes = @{}
foreach ($relative in $pages.Keys) {
    $pageBytes[$relative] = [byte[]]($encoding.GetPreamble() + $encoding.GetBytes($pages[$relative]))
}

function Assert-NoReparsePath([string]$path) {
    $current = $path
    while ($current) {
        $item = Get-Item -LiteralPath $current -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing a generated documentation path through a reparse point: $current"
        }
        $parent = [System.IO.Path]::GetDirectoryName($current)
        if (-not $parent -or $parent -eq $current) { break }
        $current = $parent
    }
}

Assert-NoReparsePath $outputRoot
Assert-NoReparsePath $coveragePath
if ([System.IO.File]::Exists($outputRoot)) { throw "API output root is a file: $outputRoot" }
if ([System.IO.Directory]::Exists($outputRoot)) {
    $pending = [System.Collections.Generic.Stack[string]]::new()
    $pending.Push($outputRoot)
    while ($pending.Count -gt 0) {
        foreach ($entry in [System.IO.Directory]::EnumerateFileSystemEntries($pending.Pop())) {
            $attributes = [System.IO.File]::GetAttributes($entry)
            if ($attributes -band [System.IO.FileAttributes]::ReparsePoint) {
                throw "Refusing API output containing a reparse point: $entry"
            }
            if ($attributes -band [System.IO.FileAttributes]::Directory) { $pending.Push($entry) }
        }
    }
}

# A marker establishes a dedicated output root. An existing unmarked root may
# be adopted only when every file is an exact page from this complete render.
$ownershipPath = Join-Path $outputRoot '.fluence-api-docs-output'
$ownershipPrefix = "Fluence.Wpf API generator output root v1`n"
$ownershipBytes = [System.Text.UTF8Encoding]::new($false).GetBytes("$ownershipPrefix$outputRoot`n")
if ([System.IO.Directory]::Exists($ownershipPath)) { throw "Invalid API output ownership marker: $ownershipPath" }
if ($Mode -eq 'Write' -and [System.IO.Directory]::Exists($outputRoot)) {
    if ([System.IO.File]::Exists($ownershipPath)) {
        $markerText = [System.Text.UTF8Encoding]::new($false, $true).GetString([System.IO.File]::ReadAllBytes($ownershipPath))
        if (-not $markerText.StartsWith($ownershipPrefix, [System.StringComparison]::Ordinal) -or
            -not $markerText.EndsWith("`n", [System.StringComparison]::Ordinal)) {
            throw "Invalid API output ownership marker: $ownershipPath"
        }
        $storedRoot = $markerText.Substring($ownershipPrefix.Length, $markerText.Length - $ownershipPrefix.Length - 1)
        $pathComparison = if ($IsWindows) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
        if (-not $storedRoot -or $storedRoot.Contains("`n") -or $storedRoot.Contains("`r") -or
            -not [string]::Equals($storedRoot, $outputRoot, $pathComparison)) {
            throw "Invalid API output ownership marker: $ownershipPath"
        }
    } else {
        $existingFiles = @(Get-ChildItem -LiteralPath $outputRoot -Recurse -File -Force)
        if ($existingFiles.Count -gt 0) {
            $verified = $existingFiles.Count -eq $pageBytes.Count
            foreach ($file in $existingFiles) {
                $relative = [System.IO.Path]::GetRelativePath($outputRoot, $file.FullName).Replace('\','/')
                if (-not $pageBytes.ContainsKey($relative) -or
                    -not [System.Linq.Enumerable]::SequenceEqual([byte[]][System.IO.File]::ReadAllBytes($file.FullName), [byte[]]$pageBytes[$relative])) {
                    $verified = $false
                    break
                }
            }
            if (-not $verified) {
                throw "Refusing to write into a directory that is not a verified API generator output: $outputRoot"
            }
        }
    }
}

$changed = [System.Collections.Generic.List[string]]::new()
foreach ($relative in @($pages.Keys | Sort-Object)) {
    $destination = Join-Path $outputRoot $relative
    $bytes = $pageBytes[$relative]
    $same = [System.IO.File]::Exists($destination) -and [System.Linq.Enumerable]::SequenceEqual([byte[]][System.IO.File]::ReadAllBytes($destination), [byte[]]$bytes)
    if (-not $same) {
        $changed.Add($relative)
        if ($Mode -eq 'Write') {
            $directory = Split-Path $destination -Parent
            [System.IO.Directory]::CreateDirectory($directory) | Out-Null
            [System.IO.File]::WriteAllBytes($destination, $bytes)
        }
    }
}
$coverageBytes = $encoding.GetPreamble() + $encoding.GetBytes($coverageContent)
$coverageSame = [System.IO.File]::Exists($coveragePath) -and [System.Linq.Enumerable]::SequenceEqual([byte[]][System.IO.File]::ReadAllBytes($coveragePath), [byte[]]$coverageBytes)
if (-not $coverageSame) {
    $changed.Add($coveragePath)
    if ($Mode -eq 'Write') {
        [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($coveragePath)) | Out-Null
        [System.IO.File]::WriteAllBytes($coveragePath, $coverageBytes)
    }
}
$existing = if ([System.IO.Directory]::Exists($outputRoot)) {
    @(Get-ChildItem -LiteralPath $outputRoot -Recurse -File -Filter '*.md' | ForEach-Object { [System.IO.Path]::GetRelativePath($outputRoot, $_.FullName).Replace('\','/') })
} else { @() }
foreach ($relative in $existing) {
    if (-not $pages.ContainsKey($relative)) {
        $changed.Add($relative)
        if ($Mode -eq 'Write') { Remove-Item -LiteralPath (Join-Path $outputRoot $relative) }
    }
}
if ($Mode -eq 'Write' -and -not [System.IO.File]::Exists($ownershipPath)) {
    [System.IO.File]::WriteAllBytes($ownershipPath, $ownershipBytes)
}
Write-Output "API pages: $($pages.Count); types: $($coverage.types); members: $($coverage.constructors + $coverage.properties + $coverage.methods + $coverage.events + $coverage.fields); implicit constructors: $($coverage.implicitConstructors.Count); without XML comments: $($coverage.missingComments.Count); changed: $($changed.Count)"
if ($Mode -eq 'Check' -and $changed.Count -gt 0) { throw 'API documentation drift: ' + ($changed -join ', ') }
