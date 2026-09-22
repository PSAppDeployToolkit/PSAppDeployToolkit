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
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Win32;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Represents the base class for flyout controls that display lightweight UI in a
    /// light-dismiss <see cref="Popup"/> anchored to a placement target, mirroring the
    /// WinUI 3 <c language="csharp">FlyoutBase</c> contract.
    /// </summary>
    /// <remarks>
    /// The popup is created lazily on the first <see cref="ShowAt"/> call. It is pinned open and
    /// <see cref="FlyoutBase"/> owns the light dismiss itself: a press outside the flyout, a press
    /// on the owning window's caption or borders, the window moving, and the application losing
    /// the foreground all close it, as does Escape pressed inside it. A press on the placement
    /// target closes the flyout and is swallowed, so the button that opened it toggles it shut
    /// rather than reopening it. The popup uses a custom placement callback so the flyout is
    /// centered on the facing edge of its placement target, matching WinUI. Derived classes
    /// supply the popup child via <see cref="CreatePresenter"/>.
    /// </remarks>
    public abstract class FlyoutBase : DependencyObject
    {
        /// <summary>
        /// Identifies the AttachedFlyout attached property, which associates a flyout with an
        /// arbitrary element so it can later be opened via <see cref="ShowAttachedFlyout"/>.
        /// </summary>
        public static readonly DependencyProperty AttachedFlyoutProperty =
            DependencyProperty.RegisterAttached(
                "AttachedFlyout",
                typeof(FlyoutBase),
                typeof(FlyoutBase),
                new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="Placement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(
                nameof(Placement),
                typeof(FlyoutPlacementMode),
                typeof(FlyoutBase),
                new PropertyMetadata(FlyoutPlacementMode.Top));

        /// <summary>
        /// Gets or sets where the flyout opens relative to its placement target.
        /// </summary>
        public FlyoutPlacementMode Placement
        {
            get => (FlyoutPlacementMode)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShouldConstrainToRootBounds"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShouldConstrainToRootBoundsProperty =
            DependencyProperty.Register(
                nameof(ShouldConstrainToRootBounds),
                typeof(bool),
                typeof(FlyoutBase),
                new PropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the flyout should stay within the bounds of
        /// the XAML root. Accepted for WinUI signature compatibility; WPF popups are positioned
        /// by the OS, so the value is not currently enforced.
        /// </summary>
        public bool ShouldConstrainToRootBounds
        {
            get => (bool)GetValue(ShouldConstrainToRootBoundsProperty);
            set => SetValue(ShouldConstrainToRootBoundsProperty, value);
        }

        /// <summary>
        /// Occurs before the flyout opens.
        /// </summary>
        public event EventHandler? Opening;

        /// <summary>
        /// Occurs after the flyout has opened.
        /// </summary>
        public event EventHandler? Opened;

        /// <summary>
        /// Occurs before the flyout closes through <see cref="Hide"/>. Set
        /// <see cref="FlyoutBaseClosingEventArgs.Cancel"/> to <see langword="true"/> to keep the
        /// flyout open. Light-dismiss closes bypass this event and raise only
        /// <see cref="Closed"/>.
        /// </summary>
        public event EventHandler<FlyoutBaseClosingEventArgs>? Closing;

        /// <summary>
        /// Occurs after the flyout has closed, whether through <see cref="Hide"/> or
        /// light dismiss.
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        /// Gets a value indicating whether the flyout is currently open.
        /// </summary>
        public bool IsOpen => (HostPopup?.IsOpen) is true;

        /// <summary>
        /// Gets the popup that hosts the presenter. Created lazily on the first
        /// <see cref="ShowAt"/> call. Internal so tests can verify popup configuration.
        /// </summary>
        internal Popup? HostPopup { get; private set; }

        /// <summary>
        /// Gets the cached presenter element returned by <see cref="CreatePresenter"/>.
        /// Internal so tests can verify the hosted content.
        /// </summary>
        internal FrameworkElement? Presenter { get; private set; }

        /// <summary>
        /// Gets the flyout attached to the specified element via
        /// <see cref="AttachedFlyoutProperty"/>.
        /// </summary>
        /// <param name="element">The element the flyout is attached to.</param>
        /// <returns>The attached flyout, or <see langword="null"/> when none is attached.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
        public static FlyoutBase? GetAttachedFlyout(FrameworkElement element)
        {
            return element is null
                ? throw new ArgumentNullException(nameof(element))
                : (FlyoutBase?)element.GetValue(AttachedFlyoutProperty);
        }

        /// <summary>
        /// Sets the flyout attached to the specified element via
        /// <see cref="AttachedFlyoutProperty"/>.
        /// </summary>
        /// <param name="element">The element to attach the flyout to.</param>
        /// <param name="value">The flyout to attach, or <see langword="null"/> to detach.</param>
        /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
        public static void SetAttachedFlyout(FrameworkElement element, FlyoutBase? value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(AttachedFlyoutProperty, value);
        }

        /// <summary>
        /// Shows the flyout attached to the specified element via
        /// <see cref="AttachedFlyoutProperty"/>, anchored to that element. Does nothing when no
        /// flyout is attached.
        /// </summary>
        /// <param name="flyoutOwner">The element whose attached flyout should be shown.</param>
        /// <exception cref="ArgumentNullException"><paramref name="flyoutOwner"/> is <see langword="null"/>.</exception>
        public static void ShowAttachedFlyout(FrameworkElement flyoutOwner)
        {
            ArgumentNullException.ThrowIfNull(flyoutOwner);
            GetAttachedFlyout(flyoutOwner)?.ShowAt(flyoutOwner);
        }

        /// <summary>
        /// Shows the flyout placed relative to the specified element. Raises
        /// <see cref="Opening"/> before the popup opens and <see cref="Opened"/> after, then
        /// moves focus to the presenter. The presenter inherits the placement target's
        /// <see cref="FrameworkElement.DataContext"/> for the lifetime of the popup so
        /// bindings inside the flyout content resolve against the anchor's view model.
        /// </summary>
        /// <remarks>
        /// The popup is open by the time this returns. When another element holds the mouse
        /// capture, which is the case inside a button's Click handler, the flyout takes the
        /// light-dismiss capture a second time once that gesture is over: a popup that took it
        /// mid-gesture loses it again the moment the button lets go, which closes the flyout as it
        /// appears.
        /// </remarks>
        /// <param name="placementTarget">The element to anchor the flyout to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="placementTarget"/> is <see langword="null"/>.</exception>
        public void ShowAt(FrameworkElement placementTarget)
        {
            ArgumentNullException.ThrowIfNull(placementTarget);
            Popup popup = EnsurePopup();
            popup.PlacementTarget = placementTarget;

            // The host popup has no visual parent, so the presenter inherits no DataContext.
            // Flow the anchor's DataContext in for the popup lifetime (cleared on close).
            Presenter?.SetCurrentValue(FrameworkElement.DataContextProperty, placementTarget.DataContext);

            // Stamp the resolved placement side onto the presenter before the popup opens so
            // its Loaded reveal slides in from the side the flyout actually opens on.
            if (Presenter is FlyoutPresenter presenter)
            {
                presenter.SetCurrentValue(FlyoutPresenter.RevealPlacementProperty, MapPlacementSide(Placement));
            }

            if (popup.IsOpen)
            {
                // Already open, so there is no second Opening: but the placement target was just
                // reassigned, and it can live in another window. Re-hook so dismissal watches the
                // window the flyout is now anchored in rather than the one it opened in.
                HookDismiss(placementTarget);
                return;
            }

            Opening?.Invoke(this, EventArgs.Empty);

            // Dismissal is watched for on the owning window rather than left to the popup's own
            // mouse capture. A Button raises Click from its mouse-up handler while it still holds
            // that capture and lets go only once the handler returns, so a light-dismiss popup
            // opened from a Click takes the capture mid-gesture and loses it again a moment later.
            // On .NET Framework that closes the flyout as it appears. Pinning the popup and
            // listening for a click outside it is the same behaviour without the race, and it
            // behaves identically on every target framework.
            popup.SetCurrentValue(Popup.StaysOpenProperty, value: true);
            popup.IsOpen = true;
            HookDismiss(placementTarget);

            Opened?.Invoke(this, EventArgs.Empty);
            if (Presenter is not null)
            {
                _ = Presenter.Focus();
            }
        }

        /// <summary>
        /// Hides the flyout. Raises <see cref="Closing"/> first; the close is abandoned when a
        /// handler sets <see cref="FlyoutBaseClosingEventArgs.Cancel"/> to
        /// <see langword="true"/>. <see cref="Closed"/> is raised once the popup has closed.
        /// </summary>
        public void Hide()
        {
            if ((HostPopup?.IsOpen) is not true)
            {
                return;
            }

            FlyoutBaseClosingEventArgs args = new();
            Closing?.Invoke(this, args);
            if (args.Cancel)
            {
                return;
            }

            HostPopup.IsOpen = false;
        }

        /// <summary>
        /// Creates the element that presents the flyout content as the popup child. Called once
        /// when the popup is created; implementations may cache and return the same instance.
        /// </summary>
        /// <returns>The presenter element hosted as the popup child.</returns>
        protected abstract FrameworkElement CreatePresenter();

        /// <summary>
        /// Maps the WinUI-style <see cref="FlyoutPlacementMode"/> to the WPF popup side the
        /// flyout opens on. <see cref="FlyoutPlacementMode.Full"/> and
        /// <see cref="FlyoutPlacementMode.Auto"/> map to the bottom side. Internal so tests
        /// can verify the mapping that feeds the custom placement callback.
        /// </summary>
        /// <param name="placement">The requested flyout placement.</param>
        /// <returns>The equivalent popup side.</returns>
        internal static PlacementMode MapPlacementSide(FlyoutPlacementMode placement)
        {
            return placement switch
            {
                FlyoutPlacementMode.Top => PlacementMode.Top,
                FlyoutPlacementMode.Left => PlacementMode.Left,
                FlyoutPlacementMode.Right => PlacementMode.Right,
                FlyoutPlacementMode.Bottom or FlyoutPlacementMode.Full or FlyoutPlacementMode.Auto or _ =>
                    PlacementMode.Bottom,
            };
        }

        /// <summary>
        /// The transparent margin every flyout presenter template reserves on all four sides so its
        /// ShadowCaster's DropShadowEffect has somewhere to render. WPF sizes a popup HWND to exactly
        /// its child's layout size, so without the gutter the effect is clipped to the plate and only
        /// the rounded corner notches survive. Placement subtracts it again so the plate lands where
        /// it did before. Keep in step with the Margin in the presenter templates.
        /// </summary>
        private const double ShadowGutter = 16.0;

        /// <summary>
        /// Computes the custom popup placements that center a popup on the facing edge of its
        /// placement target, matching WinUI flyout positioning: horizontal centering for
        /// <see cref="PlacementMode.Top"/> / <see cref="PlacementMode.Bottom"/> and vertical
        /// centering for <see cref="PlacementMode.Left"/> / <see cref="PlacementMode.Right"/>.
        /// The opposite edge is offered as the fallback so the popup flips at screen edges
        /// like the native placement modes. Shared with <see cref="TeachingTip"/>; internal
        /// so tests can verify the placement math directly.
        /// </summary>
        /// <param name="side">The target side to center on (Top, Bottom, Left, or Right).</param>
        /// <param name="popupSize">The size of the popup.</param>
        /// <param name="targetSize">The size of the placement target.</param>
        /// <param name="offset">The extra offset from the popup's HorizontalOffset and VerticalOffset.</param>
        /// <returns>The candidate placements, primary edge first.</returns>
        internal static CustomPopupPlacement[] GetEdgeCenteredPlacements(
            PlacementMode side,
            Size popupSize,
            Size targetSize,
            Point offset)
        {
            // popupSize includes the gutter on all four sides, so the plate is inset by ShadowGutter
            // inside it. Center on the plate rather than on the popup, and pull every candidate back
            // by the gutter on the axis it docks to.
            double plateWidth = popupSize.Width - (2 * ShadowGutter);
            double plateHeight = popupSize.Height - (2 * ShadowGutter);
            double centeredX = ((targetSize.Width - plateWidth) / 2.0) + offset.X - ShadowGutter;
            double centeredY = ((targetSize.Height - plateHeight) / 2.0) + offset.Y - ShadowGutter;
            CustomPopupPlacement above = new(new Point(centeredX, -popupSize.Height + offset.Y + ShadowGutter), PopupPrimaryAxis.Horizontal);
            CustomPopupPlacement below = new(new Point(centeredX, targetSize.Height + offset.Y - ShadowGutter), PopupPrimaryAxis.Horizontal);
            CustomPopupPlacement leftOf = new(new Point(-popupSize.Width + offset.X + ShadowGutter, centeredY), PopupPrimaryAxis.Vertical);
            CustomPopupPlacement rightOf = new(new Point(targetSize.Width + offset.X - ShadowGutter, centeredY), PopupPrimaryAxis.Vertical);
            return side is PlacementMode.Top
                ? [above, below]
                : side is PlacementMode.Left
                    ? [leftOf, rightOf]
                    : side is PlacementMode.Right ? [rightOf, leftOf] : [below, above];
        }

        /// <summary>
        /// Creates the light-dismiss popup on first use and hosts the presenter returned by
        /// <see cref="CreatePresenter"/> as its child. The popup uses
        /// <see cref="PlacementMode.Custom"/> with an edge-centering callback so the flyout
        /// is centered on the target edge selected by <see cref="Placement"/>.
        /// </summary>
        /// <returns>The popup that hosts the presenter.</returns>
        private Popup EnsurePopup()
        {
            if (HostPopup is null)
            {
                Presenter = CreatePresenter();
                Presenter.PreviewKeyDown += OnPresenterPreviewKeyDown;
                HostPopup = new Popup
                {
                    AllowsTransparency = true,
                    Child = Presenter,
                    CustomPopupPlacementCallback = GetPlacements,
                    Placement = PlacementMode.Custom,
                    // The FlyoutPresenter code-behind owns the open reveal (a placement-aware
                    // slide + fade on Loaded), so the popup must not add its own fade on top.
                    PopupAnimation = PopupAnimation.None,
                    StaysOpen = false,
                };
                HostPopup.Closed += OnPopupClosed;
            }

            return HostPopup;
        }

        /// <summary>
        /// The popup's custom placement callback: centers the popup on the target edge
        /// selected by the current <see cref="Placement"/> value.
        /// </summary>
        /// <param name="popupSize">The size of the popup.</param>
        /// <param name="targetSize">The size of the target element.</param>
        /// <param name="offset">The offset to apply to the placement.</param>
        private CustomPopupPlacement[] GetPlacements(Size popupSize, Size targetSize, Point offset)
        {
            return GetEdgeCenteredPlacements(MapPlacementSide(Placement), popupSize, targetSize, offset);
        }

        /// <summary>
        /// Dismisses the flyout when Escape is pressed inside the presenter, mirroring the
        /// WinUI light-dismiss keyboard contract. Runs through <see cref="Hide"/> so a
        /// <see cref="Closing"/> handler can still cancel the close.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The key event data.</param>
        private void OnPresenterPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled && e.Key is Key.Escape)
            {
                Hide();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Raises <see cref="Closed"/> once the popup has closed, releasing the placement
        /// target so the flyout does not pin the last anchor, and clearing the DataContext
        /// flowed onto the presenter by <see cref="ShowAt"/>. The clear uses SetCurrentValue
        /// (ClearValue cannot undo a current-value override on a default-source property).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnPopupClosed(object? sender, EventArgs e)
        {
            UnhookDismiss();
            _ = HostPopup?.PlacementTarget = null;
            Presenter?.SetCurrentValue(FrameworkElement.DataContextProperty, value: null);
            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Starts watching the window the flyout is anchored in for everything that light-dismisses
        /// a WPF popup: a press in the window's client area, a press on its non-client area (the
        /// caption and the resize borders, which on a <see cref="FluenceWindow"/> includes the whole
        /// title bar because it hit-tests as caption), the window moving, the application losing the
        /// foreground to another one, and activation moving to any window that is not the flyout's
        /// own popup. The last of those is what covers a second top-level window of the same
        /// application, which <c language="text">WM_ACTIVATEAPP</c> never reports because activation
        /// has not left the application. The <see cref="Window.Deactivated"/> event cannot serve for
        /// it: moving focus to the presenter activates the popup's own window and deactivates this
        /// one, and the event does not say which window took over, so the flyout would close as it
        /// opens. <c language="text">WM_ACTIVATE</c> does say, which is why the decode in
        /// <see cref="IsForeignActivationMessage"/> is the signal instead. See <see cref="ShowAt"/>
        /// for why dismissal is watched for here rather than left to the popup's own mouse capture.
        /// </summary>
        /// <param name="placementTarget">The element the flyout is anchored to.</param>
        private void HookDismiss(FrameworkElement placementTarget)
        {
            UnhookDismiss();
            _dismissWindow = Window.GetWindow(placementTarget);
            if (_dismissWindow is null)
            {
                return;
            }

            // handledEventsToo, because a control that handles its own press would otherwise keep
            // the flyout open behind it.
            _dismissWindow.AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(OnWindowPreviewMouseDown), handledEventsToo: true);
            _dismissWindow.LocationChanged += OnWindowLocationChanged;
            _dismissHwndSource = PresentationSource.FromVisual(_dismissWindow) as HwndSource;
            _dismissHwndSource?.AddHook(OnWindowMessage);
        }

        /// <summary>
        /// Stops watching the window. Safe to call when nothing is hooked.
        /// </summary>
        private void UnhookDismiss()
        {
            if (_dismissWindow is null)
            {
                return;
            }

            _dismissWindow.RemoveHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(OnWindowPreviewMouseDown));
            _dismissWindow.LocationChanged -= OnWindowLocationChanged;
            _dismissHwndSource?.RemoveHook(OnWindowMessage);
            _dismissHwndSource = null;
            _dismissWindow = null;
        }

        /// <summary>
        /// Closes the flyout on a press in the owning window. The flyout's own content lives in the
        /// popup's separate window, so a press inside it never reaches this handler. A press on the
        /// placement target is swallowed once it has closed the flyout: the target is usually the
        /// button whose Click opened the flyout, and that Click follows the press, so left to run it
        /// would call <see cref="ShowAt"/> and reopen what the press just closed, and the button
        /// could never toggle its own flyout shut. WinUI's light dismiss swallows that press the
        /// same way. Everywhere else the press is left alone, so whatever was pressed still gets
        /// its click.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            bool onPlacementTarget = HostPopup?.PlacementTarget is DependencyObject anchor
                && IsWithin(anchor, e.OriginalSource as DependencyObject);
            Hide();
            if (onPlacementTarget && !IsOpen)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Closes the flyout when the owning window moves. The popup is pinned, so it would otherwise
        /// stay where it was while its anchor travelled.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnWindowLocationChanged(object? sender, EventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// Closes the flyout on the window messages that never surface as routed input:
        /// see <see cref="IsDismissMessage"/>.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="msg">The message identifier.</param>
        /// <param name="wParam">The message parameter.</param>
        /// <param name="lParam">The message parameter.</param>
        /// <param name="handled">Indicates whether the message was handled. Never set here.</param>
        /// <returns><see cref="IntPtr.Zero"/>; the message is always left to the window.</returns>
        private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // PopupHandle walks the presenter's presentation source, so it is read only for the one
            // message that needs it rather than for every WM_MOUSEMOVE and WM_NCHITTEST the owning
            // window sees while the flyout is open.
            bool dismiss = IsDismissMessage(msg, wParam)
                || (msg == PInvoke.WM_ACTIVATE && IsForeignActivationMessage(msg, wParam, lParam, PopupHandle));
            if (dismiss)
            {
                Hide();
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// The window handle of the popup that hosts the presenter, or <see cref="IntPtr.Zero"/>
        /// when the flyout has no presented popup. Read while deciding whether an activation handoff
        /// is the flyout's own.
        /// </summary>
        private IntPtr PopupHandle =>
            Presenter is not null && PresentationSource.FromVisual(Presenter) is HwndSource source
                ? source.Handle
                : IntPtr.Zero;

        /// <summary>
        /// Whether a window message is one a light-dismiss popup closes on that never reaches the
        /// routed input events: a non-client button press (<c language="text">WM_NCLBUTTONDOWN</c>,
        /// <c language="text">WM_NCRBUTTONDOWN</c> or <c language="text">WM_NCMBUTTONDOWN</c>,
        /// which is the caption, the resize borders, and on a <see cref="FluenceWindow"/> the
        /// whole title bar), or <c language="text">WM_ACTIVATEAPP</c> with a false wParam, which is
        /// the application losing the foreground to another one: a click in another application or
        /// on the desktop, or Alt+Tab. Internal so tests can pin the set.
        /// </summary>
        /// <param name="msg">The message identifier.</param>
        /// <param name="wParam">The message parameter.</param>
        /// <returns><see langword="true"/> when the message should close an open flyout.</returns>
        internal static bool IsDismissMessage(int msg, IntPtr wParam)
        {
            return msg == PInvoke.WM_NCLBUTTONDOWN
                || msg == PInvoke.WM_NCRBUTTONDOWN
                || msg == PInvoke.WM_NCMBUTTONDOWN
                || (msg == PInvoke.WM_ACTIVATEAPP && wParam == IntPtr.Zero);
        }

        /// <summary>
        /// Whether a window message is the owning window losing activation to a window that is not
        /// the flyout's own popup. <c language="text">WM_ACTIVATEAPP</c> covers only activation
        /// leaving the application, so a second top-level window of the same application, reached by
        /// a click or by Alt+Tab, never raises it; the popup this replaced closed in both cases
        /// through capture loss. <c language="text">WM_ACTIVATE</c> is raised for that handoff too,
        /// and carries the window being activated in <paramref name="lParam"/>, which is how the
        /// flyout's own activation is told apart from any other: focusing the presenter activates the
        /// popup, and closing on that would close the flyout as it opens.
        /// Internal so tests can pin the decode.
        /// </summary>
        /// <param name="msg">The message identifier.</param>
        /// <param name="wParam">The message parameter; its low word carries the activation state.</param>
        /// <param name="lParam">The message parameter; the window being activated, which can be null.</param>
        /// <param name="popupHandle">The handle of the flyout's own popup, or <see cref="IntPtr.Zero"/> when it has none.</param>
        /// <returns><see langword="true"/> when the message should close an open flyout.</returns>
        internal static bool IsForeignActivationMessage(int msg, IntPtr wParam, IntPtr lParam, IntPtr popupHandle)
        {
            if (msg != PInvoke.WM_ACTIVATE)
            {
                return false;
            }

            // WA_INACTIVE is the low word of wParam, and the high word carries the minimised flag,
            // so the low word has to be masked out rather than the whole wParam compared. ToInt64
            // reads the wParam whole, the way the WM_NCLBUTTONUP decode had to be corrected to
            // earlier in this branch. A masked low word always fits an int, so no overflow guard is
            // needed around the cast.
            const int WA_INACTIVE = 0;
            int state = (int)(wParam.ToInt64() & 0xFFFF);
            return state == WA_INACTIVE && lParam != popupHandle;
        }

        /// <summary>
        /// Whether <paramref name="origin"/> is <paramref name="anchor"/> or sits inside it. Walks
        /// up from the origin, stepping through content elements such as a Run or a Hyperlink to
        /// their host first, because <see cref="VisualTreeHelper"/> rejects them.
        /// </summary>
        /// <param name="anchor">The element whose subtree is tested.</param>
        /// <param name="origin">The element the press originated on.</param>
        /// <returns><see langword="true"/> when the origin is the anchor or a descendant of it.</returns>
        private static bool IsWithin(DependencyObject anchor, DependencyObject? origin)
        {
            DependencyObject? current = origin;
            while (current is not null)
            {
                if (ReferenceEquals(current, anchor))
                {
                    return true;
                }

                current = current switch
                {
                    Visual or System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(current),
                    FrameworkContentElement fce => fce.Parent ?? LogicalTreeHelper.GetParent(fce),
                    ContentElement ce => ContentOperations.GetParent(ce) ?? LogicalTreeHelper.GetParent(ce),
                    _ => LogicalTreeHelper.GetParent(current),
                };
            }

            return false;
        }

        /// <summary>
        /// The window whose presses, moves and messages close this flyout while it is open.
        /// </summary>
        private Window? _dismissWindow;

        /// <summary>
        /// The message source of <see cref="_dismissWindow"/>, hooked while the flyout is open.
        /// </summary>
        private HwndSource? _dismissHwndSource;
    }
}
