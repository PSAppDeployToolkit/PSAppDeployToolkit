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
    /// high-contrast SystemColors aliases) that are not the plain solid twins <c language="csharp">BrushFactory</c>
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
            //
            // ApplicationPageBackgroundThemeBrush is WinUI's name for the same role and is the
            // preferred key for new code. Both ship: the Fluence name is bound downstream with
            // DynamicResource, where a rename renders a consumer surface transparent with no
            // error and no build failure. Same instance, so the two can never diverge. HighContrast
            // reassigns both together below in AddHighContrastBrushes, since that override runs
            // after this one.
            SolidColorBrush applicationBackgroundBrush = Solid(colors["ApplicationBackgroundColor"]);
            dict["ApplicationBackgroundBrush"] = applicationBackgroundBrush;
            dict["ApplicationPageBackgroundThemeBrush"] = applicationBackgroundBrush;

            // Brush-only keys with no Color twin.

            // InfoBadge foregrounds, one per severity plate. WinUI keys the badge's own foreground
            // (InfoBadgeForeground) rather than reusing a text token, because the pair has to move
            // with the plate: a badge is a filled capsule, so the numeral's legibility depends on
            // what it sits on. Outside high contrast every plate is an accent or status fill and
            // the on-accent text token is right for all five; AddHighContrastBrushes splits them,
            // because there the Attention plate is the system highlight and the other four are the
            // window text colour.
            Color textOnAccent = colors["TextOnAccentFillColorPrimary"];
            dict["InfoBadgeAttentionForegroundBrush"] = Solid(textOnAccent);
            dict["InfoBadgeInformationalForegroundBrush"] = Solid(textOnAccent);
            dict["InfoBadgeSuccessForegroundBrush"] = Solid(textOnAccent);
            dict["InfoBadgeCautionForegroundBrush"] = Solid(textOnAccent);
            dict["InfoBadgeCriticalForegroundBrush"] = Solid(textOnAccent);

            dict["AccentFillColorSelectedTextBackgroundBrush"] = Solid(colors["SystemAccentColor"]);
            // Shared selection-pill accent for NavigationView, ListView, ListBox, TreeView, and SelectorBar.
            // Light/Dark use AccentFillColorDefault (WinUI NavigationView_themeresources.xaml:180
            // uses the same accent fill for its Default/Light/Dark dictionaries); HighContrast is
            // overridden below with the live SystemColors.HighlightColor (HighlightText would be
            // invisible on the HC selected-row fill; see AddHighContrastBrushes).
            dict["NavigationViewSelectionIndicatorForeground"] = Solid(colors["AccentFillColorDefault"]);
            // WinUI ScrollBarTrackFill is AcrylicInAppFillColorDefaultBrush, which its acrylic theme
            // dictionary defines with the same tint, opacity, and fallback as
            // AcrylicBackgroundFillColorDefaultBrush in every theme, so the two resolve identically.
            // AddHighContrastBrushes overrides this with the live system window color.
            dict["ScrollBarTrackFillBrush"] = Solid(colors["AcrylicBackgroundFillColorDefault"]);

            // SystemColor aliases used by the WinUI Gallery color guidance page. These read live
            // SystemColors in every theme, not the computed palette.
            AddSystemColorAliases(dict);

            if (theme is ApplicationTheme.HighContrast)
            {
                AddHighContrastBrushes(dict);
                return;
            }

            AddElevationBorderBrushes(dict, colors, dark);
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

            // WinUI ComboBoxItemCornerRadius (ComboBox_themeresources_perf2026.xaml:345), which WinUI
            // deliberately keeps distinct from ControlCornerRadius. Published beside the other corner
            // radii rather than kept inside ComboBox.xaml so an application overriding corner radii
            // can reach the drop-down item through a supported key.
            dict["ComboBoxItemCornerRadius"] = new CornerRadius(3);

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
        /// the WinUI 3 <c language="xaml">DefaultControlFocusVisualStyle</c>. The border brushes are
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
        /// Builds the two-stroke collection focus visual (outer + inset inner ring), the WinUI 3
        /// <c language="xaml">DefaultCollectionFocusVisualStyle</c>. The inner ring is
        /// ListViewItemFocusVisualSecondaryBrush (ListViewItem_themeresources.xaml:31), which
        /// resolves to FocusStrokeColorInnerBrush; NavigationViewItemFocusVisual
        /// (NavigationView.xaml) already pairs the same two rings for its own items.
        /// </summary>
        private static Style BuildCollectionFocusVisualStyle()
        {
            FrameworkElementFactory outer = new(typeof(Rectangle));
            outer.SetValue(Rectangle.RadiusXProperty, 4.0);
            outer.SetValue(Rectangle.RadiusYProperty, 4.0);
            outer.SetValue(Shape.StrokeProperty, new DynamicResourceExtension("FocusStrokeColorOuterBrush"));
            outer.SetValue(Shape.StrokeThicknessProperty, 2.0);

            FrameworkElementFactory inner = new(typeof(Rectangle));
            inner.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
            inner.SetValue(Rectangle.RadiusXProperty, 3.0);
            inner.SetValue(Rectangle.RadiusYProperty, 3.0);
            inner.SetValue(Shape.StrokeProperty, new DynamicResourceExtension("FocusStrokeColorInnerBrush"));
            inner.SetValue(Shape.StrokeThicknessProperty, 1.0);

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
        /// Light/Dark elevation-border brushes are <see cref="LinearGradientBrush"/> values whose
        /// stop colors come from the computed control-stroke tokens. Definitions (start/end points,
        /// transform, stop offsets) match the WinUI 3 elevation borders.
        /// </summary>
        /// <param name="dict">The resource dictionary to populate.</param>
        /// <param name="colors">The computed color map for the resolved theme.</param>
        /// <param name="dark">True when the resolved theme is Dark.</param>
        private static void AddElevationBorderBrushes(ResourceDictionary dict, IReadOnlyDictionary<string, Color> colors, bool dark)
        {
            // ControlElevationBorderBrush: absolute 0,0 -> 0,3 gradient from
            // ControlStrokeColorSecondary -> Default. WinUI 3 flips this gradient vertically only
            // in the Light theme dictionary (Common_themeresources_any.xaml Light block has a
            // ScaleY="-1" RelativeTransform on this key); the Default/Dark theme dictionary defines
            // the same key with no transform, so Dark must stay unflipped.
            const string ControlStrokeColorDefault = "ControlStrokeColorDefault";
            dict["ControlElevationBorderBrush"] = dark
                ? AbsoluteGradient(colors["ControlStrokeColorSecondary"], colors[ControlStrokeColorDefault])
                : AbsoluteFlippedGradient(colors["ControlStrokeColorSecondary"], colors[ControlStrokeColorDefault]);

            // TextControlElevationBorderBrush: the WinUI 3 text-control rest border is a
            // distinct absolute 0,0 -> 0,2 gradient whose 0.5 stop is the strong stroke,
            // painting the visible bottom underline of every text field at rest
            // (WinUI CommonStyles TextBox_themeresources.xaml).
            dict["TextControlElevationBorderBrush"] = TextControlGradient(
                colors["ControlStrongStrokeColorDefault"], colors[ControlStrokeColorDefault]);

            // TextControlElevationBorderFocusedBrush: same geometry with both stops at 1.0,
            // a hard step to a 2px accent band at the bottom edge. WinUI seeds the accent stop
            // with SystemAccentColorDark1 (Light) / SystemAccentColorLight2 (Dark), which is
            // exactly what ColorMap computes as SystemAccentColorPrimary.
            dict["TextControlElevationBorderFocusedBrush"] = TextControlFocusedGradient(
                colors["SystemAccentColorPrimary"], colors[ControlStrokeColorDefault]);

            // AccentControlElevationBorderBrush: same geometry, on-accent stroke stops. WinUI 3
            // flips this one in both the Light and the Default/Dark theme dictionaries, so it
            // always uses the flipped gradient regardless of theme.
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
            circle.GradientStops.Add(new GradientStop(colors[ControlStrokeColorDefault], 0.50));
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
        /// Builds the unflipped counterpart of <see cref="AbsoluteFlippedGradient"/>: absolute
        /// mapping, 0,0 -> 0,3, no <see cref="ScaleTransform"/>, with stops at 0.33
        /// (<paramref name="stop33"/>) and 1.0 (<paramref name="stop100"/>).
        /// </summary>
        /// <param name="stop33">The color for the 0.33 stop.</param>
        /// <param name="stop100">The color for the 1.0 stop.</param>
        private static LinearGradientBrush AbsoluteGradient(Color stop33, Color stop100)
        {
            LinearGradientBrush b = new()
            {
                MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 3),
            };
            b.GradientStops.Add(new GradientStop(stop33, 0.33));
            b.GradientStops.Add(new GradientStop(stop100, 1.0));
            b.Freeze();
            return b;
        }

        /// <summary>
        /// Builds the WinUI 3 text-control rest border gradient: absolute mapping, 0,0 -> 0,2,
        /// flipped vertically about its centre, with stops at 0.5 (<paramref name="strong"/>, the
        /// visible bottom underline) and 1.0 (<paramref name="stroke"/>, the hairline on the
        /// remaining edges).
        /// </summary>
        /// <param name="strong">The strong stroke color for the 0.5 stop.</param>
        /// <param name="stroke">The default stroke color for the 1.0 stop.</param>
        private static LinearGradientBrush TextControlGradient(Color strong, Color stroke)
        {
            LinearGradientBrush b = NewTextControlGradientShell();
            b.GradientStops.Add(new GradientStop(strong, 0.5));
            b.GradientStops.Add(new GradientStop(stroke, 1.0));
            b.Freeze();
            return b;
        }

        /// <summary>
        /// Builds the WinUI 3 text-control focused border gradient: the same absolute 0,0 -> 0,2
        /// flipped geometry with both stops at offset 1.0, producing a hard step so the bottom
        /// band is solid <paramref name="accent"/> and the remaining edges stay
        /// <paramref name="stroke"/>.
        /// </summary>
        /// <param name="accent">The accent color for the bottom band.</param>
        /// <param name="stroke">The default stroke color for the remaining edges.</param>
        private static LinearGradientBrush TextControlFocusedGradient(Color accent, Color stroke)
        {
            LinearGradientBrush b = NewTextControlGradientShell();
            b.GradientStops.Add(new GradientStop(accent, 1.0));
            b.GradientStops.Add(new GradientStop(stroke, 1.0));
            b.Freeze();
            return b;
        }

        private static LinearGradientBrush NewTextControlGradientShell()
        {
            ScaleTransform flip = new() { CenterY = 0.5, ScaleY = -1 };
            flip.Freeze();
            return new LinearGradientBrush
            {
                MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 2),
                RelativeTransform = flip,
            };
        }

        /// <summary>
        /// High-contrast brush overrides snapshot live <see cref="SystemColors"/> members,
        /// reproducing the non-accent brush overrides in Section 2 of Theme.HighContrast.xaml.
        /// Accent-derived brushes are intentionally left as their computed twins (the legacy C#
        /// accent overlay took precedence over the HC XAML for those keys, so the golden records
        /// the computed palette, not the HC SystemColors). A re-Apply on <c language="csharp">WM_SETTINGCHANGE</c>
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

            // Application background. ApplicationPageBackgroundThemeBrush must be kept pointing at
            // the same instance here too, since this override runs after the general Add assignment
            // and would otherwise leave the WinUI alias stale at the non-HC value.
            SolidColorBrush applicationBackgroundBrush = Solid(window);
            dict["ApplicationBackgroundBrush"] = applicationBackgroundBrush;
            dict["ApplicationPageBackgroundThemeBrush"] = applicationBackgroundBrush;

            // Text fill
            dict["TextFillColorPrimaryBrush"] = Solid(windowText);
            dict["TextFillColorSecondaryBrush"] = Solid(windowText);
            dict["TextFillColorTertiaryBrush"] = Solid(grayText);
            dict["TextFillColorDisabledBrush"] = Solid(grayText);
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

            // Accent acrylic background fill. ColorMap computes these from the accent ramp and its
            // dark flag is Dark only, so high contrast would otherwise publish the light theme's
            // raw accent tint here while every acrylic sibling above maps to a system colour. The
            // accent surface's high contrast counterpart is the highlight, as
            // LayerOnAccentAcrylicFillColorDefaultBrush already uses.
            dict["AccentAcrylicBackgroundFillColorDefaultBrush"] = Solid(highlight);
            dict["AccentAcrylicBackgroundFillColorBaseBrush"] = Solid(highlight);

            // Scroll bar track. WinUI resolves ScrollBarTrackFill to AcrylicInAppFillColorDefaultBrush,
            // which the high contrast dictionary redefines as a solid SystemColorWindowColor brush.
            // The computed AcrylicBackgroundFillColorDefault token is a fixed black in the HC table, so
            // the seed assigned in Add would ignore the white on black variants.
            dict["ScrollBarTrackFillBrush"] = Solid(window);

            // InfoBadge foregrounds follow whichever plate the severity selected. Attention paints
            // the live highlight, whose guaranteed partner is the highlight text colour; the other
            // four paint window text, whose partner is the window colour. A single fixed value
            // cannot serve both, which is what left a black numeral on a window text plate.
            dict["InfoBadgeAttentionForegroundBrush"] = Solid(highlightText);
            dict["InfoBadgeInformationalForegroundBrush"] = Solid(window);
            dict["InfoBadgeSuccessForegroundBrush"] = Solid(window);
            dict["InfoBadgeCautionForegroundBrush"] = Solid(window);
            dict["InfoBadgeCriticalForegroundBrush"] = Solid(window);

            // System fill (SystemFillColorAttention skipped by Build in HC; brush -> Highlight)
            dict["SystemFillColorAttentionBrush"] = Solid(highlight);
            dict["SystemFillColorSuccessBrush"] = Solid(windowText);
            dict["SystemFillColorCautionBrush"] = Solid(windowText);
            dict["SystemFillColorCriticalBrush"] = Solid(windowText);
            dict["SystemFillColorNeutralBrush"] = Solid(windowText);
            dict["SystemFillColorSolidNeutralBrush"] = Solid(windowText);
            // WinUI maps every severity background to the window colour in high contrast
            // (Common_themeresources_any.xaml:499-505), so a severity reads from its icon and its
            // border rather than from a coloured plate. Attention was the one that did not: an
            // InfoBar at Informational severity painted the system highlight behind window text.
            dict["SystemFillColorAttentionBackgroundBrush"] = Solid(window);
            dict["SystemFillColorSuccessBackgroundBrush"] = Solid(window);
            dict["SystemFillColorCautionBackgroundBrush"] = Solid(window);
            dict["SystemFillColorCriticalBackgroundBrush"] = Solid(window);
            dict["SystemFillColorNeutralBackgroundBrush"] = Solid(window);
            dict["SystemFillColorSolidAttentionBackgroundBrush"] = Solid(window);
            dict["SystemFillColorSolidNeutralBackgroundBrush"] = Solid(control);

            // Window chrome close button (hover/pressed track HC accent in HC). FluenceWindow.xaml
            // binds the WindowCloseButton* keys via DynamicResource, so those are the ones that must
            // be overridden here; the theme-independent brand red seeded by
            // BaseColorTables.AddSharedColors would otherwise fail contrast in High Contrast.
            dict["WindowCloseButtonBackgroundPointerOverBrush"] = Solid(highlight);
            dict["WindowCloseButtonBackgroundPressedBrush"] = Solid(highlight);
            dict["WindowCloseButtonForegroundPointerOverBrush"] = Solid(highlightText);

            // NavigationView (and ListView/ListBox/TreeView) selection indicator binds to the
            // live Highlight color in HC (WinUI TreeView_themeresources.xaml:115's
            // SystemColorHighlightColorBrush precedent), which drifts from the computed
            // AccentFillColorDefault. Fluence's HC selected row background stays SystemColors.Control,
            // so HighlightText (designed to sit on a Highlight-colored fill) would be invisible here.
            // The content background binds to Window.
            dict["NavigationViewSelectionIndicatorForeground"] = Solid(highlight);
            dict["NavigationViewContentBackgroundBrush"] = Solid(window);

            // Elevation borders are solid in HC.
            dict["ControlElevationBorderBrush"] = Solid(controlDark);
            dict["CircleElevationBorderBrush"] = Solid(controlDark);
            dict["AccentControlElevationBorderBrush"] = Solid(highlight);
            dict["TextControlElevationBorderBrush"] = Solid(controlDark);
            dict["TextControlElevationBorderFocusedBrush"] = Solid(highlight);

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
