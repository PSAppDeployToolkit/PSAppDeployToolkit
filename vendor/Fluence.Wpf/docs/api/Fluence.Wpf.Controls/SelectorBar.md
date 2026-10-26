# SelectorBar

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class SelectorBar : ListBox
```

A horizontal row of peer destinations, mirroring the WinUI 3 `SelectorBar`: one [SelectorBarItem](SelectorBarItem.md) per destination, exactly one of them selected, with an accent pill under the selected label. Use it to switch between sibling views of the same page rather than to host the views themselves.

**Remarks:** The control derives from [ListBox](ListBox.md), so selection, keyboard navigation, and the selection automation pattern come from the framework. WinUI's SelectorBar is always single-select, so `SelectionMode` is coerced to `Single` and a consumer cannot widen it.



Items may be declared inline as [SelectorBarItem](SelectorBarItem.md) elements or come from `ItemsSource`. For a generated container the label is bound to `DisplayMemberPath` when one is set (honouring `ItemStringFormat`) and taken from the item's own string form otherwise, because the WinUI-shaped [Text](SelectorBarItem.md#api-8a00a2b51d96) is what the default template presents rather than the inherited content.

**Base type:** [ListBox](ListBox.md)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/SelectorBar.cs)

## Constructors

<a id="api-c8d873e4968a"></a>

### SelectorBar

```csharp
public SelectorBar()
```

Creates a new `SelectorBar` instance.

## Methods

<a id="api-b4d034146d07"></a>

### GetContainerForItemOverride

```csharp
protected override DependencyObject GetContainerForItemOverride()
```

Documentation inherited from the [ListBox API](../Fluence.Wpf.Controls/ListBox.md).

<a id="api-2e34c027a7a1"></a>

### IsItemItsOwnContainerOverride

```csharp
protected override bool IsItemItsOwnContainerOverride(object item)
```

Documentation inherited from the [ListBox API](../Fluence.Wpf.Controls/ListBox.md).

<a id="api-bed42ce8081e"></a>

### OnGotKeyboardFocus

```csharp
protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
```

Documentation inherited from the [ListBox API](../Fluence.Wpf.Controls/ListBox.md).

**Remarks:** WinUI's SelectorBar selects the current item, or the first one, when the bar takes focus with nothing selected (SelectorBar.cpp OnGotFocus), so a keyboard user never lands on a bar with no pill.

<a id="api-4c4ed334bf73"></a>

### OnSelectionChanged

```csharp
protected override void OnSelectionChanged(SelectionChangedEventArgs e)
```

Documentation inherited from the [ListBox API](../Fluence.Wpf.Controls/ListBox.md).

**Remarks:** The bar never settles with nothing selected. [SelectorBarItem](SelectorBarItem.md) already swallows the two deselect gestures WPF's [ListBox](ListBox.md) honours in `Single`, so this is the net under anything else that empties the selection: the item that just left is put back, because the pill is the page's current destination and WinUI's SelectorBar has no state without one.

<a id="api-162eca56bb31"></a>

### PrepareContainerForItemOverride

```csharp
protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
```

Documentation inherited from the [ListBox API](../Fluence.Wpf.Controls/ListBox.md).

## Related types

- [Fluence.Wpf.Controls.ListBox](ListBox.md)
