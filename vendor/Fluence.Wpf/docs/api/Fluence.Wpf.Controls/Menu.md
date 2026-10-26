# Menu

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class Menu : System.Windows.Controls.Menu
```

A Fluent Design horizontal menu bar. The menu bar itself is transparent; each top-level [MenuItem](MenuItem.md) entry receives the shared Fluent `MenuItem` style and opens a flyout-styled popup for its sub-items.

**Base type:** [`System.Windows.Controls.Menu`](https://learn.microsoft.com/dotnet/api/system.windows.controls.menu) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/Menu.cs)

## Constructors

<a id="api-dc00f4b6021f"></a>

### Menu

```csharp
public Menu()
```

Creates a new `Menu` instance.

## Methods

<a id="api-dd9bb1e46383"></a>

### GetContainerForItemOverride

```csharp
protected override DependencyObject GetContainerForItemOverride()
```

Documentation inherited from the [`Menu` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.menu).

<a id="api-9edf1c0b5969"></a>

### IsItemItsOwnContainerOverride

```csharp
protected override bool IsItemItsOwnContainerOverride(object item)
```

Documentation inherited from the [`Menu` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.menu).
