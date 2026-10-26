# TextBlockExtensions

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public static class TextBlockExtensions
```

Provides attached properties for extending TextBlock with Fluent Design features.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TextBlockExtensions.cs)

## Methods

<a id="api-56dee452b1a2"></a>

### GetIcon

```csharp
public static object GetIcon(this DependencyObject obj)
```

Gets the icon for the specified element.

**Parameter `obj`:** The target dependency object.

**Returns:** The icon content.

<a id="api-45909f3535e0"></a>

### GetIconPlacement

```csharp
public static ElementPlacement GetIconPlacement(this DependencyObject obj)
```

Gets the icon placement for the specified element.

**Parameter `obj`:** The target dependency object.

**Returns:** The requested icon placement.

<a id="api-286eb5850674"></a>

### GetIsTextSelectionEnabled

```csharp
public static bool GetIsTextSelectionEnabled(this DependencyObject obj)
```

Gets the value of the [IsTextSelectionEnabledProperty](TextBlockExtensions.md#api-e8a019336192) attached property for the specified object.

**Parameter `obj`:** The target `TextBlock`.

**Returns:** `true` if selection is enabled; otherwise `false`.

<a id="api-c247f4bbcff5"></a>

### GetPlaceholderText

```csharp
public static string GetPlaceholderText(this DependencyObject obj)
```

Gets the placeholder text for the specified element.

**Parameter `obj`:** The target dependency object.

**Returns:** The placeholder text.

<a id="api-a112bb00af2c"></a>

### GetShowPlaceholder

```csharp
public static bool GetShowPlaceholder(this DependencyObject obj)
```

Gets whether the placeholder should be shown.

**Parameter `obj`:** The target dependency object.

**Returns:** `true` when the placeholder should be shown; otherwise `false`.

<a id="api-bf9b5abd582a"></a>

### GetTextTrimming

```csharp
public static TextTrimming GetTextTrimming(this DependencyObject obj)
```

Gets the value of the [TextTrimmingProperty](TextBlockExtensions.md#api-74d95ebd22a3) attached property for the specified object.

**Parameter `obj`:** The target `TextBlock`.

**Returns:** The requested text trimming mode.

<a id="api-c7c7a40e5acc"></a>

### GetTypography

```csharp
public static FluentTypography GetTypography(this DependencyObject obj)
```

Gets the typography style for the specified TextBlock.

**Parameter `obj`:** The target `TextBlock`.

**Returns:** The requested Fluent typography style.

<a id="api-51660e55a41f"></a>

### SetIcon

```csharp
public static void SetIcon(this DependencyObject obj, object value)
```

Sets the icon for the specified element.

**Parameter `obj`:** The target dependency object.

**Parameter `value`:** The icon content to store.

<a id="api-a983795f4d76"></a>

### SetIconPlacement

```csharp
public static void SetIconPlacement(this DependencyObject obj, ElementPlacement value)
```

Sets the icon placement for the specified element.

**Parameter `obj`:** The target dependency object.

**Parameter `value`:** The icon placement to apply.

<a id="api-ef21a34ebed4"></a>

### SetIsTextSelectionEnabled

```csharp
public static void SetIsTextSelectionEnabled(this DependencyObject obj, bool value)
```

Sets the value of the [IsTextSelectionEnabledProperty](TextBlockExtensions.md#api-e8a019336192) attached property for the specified object.

**Parameter `obj`:** The target `TextBlock`.

**Parameter `value`:** `true` to enable text selection; otherwise `false`.

<a id="api-53212fbae3b3"></a>

### SetPlaceholderText

```csharp
public static void SetPlaceholderText(this DependencyObject obj, string value)
```

Sets the placeholder text for the specified element.

**Parameter `obj`:** The target dependency object.

**Parameter `value`:** The placeholder text to store.

<a id="api-6cc8914d8c22"></a>

### SetShowPlaceholder

```csharp
public static void SetShowPlaceholder(this DependencyObject obj, bool value)
```

Sets whether the placeholder should be shown.

**Parameter `obj`:** The target dependency object.

**Parameter `value`:** `true` to show the placeholder; otherwise `false`.

<a id="api-7c773f77c104"></a>

### SetTextTrimming

```csharp
public static void SetTextTrimming(this DependencyObject obj, TextTrimming value)
```

Sets the value of the [TextTrimmingProperty](TextBlockExtensions.md#api-74d95ebd22a3) attached property for the specified object.

**Parameter `obj`:** The target `TextBlock`.

**Parameter `value`:** The text trimming mode to apply.

<a id="api-2b048b82a31d"></a>

### SetTypography

```csharp
public static void SetTypography(this DependencyObject obj, FluentTypography value)
```

Sets the typography style for the specified TextBlock.

**Parameter `obj`:** The target `TextBlock`.

**Parameter `value`:** The Fluent typography style to apply.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-92d5bf165689"></a>

### IconPlacementProperty

```csharp
public static readonly DependencyProperty IconPlacementProperty
```

Identifies the IconPlacement attached property.

<a id="api-f1431772e8e5"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the Icon attached property.

<a id="api-e8a019336192"></a>

### IsTextSelectionEnabledProperty

```csharp
public static readonly DependencyProperty IsTextSelectionEnabledProperty
```

Identifies the IsTextSelectionEnabled attached property.

<a id="api-6a140cd31887"></a>

### PlaceholderTextProperty

```csharp
public static readonly DependencyProperty PlaceholderTextProperty
```

Identifies the PlaceholderText attached property.

<a id="api-8cd578e24594"></a>

### ShowPlaceholderProperty

```csharp
public static readonly DependencyProperty ShowPlaceholderProperty
```

Identifies the ShowPlaceholder attached property.

<a id="api-74d95ebd22a3"></a>

### TextTrimmingProperty

```csharp
public static readonly DependencyProperty TextTrimmingProperty
```

Identifies the TextTrimming attached property.

<a id="api-c398d6307410"></a>

### TypographyProperty

```csharp
public static readonly DependencyProperty TypographyProperty
```

Identifies the Typography attached property.

## Related types

- [Fluence.Wpf.ElementPlacement](../Fluence.Wpf/ElementPlacement.md)
- [Fluence.Wpf.FluentTypography](../Fluence.Wpf/FluentTypography.md)
