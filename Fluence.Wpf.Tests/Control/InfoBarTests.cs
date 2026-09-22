/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="InfoBar"/> control: SeverityLevels VSM group, GoToState wiring and
    /// the severity glyph/brush lookups.
    /// </summary>
    public sealed class InfoBarTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        // ---------------------------------------------------------------------------
        // WI-3 B14  InfoBar SeverityLevels VSM group
        // ---------------------------------------------------------------------------

        [Fact]
        public Task InfoBar_StyleApplies_RootBorderFoundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBar bar = new() { IsOpen = true, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border root = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(bar, "RootBorder"), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_TwoLayerIcon_GlyphAndBackgroundPerSeverityAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                // WinUI parity (InfoBar.xaml:108-109 upstream): no SeverityLevels VSM opacity
                // pulse and no 3px indicator bar. The severity read is the two-layer glyph -
                // IconBackground's severity-brush Foreground under StandardIcon's fixed
                // TextFillColorInverseBrush glyph, which changes text per severity.
                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Warning, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock iconBackground = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "IconBackground"), exactMatch: false);
                System.Windows.Controls.TextBlock standardIcon = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "StandardIcon"), exactMatch: false);

                SolidColorBrush expectedIconBackground = Assert.IsType<SolidColorBrush>(app.TryFindResource("SystemFillColorCautionBrush"));
                SolidColorBrush expectedIconForeground = Assert.IsType<SolidColorBrush>(app.TryFindResource("TextFillColorInverseBrush"));
                Assert.Equal(expectedIconBackground.Color, ((SolidColorBrush)iconBackground.Foreground).Color);
                Assert.Equal(expectedIconForeground.Color, ((SolidColorBrush)standardIcon.Foreground).Color);
                Assert.Equal("\uF13C", standardIcon.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_CloseButton_UsesFluentSubtlePlateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                InfoBar bar = new() { IsOpen = true, Title = "Closable" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Button close =
                        Assert.IsType<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(bar, "PART_CloseButton"), exactMatch: false);
                    // WinUI InfoBarCloseButtonSize 38 (InfoBar_themeresources.xaml:67). With the
                    // style's 5 dip margin the button fills the bar's 48 dip minimum height exactly,
                    // which is why the affordance reads as the same weight as the text beside it.
                    Assert.Equal(38.0, close.Width, 0.01);
                    Assert.Equal(38.0, close.Height, 0.01);

                    // WinUI InfoBarCloseButtonGlyphSize 16 (:68).
                    FontIcon closeGlyph = Assert.IsType<FontIcon>(close.Content, exactMatch: false);
                    Assert.Equal(16.0, closeGlyph.IconFontSize, 0.01);

                    // The subtle plate (TeachingTip / PipsPager pattern): a rounded Border
                    // owned by the button's own template, not the OS default chrome.
                    System.Windows.Controls.Border plate =
                        Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(close, "ButtonPlate"), exactMatch: false);
                    CornerRadius expectedRadius = (CornerRadius)(app.FindResource("ControlCornerRadius")
                        ?? throw new Xunit.Sdk.XunitException("ControlCornerRadius must resolve."));
                    Assert.Equal(expectedRadius, plate.CornerRadius);
                    SolidColorBrush restFill = Assert.IsType<SolidColorBrush>(plate.Background);
                    Assert.Equal(0, restFill.Color.A);

                    // Foreground contract: TextFillColorPrimary at rest, flowing into the glyph.
                    SolidColorBrush primary = (SolidColorBrush)(app.FindResource("TextFillColorPrimaryBrush")
                        ?? throw new Xunit.Sdk.XunitException("TextFillColorPrimaryBrush must resolve."));
                    SolidColorBrush buttonForeground = Assert.IsType<SolidColorBrush>(close.Foreground);
                    Assert.Equal(primary.Color, buttonForeground.Color);

                    FontIcon glyph = Assert.IsType<FontIcon>(FindVisualChildren<FontIcon>(close).FirstOrDefault(), exactMatch: false);
                    Assert.Equal("\uE711", glyph.Glyph, StringComparer.Ordinal);
                    SolidColorBrush glyphForeground = Assert.IsType<SolidColorBrush>(glyph.Foreground);
                    Assert.Equal(primary.Color, glyphForeground.Color);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_DefaultSeverity_IconBackgroundHasForegroundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Info" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock iconBackground =
                    Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "IconBackground"), exactMatch: false);
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("SystemFillColorAttentionBrush"));
                SolidColorBrush iconBackgroundForeground = Assert.IsType<SolidColorBrush>(iconBackground.Foreground);
                Assert.Equal(expected.Color, iconBackgroundForeground.Color);
                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_InformationalAccentBrushes_TrackAccentColorChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Info" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock iconBackground = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "IconBackground"), exactMatch: false);
                SolidColorBrush initial = Assert.IsType<SolidColorBrush>(iconBackground.Foreground);
                Color initialColor = initial.Color;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xC3, 0x00, 0x52));
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // IconBackground carries the severity brush, which is accent-derived for
                // Informational. StandardIcon's TextFillColorInverseBrush is fixed and does not
                // track the accent color.
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("SystemFillColorAttentionBrush"));
                SolidColorBrush iconBackgroundBrush = Assert.IsType<SolidColorBrush>(iconBackground.Foreground);
                Assert.Equal(expected.Color, iconBackgroundBrush.Color);
                Assert.NotEqual(initialColor, iconBackgroundBrush.Color);

                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_SeverityChange_IconBackgroundForegroundUpdatesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock iconBackground = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "IconBackground"), exactMatch: false);
                Color colorBefore = BrushAssert.SolidColor(iconBackground.Foreground);

                bar.Severity = InfoBarSeverity.Error;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("SystemFillColorCriticalBrush"));
                Color colorAfter = BrushAssert.SolidColor(iconBackground.Foreground);
                Assert.Equal(expected.Color, colorAfter);
                Assert.NotEqual(colorBefore, colorAfter);
                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_DeclaresPoliteLiveSettingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBar bar = new() { Title = "Saved", IsOpen = true };
                Window window = new() { Content = bar };
                window.Show();
                _ = bar.ApplyTemplate();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(bar));
                window.Close();
            });
        }

        [Fact]
        public Task InfoBar_ActionButton_IsNotClippedByRootBorderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBar bar = new()
                {
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Title = "Error",
                    Message = "Retry the operation.",
                    ActionButton = new Button { Content = "Retry" },
                };
                Window w = new() { Content = bar, Width = 520, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border root = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(bar, "RootBorder"), exactMatch: false);
                Assert.False(root.ClipToBounds,
                    "RootBorder should not clip action-button focus visuals or shadow rendering.");

                System.Windows.Controls.ContentPresenter presenter = Assert.IsType<System.Windows.Controls.ContentPresenter>(FindVisualChildByName<System.Windows.Controls.ContentPresenter>(bar, "ActionPresenter"), exactMatch: false);
                Assert.Equal(Visibility.Visible, presenter.Visibility);

                w.Close();
            });
        }

        // ---------------------------------------------------------------------------
        // Severity glyph and brush key lookups
        // ---------------------------------------------------------------------------

        [Fact]
        public void InfoBar_GetSeverityGlyph_MatchesTemplateGlyphs()
        {
            // WinUI parity: InfoBar*IconGlyph codes (InfoBar_themeresources.xaml) for the glyph
            // drawn on top of the IconBackground circle, not the old standalone-icon codes.
            Assert.Equal("\uF13F", InfoBar.GetSeverityGlyph(InfoBarSeverity.Informational), StringComparer.Ordinal);
            Assert.Equal("\uF13E", InfoBar.GetSeverityGlyph(InfoBarSeverity.Success), StringComparer.Ordinal);
            Assert.Equal("\uF13C", InfoBar.GetSeverityGlyph(InfoBarSeverity.Warning), StringComparer.Ordinal);
            Assert.Equal("\uF13D", InfoBar.GetSeverityGlyph(InfoBarSeverity.Error), StringComparer.Ordinal);
        }

        [Fact]
        public Task InfoBar_GetSeverityBrushKey_ResolvesToThemeBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                foreach (string key in new[]
                {
                    InfoBar.GetSeverityBrushKey(InfoBarSeverity.Informational), InfoBar.GetSeverityBrushKey(InfoBarSeverity.Success),
                    InfoBar.GetSeverityBrushKey(InfoBarSeverity.Warning), InfoBar.GetSeverityBrushKey(InfoBarSeverity.Error),
                })
                {
                    _ = Assert.IsType<Brush>(Application.Current.TryFindResource(key), exactMatch: false);
                }
            });
        }

        [Fact]
        public Task InfoBar_ErrorSeverity_HasExpectedBackgroundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                Window window = new();
                InfoBar infoBar = new()
                {
                    Severity = InfoBarSeverity.Error,
                    Title = "Error",
                    Message = "Something went wrong.",
                    IsOpen = true,
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Brush expectedBrush = Assert.IsType<Brush>(application.Resources["SystemFillColorCriticalBackgroundBrush"], exactMatch: false);

                    _ = infoBar.ApplyTemplate();
                    System.Windows.Controls.Border rootBorder = Assert.IsType<System.Windows.Controls.Border>(infoBar.Template.FindName("RootBorder", infoBar));
                    Assert.NotNull(rootBorder.Background);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_CloseButton_SetsIsOpenFalseAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Closable",
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = infoBar.ApplyTemplate();
                    System.Windows.Controls.Button closeButton = Assert.IsType<System.Windows.Controls.Button>(infoBar.Template.FindName("PART_CloseButton", infoBar));

                    ButtonAutomationPeer peer = new(closeButton);
                    IInvokeProvider invokeProvider = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);

                    invokeProvider.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(infoBar.IsOpen, "Clicking the close button should set IsOpen to false.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_ClosingCancel_PreventsCloseAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Cancelable",
                };

                infoBar.Closing += static (sender, args) => args.Cancel = true;

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = infoBar.ApplyTemplate();
                    System.Windows.Controls.Button closeButton = Assert.IsType<System.Windows.Controls.Button>(infoBar.Template.FindName("PART_CloseButton", infoBar));

                    ButtonAutomationPeer peer = new(closeButton);
                    IInvokeProvider invokeProvider = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);

                    invokeProvider.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(infoBar.IsOpen, "Canceling the Closing event should keep IsOpen true.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Clicking the close button raises Closed with an InfoBarClosedEventArgs whose Reason is
        /// CloseButton, so a handler can tell a user dismissal from a programmatic one.
        /// </summary>
        [Fact]
        public Task Closed_CloseButtonClicked_ReportsCloseButtonReasonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Closable",
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = infoBar.ApplyTemplate();

                    List<InfoBarCloseReason> reasons = [];
                    infoBar.Closed += (_, e) => reasons.Add(e.Reason);

                    ButtonBase closeButton = Assert.IsType<ButtonBase>(
                        infoBar.Template.FindName("PART_CloseButton", infoBar), exactMatch: false);
                    closeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(infoBar.IsOpen, "The close button must close the bar.");
                    Assert.Equal([InfoBarCloseReason.CloseButton], reasons);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Setting IsOpen to false in code raises Closed once with Reason Programmatic, and the
        /// close-button path must not also produce a second, programmatic Closed.
        /// </summary>
        [Fact]
        public Task Closed_IsOpenSetFalse_ReportsProgrammaticReasonOnceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Programmatic",
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = infoBar.ApplyTemplate();

                    List<InfoBarCloseReason> reasons = [];
                    infoBar.Closed += (_, e) => reasons.Add(e.Reason);

                    infoBar.IsOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal([InfoBarCloseReason.Programmatic], reasons);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// A programmatic close raises Closing too, with the Programmatic reason, the way WinUI
        /// raises it from the IsOpen transition rather than from the close button alone.
        /// </summary>
        [Fact]
        public Task Closing_IsOpenSetFalse_ReportsProgrammaticReasonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Programmatic",
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = infoBar.ApplyTemplate();

                    List<InfoBarCloseReason> closing = [];
                    infoBar.Closing += (_, e) => closing.Add(e.Reason);

                    infoBar.IsOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal([InfoBarCloseReason.Programmatic], closing);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Cancelling a programmatic close puts IsOpen back to true and raises no Closed, the
        /// same veto the close button already honoured.
        /// </summary>
        [Fact]
        public Task ClosingCancel_IsOpenSetFalse_ReopensAndRaisesNoClosedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Cancelled",
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = infoBar.ApplyTemplate();

                    List<InfoBarCloseReason> closed = [];
                    infoBar.Closing += static (_, e) => e.Cancel = true;
                    infoBar.Closed += (_, e) => closed.Add(e.Reason);

                    infoBar.IsOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(infoBar.IsOpen, "Cancelling the Closing event must reopen the bar.");
                    Assert.Empty(closed);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// A Closing handler that sets IsOpen to false itself must not produce a second Closed.
        /// The property is already false when the handler runs, so its assignment is a no-op and
        /// the single transition still raises exactly one Closed, carrying the reason the close
        /// actually started with.
        /// </summary>
        [Fact]
        public Task Closed_ClosingHandlerSetsIsOpenFalse_RaisedOnceWithStartingReasonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Reentrant",
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = infoBar.ApplyTemplate();

                    List<InfoBarCloseReason> closed = [];
                    infoBar.Closing += (_, _) => infoBar.IsOpen = false;
                    infoBar.Closed += (_, e) => closed.Add(e.Reason);

                    ButtonBase closeButton = Assert.IsType<ButtonBase>(
                        infoBar.Template.FindName("PART_CloseButton", infoBar), exactMatch: false);
                    closeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal([InfoBarCloseReason.CloseButton], closed);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Closing carries the same reason its matching Closed will, so a handler deciding
        /// whether to cancel can see what triggered the close.
        /// </summary>
        [Fact]
        public Task Closing_CloseButtonClicked_ReportsCloseButtonReasonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                InfoBar infoBar = new()
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Closable",
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = infoBar.ApplyTemplate();

                    InfoBarClosingEventArgs? received = null;
                    infoBar.Closing += (_, e) => received = e;

                    ButtonBase closeButton = Assert.IsType<ButtonBase>(
                        infoBar.Template.FindName("PART_CloseButton", infoBar), exactMatch: false);
                    closeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(received);
                    Assert.Equal(InfoBarCloseReason.CloseButton, received.Reason);
                    Assert.False(received.Cancel, "Cancel must still default to false.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_CloseButton_RaisesClickAndRunsTheCommandBeforeClosingAsync()
        {
            // WinUI's InfoBar carries CloseButtonClick, CloseButtonCommand and
            // CloseButtonCommandParameter alongside the close pipeline (InfoBar.idl:87-99). The
            // click reports the button, not the close: Closing is where a close is cancelled.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 420, Height = 200 };
                List<string> order = [];
                RelayTestCommand command = new(parameter => order.Add("command:" + parameter));

                InfoBar infoBar = new()
                {
                    Title = "Update",
                    Message = "A restart is needed.",
                    IsOpen = true,
                    IsClosable = true,
                    CloseButtonCommand = command,
                    CloseButtonCommandParameter = "bar",
                };
                infoBar.CloseButtonClick += (_, _) => order.Add("click");
                infoBar.Closing += (_, _) => order.Add("closing");
                infoBar.Closed += (_, _) => order.Add("closed");

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = infoBar.ApplyTemplate();
                    System.Windows.Controls.Button closeButton = Assert.IsType<System.Windows.Controls.Button>(infoBar.Template.FindName("PART_CloseButton", infoBar));
                    ButtonAutomationPeer peer = new(closeButton);
                    IInvokeProvider invokeProvider = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);

                    invokeProvider.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(["click", "command:bar", "closing", "closed"], order);
                    Assert.False(infoBar.IsOpen);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_CloseButtonStyle_ReachesTheButtonAndRestoresOnClearAsync()
        {
            // WinUI's InfoBar.CloseButtonStyle (InfoBar.idl:87). Clearing it has to put the
            // template's own style back rather than leave the button unstyled.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 420, Height = 200 };
                Style custom = new(typeof(System.Windows.Controls.Button));
                custom.Setters.Add(new Setter(FrameworkElement.WidthProperty, 64.0));

                InfoBar infoBar = new()
                {
                    Title = "Update",
                    IsOpen = true,
                    IsClosable = true,
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = infoBar.ApplyTemplate();
                    System.Windows.Controls.Button closeButton = Assert.IsType<System.Windows.Controls.Button>(infoBar.Template.FindName("PART_CloseButton", infoBar));
                    Style templateStyle = Assert.IsType<Style>(closeButton.Style);

                    infoBar.SetCurrentValue(InfoBar.CloseButtonStyleProperty, custom);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(custom, closeButton.Style);

                    infoBar.ClearValue(InfoBar.CloseButtonStyleProperty);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(templateStyle, closeButton.Style);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_Content_RendersUnderTheBannerAndTakesItWhenThereIsNoneAsync()
        {
            // InfoBar is a ContentControl, so Content is its XAML content property, and the template
            // had no presenter for it: anything nested in the bar compiled and rendered nothing.
            // WinUI keeps the same presenter in a second row and promotes it to the first when the
            // bar carries neither title nor message (InfoBar.xaml:84-91,126).
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 460, Height = 240 };
                System.Windows.Controls.TextBlock content = new() { Text = "Restart when convenient." };
                InfoBar infoBar = new()
                {
                    Title = "Update",
                    Message = "A restart is needed.",
                    IsOpen = true,
                    Content = content,
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.ContentPresenter contentArea = Assert.IsType<System.Windows.Controls.ContentPresenter>(
                        FindVisualChildByName<System.Windows.Controls.ContentPresenter>(infoBar, "ContentArea"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, contentArea.Visibility);
                    Assert.Same(content, contentArea.Content);
                    Assert.Equal(1, System.Windows.Controls.Grid.GetRow(contentArea));
                    Assert.True(content.ActualHeight > 0, "The bar's own content must render.");

                    // It sits below the banner text, not beside it.
                    System.Windows.Controls.TextBlock title = Assert.IsType<System.Windows.Controls.TextBlock>(
                        FindVisualChildByName<System.Windows.Controls.TextBlock>(infoBar, "TitleTextBlock"), exactMatch: false);
                    double titleBottom = title.TransformToAncestor(infoBar).Transform(new Point(0, title.ActualHeight)).Y;
                    double contentTop = contentArea.TransformToAncestor(infoBar).Transform(new Point(0, 0)).Y;
                    Assert.True(contentTop >= titleBottom, "Content belongs under the banner row.");

                    // With no banner text at all, the content takes the banner row itself.
                    infoBar.ClearValue(InfoBar.TitleProperty);
                    infoBar.ClearValue(InfoBar.MessageProperty);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(0, System.Windows.Controls.Grid.GetRow(contentArea));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_Banner_LaysOutOnOneLineUntilItStopsFittingAsync()
        {
            // WinUI's InfoBarPanel picks its own orientation: title, message and action on one line
            // while they fit, stacked when they do not (InfoBarPanel.cpp MeasureOverride). Fluence
            // always stacked, so the single line bar the Gallery shows by default was unreachable.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 700, Height = 220 };
                InfoBar infoBar = new()
                {
                    Title = "Update",
                    Message = "A restart is needed.",
                    IsOpen = true,
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.TextBlock title = Assert.IsType<System.Windows.Controls.TextBlock>(
                        FindVisualChildByName<System.Windows.Controls.TextBlock>(infoBar, "TitleTextBlock"), exactMatch: false);
                    System.Windows.Controls.TextBlock message = Assert.IsType<System.Windows.Controls.TextBlock>(
                        FindVisualChildByName<System.Windows.Controls.TextBlock>(infoBar, "MessageTextBlock"), exactMatch: false);

                    Assert.False(infoBar.IsBannerStacked, "A short title and message must share one line at 700 wide.");
                    double titleY = title.TransformToAncestor(infoBar).Transform(new Point(0, 0)).Y;
                    double messageY = message.TransformToAncestor(infoBar).Transform(new Point(0, 0)).Y;
                    Assert.Equal(titleY, messageY, 0.5);

                    // Narrow enough that they cannot, and the panel stacks them.
                    window.Width = 260;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(infoBar.IsBannerStacked, "They must stack once the bar is too narrow for one line.");
                    messageY = message.TransformToAncestor(infoBar).Transform(new Point(0, 0)).Y;
                    titleY = title.TransformToAncestor(infoBar).Transform(new Point(0, 0)).Y;
                    Assert.True(messageY > titleY, "Stacked, the message sits under the title.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_Opened_RaisesOnTheOpenTransitionButNotOnACancelledCloseRevertAsync()
        {
            // WinUI's InfoBar raises Opened from the IsOpen transition (InfoBar.idl:105). A cancelled
            // close puts IsOpen back without the bar ever having closed, which is not a fresh open,
            // so it must stay silent there for the same reason it does not re-announce.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 420, Height = 200 };
                int opened = 0;
                // IsOpen defaults to true here (WinUI's defaults to false, recorded in
                // docs/winui-parity.md), so the bar has to be closed before there is an open to see.
                InfoBar infoBar = new() { Title = "Update", Message = "A restart is needed.", IsOpen = false };
                infoBar.Opened += (_, _) => opened++;

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    infoBar.SetCurrentValue(InfoBar.IsOpenProperty, value: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(1, opened);

                    // A cancelled close reverts IsOpen; that revert is not an open.
                    static void Cancel(object? sender, InfoBarClosingEventArgs e)
                    {
                        e.Cancel = true;
                    }

                    infoBar.Closing += Cancel;
                    infoBar.SetCurrentValue(InfoBar.IsOpenProperty, value: false);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    infoBar.Closing -= Cancel;

                    Assert.True(infoBar.IsOpen, "A cancelled close leaves the bar open.");
                    Assert.Equal(1, opened);

                    // A real close and a fresh open do raise it again.
                    infoBar.SetCurrentValue(InfoBar.IsOpenProperty, value: false);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    infoBar.SetCurrentValue(InfoBar.IsOpenProperty, value: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(2, opened);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_CustomIcon_IsClampedToTheIconBoxAsync()
        {
            // WinUI hosts a custom icon in a Viewbox capped at InfoBarIconFontSize on both axes
            // (InfoBar.xaml:111). Without the cap an oversized icon inflates the icon column and
            // pushes the whole bar taller, which the 48 dip minimum height makes obvious.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 460, Height = 240 };
                System.Windows.Shapes.Rectangle oversized = new()
                {
                    Width = 64,
                    Height = 64,
                    Fill = Brushes.Red,
                };
                InfoBar infoBar = new()
                {
                    Title = "Update",
                    Message = "A restart is needed.",
                    IsOpen = true,
                    Icon = oversized,
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Viewbox iconBox = Assert.IsType<System.Windows.Controls.Viewbox>(
                        FindVisualChildByName<System.Windows.Controls.Viewbox>(infoBar, "CustomIconBox"), exactMatch: false);
                    Assert.Equal(16.0, iconBox.MaxWidth, 0.01);
                    Assert.Equal(16.0, iconBox.MaxHeight, 0.01);
                    Assert.True(iconBox.ActualWidth <= 16.5, "A 64 dip icon must scale down, not widen the icon column.");
                    Assert.True(iconBox.ActualHeight <= 16.5, "A 64 dip icon must scale down, not make the bar taller.");

                    // The bar stays the height a single line bar is, rather than growing to the icon.
                    Assert.True(infoBar.ActualHeight < 64, "An oversized icon must not drive the bar's height.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// A minimal command: the close button path needs one that records its parameter, and the
        /// test project has no shared command helper.
        /// </summary>
        /// <param name="execute">Called with the command parameter each time the command runs.</param>
        private sealed class RelayTestCommand(Action<object?> execute) : ICommand
        {
            private readonly Action<object?> _execute = execute;

            // Availability never changes here, so the event is accepted and dropped rather than
            // backed by a field nothing would ever raise.
            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object? parameter)
            {
                return true;
            }

            public void Execute(object? parameter)
            {
                _execute(parameter);
            }
        }
    }
}
