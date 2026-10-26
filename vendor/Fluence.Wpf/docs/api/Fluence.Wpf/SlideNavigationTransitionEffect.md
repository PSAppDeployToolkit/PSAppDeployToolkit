# SlideNavigationTransitionEffect

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum SlideNavigationTransitionEffect
```

The direction the incoming content slides in from during a [SlideNavigationPresenter](../Fluence.Wpf.Controls/SlideNavigationPresenter.md) content change, mirroring the WinUI 3 `SlideNavigationTransitionEffect`.

**Remarks:** WinUI's enumeration also carries `FromBottom`, which its own implementation animates with a different curve family (an exponential ease over the vertical axis rather than the two horizontal key splines). Only the two horizontal effects are ported, so the enumeration does not advertise a value the presenter cannot play. The gap is recorded in docs/winui-parity.md.



The numbers are WinUI's own: `FromBottom` keeps 0 even though it is not declared here, so the two ported values sit on the numbers WinUI gives them. Code or XAML ported from WinUI that touches the numeric value maps across unchanged, and `FromBottom` can be added later without moving anything.



The cost of that numbering is that `default` is not a declared member: it is the 0 held for `FromBottom`. The dependency property defaults to [FromRight](SlideNavigationTransitionEffect.md#api-7038c906e4aa) explicitly, and [TransitionEffect](../Fluence.Wpf.Controls/SlideNavigationPresenter.md#api-9bde54d0b79f) rejects 0, or any other undeclared value, with an `ArgumentException` rather than playing a horizontal effect the caller did not ask for.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/SlideNavigationTransitionEffect.cs)

## Values

<a id="api-9281828995c5"></a>

### FromLeft

```csharp
FromLeft = 1
```

The incoming content enters from the left and the outgoing content leaves to the right, the effect WinUI uses when moving backward through a set of peers.

<a id="api-7038c906e4aa"></a>

### FromRight

```csharp
FromRight = 2
```

The incoming content enters from the right and the outgoing content leaves to the left, the effect WinUI uses when moving forward through a set of peers.
