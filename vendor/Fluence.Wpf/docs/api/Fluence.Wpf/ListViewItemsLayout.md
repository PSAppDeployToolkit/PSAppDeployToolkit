# ListViewItemsLayout

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum ListViewItemsLayout
```

Specifies how a [ListView](../Fluence.Wpf.Controls/ListView.md) arranges its items: one per row down the list, or wrapped across it as a grid of tiles.

**Remarks:** This is a Fluence property with no WinUI counterpart, because WinUI ships the two arrangements as separate controls (`ListView` and `GridView`). It is deliberately not named after WinUI's `GridView`: in WPF, `View` already takes a `GridView`, and that one means a column view.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ListViewItemsLayout.cs)

## Values

<a id="api-d0013adee797"></a>

### Grid

```csharp
Grid = 1
```

Items wrap across the list as tiles, the way WinUI's `GridView` lays them out with its `ItemsWrapGrid`.

<a id="api-2aadfa301590"></a>

### List

```csharp
List = 0
```

One item per row, in WPF's own virtualizing vertical panel. The default.
