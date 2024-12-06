// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Input;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Displays the rating scale with interactions.
/// </summary>
[TemplatePart(Name = "PART_Star1", Type = typeof(SymbolIcon))]
[TemplatePart(Name = "PART_Star2", Type = typeof(SymbolIcon))]
[TemplatePart(Name = "PART_Star3", Type = typeof(SymbolIcon))]
[TemplatePart(Name = "PART_Star4", Type = typeof(SymbolIcon))]
[TemplatePart(Name = "PART_Star5", Type = typeof(SymbolIcon))]
public class RatingControl : System.Windows.Controls.ContentControl
{
    private enum StarValue
    {
        Empty,
        HalfFilled,
        Filled
    }

    private const double MaxValue = 5.0D;
    private const double MinValue = 0.0D;
    private const int OffsetTolerance = 8;
    private static readonly SymbolRegular StarSymbol = SymbolRegular.Star28;
    private static readonly SymbolRegular StarHalfSymbol = SymbolRegular.StarHalf28;
    private SymbolIcon? _symbolIconStarOne;
    private SymbolIcon? _symbolIconStarTwo;
    private SymbolIcon? _symbolIconStarThree;
    private SymbolIcon? _symbolIconStarFour;
    private SymbolIcon? _symbolIconStarFive;

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(RatingControl),
        new PropertyMetadata(0.0D, OnValueChanged)
    );

    /// <summary>Identifies the <see cref="MaxRating"/> dependency property.</summary>
    public static readonly DependencyProperty MaxRatingProperty = DependencyProperty.Register(
        nameof(MaxRating),
        typeof(int),
        typeof(RatingControl),
        new PropertyMetadata(5)
    );

    /// <summary>Identifies the <see cref="HalfStarEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty HalfStarEnabledProperty = DependencyProperty.Register(
        nameof(HalfStarEnabled),
        typeof(bool),
        typeof(RatingControl),
        new PropertyMetadata(true)
    );

    /// <summary>Identifies the <see cref="ValueChanged"/> routed event.</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(RatingControl)
    );

    /// <summary>
    /// Gets or sets the rating value.
    /// </summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum allowed rating value.
    /// </summary>
    public int MaxRating
    {
        get => (int)GetValue(MaxRatingProperty);
        set => SetValue(MaxRatingProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether half of the star can be selected.
    /// </summary>
    public bool HalfStarEnabled
    {
        get => (bool)GetValue(HalfStarEnabledProperty);
        set => SetValue(HalfStarEnabledProperty, value);
    }

    /// <summary>
    /// Occurs after the user selects the rating.
    /// </summary>
    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>
    /// Is called when <see cref="Value"/> changes.
    /// </summary>
    protected virtual void OnValueChanged(double oldValue)
    {
        if (Value > MaxValue)
        {
            SetCurrentValue(ValueProperty, MaxValue);

            return;
        }

        if (Value < MinValue)
        {
            SetCurrentValue(ValueProperty, MinValue);

            return;
        }

        if (!Value.Equals(oldValue))
        {
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

        UpdateStarsFromValue();
    }

    /// <summary>
    /// Is called when mouse is moved away from the control.
    /// </summary>
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        UpdateStarsFromValue();
    }

    /// <summary>
    /// Is called when mouse is moved around the control.
    /// </summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        Point currentPossition = e.GetPosition(this);
        var mouseOffset = currentPossition.X * 100 / ActualWidth;

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            UpdateStarsOnMousePreview(mouseOffset);
        }
    }

    /// <summary>
    /// Is called when mouse is cliked down.
    /// </summary>
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        Point currentPossition = e.GetPosition(this);
        var mouseOffset = currentPossition.X * 100 / ActualWidth;

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateStarsOnMouseClick(mouseOffset);
        }
    }

    /// <summary>
    /// Adjusts the control's <see cref="Value" /> in response to keyboard input, incrementing or decrementing based on the key pressed.
    /// </summary>
    /// <param name="e">Key event arguments containing details about the key press.</param>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if ((e.Key == Key.Right || e.Key == Key.Up) && Value < MaxValue)
        {
            Value += HalfStarEnabled ? 0.5D : 1;
        }

        if ((e.Key == Key.Left || e.Key == Key.Down) && Value > MinValue)
        {
            Value -= HalfStarEnabled ? 0.5D : 1;
        }
    }

    /// <summary>
    /// Is called when Template is changed.
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_Star1") is SymbolIcon starOne)
        {
            _symbolIconStarOne = starOne;
        }

        if (GetTemplateChild("PART_Star2") is SymbolIcon starTwo)
        {
            _symbolIconStarTwo = starTwo;
        }

        if (GetTemplateChild("PART_Star3") is SymbolIcon starThree)
        {
            _symbolIconStarThree = starThree;
        }

        if (GetTemplateChild("PART_Star4") is SymbolIcon starFour)
        {
            _symbolIconStarFour = starFour;
        }

        if (GetTemplateChild("PART_Star5") is SymbolIcon starFive)
        {
            _symbolIconStarFive = starFive;
        }

        UpdateStarsFromValue();
    }

    private void UpdateStarsOnMousePreview(double offsetPercentage)
    {
        SetStarsPresence(ExtractValueFromOffset(offsetPercentage));
    }

    private void UpdateStarsOnMouseClick(double offsetPercentage)
    {
        var currentValue = ExtractValueFromOffset(offsetPercentage);

        SetCurrentValue(ValueProperty, currentValue / 2.0);
    }

    private void UpdateStarsFromValue()
    {
        SetStarsPresence(ExtractValueFromOffset(Value * 100 / 5));
    }

    private void SetStarsPresence(int index)
    {
        switch (index)
        {
            case 10:
                UpdateStar(4, StarValue.Filled);
                UpdateStar(3, StarValue.Filled);
                UpdateStar(2, StarValue.Filled);
                UpdateStar(1, StarValue.Filled);
                UpdateStar(0, StarValue.Filled);
                break;

            case 9:
                UpdateStar(4, StarValue.HalfFilled);
                UpdateStar(3, StarValue.Filled);
                UpdateStar(2, StarValue.Filled);
                UpdateStar(1, StarValue.Filled);
                UpdateStar(0, StarValue.Filled);
                break;

            case 8:
                UpdateStar(4, StarValue.Empty);
                UpdateStar(3, StarValue.Filled);
                UpdateStar(2, StarValue.Filled);
                UpdateStar(1, StarValue.Filled);
                UpdateStar(0, StarValue.Filled);
                break;

            case 7:
                UpdateStar(4, StarValue.Empty);
                UpdateStar(3, StarValue.HalfFilled);
                UpdateStar(2, StarValue.Filled);
                UpdateStar(1, StarValue.Filled);
                UpdateStar(0, StarValue.Filled);
                break;

            case 6:
                UpdateStar(4, StarValue.Empty);
                UpdateStar(3, StarValue.Empty);
                UpdateStar(2, StarValue.Filled);
                UpdateStar(1, StarValue.Filled);
                UpdateStar(0, StarValue.Filled);
                break;

            case 5:
                UpdateStar(4, StarValue.Empty);
                UpdateStar(3, StarValue.Empty);
                UpdateStar(2, StarValue.HalfFilled);
                UpdateStar(1, StarValue.Filled);
                UpdateStar(0, StarValue.Filled);
                break;

            case 4:
                UpdateStar(4, StarValue.Empty);
                UpdateStar(3, StarValue.Empty);
                UpdateStar(2, StarValue.Empty);
                UpdateStar(1, StarValue.Filled);
                UpdateStar(0, StarValue.Filled);
                break;

            case 3:
                UpdateStar(4, StarValue.Empty);
                UpdateStar(3, StarValue.Empty);
                UpdateStar(2, StarValue.Empty);
                UpdateStar(1, StarValue.HalfFilled);
                UpdateStar(0, StarValue.Filled);
                break;

            case 2:
                UpdateStar(4, StarValue.Empty);
                UpdateStar(3, StarValue.Empty);
                UpdateStar(2, StarValue.Empty);
                UpdateStar(1, StarValue.Empty);
                UpdateStar(0, StarValue.Filled);
                break;

            case 1:
                UpdateStar(4, StarValue.Empty);
                UpdateStar(3, StarValue.Empty);
                UpdateStar(2, StarValue.Empty);
                UpdateStar(1, StarValue.Empty);
                UpdateStar(0, StarValue.HalfFilled);
                break;

            default:
                UpdateStar(4, StarValue.Empty);
                UpdateStar(3, StarValue.Empty);
                UpdateStar(2, StarValue.Empty);
                UpdateStar(1, StarValue.Empty);
                UpdateStar(0, StarValue.Empty);
                break;
        }
    }

    private void UpdateStar(int starIndex, StarValue starValue)
    {
        SymbolIcon? selectedIcon = starIndex switch
        {
            1 => _symbolIconStarTwo,
            2 => _symbolIconStarThree,
            3 => _symbolIconStarFour,
            4 => _symbolIconStarFive,
            _ => _symbolIconStarOne,
        };

        if (selectedIcon is null)
        {
            return;
        }

        switch (starValue)
        {
            case StarValue.HalfFilled:
                selectedIcon.Filled = false;
                selectedIcon.Symbol = StarHalfSymbol;
                break;

            case StarValue.Filled:
                selectedIcon.Filled = true;
                selectedIcon.Symbol = StarSymbol;
                break;

            default:
                selectedIcon.Filled = false;
                selectedIcon.Symbol = StarSymbol;
                break;
        }
    }

    private int ExtractValueFromOffset(double offset)
    {
        var starValue = (int)(offset + OffsetTolerance) / 10;

        if (!HalfStarEnabled)
        {
            if (starValue < 2)
            {
                return 0;
            }

            if (starValue % 2 != 0)
            {
                starValue += 1;
            }
        }

        return starValue;
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RatingControl ratingControl)
        {
            return;
        }

        ratingControl.OnValueChanged((double)e.OldValue);
    }
}
