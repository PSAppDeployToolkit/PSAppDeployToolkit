# ThemeResourceDictionaryCollection

[C# API](../index.md) / [Fluence.Wpf.Markup](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Markup`

```csharp
public sealed class ThemeResourceDictionaryCollection : Collection<ThemeResourceDictionary>
```

The collection behind [ThemeDictionaries](ThemeDictionary.md#api-a183efe2ca7b). Every mutation re-evaluates the owner's selected table, so tables added after construction (the XAML parse order) or at runtime take effect immediately.

**Base type:** [`Collection<ThemeResourceDictionary>`](https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.collection-1) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Markup/ThemeResourceDictionaryCollection.cs)

## Methods

<a id="api-445a1d108b8b"></a>

### ClearItems

```csharp
protected override void ClearItems()
```

Documentation inherited from the [`Collection<ThemeResourceDictionary>` API](https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.collection-1).

<a id="api-0b656b529687"></a>

### InsertItem

```csharp
protected override void InsertItem(int index, ThemeResourceDictionary item)
```

Documentation inherited from the [`Collection<ThemeResourceDictionary>` API](https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.collection-1).

<a id="api-8dee686613d6"></a>

### RemoveItem

```csharp
protected override void RemoveItem(int index)
```

Documentation inherited from the [`Collection<ThemeResourceDictionary>` API](https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.collection-1).

<a id="api-f12682ed9dfb"></a>

### SetItem

```csharp
protected override void SetItem(int index, ThemeResourceDictionary item)
```

Documentation inherited from the [`Collection<ThemeResourceDictionary>` API](https://learn.microsoft.com/dotnet/api/system.collections.objectmodel.collection-1).

## Related types

- [Fluence.Wpf.Markup.ThemeResourceDictionary](ThemeResourceDictionary.md)
