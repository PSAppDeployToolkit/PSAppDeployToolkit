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
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Colors design-reference page, a WPF rendering of the WinUI 3 Gallery Color page.
    /// </summary>
    /// <remarks>
    /// The six sections below are transcribed from
    /// <c language="text">WinUIGallery/Controls/DesignGuidance/ColorSections/*Section.xaml</c>: every group is one
    /// <see cref="ColorPageExample"/> card followed by rows of <see cref="ColorTile"/> swatches, in the
    /// Gallery's order and with the Gallery's column counts. Each tile is painted in the brush it names and
    /// takes its text brush from the Gallery's per-tile pairing. Every brush, size, margin and radius is a
    /// resource reference, so the whole page follows theme, accent and high contrast changes.
    /// </remarks>
    public partial class GalleryColorsPage : Page
    {
        private const string PrimaryText = "TextFillColorPrimaryBrush";
        private const string InverseText = "TextFillColorInverseBrush";
        private const string OnAccentText = "TextOnAccentFillColorPrimaryBrush";

        // The Gallery paints its accent tiles with a foreground that is white in every theme (its
        // TextOnAccentFillColorDefaultBrush); the published token with that shape here is
        // TextOnAccentFillColorSelectedText, which ColorMap seeds white for both themes. Only the
        // accent fill tiles below are dark in both themes, so a theme-following foreground would
        // render black-on-dark in Light for those. The text-control border and accent-acrylic
        // tiles are not dark in both themes (in Light the acrylic fills resolve pale and the
        // border reads as a light grey), so those pick their foreground by contrast instead; see
        // ColorTile.AutoContrastForeground.
        private const string AlwaysWhiteText = "TextOnAccentFillColorSelectedTextBrush";
        private const string QuarternarySurface = "SolidBackgroundFillColorQuarternaryBrush";
        private const string CardStroke = "CardStrokeColorDefaultBrush";
        private const string SingleStroke = "DemoSingleBorderThickness";
        private const string ControlRadius = "ControlCornerRadius";
        private const string OverlayRadius = "OverlayCornerRadius";
        private const string SurfaceWidth = "DemoColorExampleSurfaceWidth";
        private const string SurfaceHeight = "DemoColorExampleSurfaceHeight";

        // A generated abstract image standing in for photography the Control On Image Fill
        // example floats a control over; see CreateControlOnImageExample and FrozenBitmap.
        private static readonly BitmapImage ControlOnImageSample =
            FrozenBitmap("Resources/SampleMedia/ControlOnImageSample.png");

        private static readonly ColorSectionData[] Sections =
        [
            new(
                "Text",
                intro: null,
                [
                    new(
                        "Text",
                        "For UI labels and static text.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateGlyph(PrimaryText),
                        [
                            new(rows: 1,
                            [
                                new("Text / Primary", "Rest or Hover", "TextFillColorPrimaryBrush", OnAccentText),
                                new("Text / Secondary", "Rest or Hover", "TextFillColorSecondaryBrush", OnAccentText),
                                new("Text / Tertiary", "Pressed only (not accessible)", "TextFillColorTertiaryBrush", OnAccentText),
                                new("Text / Disabled", "Disabled only (not accessible)", "TextFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Accent Text",
                        "Recommended for links.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateGlyph("AccentTextFillColorPrimaryBrush"),
                        [
                            new(rows: 1,
                            [
                                new("Accent Text / Primary", "Rest or Hover", "AccentTextFillColorPrimaryBrush", OnAccentText),
                                new("Accent Text / Secondary", "Rest or Hover", "AccentTextFillColorSecondaryBrush", OnAccentText),
                                new("Accent Text / Tertiary", "Pressed only (not accessible)", "AccentTextFillColorTertiaryBrush", OnAccentText),
                                new("Accent Text / Disabled", "Disabled only (not accessible)", "AccentTextFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Text On Accent",
                        "Used for text on accent colored controls or fills.",
                        "AccentFillColorDefaultBrush",
                        OnAccentText,
                        static () => CreateGlyph(OnAccentText),
                        [
                            new(rows: 1,
                            [
                                new("Text on Accent / Primary", "Rest or Hover", "TextOnAccentFillColorPrimaryBrush", PrimaryText),
                                new("Text on Accent / Secondary", "Pressed only (not accessible)", "TextOnAccentFillColorSecondaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Text on Accent / Disabled", "Disabled only (not accessible)", "TextOnAccentFillColorDisabledBrush", foregroundKey: null),
                                new("Text on Accent / Selected Text", "For highlighted text in text entry experiences", "TextOnAccentFillColorSelectedTextBrush", foregroundKey: null),
                            ]),
                        ]),
                ]),
            new(
                "Fill",
                intro: null,
                [
                    new(
                        "Control Fill",
                        "Fill used for standard controls.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => new Controls.Button { Content = "Text" },
                        [
                            new(rows: 1,
                            [
                                new("Control / Default", "Rest", "ControlFillColorDefaultBrush", PrimaryText),
                                new("Control / Secondary", "Hover", "ControlFillColorSecondaryBrush", PrimaryText),
                                new("Control / Tertiary", "Pressed", "ControlFillColorTertiaryBrush", PrimaryText),
                                new("Control / Quarternary", "Rest (Pill Button control)", "ControlFillColorQuarternaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Control / Disabled", "Disabled", "ControlFillColorDisabledBrush", PrimaryText),
                                new("Control / Transparent", "Rest", "ControlFillColorTransparentBrush", PrimaryText),
                                new("Control / Input Active", "Active/focused text input fields", "ControlFillColorInputActiveBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control Alt Fill",
                        "Fill used for the 'off' states of toggle controls.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateToggleSwitch(narrow: false),
                        [
                            new(rows: 1,
                            [
                                new("Control Alt / Transparent", string.Empty, "ControlAltFillColorTransparentBrush", PrimaryText),
                                new("Control Alt / Secondary", "Rest", "ControlAltFillColorSecondaryBrush", PrimaryText),
                                new("Control Alt / Tertiary", "Hover", "ControlAltFillColorTertiaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Control Alt / Quarternary", "Pressed", "ControlAltFillColorQuarternaryBrush", PrimaryText),
                                new("Control Alt / Disabled", "Disabled", "ControlAltFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Neutral Solid",
                        "Fills used for Sliders thumb control to cover the track beneath it.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateSlider,
                        [
                            new(rows: 1,
                            [
                                new("Control Solid / Default", "Rest", "ControlSolidFillColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Neutral Strong",
                        "Used for controls that must meet contrast ratio requirements of 3:1.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateScrollBar,
                        [
                            new(rows: 1,
                            [
                                new("Control Strong / Default", "Rest or hover", "ControlStrongFillColorDefaultBrush", InverseText),
                                new("Control Strong / Disabled", "Disabled only (not accessible)", "ControlStrongFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Subtle Fill",
                        "Used for list items and fills that are transparent at rest and appear upon interaction.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateSubtleFillExample,
                        [
                            new(rows: 1,
                            [
                                new("Subtle / Transparent", "Rest", "SubtleFillColorTransparentBrush", PrimaryText),
                                new("Subtle / Secondary", "Hover", "SubtleFillColorSecondaryBrush", PrimaryText),
                                new("Subtle / Tertiary", "Pressed", "SubtleFillColorTertiaryBrush", PrimaryText),
                                new("Subtle / Disabled", "Disabled only (not accessible)", "SubtleFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control On Image Fill",
                        "Used for controls living on top of imagery.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateControlOnImageExample,
                        [
                            new(rows: 1,
                            [
                                new("Control On Image Fill Default", "Rest", "ControlOnImageFillColorDefaultBrush", PrimaryText),
                                new("Control On Image Fill Secondary", "Hover", "ControlOnImageFillColorSecondaryBrush", PrimaryText),
                                new("Control On Image Fill Tertiary", "Pressed", "ControlOnImageFillColorTertiaryBrush", PrimaryText),
                                new("Control On Image Fill Disabled", "Disabled only (not accessible)", "ControlOnImageFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Accent Fill",
                        "Used for accent fills on controls.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => new Controls.Button { Content = "Text", Appearance = ControlAppearance.Accent },
                        [
                            new(rows: 1,
                            [
                                new("Accent / Default", "Rest", "AccentFillColorDefaultBrush", AlwaysWhiteText),
                                new("Accent / Secondary", "Hover", "AccentFillColorSecondaryBrush", AlwaysWhiteText),
                                new("Accent / Tertiary", "Pressed", "AccentFillColorTertiaryBrush", AlwaysWhiteText),
                            ]),
                            new(rows: 1,
                            [
                                new("Accent / Disabled", "Disabled", "AccentFillColorDisabledBrush", PrimaryText),
                                new("Accent / Selected Text Background", "Highlighted/selected text background", "AccentFillColorSelectedTextBackgroundBrush", AlwaysWhiteText),
                            ]),
                        ]),
                ]),
            new(
                "Stroke",
                intro: null,
                [
                    new(
                        "Card Stroke",
                        "Used for card and layer colors.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("CardBackgroundFillColorDefaultBrush", CardStroke, ControlRadius, "DemoColorExampleCardWidth", "DemoColorExampleCardStrokeHeight"),
                        [
                            new(rows: 1,
                            [
                                new("Card Stroke / Default", "Card layer and strokes", "CardStrokeColorDefaultBrush", PrimaryText),
                                new("Card Stroke / Default Solid", "Solid equivalent of Card Stroke / Default. Used in command bar for expanded states", "CardStrokeColorDefaultSolidBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control Elevation (gradient strokes)",
                        "Used for standard control strokes and stroke states.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => new Controls.Button { Content = "Text" },
                        [
                            new(rows: 1,
                            [
                                new("Control / Border", "Rest", "ControlElevationBorderBrush", PrimaryText),
                                new("Circle / Border", "Rest", "CircleElevationBorderBrush", PrimaryText),
                                new("Text Control / Border", "Rest", "TextControlElevationBorderBrush", foregroundKey: null),
                            ]),
                            new(rows: 1,
                            [
                                new("Text Control / Border Focused", "Active text fields", "TextControlElevationBorderFocusedBrush", PrimaryText),
                                new("Accent Control / Border", "Rest", "AccentControlElevationBorderBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control Stroke",
                        "Used for gradient stops in elevation borders, and for control states.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => new Controls.Button { Content = "Text" },
                        [
                            new(rows: 1,
                            [
                                new("Control Stroke / Default", "Used in Control Elevation Brushes. Pressed or Disabled", "ControlStrokeColorDefaultBrush", PrimaryText),
                                new("Control Stroke / Secondary", "Used in Control Elevation Brushes", "ControlStrokeColorSecondaryBrush", PrimaryText),
                                new("Control Stroke / On Accent Default", "Used in Control Elevation Brushes. Pressed or Disabled", "ControlStrokeColorOnAccentDefaultBrush", PrimaryText),
                                new("Control Stroke / On Accent Secondary", "Used in Control Elevation Brushes", "ControlStrokeColorOnAccentSecondaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Control Stroke / On Accent Tertiary", "Linework on Accent controls, ie: dividers", "ControlStrokeColorOnAccentTertiaryBrush", PrimaryText),
                                new("Control Stroke / On Accent Disabled", "Disabled", "ControlStrokeColorOnAccentDisabledBrush", PrimaryText),
                                new("Control Stroke / For Strong Fill When On Image", "When used with a 'strong' fill color, ensures a 3:1 contrast on any background", "ControlStrokeColorForStrongFillWhenOnImageBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control Strong Stroke",
                        "Used for control strokes that must meet contrast ratio requirements of 3:1.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateToggleSwitch(narrow: true),
                        [
                            new(rows: 1,
                            [
                                new("Control Strong Stroke / Default", "3:1 control border", "ControlStrongStrokeColorDefaultBrush", InverseText),
                                new("Control Strong Stroke / Disabled", "Disabled", "ControlStrongStrokeColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Surface Stroke",
                        "Used for strokes on background surfaces, ie: flyouts, windows, dialogs.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("AcrylicBackgroundFillColorBaseBrush", "SurfaceStrokeColorDefaultBrush", OverlayRadius, SurfaceWidth, SurfaceHeight),
                        [
                            new(rows: 1,
                            [
                                new("Surface Stroke / Default", "Window and dialog borders, theme inverse", "SurfaceStrokeColorDefaultBrush", PrimaryText),
                                new("Surface Stroke / Flyout", "Control flyouts, always dark", "SurfaceStrokeColorFlyoutBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Divider Stroke",
                        "Used for divider and graphic lines.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateDividerExample,
                        [
                            new(rows: 1,
                            [
                                new("Divider Stroke / Default", "Content dividers", "DividerStrokeColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Focus Stroke",
                        "Used for divider and graphic lines. Theme inverse; dark in light theme and light in dark theme.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateFocusExample,
                        [
                            new(rows: 1,
                            [
                                new("Focus / Outer", "Outer stroke color", "FocusStrokeColorOuterBrush", InverseText),
                                new("Focus / Inner", "Inner stroke color", "FocusStrokeColorInnerBrush", PrimaryText),
                            ]),
                        ]),
                ]),
            new(
                "Background",
                intro: null,
                [
                    new(
                        "Card Background",
                        "Used to create 'cards' - content blocks that live on page and layer backgrounds.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("CardBackgroundFillColorDefaultBrush", CardStroke, ControlRadius, "DemoColorExampleCardWidth", "DemoColorExampleCardBackgroundHeight"),
                        [
                            new(rows: 1,
                            [
                                new("Card Background / Default", "Default card color", "CardBackgroundFillColorDefaultBrush", PrimaryText),
                                new("Card Background / Secondary", "Alternate card color: slightly darker", "CardBackgroundFillColorSecondaryBrush", PrimaryText),
                                new("Card Background / Tertiary", "Default card hover and pressed color", "CardBackgroundFillColorTertiaryBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Smoke Background",
                        "Used over windows and desktop to block them out as inaccessible.",
                        "SmokeFillColorDefaultBrush",
                        foregroundKey: null,
                        static () => CreateSurface("CardBackgroundFillColorDefaultBrush", CardStroke, OverlayRadius, SurfaceWidth, SurfaceHeight),
                        [
                            new(rows: 1,
                            [
                                new("Smoke / Default", "Dims the background behind dialogs", "SmokeFillColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Layer",
                        "Used on background colors of any material to create layering.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateLayerExample("AcrylicBackgroundFillColorBaseBrush", "LayerFillColorDefaultBrush"),
                        [
                            new(rows: 1,
                            [
                                new("Layer / Default", "Content layer color", "LayerFillColorDefaultBrush", PrimaryText),
                                new("Layer / Alt", "Alternate content layer color", "LayerFillColorAltBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Layer on Acrylic",
                        "Used on background colors of any material to create layering.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateLayerExample("AcrylicBackgroundFillColorBaseBrush", "LayerOnAcrylicFillColorDefaultBrush"),
                        [
                            new(rows: 1,
                            [
                                new("Layer On Acrylic / Default", "Content layer color on acrylic surfaces", "LayerOnAcrylicFillColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Layer on Mica Base Alt",
                        "Used for fills on Tab control.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateTabExample,
                        [
                            new(rows: 1,
                            [
                                new("Layer On Mica Base Alt / Default", "Active Tab Rest, Content layer", "LayerOnMicaBaseAltFillColorDefaultBrush", PrimaryText),
                                new("Layer On Mica Base Alt / Tertiary", "Active Tab Drag", "LayerOnMicaBaseAltFillColorTertiaryBrush", PrimaryText),
                                new("Layer On Mica Base Alt / Transparent", "Inactive Tab Rest", "LayerOnMicaBaseAltFillColorTransparentBrush", PrimaryText),
                                new("Layer On Mica Base Alt / Secondary", "Inactive Tab Hover", "LayerOnMicaBaseAltFillColorSecondaryBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Solid Background",
                        "Solid background colors to place layers, cards or controls on.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("SolidBackgroundFillColorBaseBrush", CardStroke, ControlRadius, SurfaceWidth, SurfaceHeight),
                        [
                            new(rows: 1,
                            [
                                new("Solid Background / Base", "Used for the bottom most layer of an experience", "SolidBackgroundFillColorBaseBrush", PrimaryText),
                                new("Solid Background / Base Alt", "Used for the bottom most layer of an experience", "SolidBackgroundFillColorBaseAltBrush", PrimaryText),
                                new("Solid Background / Secondary", "Alternate base color for those who need a darker background color", "SolidBackgroundFillColorSecondaryBrush", PrimaryText),
                                new("Solid Background / Tertiary", "Content layer color", "SolidBackgroundFillColorTertiaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Solid Background / Quarternary", "Alt content layer color", "SolidBackgroundFillColorQuarternaryBrush", PrimaryText),
                                new("Solid Background / Quinary", "Used for solid default card colors", "SolidBackgroundFillColorQuinaryBrush", PrimaryText),
                                new("Solid Background / Senary", "Used for solid default card colors", "SolidBackgroundFillColorSenaryBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Acrylic Background",
                        "Acrylic background colors to place layers, cards, or controls on.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("AcrylicBackgroundFillColorBaseBrush", CardStroke, OverlayRadius, SurfaceWidth, SurfaceHeight),
                        [
                            new(rows: 1,
                            [
                                new("Acrylic Background / Base", "Used for the bottom most layer of an acrylic surface only when the surface will use layers", "AcrylicBackgroundFillColorBaseBrush", PrimaryText),
                                new("Acrylic Background / Default", "Default acrylic recipe used for control flyouts and surfaces that live with in the context of an app", "AcrylicBackgroundFillColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Accent Acrylic Background",
                        "Acrylic background colors to place layers, cards, or controls on.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("AccentAcrylicBackgroundFillColorBaseBrush", CardStroke, OverlayRadius, SurfaceWidth, SurfaceHeight),
                        [
                            new(rows: 1,
                            [
                                new("Accent Acrylic Background / Base", "Used for the bottom most layer of an acrylic surface only when the surface will use layers", "AccentAcrylicBackgroundFillColorBaseBrush", foregroundKey: null),
                                new("Accent Acrylic Background / Default", "Default acrylic recipe used for control flyouts and surfaces that live with in the context of an app", "AccentAcrylicBackgroundFillColorDefaultBrush", foregroundKey: null),
                            ]),
                        ]),
                ]),
            new(
                "Signal",
                intro: null,
                [
                    new(
                        "System",
                        "Used for accent fills on controls.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateInfoBar,
                        [
                            new(rows: 1,
                            [
                                new("System / Success", "Badge", "SystemFillColorSuccessBrush", InverseText),
                                new("System / Caution", "Badge", "SystemFillColorCautionBrush", InverseText),
                                new("System / Critical", "Badge", "SystemFillColorCriticalBrush", InverseText),
                            ]),
                            new(rows: 1,
                            [
                                new("System / Success Background", "Infobar Background", "SystemFillColorSuccessBackgroundBrush", PrimaryText),
                                new("System / Caution Background", "Infobar Background", "SystemFillColorCautionBackgroundBrush", PrimaryText),
                                new("System / Critical Background", "Infobar Background", "SystemFillColorCriticalBackgroundBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("System / Attention", "Badge", "SystemFillColorAttentionBrush", InverseText),
                                new("System / Neutral", "Badge", "SystemFillColorNeutralBrush", InverseText),
                                new("System / Solid Neutral", "Neutral badges over content", "SystemFillColorSolidNeutralBrush", InverseText),
                            ]),
                            new(rows: 1,
                            [
                                new("System / Attention Background", "Infobar Background", "SystemFillColorAttentionBackgroundBrush", PrimaryText),
                                new("System / Neutral Background", "Infobar Background", "SystemFillColorNeutralBackgroundBrush", PrimaryText),
                                new("System / Solid Neutral Background", "Neutral badges over content", "SystemFillColorSolidNeutralBackgroundBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("System / Solid Attention Background", string.Empty, "SystemFillColorSolidAttentionBackgroundBrush", PrimaryText),
                            ]),
                        ]),
                ]),
            new(
                "High Contrast",
                "Brush names are the same in every theme; Windows chooses the colors from the active contrast theme. The first row shows the live system colors, so it is the palette this machine is running; the four below are the contrast themes Windows ships.",
                [
                    new(
                        title: null,
                        string.Empty,
                        QuarternarySurface,
                        foregroundKey: null,
                        example: null,
                        [
                            new(rows: 2,
                            [
                                new("Window Text Color", "Foreground / Text color for Headings, body copy, lists, placeholder text, app and window borders, any UI that can't be interacted with", "SystemColorWindowTextColorBrush", "SystemColorWindowColorBrush"),
                                new("Highlight Text Color", "Foreground color for text or UI that is selected, interacted with (hover, pressed), or in progress", "SystemColorHighlightTextColorBrush", "SystemColorHighlightColorBrush"),
                                new("Button Text Color", "Foreground color for buttons and any UI that can be interacted with", "SystemColorButtonTextColorBrush", "SystemColorButtonFaceColorBrush"),
                                new("Hotlight Color", "Foreground / Text color for hyperlink text", "SystemColorHotlightColorBrush", "SystemColorWindowColorBrush"),
                                new("Window Color", "Background of pages, panes, popups, and windows", "SystemColorWindowColorBrush", "SystemColorWindowTextColorBrush"),
                                new("Highlight Color", "Background or accent color for UI that is selected, interacted with (hover, pressed), or in progress", "SystemColorHighlightColorBrush", "SystemColorHighlightTextColorBrush"),
                                new("Button Face Color", "Background color for buttons and any UI that can be interacted with", "SystemColorButtonFaceColorBrush", "SystemColorButtonTextColorBrush"),
                                new("Gray Text Color / Disabled", "Foreground / Text color for Inactive (disabled) UI", "SystemColorGrayTextColorBrush", "SystemColorWindowColorBrush"),
                            ]),
                        ]),
                    new(
                        "Aquatic",
                        "The shipped Aquatic contrast theme.",
                        QuarternarySurface,
                        foregroundKey: null,
                        example: null,
                        [
                            new(rows: 2, HighContrastPalette("#FFFFFF", "#202020", "#263B50", "#8EE3F0", "#FFFFFF", "#202020", "#75E9FC", "#A6A6A6")),
                        ],
                        headingOnly: true),
                    new(
                        "Desert",
                        "The shipped Desert contrast theme.",
                        QuarternarySurface,
                        foregroundKey: null,
                        example: null,
                        [
                            new(rows: 2, HighContrastPalette("#3D3D3D", "#FFFAEF", "#FFF5E3", "#903909", "#202020", "#FFFAEF", "#1C5E75", "#676767")),
                        ],
                        headingOnly: true),
                    new(
                        "Dusk",
                        "The shipped Dusk contrast theme.",
                        QuarternarySurface,
                        foregroundKey: null,
                        example: null,
                        [
                            new(rows: 2, HighContrastPalette("#FFFFFF", "#2D3236", "#212D3B", "#ABCFF2", "#B6F6F0", "#2D3236", "#70EBDE", "#A6A6A6")),
                        ],
                        headingOnly: true),
                    new(
                        "Night Sky",
                        "The shipped Night Sky contrast theme.",
                        QuarternarySurface,
                        foregroundKey: null,
                        example: null,
                        [
                            new(rows: 2, HighContrastPalette("#FFFFFF", "#000000", "#2B2B2B", "#D6B4FD", "#FFEE32", "#000000", "#8080FF", "#A6A6A6")),
                        ],
                        headingOnly: true),
                ]),
        ];

        /// <summary>
        /// Initializes a new instance of the <see cref="GalleryColorsPage"/> class.
        /// </summary>
        public GalleryColorsPage()
        {
            InitializeComponent();
            BuildSections();
        }

        private void BuildSections()
        {
            if (ColorSectionSelector.Items.Count != Sections.Length)
            {
                throw new InvalidOperationException("The Colors page declares " + ColorSectionSelector.Items.Count.ToString(CultureInfo.InvariantCulture) + " selector items but " + Sections.Length.ToString(CultureInfo.InvariantCulture) + " sections.");
            }

            _sectionPanels = new FrameworkElement[Sections.Length];
            for (int i = 0; i < Sections.Length; i++)
            {
                Controls.SelectorBarItem item = (Controls.SelectorBarItem)ColorSectionSelector.Items[i];
                if (!string.Equals(item.Text, Sections[i].Title, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Colors selector item '" + item.Text + "' does not match section '" + Sections[i].Title + "'.");
                }

                _sectionPanels[i] = CreateSection(Sections[i]);
            }

            // Every section is built up front, as the tabbed version built every tab's content,
            // so switching sections is a content swap rather than a rebuild.
            ColorSectionSelector.SelectionChanged += OnSectionSelectionChanged;
            ColorSectionSelector.SelectedIndex = 0;
        }

        /// <summary>
        /// Navigates the section presenter, choosing the slide direction from the move the way the
        /// WinUI Gallery's own Color page does (ColorPage.xaml.cs): a later section slides in from
        /// the right, an earlier one from the left.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnSectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ColorSectionSelector.SelectedIndex;
            if (_sectionPanels is null || index < 0 || index >= _sectionPanels.Length)
            {
                return;
            }

            ColorSectionPresenter.TransitionEffect = index > _selectedSectionIndex
                ? SlideNavigationTransitionEffect.FromRight
                : SlideNavigationTransitionEffect.FromLeft;
            ColorSectionPresenter.Content = _sectionPanels[index];
            _selectedSectionIndex = index;
        }

        /// <summary>
        /// The built section views, one per selector item, or null before the page is built. Each
        /// is the section's own scroll host, so the scrollbar sits inside the presenter and slides
        /// with the section it belongs to.
        /// </summary>
        private FrameworkElement[]? _sectionPanels;

        /// <summary>
        /// The section the presenter is showing, so the next navigation knows which way to slide.
        /// </summary>
        private int _selectedSectionIndex;

        /// <summary>
        /// Builds one section: its content stack inside its own scroll host. The scroll host is
        /// focusable so a click in the section hands it Home, End, Page Up and Page Down, leaving
        /// the arrow keys to the SelectorBar the click came from.
        /// </summary>
        /// <param name="section">The section to build.</param>
        /// <returns>The section view.</returns>
        private static FrameworkElement CreateSection(ColorSectionData section)
        {
            Controls.StackPanel panel = new();
            panel.SetResourceReference(Controls.StackPanel.SpacingProperty, "DemoColorSectionSpacing");
            panel.SetResourceReference(MarginProperty, "DemoColorSectionContentMargin");

            if (section.Intro is not null)
            {
                TextBlock intro = new() { Text = section.Intro, TextWrapping = TextWrapping.Wrap };
                intro.SetResourceReference(StyleProperty, "BodyTextBlockStyle");
                intro.SetResourceReference(MarginProperty, "DemoMediumTopGapMargin");
                _ = panel.Children.Add(intro);
            }

            foreach (ColorGroupData group in section.Groups)
            {
                if (group.Title is not null && group.HeadingOnly)
                {
                    TextBlock heading = new() { Text = group.Title, TextWrapping = TextWrapping.Wrap };
                    heading.SetResourceReference(StyleProperty, "SubtitleTextBlockStyle");
                    heading.SetResourceReference(MarginProperty, "DemoColorExampleMargin");
                    _ = panel.Children.Add(heading);
                }
                else if (group.Title is not null)
                {
                    ColorPageExample example = new()
                    {
                        Title = group.Title,
                        Description = group.Description,
                        ExampleContent = group.Example?.Invoke(),
                    };
                    example.SetResourceReference(BackgroundProperty, group.BackgroundKey);
                    if (group.ForegroundKey is not null)
                    {
                        example.SetResourceReference(ForegroundProperty, group.ForegroundKey);
                    }

                    _ = panel.Children.Add(example);
                }

                foreach (ColorTileRowData row in group.Rows)
                {
                    _ = panel.Children.Add(CreateTileGrid(row));
                }
            }

            Controls.SmoothScrollViewer scroll = new()
            {
                Content = panel,
                Focusable = true,
            };
            scroll.SetResourceReference(StyleProperty, "GalleryPageScrollViewerStyle");
            scroll.SetResourceReference(MarginProperty, "DemoPageScrollHostMargin");
            return scroll;
        }

        // WinUI Gallery GalleryTileGridStyle: tiles sit on the base solid background inside a 1px card stroke at
        // OverlayCornerRadius. WPF's Border does not clip children to that radius, so the corner tiles round
        // their own outer corners instead.
        /// <summary>
        /// Builds the eight tiles of one shipped high contrast palette, in the Gallery's order and
        /// with its colour pairings (HighContrastSection.xaml). The brush keys are the same in every
        /// palette, which is the point the section makes: Windows picks the colours, the app keeps
        /// naming the same brushes.
        /// </summary>
        /// <param name="windowText">Window text colour.</param>
        /// <param name="window">Window background colour.</param>
        /// <param name="highlightText">Highlight text colour.</param>
        /// <param name="highlight">Highlight background colour.</param>
        /// <param name="buttonText">Button text colour.</param>
        /// <param name="buttonFace">Button face colour.</param>
        /// <param name="hotlight">Hyperlink colour.</param>
        /// <param name="grayText">Disabled text colour.</param>
        /// <returns>The palette's tiles, laid out as four columns over two rows.</returns>
        private static ColorTileData[] HighContrastPalette(
            string windowText,
            string window,
            string highlightText,
            string highlight,
            string buttonText,
            string buttonFace,
            string hotlight,
            string grayText)
        {
            return
            [
                new("Window Text Color", "Foreground / Text color for Headings, body copy, lists, placeholder text, app and window borders, any UI that can't be interacted with", "SystemColorWindowTextColor", foregroundKey: null, windowText, window),
                new("Highlight Text Color", "Foreground color for text or UI that is selected, interacted with (hover, pressed), or in progress", "SystemColorHighlightTextColor", foregroundKey: null, highlightText, highlight),
                new("Button Text Color", "Foreground color for buttons and any UI that can be interacted with", "SystemColorButtonTextColor", foregroundKey: null, buttonText, buttonFace),
                new("Hotlight Color", "Foreground / Text color for hyperlink text", "SystemColorHotlightColor", foregroundKey: null, hotlight, window),
                new("Window Color", "Background of pages, panes, popups, and windows", "SystemColorWindowColor", foregroundKey: null, window, windowText),
                new("Highlight Color", "Background or accent color for UI that is selected, interacted with (hover, pressed), or in progress", "SystemColorHighlightColor", foregroundKey: null, highlight, highlightText),
                new("Button Face Color", "Background color for buttons and any UI that can be interacted with", "SystemColorButtonFaceColor", foregroundKey: null, buttonFace, buttonText),
                new("Gray Text Color / Disabled", "Foreground / Text color for Inactive (disabled) UI", "SystemColorGrayTextColor", foregroundKey: null, grayText, window),
            ];
        }

        /// <summary>
        /// Returns a frozen brush for a literal hex colour from one of the shipped high contrast
        /// palettes.
        /// </summary>
        /// <param name="hex">The colour, as the Gallery writes it.</param>
        /// <returns>The frozen brush.</returns>
        private static SolidColorBrush Frozen(string hex)
        {
            SolidColorBrush brush = new((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        private static Controls.Border CreateTileGrid(ColorTileRowData row)
        {
            int columns = row.Tiles.Length / row.Rows;
            UniformGrid grid = new() { Rows = row.Rows, Columns = columns };

            for (int i = 0; i < row.Tiles.Length; i++)
            {
                ColorTileData data = row.Tiles[i];
                int rowIndex = i / columns;
                int columnIndex = i % columns;
                ColorTile tile = new()
                {
                    ColorName = data.Name,
                    ColorExplanation = data.Explanation,
                    ColorBrushName = data.BrushKey,
                    ShowSeparator = columnIndex < columns - 1,
                    // A live tile carries its key so the tests and the copy button can read it; a
                    // high contrast palette tile paints a fixed colour and has no live brush.
                    Tag = data.LiteralBackground is null ? data.BrushKey : null,
                };
                if (data.LiteralBackground is not null)
                {
                    // A high contrast palette tile: the Gallery prints the four shipped contrast
                    // themes as fixed values, so these do not follow the live theme.
                    tile.Background = Frozen(data.LiteralBackground);
                    tile.Foreground = Frozen(data.LiteralForeground ?? "#FFFFFF");
                }
                else
                {
                    tile.SetResourceReference(BackgroundProperty, data.BrushKey);
                    if (data.ForegroundKey is null)
                    {
                        // The Gallery paints this tile with literal Black; see ColorTile.AutoContrastForeground.
                        tile.AutoContrastForeground = true;
                    }
                    else
                    {
                        tile.SetResourceReference(ForegroundProperty, data.ForegroundKey);
                    }
                }
                tile.SetResourceReference(ColorTile.TileCornerRadiusProperty, GetTileCornerRadiusKey(rowIndex, columnIndex, row.Rows, columns));
                _ = grid.Children.Add(tile);
            }

            Controls.Border surface = new() { Child = grid };
            surface.SetResourceReference(BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
            surface.SetResourceReference(Border.BorderBrushProperty, CardStroke);
            surface.SetResourceReference(Border.BorderThicknessProperty, SingleStroke);
            surface.SetResourceReference(Border.CornerRadiusProperty, OverlayRadius);
            return surface;
        }

        private static string GetTileCornerRadiusKey(int rowIndex, int columnIndex, int rows, int columns)
        {
            bool first = columnIndex is 0;
            bool last = columnIndex == columns - 1;
            bool top = rowIndex is 0;
            bool bottom = rowIndex == rows - 1;
            return (rows is 1, first, last, top, bottom) switch
            {
                (true, true, true, _, _) => "DemoColorTileOnlyCornerRadius",
                (true, true, false, _, _) => "DemoColorTileFirstCornerRadius",
                (true, false, true, _, _) => "DemoColorTileLastCornerRadius",
                (true, _, _, _, _) => "DemoColorTileMiddleCornerRadius",
                (false, true, _, true, _) => "DemoColorTileTopLeftCornerRadius",
                (false, _, true, true, _) => "DemoColorTileTopRightCornerRadius",
                (false, true, _, _, true) => "DemoColorTileBottomLeftCornerRadius",
                (false, _, true, _, true) => "DemoColorTileBottomRightCornerRadius",
                _ => "DemoColorTileMiddleCornerRadius",
            };
        }

        private static TextBlock CreateGlyph(string foregroundKey)
        {
            TextBlock glyph = new() { Text = "Aa", FontWeight = FontWeights.SemiBold };
            glyph.SetResourceReference(TextBlock.FontSizeProperty, "DemoColorExampleGlyphFontSize");
            glyph.SetResourceReference(TextBlock.ForegroundProperty, foregroundKey);
            return glyph;
        }

        private static Controls.ToggleSwitch CreateToggleSwitch(bool narrow)
        {
            Controls.ToggleSwitch toggle = new() { OnContent = string.Empty, OffContent = string.Empty };
            if (narrow)
            {
                toggle.SetResourceReference(MinWidthProperty, "DemoColorExampleToggleSwitchWidth");
                toggle.SetResourceReference(MaxWidthProperty, "DemoColorExampleToggleSwitchWidth");
            }

            return toggle;
        }

        private static Controls.Slider CreateSlider()
        {
            Controls.Slider slider = new() { Maximum = 100, Value = 40 };
            slider.SetResourceReference(MinWidthProperty, "DemoColorExampleSliderMinWidth");
            return slider;
        }

        private static ScrollBar CreateScrollBar()
        {
            ScrollBar scrollBar = new()
            {
                Orientation = Orientation.Horizontal,
                Maximum = 100,
                Value = 40,
                ViewportSize = 40,
            };
            scrollBar.SetResourceReference(StyleProperty, "HorizontalScrollBarStyle");
            scrollBar.SetResourceReference(WidthProperty, "DemoColorExampleScrollBarWidth");
            scrollBar.SetResourceReference(HeightProperty, "DemoColorExampleScrollBarHeight");
            return scrollBar;
        }

        private static StackPanel CreateSubtleFillExample()
        {
            Controls.Border rest = new() { Child = new TextBlock { Text = "Rest" } };
            rest.SetResourceReference(Border.PaddingProperty, "DemoColorExampleSubtleRestPadding");

            Controls.Border hover = new() { Child = new TextBlock { Text = "Hover" } };
            hover.SetResourceReference(Border.PaddingProperty, "DemoColorExampleSubtleHoverPadding");
            hover.SetResourceReference(MinWidthProperty, "DemoColorExampleSubtleHoverMinWidth");
            hover.SetResourceReference(BackgroundProperty, "SubtleFillColorSecondaryBrush");
            hover.SetResourceReference(Border.CornerRadiusProperty, ControlRadius);

            StackPanel panel = new();
            _ = panel.Children.Add(rest);
            _ = panel.Children.Add(hover);
            return panel;
        }

        // The Gallery places a control over a photo. This repo is BSD 3-Clause and cannot ship
        // third party stock photography, so ControlOnImageSample.png is a generated abstract
        // image (a soft diagonal gradient plus a few blurred translucent shapes) that reads as
        // imagery rather than a flat UI surface.
        private static Grid CreateControlOnImageExample()
        {
            Controls.Image photo = new() { Source = ControlOnImageSample, Stretch = Stretch.UniformToFill };
            photo.SetResourceReference(Controls.Image.CornerRadiusProperty, ControlRadius);
            AutomationProperties.SetName(photo, "Sample photograph");

            Controls.Border badge = CreateSurface("ControlOnImageFillColorDefaultBrush", "ControlStrongStrokeColorDefaultBrush", ControlRadius, "DemoColorExampleOnImageBadgeSize", "DemoColorExampleOnImageBadgeSize");
            badge.HorizontalAlignment = HorizontalAlignment.Right;
            badge.VerticalAlignment = VerticalAlignment.Top;
            badge.SetResourceReference(MarginProperty, "DemoColorExampleOnImageBadgeMargin");

            Grid image = new();
            image.SetResourceReference(WidthProperty, "DemoColorExampleImageWidth");
            image.SetResourceReference(HeightProperty, "DemoColorExampleImageHeight");
            _ = image.Children.Add(photo);
            _ = image.Children.Add(badge);
            return image;
        }

        /// <summary>
        /// Loads a frozen, cached <see cref="BitmapImage"/> for a demo asset embedded as a
        /// resource in this assembly.
        /// </summary>
        /// <param name="assemblyRelativePath">The resource path, relative to this assembly.</param>
        /// <returns>The frozen bitmap, safe to share across every instance of the example.</returns>
        private static BitmapImage FrozenBitmap(string assemblyRelativePath)
        {
            // Composed rather than a literal absolute pack URI (S1075): a relative Uri resolves
            // against Application.ResourceAssembly, which defaults to the entry assembly, so it
            // would miss the resource under the test host, whose entry assembly is not
            // Fluence.Wpf.Demo. Naming this assembly explicitly by its own name keeps the pack URI
            // correct in both the shipped demo executable and the test host process.
            string assemblyName = typeof(GalleryColorsPage).Assembly.GetName().Name ?? "Fluence.Wpf.Demo";
            string packUri = $"pack://application:,,,/{assemblyName};component/{assemblyRelativePath}";
            BitmapImage bitmap = new(new Uri(packUri, UriKind.Absolute));
            bitmap.Freeze();
            return bitmap;
        }

        private static Controls.Border CreateDividerExample()
        {
            Controls.Border divider = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch };
            divider.SetResourceReference(Border.BorderBrushProperty, "DividerStrokeColorDefaultBrush");
            divider.SetResourceReference(Border.BorderThicknessProperty, "DemoColorTileSeparatorThickness");

            Controls.Border surface = CreateSurface("AcrylicBackgroundFillColorBaseBrush", "SurfaceStrokeColorDefaultBrush", OverlayRadius, SurfaceWidth, SurfaceHeight);
            surface.Child = divider;
            return surface;
        }

        private static Controls.Border CreateFocusExample()
        {
            Controls.Border content = CreateSurface(backgroundKey: null, "SurfaceStrokeColorDefaultBrush", OverlayRadius, SurfaceWidth, SurfaceHeight);
            content.Child = new TextBlock { Text = "Text", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            Controls.Border inner = new() { Child = content };
            inner.SetResourceReference(Border.BorderBrushProperty, "FocusStrokeColorInnerBrush");
            inner.SetResourceReference(Border.BorderThicknessProperty, "DemoColorExampleFocusStrokeThickness");
            inner.SetResourceReference(Border.CornerRadiusProperty, "DemoColorExampleFocusInnerCornerRadius");

            Controls.Border outer = new() { Child = inner };
            outer.SetResourceReference(Border.BorderBrushProperty, "FocusStrokeColorOuterBrush");
            outer.SetResourceReference(Border.BorderThicknessProperty, "DemoColorExampleFocusStrokeThickness");
            outer.SetResourceReference(Border.CornerRadiusProperty, "DemoColorExampleFocusOuterCornerRadius");
            return outer;
        }

        private static Controls.Border CreateLayerExample(string? backgroundKey, string layerKey)
        {
            Controls.Border layer = new() { HorizontalAlignment = HorizontalAlignment.Right };
            layer.SetResourceReference(WidthProperty, "DemoColorExampleLayerInnerWidth");
            layer.SetResourceReference(BackgroundProperty, layerKey);
            layer.SetResourceReference(Border.BorderBrushProperty, CardStroke);
            layer.SetResourceReference(Border.BorderThicknessProperty, "DemoColorExampleLayerInnerBorderThickness");

            Controls.Border surface = CreateSurface(backgroundKey, CardStroke, OverlayRadius, SurfaceWidth, SurfaceHeight);
            surface.Child = layer;
            return surface;
        }

        // The Gallery shows a TabViewItem over live Mica; here a tab-shaped surface is painted with the Mica Base Alt layer fallback.
        private static Controls.Border CreateTabExample()
        {
            Controls.Border tab = CreateSurface("LayerOnMicaBaseAltFillColorDefaultBrush", "ControlStrokeColorSecondaryBrush", ControlRadius, "DemoColorExampleTabItemWidth", "DemoColorExampleTabItemHeight");
            tab.Child = new TextBlock { Text = "Text", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            tab.SetResourceReference(MarginProperty, "DemoColorExampleTabItemMargin");
            return tab;
        }

        private static Controls.InfoBar CreateInfoBar()
        {
            return new Controls.InfoBar
            {
                Title = "Title",
                Message = "This is body text. Windows 11 is faster and more intuitive.",
                Severity = InfoBarSeverity.Error,
                IsOpen = true,
                IsClosable = false,
            };
        }

        private static Controls.Border CreateSurface(string? backgroundKey, string borderKey, string cornerRadiusKey, string widthKey, string heightKey)
        {
            Controls.Border surface = new();
            if (backgroundKey is not null)
            {
                surface.SetResourceReference(BackgroundProperty, backgroundKey);
            }

            surface.SetResourceReference(Border.BorderBrushProperty, borderKey);
            surface.SetResourceReference(Border.BorderThicknessProperty, SingleStroke);
            surface.SetResourceReference(Border.CornerRadiusProperty, cornerRadiusKey);
            surface.SetResourceReference(WidthProperty, widthKey);
            surface.SetResourceReference(HeightProperty, heightKey);
            return surface;
        }

        private sealed class ColorSectionData(string title, string? intro, ColorGroupData[] groups)
        {
            public string Title { get; } = title;

            public string? Intro { get; } = intro;

            public ColorGroupData[] Groups { get; } = groups;
        }

        private sealed class ColorGroupData(string? title, string description, string backgroundKey, string? foregroundKey, Func<UIElement>? example, ColorTileRowData[] rows, bool headingOnly = false)
        {
            public string? Title { get; } = title;

            /// <summary>
            /// True for a group the Gallery introduces with a plain heading rather than a
            /// ColorPageExample card, as its high contrast palettes are.
            /// </summary>
            public bool HeadingOnly { get; } = headingOnly;

            public string Description { get; } = description;

            public string BackgroundKey { get; } = backgroundKey;

            public string? ForegroundKey { get; } = foregroundKey;

            public Func<UIElement>? Example { get; } = example;

            public ColorTileRowData[] Rows { get; } = rows;
        }

        private sealed class ColorTileRowData(int rows, ColorTileData[] tiles)
        {
            public int Rows { get; } = rows;

            public ColorTileData[] Tiles { get; } = tiles;
        }

        private sealed class ColorTileData(string name, string explanation, string brushKey, string? foregroundKey, string? literalBackground = null, string? literalForeground = null)
        {
            public string Name { get; } = name;

            public string Explanation { get; } = explanation;

            public string BrushKey { get; } = brushKey;

            public string? ForegroundKey { get; } = foregroundKey;

            /// <summary>
            /// The literal tile colour, for the high contrast palettes the Gallery prints as fixed
            /// values rather than as live theme brushes. Null means the tile paints
            /// <see cref="BrushKey"/> from the current theme.
            /// </summary>
            public string? LiteralBackground { get; } = literalBackground;

            /// <summary>
            /// The literal text colour that pairs with <see cref="LiteralBackground"/>.
            /// </summary>
            public string? LiteralForeground { get; } = literalForeground;
        }
    }
}
