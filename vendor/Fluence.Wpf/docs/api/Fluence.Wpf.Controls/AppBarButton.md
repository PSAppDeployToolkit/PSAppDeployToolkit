# AppBarButton

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class AppBarButton : Button
```

Represents a templated command button for command bar surfaces such as [CommandBarFlyout](CommandBarFlyout.md), mirroring the WinUI 3 `AppBarButton` contract.

**Remarks:** The default style renders the compact primary-bar appearance: a 40x40 hit target with a centered [Icon](AppBarButton.md#api-ecff08706cb5) and the [Label](AppBarButton.md#api-544712238e50) surfaced as a tooltip. Inside the overflow menu of a [CommandBarFlyout](CommandBarFlyout.md) the presenter applies the `CommandBarFlyoutSecondaryAppBarButtonStyle` resource, which renders the icon and label side by side in a full-width menu-item row.

**Base type:** [`Button`](https://learn.microsoft.com/dotnet/api/system.windows.controls.button) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/AppBarButton.cs)

## Constructors

<a id="api-3267330a2d75"></a>

### AppBarButton

```csharp
public AppBarButton()
```

Creates a new `AppBarButton` instance.

## Properties

<a id="api-ecff08706cb5"></a>

### Icon

```csharp
public object? Icon { get; set; }
```

Gets or sets the icon shown on the button, typically a [FontIcon](FontIcon.md). Arbitrary content is supported and is hosted in a centered content presenter.

<a id="api-544712238e50"></a>

### Label

```csharp
public string Label { get; set; }
```

Gets or sets the text label that describes the command. The compact default style surfaces the label as a tooltip; the overflow style renders it next to the icon.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-8d2618a48e03"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](AppBarButton.md#api-ecff08706cb5) dependency property.

<a id="api-4a8a677d7079"></a>

### LabelProperty

```csharp
public static readonly DependencyProperty LabelProperty
```

Identifies the [Label](AppBarButton.md#api-544712238e50) dependency property.
