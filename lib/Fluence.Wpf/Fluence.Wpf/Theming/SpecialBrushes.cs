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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// Constructs every brush that is not a plain solid twin of a Color token, plus the
    /// theme-independent layout/shadow/focus tokens consumed by control templates. This is the
    /// single C# home for the special-brush definitions (elevation gradients, focus visuals, and
    /// high-contrast SystemColors aliases) that are not the plain solid twins <c>BrushFactory</c>
    /// emits for each Color key. All produced brushes are frozen.
    /// </summary>
    internal static class SpecialBrushes
    {
        /// <summary>
        /// Adds and/or overrides the special brushes and shared resources on
        /// <paramref name="dict"/>. <paramref name="colors"/> is the computed color map for the
        /// resolved <paramref name="theme"/>; brush colors are resolved against it so they track
        /// the active palette without any <see cref="DynamicResourceExtension"/>
        /// indirection.
        /// </summary>
        /// <param name="dict">The resource dictionary to populate.</param>
        /// <param name="colors">The computed color map for the resolved theme.</param>
        /// <param name="theme">The resolved application theme.</param>
        internal static void Add(ResourceDictionary dict, IReadOnlyDictionary<string, Color> colors, ApplicationTheme theme)
        {
            AddSharedTokens(dict);

            // Divergent accent brushes whose Color twin differs from the brush value.
            // Ported verbatim from Theme.Light/Dark.xaml accent-brush overrides (and the legacy
            // UpdateResources isDark branch). HighContrast keeps these computed values: the legacy
            // C# accent overlay took precedence over the HC XAML for accent-derived brush keys.
            bool dark = theme is ApplicationTheme.Dark;
            dict["SystemAccentColorPrimaryBrush"] = Solid(dark ? colors["SystemAccentColorDark3"] : colors["SystemAccentColorDark2"]);
            dict["SystemAccentColorSecondaryBrush"] = Solid(colors["SystemAccentColorDark3"]);
            dict["SystemAccentColorTertiaryBrush"] = Solid(dark ? colors["SystemAccentColorLight2"] : colors["SystemAccentColorDark1"]);

            // ApplicationBackgroundBrush is the irregular twin of ApplicationBackgroundColor
            // (the brush key drops the "Color" suffix), so BrushFactory does not emit it.
            dict["ApplicationBackgroundBrush"] = Solid(colors["ApplicationBackgroundColor"]);

            // Brush-only keys with no Color twin.
            dict["AccentFillColorSelectedTextBackgroundBrush"] = Solid(colors["SystemAccentColor"]);
            dict["NavigationViewSelectionIndicatorBrush"] = Solid(colors["SystemAccentColor"]);
            dict["ScrollBarTrackFillBrush"] = Solid(colors["AcrylicBackgroundFillColorDefault"]);

            // SystemColor aliases used by the WinUI Gallery color guidance page. These read live
            // SystemColors in every theme, not the computed palette.
            AddSystemColorAliases(dict);

            if (theme is ApplicationTheme.HighContrast)
            {
                AddHighContrastBrushes(dict);
                return;
            }

            AddElevationBorderBrushes(dict, colors);
        }

        /// <summary>
        /// Theme-independent layout, shadow, and focus tokens. These never change with theme or accent.
        /// </summary>
        /// <param name="dict">The resource dictionary to populate.</param>
        private static void AddSharedTokens(ResourceDictionary dict)
        {
            dict["ControlCornerRadius"] = new CornerRadius(4);
            dict["OverlayCornerRadius"] = new CornerRadius(8);
            dict["PopupCornerRadius"] = new CornerRadius(8);

            DropShadowEffect flyoutShadow = new()
            {
                BlurRadius = 18,
                Direction = 270,
                Opacity = 0.22,
                ShadowDepth = 4,
                Color = Colors.Black,
            };
            flyoutShadow.Freeze();
            dict["FlyoutShadowEffect"] = flyoutShadow;

            dict["DefaultControlFocusVisualStyle"] = BuildControlFocusVisualStyle();
            dict["DefaultCollectionFocusVisualStyle"] = BuildCollectionFocusVisualStyle();
        }

        /// <summary>
        /// Builds the two-border control focus visual (outer + inner stroke, 4 px radius),
        /// the WinUI 3 <c>DefaultControlFocusVisualStyle</c>. The border brushes are
        /// resolved via <see cref="DynamicResourceExtension"/> so they re-evaluate
        /// against whichever computed dictionary is active.
        /// </summary>
        private static Style BuildControlFocusVisualStyle()
        {
            FrameworkElementFactory outer = new(typeof(Border));
            outer.SetValue(FrameworkElement.MarginProperty, new Thickness(-3));
            outer.SetValue(Border.BorderBrushProperty, new DynamicResourceExtension("FocusStrokeColorOuterBrush"));
            outer.SetValue(Border.BorderThicknessProperty, new Thickness(2));
            outer.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

            FrameworkElementFactory inner = new(typeof(Border));
            inner.SetValue(FrameworkElement.MarginProperty, new Thickness(-2));
            inner.SetValue(Border.BorderBrushProperty, new DynamicResourceExtension("FocusStrokeColorInnerBrush"));
            inner.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            inner.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

            FrameworkElementFactory grid = new(typeof(Grid));
            grid.AppendChild(outer);
            grid.AppendChild(inner);

            ControlTemplate template = new() { VisualTree = grid };
            template.Seal();

            Style style = new();
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            style.Seal();
            return style;
        }

        /// <summary>
        /// Builds the inset single-stroke collection focus visual, the WinUI 3
        /// <c>DefaultCollectionFocusVisualStyle</c>.
        /// </summary>
        private static Style BuildCollectionFocusVisualStyle()
        {
            FrameworkElementFactory rect = new(typeof(Rectangle));
            rect.SetValue(Rectangle.RadiusXProperty, 4.0);
            rect.SetValue(Rectangle.RadiusYProperty, 4.0);
            rect.SetValue(Shape.StrokeProperty, new DynamicResourceExtension("FocusStrokeColorOuterBrush"));
            rect.SetValue(Shape.StrokeThicknessProperty, 2.0);

            ControlTemplate template = new() { VisualTree = rect };
            template.Seal();

            Style style = new();
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            style.Seal();
            return style;
        }

        /// <summary>
        /// Light/Dark elevation-border brushes are <see cref="LinearGradientBrush"/> values whose
        /// stop colors come from the computed control-stroke tokens. Definitions (start/end points,
        /// transform, stop offsets) match the WinUI 3 elevation borders.
        /// </summary>
        /// <param name="dict">The resource dictionary to populate.</param>
        /// <param name="colors">The computed color map for the resolved theme.</param>
        private static void AddElevationBorderBrushes(ResourceDictionary dict, IReadOnlyDictionary<string, Color> colors)
        {
            // ControlElevationBorderBrush / TextControlElevationBorderBrush: identical absolute
            // 0,0 -> 0,3 gradient (flipped vertically) from ControlStrokeColorSecondary -> Default.
            dict["ControlElevationBorderBrush"] = AbsoluteFlippedGradient(
                colors["ControlStrokeColorSecondary"], colors["ControlStrokeColorDefault"]);
            dict["TextControlElevationBorderBrush"] = AbsoluteFlippedGradient(
                colors["ControlStrokeColorSecondary"], colors["ControlStrokeColorDefault"]);

            // AccentControlElevationBorderBrush: same geometry, on-accent stroke stops.
            dict["AccentControlElevationBorderBrush"] = AbsoluteFlippedGradient(
                colors["ControlStrokeColorOnAccentSecondary"], colors["ControlStrokeColorOnAccentDefault"]);

            // CircleElevationBorderBrush: relative-to-bounding-box 0,0 -> 0,1, no transform,
            // stops at 0.50 (ControlStrokeColorDefault) and 0.70 (ControlStrokeColorSecondary).
            LinearGradientBrush circle = new()
            {
                MappingMode = BrushMappingMode.RelativeToBoundingBox,
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
            };
            circle.GradientStops.Add(new GradientStop(colors["ControlStrokeColorDefault"], 0.50));
            circle.GradientStops.Add(new GradientStop(colors["ControlStrokeColorSecondary"], 0.70));
            circle.Freeze();
            dict["CircleElevationBorderBrush"] = circle;
        }

        /// <summary>
        /// Builds the canonical Fluent elevation gradient: absolute mapping, 0,0 -> 0,3, flipped
        /// vertically about its centre, with stops at 0.33 (<paramref name="stop33"/>) and 1.0
        /// (<paramref name="stop100"/>).
        /// </summary>
        /// <param name="stop33">The color for the 0.33 stop.</param>
        /// <param name="stop100">The color for the 1.0 stop.</param>
        private static LinearGradientBrush AbsoluteFlippedGradient(Color stop33, Color stop100)
        {
            ScaleTransform flip = new() { CenterY = 0.5, ScaleY = -1 };
            flip.Freeze();
            LinearGradientBrush b = new()
            {
                MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 3),
                RelativeTransform = flip,
            };
            b.GradientStops.Add(new GradientStop(stop33, 0.33));
            b.GradientStops.Add(new GradientStop(stop100, 1.0));
            b.Freeze();
            return b;
        }

        /// <summary>
        /// High-contrast brush overrides snapshot live <see cref="SystemColors"/> members,
        /// reproducing the non-accent brush overrides in Section 2 of Theme.HighContrast.xaml.
        /// Accent-derived brushes are intentionally left as their computed twins (the legacy C#
        /// accent overlay took precedence over the HC XAML for those keys, so the golden records
        /// the computed palette, not the HC SystemColors). A re-Apply on <c>WM_SETTINGCHANGE</c>
        /// refreshes the snapshot when the HC variant changes.
        /// </summary>
        /// <param name="dict">The resource dictionary to populate.</param>
        private static void AddHighContrastBrushes(ResourceDictionary dict)
        {
            Color window = SystemColors.WindowColor;
            Color windowText = SystemColors.WindowTextColor;
            Color grayText = SystemColors.GrayTextColor;
            Color highlight = SystemColors.HighlightColor;
            Color highlightText = SystemColors.HighlightTextColor;
            Color control = SystemColors.ControlColor;
            Color controlText = SystemColors.ControlTextColor;
            Color controlDark = SystemColors.ControlDarkColor;
            Color controlLight = SystemColors.ControlLightColor;
            Color transparent = Colors.Transparent;

            // Application background / keyboard focus
            dict["ApplicationBackgroundBrush"] = Solid(window);
            dict["KeyboardFocusBorderColorBrush"] = Solid(highlight);

            // Text fill
            dict["TextFillColorPrimaryBrush"] = Solid(windowText);
            dict["TextFillColorSecondaryBrush"] = Solid(windowText);
            dict["TextFillColorTertiaryBrush"] = Solid(grayText);
            dict["TextFillColorDisabledBrush"] = Solid(grayText);
            dict["TextPlaceholderColorBrush"] = Solid(grayText);
            dict["TextFillColorInverseBrush"] = Solid(highlightText);

            // Control fill
            dict["ControlFillColorDefaultBrush"] = Solid(control);
            dict["ControlFillColorSecondaryBrush"] = Solid(control);
            dict["ControlFillColorTertiaryBrush"] = Solid(control);
            dict["ControlFillColorQuarternaryBrush"] = Solid(control);
            dict["ControlFillColorDisabledBrush"] = Solid(control);
            dict["ControlFillColorTransparentBrush"] = Solid(transparent);
            dict["ControlFillColorInputActiveBrush"] = Solid(window);

            // Control strong fill
            dict["ControlStrongFillColorDefaultBrush"] = Solid(controlText);
            dict["ControlStrongFillColorDisabledBrush"] = Solid(grayText);

            // Control solid fill
            dict["ControlSolidFillColorDefaultBrush"] = Solid(control);

            // Subtle fill
            dict["SubtleFillColorTransparentBrush"] = Solid(transparent);
            dict["SubtleFillColorSecondaryBrush"] = Solid(control);
            dict["SubtleFillColorTertiaryBrush"] = Solid(control);
            dict["SubtleFillColorDisabledBrush"] = Solid(transparent);

            // Control alt fill
            dict["ControlAltFillColorTransparentBrush"] = Solid(transparent);
            dict["ControlAltFillColorSecondaryBrush"] = Solid(control);
            dict["ControlAltFillColorTertiaryBrush"] = Solid(control);
            dict["ControlAltFillColorQuarternaryBrush"] = Solid(control);
            dict["ControlAltFillColorDisabledBrush"] = Solid(transparent);

            // Control on image fill
            dict["ControlOnImageFillColorDefaultBrush"] = Solid(control);
            dict["ControlOnImageFillColorSecondaryBrush"] = Solid(control);
            dict["ControlOnImageFillColorTertiaryBrush"] = Solid(control);
            dict["ControlOnImageFillColorDisabledBrush"] = Solid(control);

            // Accent fill disabled (Build skips AccentFillColorDisabled in HC; brush -> GrayText)
            dict["AccentFillColorDisabledBrush"] = Solid(grayText);

            // Control stroke
            dict["ControlStrokeColorDefaultBrush"] = Solid(controlDark);
            dict["ControlStrokeColorSecondaryBrush"] = Solid(controlDark);
            // NavigationView pane/content seam follows the same system control-dark stroke in HC.
            dict["NavigationViewContentSeparatorBrush"] = Solid(controlDark);
            dict["ControlStrokeColorTertiaryBrush"] = Solid(controlText);
            dict["ControlStrokeColorOnAccentDefaultBrush"] = Solid(highlightText);
            dict["ControlStrokeColorOnAccentSecondaryBrush"] = Solid(highlightText);
            dict["ControlStrokeColorOnAccentTertiaryBrush"] = Solid(highlightText);
            dict["ControlStrokeColorOnAccentDisabledBrush"] = Solid(grayText);
            dict["ControlStrokeColorForStrongFillWhenOnImageBrush"] = Solid(controlDark);

            // Card stroke
            dict["CardStrokeColorDefaultBrush"] = Solid(controlDark);
            dict["CardStrokeColorDefaultSolidBrush"] = Solid(controlDark);

            // Control strong stroke
            dict["ControlStrongStrokeColorDefaultBrush"] = Solid(controlText);
            dict["ControlStrongStrokeColorDisabledBrush"] = Solid(grayText);

            // Surface stroke
            dict["SurfaceStrokeColorDefaultBrush"] = Solid(controlDark);
            dict["SurfaceStrokeColorFlyoutBrush"] = Solid(controlDark);
            dict["SurfaceStrokeColorInverseBrush"] = Solid(controlLight);

            // Divider stroke
            dict["DividerStrokeColorDefaultBrush"] = Solid(controlDark);

            // Focus stroke
            dict["FocusStrokeColorOuterBrush"] = Solid(highlight);
            dict["FocusStrokeColorInnerBrush"] = Solid(highlightText);

            // Card background fill
            dict["CardBackgroundFillColorDefaultBrush"] = Solid(control);
            dict["CardBackgroundFillColorSecondaryBrush"] = Solid(control);
            dict["CardBackgroundFillColorTertiaryBrush"] = Solid(window);

            // Smoke fill
            dict["SmokeFillColorDefaultBrush"] = Solid(window);

            // Layer fill
            dict["LayerFillColorDefaultBrush"] = Solid(window);
            dict["LayerFillColorAltBrush"] = Solid(window);
            dict["LayerOnAcrylicFillColorDefaultBrush"] = Solid(window);
            dict["LayerOnAccentAcrylicFillColorDefaultBrush"] = Solid(highlight);

            // Layer on mica base alt
            dict["LayerOnMicaBaseAltFillColorDefaultBrush"] = Solid(window);
            dict["LayerOnMicaBaseAltFillColorSecondaryBrush"] = Solid(window);
            dict["LayerOnMicaBaseAltFillColorTertiaryBrush"] = Solid(window);
            dict["LayerOnMicaBaseAltFillColorTransparentBrush"] = Solid(transparent);

            // Solid background fill
            dict["SolidBackgroundFillColorBaseBrush"] = Solid(window);
            dict["SolidBackgroundFillColorSecondaryBrush"] = Solid(window);
            dict["SolidBackgroundFillColorTertiaryBrush"] = Solid(window);
            dict["SolidBackgroundFillColorQuarternaryBrush"] = Solid(window);
            dict["SolidBackgroundFillColorQuinaryBrush"] = Solid(window);
            dict["SolidBackgroundFillColorSenaryBrush"] = Solid(window);
            dict["SolidBackgroundFillColorTransparentBrush"] = Solid(transparent);
            dict["SolidBackgroundFillColorBaseAltBrush"] = Solid(window);

            // Acrylic background fill
            dict["AcrylicBackgroundFillColorDefaultBrush"] = Solid(window);
            dict["AcrylicBackgroundFillColorBaseBrush"] = Solid(window);

            // System fill (SystemFillColorAttention skipped by Build in HC; brush -> Highlight)
            dict["SystemFillColorAttentionBrush"] = Solid(highlight);
            dict["SystemFillColorInformationalBrush"] = Solid(windowText);
            dict["SystemFillColorSuccessBrush"] = Solid(windowText);
            dict["SystemFillColorCautionBrush"] = Solid(windowText);
            dict["SystemFillColorCriticalBrush"] = Solid(windowText);
            dict["SystemFillColorNeutralBrush"] = Solid(windowText);
            dict["SystemFillColorSolidNeutralBrush"] = Solid(windowText);
            dict["SystemFillColorAttentionBackgroundBrush"] = Solid(highlight);
            dict["SystemFillColorSuccessBackgroundBrush"] = Solid(window);
            dict["SystemFillColorCautionBackgroundBrush"] = Solid(window);
            dict["SystemFillColorCriticalBackgroundBrush"] = Solid(window);
            dict["SystemFillColorNeutralBackgroundBrush"] = Solid(window);
            dict["SystemFillColorSolidAttentionBackgroundBrush"] = Solid(highlight);
            dict["SystemFillColorSolidNeutralBackgroundBrush"] = Solid(control);

            // Window chrome close button (hover/pressed track HC accent in HC). FluenceWindow.xaml
            // binds the WindowCloseButton* keys via DynamicResource, so those are the ones that must
            // be overridden here; the theme-independent brand red seeded by
            // BaseColorTables.AddSharedColors would otherwise fail contrast in High Contrast.
            // WindowCloseFillColor*/WindowCloseForeground* (below) are legacy keys nothing currently
            // consumes; kept for parity with existing golden snapshots and tests.
            dict["WindowCloseButtonBackgroundPointerOverBrush"] = Solid(highlight);
            dict["WindowCloseButtonBackgroundPressedBrush"] = Solid(highlight);
            dict["WindowCloseButtonForegroundPointerOverBrush"] = Solid(highlightText);
            dict["WindowCloseFillColorHoverBrush"] = Solid(highlight);
            dict["WindowCloseFillColorPressedBrush"] = Solid(highlight);
            dict["WindowCloseForegroundHoverBrush"] = Solid(highlightText);
            dict["WindowCloseForegroundPressedBrush"] = Solid(highlightText);

            // NavigationView selection indicator binds to live Highlight in HC, which drifts from
            // the computed SystemAccentColor. The content background binds to Window.
            dict["NavigationViewSelectionIndicatorBrush"] = Solid(highlight);
            dict["NavigationViewContentBackgroundBrush"] = Solid(window);

            // Elevation borders are solid in HC.
            dict["ControlElevationBorderBrush"] = Solid(controlDark);
            dict["CircleElevationBorderBrush"] = Solid(controlDark);
            dict["AccentControlElevationBorderBrush"] = Solid(highlight);
            dict["TextControlElevationBorderBrush"] = Solid(controlDark);

            // ToggleSwitch internal sub-layer base. HC omits the AccentFillBackdrop Color token, so
            // derive it from the live SystemColors window color (matching the brush twin below)
            // rather than a frozen Dark-theme constant, which would be wrong under HC-White.
            dict["AccentFillBackdrop"] = window;
            dict["AccentFillBackdropBrush"] = Solid(window);

            // HC overrides the SelectedText-on-accent Color to black (the brush stays the
            // computed white, matching the legacy promoted value).
            dict["TextOnAccentFillColorSelectedText"] = Color.FromRgb(0x00, 0x00, 0x00);
        }

        /// <summary>
        /// SystemColor aliases consumed by the WinUI Gallery color guidance page. These resolve to
        /// live <see cref="SystemColors"/> in every theme (bound to SystemColors keys unconditionally).
        /// </summary>
        /// <param name="dict">The resource dictionary to populate.</param>
        private static void AddSystemColorAliases(ResourceDictionary dict)
        {
            dict["SystemColorWindowTextColorBrush"] = Solid(SystemColors.WindowTextColor);
            dict["SystemColorWindowColorBrush"] = Solid(SystemColors.WindowColor);
            dict["SystemColorButtonFaceColorBrush"] = Solid(SystemColors.ControlColor);
            dict["SystemColorButtonTextColorBrush"] = Solid(SystemColors.ControlTextColor);
            dict["SystemColorHighlightColorBrush"] = Solid(SystemColors.HighlightColor);
            dict["SystemColorHighlightTextColorBrush"] = Solid(SystemColors.HighlightTextColor);
            dict["SystemColorHotlightColorBrush"] = Solid(SystemColors.HotTrackColor);
            dict["SystemColorGrayTextColorBrush"] = Solid(SystemColors.GrayTextColor);
        }

        private static SolidColorBrush Solid(Color c)
        {
            SolidColorBrush b = new(c);
            b.Freeze();
            return b;
        }
    }
}
