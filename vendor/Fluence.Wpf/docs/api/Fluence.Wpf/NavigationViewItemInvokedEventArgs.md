# NavigationViewItemInvokedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public class NavigationViewItemInvokedEventArgs : EventArgs
```

Provides data for the [ItemInvoked](../Fluence.Wpf.Controls/NavigationView.md#api-90edd0f83051) event.

**Remarks:** Initializes a new instance of the [NavigationViewItemInvokedEventArgs](NavigationViewItemInvokedEventArgs.md) class.

**Parameter `invokedItem`:** The data item that was invoked.

**Parameter `invokedItemContainer`:** The navigation item container that was invoked.

**Parameter `isSettingsInvoked`:** A value indicating whether the settings entry was invoked.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/NavigationViewItemInvokedEventArgs.cs)

## Constructors

<a id="api-4bd8987c3c89"></a>

### NavigationViewItemInvokedEventArgs

```csharp
public NavigationViewItemInvokedEventArgs(object invokedItem, NavigationViewItem invokedItemContainer, bool isSettingsInvoked)
```

Provides data for the [ItemInvoked](../Fluence.Wpf.Controls/NavigationView.md#api-90edd0f83051) event.

**Remarks:** Initializes a new instance of the [NavigationViewItemInvokedEventArgs](NavigationViewItemInvokedEventArgs.md) class.

**Parameter `invokedItem`:** The data item that was invoked.

**Parameter `invokedItemContainer`:** The navigation item container that was invoked.

**Parameter `isSettingsInvoked`:** A value indicating whether the settings entry was invoked.

## Properties

<a id="api-ec82540d0a7b"></a>

### InvokedItem

```csharp
public object InvokedItem { get; }
```

Gets the data item that was invoked.

<a id="api-aa642404a563"></a>

### InvokedItemContainer

```csharp
public NavigationViewItem InvokedItemContainer { get; }
```

Gets the navigation item container that was invoked.

<a id="api-fd83ca3ec6f3"></a>

### IsSettingsInvoked

```csharp
public bool IsSettingsInvoked { get; }
```

Gets a value indicating whether the settings entry was invoked.

## Related types

- [Fluence.Wpf.Controls.NavigationViewItem](../Fluence.Wpf.Controls/NavigationViewItem.md)
