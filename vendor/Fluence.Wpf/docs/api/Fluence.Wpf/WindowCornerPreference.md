# WindowCornerPreference

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum WindowCornerPreference
```

Rounded corner preference for a top-level window (DWM), used with [CornerStyle](../Fluence.Wpf.Controls/FluenceWindow.md#api-766d38728cc4). The name follows the .NET 10 WPF Fluent theme, this library's authority for window chrome.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/WindowCornerPreference.cs)

## Values

<a id="api-35375e1bd1d2"></a>

### Default

```csharp
Default = 0
```

OS default rounding.

<a id="api-17a087d58bb0"></a>

### DoNotRound

```csharp
DoNotRound = 1
```

Sharp corners.

<a id="api-0db6037f4388"></a>

### Round

```csharp
Round = 2
```

Large radius.

<a id="api-b79fe7cc3f98"></a>

### RoundSmall

```csharp
RoundSmall = 3
```

Smaller radius.
