# HyperlinkButton

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class HyperlinkButton : Button
```

A hyperlink-styled button that can optionally navigate to a URI.

**Remarks:** Inspired by WInUI's HyperlinkButton.

**Base type:** [`Button`](https://learn.microsoft.com/dotnet/api/system.windows.controls.button) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/HyperlinkButton.cs)

## Constructors

<a id="api-d19fbbe6bbf1"></a>

### HyperlinkButton

```csharp
public HyperlinkButton()
```

Initializes a new instance of the [HyperlinkButton](HyperlinkButton.md) class.

## Properties

<a id="api-928678700d53"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the button.

<a id="api-3471921e982d"></a>

### Icon

```csharp
public object Icon { get; set; }
```

Gets or sets the icon displayed in the button.

<a id="api-18106de5d58b"></a>

### IconPlacement

```csharp
public ElementPlacement IconPlacement { get; set; }
```

Gets or sets the placement of the icon relative to the content.

<a id="api-6f3f9d073700"></a>

### NavigateUri

```csharp
public Uri NavigateUri { get; set; }
```

Gets or sets the URI to navigate to when the button is clicked.

## Methods

<a id="api-b5df77344780"></a>

### OnClick

```csharp
protected override void OnClick()
```

Documentation inherited from the [`Button` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.button).

<a id="api-99d211727584"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Button` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.button).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-92a28867890c"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](HyperlinkButton.md#api-928678700d53) dependency property.

<a id="api-ce9fde2001a6"></a>

### IconPlacementProperty

```csharp
public static readonly DependencyProperty IconPlacementProperty
```

Identifies the [IconPlacement](HyperlinkButton.md#api-18106de5d58b) dependency property.

<a id="api-ac40cdce511d"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](HyperlinkButton.md#api-3471921e982d) dependency property.

<a id="api-49f2cb689af1"></a>

### NavigateUriProperty

```csharp
public static readonly DependencyProperty NavigateUriProperty
```

Identifies the [NavigateUri](HyperlinkButton.md#api-6f3f9d073700) dependency property.

## Related types

- [Fluence.Wpf.ElementPlacement](../Fluence.Wpf/ElementPlacement.md)
