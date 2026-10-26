# Fluence brand assets

`Fluence.Wpf.af` is the editable design file. The exported filenames use the visual role and target theme. `Light` has dark lettering for a light background; `Dark` has light lettering for a dark background. The icon mark itself has no theme variant.

| Role | Files |
| --- | --- |
| Color icon | `Fluence_Icon.svg`, `Fluence_Icon.png`, `Fluence_Icon_16.png`, `Fluence_Icon_32.png`, `Fluence_Icon_64.png`, `Fluence_Icon_128.png`, `Fluence_Icon_256.png` |
| Application icon | `Fluence_Icon.ico` |
| Plated icon | `Fluence_Icon_Background_Light.svg`, `Fluence_Icon_Background_Light_256.png`, `Fluence_Icon_Background_Light_512.png`, and the corresponding `Dark` files |
| Horizontal lockup | `Fluence_Lockup_Horizontal_{Light,Dark,Rainbow,Gradient}.{png,svg}` |
| Stacked lockup | `Fluence_Lockup_Stacked_{Light,Dark,Rainbow,Gradient}.{png,svg}` |
| Supplied tagline banner | `Fluence_Banner_{Light,Dark}.{png,svg}` |

The ICO contains the supplied 16, 32, 64, 128, and 256 pixel PNG frames byte-for-byte. The incoming `logo_*_horizontal` files are side-by-side lockups; the source files named `logoo_*_hoeizontal` are stacked layouts despite their spelling. `Rainbow` uses individual letter colors, while `Gradient` uses a continuous text gradient.

The gallery homepage uses the Light and Dark `Fluence_Banner_*.svg` artwork. WPF does not load SVG through its built-in image codecs, so `tools/BannerDrawingGenerator` outlines the installed source fonts into native WPF `DrawingImage` resources in `Fluence.Wpf.Demo/Resources/BannerDrawings.xaml`. It extracts each SVG's embedded fan, wordmark, and tagline PNGs byte-for-byte; the fan and tagline are shared between themes, while the wordmark has separate Light and Dark resources. Run `dotnet run --project tools/BannerDrawingGenerator/BannerDrawingGenerator.csproj -c Release -- <repository-root>` after changing either banner SVG. Generation requires the registered Nebula Sans Book, Medium, Semibold, and Bold faces and Nachlieli CLM Bold face; the generated gallery resources do not require those fonts on consumer machines. The supplied banners state .NET 4.7.2, 8, and 10+ and PowerShell 5.1 and 7.4+. The PNG banner exports are retained as supplied references and are not used for the gallery hero.

The library keeps its existing `FluenceIconBrandDrawingImage`, plated icon, and `FluenceHeaderLightDrawingImage` / `FluenceHeaderDarkDrawingImage` resource keys. The color icon DrawingImage uses native WPF geometry translated from `Fluence_Icon.svg`; header DrawingImages wrap the corresponding supplied PNG lockups.
