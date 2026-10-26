# SlideNavigationPresenter

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class SlideNavigationPresenter : ContentControl
```

A content host that plays the WinUI 3 slide navigation transition whenever its `Content` changes: the outgoing content fades out as it slides away, and the incoming content slides in behind it from the side named by [TransitionEffect](SlideNavigationPresenter.md#api-9bde54d0b79f).

**Remarks:** This is the WPF counterpart of navigating a WinUI `Frame` with a `SlideNavigationTransitionInfo`. The timings and offsets are the ones WinUI's own implementation uses for the horizontal effects (dxaml/phone/lib/ThemeTransitions.cpp, SlideNavigationTransitionInfo::CreateStoryboards): the outgoing content translates 150 px and fades to transparent over 150 ms on the 0.7,0,1,0.5 spline, and the incoming content waits out those 150 ms at a 200 px offset, then settles to rest over a further 300 ms on the 0.1,0.9,0.2,1 spline. Both offsets are mirrored for [FromLeft](../Fluence.Wpf/SlideNavigationTransitionEffect.md#api-9281828995c5).



The template carries two presenters so the two halves overlap the way they do in WinUI. The presenter that is not showing the current content is emptied once the transition settles, so only one copy of the content tree stays alive between transitions. The outgoing presenter is not hit-testable; the incoming one stays live for the whole slide, where WinUI takes hit testing off both (NavigateTransitionHelper::RemoveHitTestVisbility). A WPF clock that is replaced mid-flight never raises Completed, and restoring hit testing from that handler risked leaving the content permanently dead to the mouse. Both halves hold their end pose until that teardown runs, the way WinUI's own storyboards do; with FillBehavior.Stop the outgoing content would revert to full opacity at its rest position the moment its 150 ms clock ended and cover the arriving content for the remaining 300 ms. When motion is disabled the content is swapped with no animation at all.

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/SlideNavigationPresenter.cs)

## Constructors

<a id="api-3220aa8ca8a8"></a>

### SlideNavigationPresenter

```csharp
public SlideNavigationPresenter()
```

Creates a new `SlideNavigationPresenter` instance.

## Properties

<a id="api-9bde54d0b79f"></a>

### TransitionEffect

```csharp
public SlideNavigationTransitionEffect TransitionEffect { get; set; }
```

Gets or sets the side the incoming content enters from on the next content change. Set it before assigning `Content`, the way a WinUI caller passes a fresh transition info to each `Frame.Navigate` call: forward through a set of peers is [FromRight](../Fluence.Wpf/SlideNavigationTransitionEffect.md#api-7038c906e4aa), backward is [FromLeft](../Fluence.Wpf/SlideNavigationTransitionEffect.md#api-9281828995c5).

## Methods

<a id="api-6e50d558d07e"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-ba672f7efd39"></a>

### OnContentChanged

```csharp
protected override void OnContentChanged(object oldContent, object newContent)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-f38569dd4473"></a>

### TransitionEffectProperty

```csharp
public static readonly DependencyProperty TransitionEffectProperty
```

Identifies the [TransitionEffect](SlideNavigationPresenter.md#api-9bde54d0b79f) dependency property.

## Related types

- [Fluence.Wpf.SlideNavigationTransitionEffect](../Fluence.Wpf/SlideNavigationTransitionEffect.md)
