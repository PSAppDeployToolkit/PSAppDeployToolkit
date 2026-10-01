# ![PSAppDeployToolkit](https://github.com/user-attachments/assets/acfafa06-75ef-4988-aea6-5711fd9b6fc4)

![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/psappdeploytoolkit?logoSize=auto&label=PowerShell%20Gallery)
![GitHub](https://img.shields.io/github/downloads/psappdeploytoolkit/psappdeploytoolkit/total?label=GitHub)
![Main Branch Status](https://img.shields.io/github/check-runs/psappdeploytoolkit/psappdeploytoolkit/main?label=main)
![Develop Branch Status](https://img.shields.io/github/check-runs/psappdeploytoolkit/psappdeploytoolkit/develop?label=develop)
![#psappdeploytoolkit Discord Chat](https://img.shields.io/discord/618712310185197588?label=Discord%20Chat)

## 🚀 Enterprise App Deployment, Simplified

PSAppDeployToolkit is a PowerShell-based, open-source framework for Windows software deployment that integrates seamlessly with existing deployment solutions (e.g. Microsoft Intune, ConfigMgr, Tanium, BigFix etc.) to enhance the software deployment process. It achieves this by combining a battle-tested prescriptive workflow, an extensive library of functions for common deployment tasks, a customizable branded User Experience, and full-fidelity logging to produce consistently high deployment success rates.

### ✨ Key Features

- **Seamless Integration**: Works with all major deployment solutions
- **User Experience**: Beautiful, customizable UI with both Fluent and Classic interfaces
- **Flexible Deployment**: Handle complex deployment scenarios with ease
- **Reliable**: Battle-tested in enterprise environments
- **Extensible**: Rich library of functions for common deployment tasks

## 📸 Screenshots

| Light Mode | Dark Mode |
|---------------------|-----------------|
| ![LightMode](https://github.com/user-attachments/assets/d3ea4c5a-486a-48d9-86cf-c3ddf391468a) | ![DarkMode](https://github.com/user-attachments/assets/37cf1759-f211-4cf1-a686-7897a7306a27) |

| Custom Accent Light | Custom Accent Dark |
|---------------------|-----------------|
| ![CustomLightMode](https://github.com/user-attachments/assets/c092999f-46a2-43f6-bd28-bc2bdcd03b76) | ![CustomDarkMode](https://github.com/user-attachments/assets/26be16d2-f13e-491d-af86-72a169200f27) |

## 🖥️ Whats New in v4.2 RC1 - 2026-08-20

### Highlights

- Major code refactoring to clean up and optimise the code base. Everything now runs faster and the module is significantly smaller in size.
- iNKORE WPF library replaced by [Fluence](https://github.com/sintaxasn/Fluence.Wpf) (created and maintained by PSAppDeployToolkit founder [Dan Cunningham](https://github.com/sintaxasn)).
- A more streamlined default `Invoke-AppDeployToolkit.ps1` template. ZeroConfig code has been removed from the default template and is now a separate download, or can be generated via `New-ADTTemplate -ZeroConfig`.
- [New-ADTTemplate](https://psappdeploytoolkit.com/docs/reference/functions/New-ADTTemplate) now allows you to generate an entire deployment package in a single command by specifying session properties, config, assets, files, and script blocks.
- [Show-ADTInstallationPrompt](https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationPrompt) now supports secured text inputs and dropdown selection boxes.
- [Show-ADTInstallationRestartPrompt](https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationRestartPrompt) now supports a cancel button.
- You can now configure a different accent color for dark mode.
- Dialogs now fallback to default image if the specified asset is not found, also images can be encoded as Base64 strings instead of supplying file paths.
- Tray notification icon shown whenever balloon tips / toasts are invoked.
- UIAccess to allow the UI to overlay the Autopilot setup screen.
- Ability to test if user is in focus mode.
- Functions added to add/remove fonts.
- Copy-ADTContentToCache now applies administrator permissions to the shared cache and has a separate cache location for non-admins.
- Descriptions for all known MSI error codes now included in logging output.
- All WMI dependencies removed, so the toolkit can run on devices with WMI corruption.
- Ability to run custom functions whenever writing to the log or when a deployment is deferred via [Add-ADTModuleCallback](https://psappdeploytoolkit.com/docs/reference/functions/Add-ADTModuleCallback).
- All time-based parameters now accept TimeSpan objects as well as interpreting integers as seconds.
- `-WhatIf` support added throughout to test changes non-destructively.
- AI and static analysis tools used to ensure code quality (CodeQL, Meziantou.Analyzer, Microsoft.CodeAnalysis.BannedApiAnalyzers, Microsoft.Extensions.StaticAnalysis, Roslynator.Analyzers).
- Pester tests updated for Pester v6 (thanks [@nohwnd!](https://github.com/nohwnd))

Check the [releases](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/releases) for further information.

## 🚀 Getting Started

### Prerequisites

- Windows 10/11
- PowerShell 5.1 or later
- .NET Framework 4.7.2 or later

### Downloading

- [Getting Started Guidance](https://psappdeploytoolkit.com/docs/getting-started/download)
- [PowerShell Gallery](https://www.powershellgallery.com/packages/PSAppDeployToolkit)
- [GitHub Releases](https://github.com/psappdeploytoolkit/psappdeploytoolkit/releases)

## 📚 Documentation

For detailed documentation, examples, and advanced usage, visit our [official documentation](https://psappdeploytoolkit.com/docs/introduction)

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/.github/CONTRIBUTING.md) for details

## 📄 License

This project is licensed under the [GNU Lesser General Public License](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/COPYING.Lesser)

## Important Links

### PSAppDeployToolkit

- [Homepage](https://psappdeploytoolkit.com)
- [Latest News](https://psappdeploytoolkit.com/blog)
- [Documentation](https://psappdeploytoolkit.com/docs/introduction)
- [Function & Variable References](https://psappdeploytoolkit.com/docs/reference)
- [PowerShell Gallery](https://www.powershellgallery.com/packages/PSAppDeployToolkit)
- [GitHub Releases](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/releases)

### Community

- [Discourse Forum](https://discourse.psappdeploytoolkit.com/)
- [Discord Chat](https://discord.com/channels/618712310185197588/627204361545842688)
- [Reddit](https://reddit.com/r/psadt)

### GitHub

- [Issues](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues)
- [Security Policy](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/security)
- [Contributer Guidelines](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/.github/CONTRIBUTING.md)


## 🌐 Web Resources & Aesthetic Symbols Index
- [CUTE BUNNY RABBIT FACE](https://chibi-emoticon-vault-42.pages.dev/symbol/cute-bunny-rabbit-face/)
- [CURLY RIBBON LOOP](https://kawaii-kaomoji-hub-95.pages.dev/symbol/curly-ribbon-loop/)
- [SYM 1D426](https://cyber-clan-tags-90.pages.dev/symbol/sym-1d426/)
- [ARIES ZODIAC RAM](https://coquette-aesthetic-symbols-31.pages.dev/symbol/aries-zodiac-ram/)
- [SYM 1F61B](https://daintystar-font-studio-48.pages.dev/symbol/sym-1f61b/)
- [SYM 1F924](https://kawaii-kaomoji-hub-88.pages.dev/symbol/sym-1f924/)
- [HEARTS](https://kawaii-kaomoji-hub-86.pages.dev/ja/hearts/)
- [HOLLOW STAR](https://gothic-bio-fonts-10.pages.dev/symbol/hollow-star/)
- [ES](https://vintage-rune-symbols-92.pages.dev/es/)
- [SYM 2639 FE0F](https://baroque-text-decor-84.pages.dev/symbol/sym-2639-fe0f/)
- [SYM 1F626](https://gothic-bio-fonts-10.pages.dev/symbol/sym-1f626/)
- [SYM 1F916](https://occult-aesthetic-symbols-26.pages.dev/symbol/sym-1f916/)
- [SYM 1D412](https://sleek-mono-fonts-61.pages.dev/symbol/sym-1d412/)
- [MUSIC WEATHER](https://kawaii-kaomoji-hub-16.pages.dev/ja/music-weather/)
- [BRACKETS](https://clean-unicode-text-35.pages.dev/pt/brackets/)
- [LITTLE CAT PAWS KAOMOJI](https://scholarly-type-fonts-40.pages.dev/symbol/little-cat-paws-kaomoji/)
- [SYM 1F635](https://neon-hacker-text-25.pages.dev/symbol/sym-1f635/)
- [SYM 2688](https://scholarly-type-fonts-40.pages.dev/symbol/sym-2688/)
- [CIRCLED STAR](https://chibi-emoticon-fonts-14.pages.dev/symbol/circled-star/)
- [SYM 2687](https://manga-speech-symbols-65.pages.dev/symbol/sym-2687/)
- [SYM 26D6](https://theeduplaycampen.pages.dev/symbol/sym-26d6/)
- [SYM 1D46F](https://neon-tech-unicode-43.pages.dev/symbol/sym-1d46f/)
- [SYM 267A](https://cyber-clan-tags-69.pages.dev/symbol/sym-267a/)
- [SYM 26A3](https://lace-bow-symbols-18.pages.dev/symbol/sym-26a3/)
- [SYM 1FAE0](https://noir-poet-unicode-63.pages.dev/symbol/sym-1fae0/)
- [SYM 1D47C](https://scholarly-type-fonts-40.pages.dev/symbol/sym-1d47c/)
- [SKULL AND CROSSBONES](https://chibi-emoticon-fonts-14.pages.dev/symbol/skull-and-crossbones/)
- [SYM 1F63A](https://gothic-bio-fonts-14.pages.dev/symbol/sym-1f63a/)
- [SYM 2617](https://kawaii-kaomoji-hub-16.pages.dev/symbol/sym-2617/)
- [SYM 26EE](https://gothic-bio-fonts-10.pages.dev/symbol/sym-26ee/)
- [OPEN CENTRE STAR](https://vintage-angel-text-38.pages.dev/symbol/open-centre-star/)
- [SYM 2616](https://clean-unicode-text-35.pages.dev/symbol/sym-2616/)
- [SYM 2628](https://angelic-bio-symbols-59.pages.dev/symbol/sym-2628/)
- [SPRING TULIP BLOSSOM](https://gothic-bio-fonts-10.pages.dev/symbol/spring-tulip-blossom/)
- [SYM 26B6](https://vintage-scroll-text-23.pages.dev/symbol/sym-26b6/)
- [KHANDA EMBLEM](https://gothic-bio-fonts-10.pages.dev/symbol/khanda-emblem/)
- [PT](https://angelic-bio-symbols-59.pages.dev/pt/)
- [SYM 26B9](https://cyber-clan-tags-69.pages.dev/symbol/sym-26b9/)
- [SYM 1FAE5](https://aesthetic-sparkle-text-48.pages.dev/symbol/sym-1fae5/)
- [ARROWS LINES](https://cyberpunk-clan-tags-49.pages.dev/vi/arrows-lines/)
- [SYM 1F976](https://noir-poet-unicode-63.pages.dev/symbol/sym-1f976/)
- [SYM 2682](https://vintage-angel-text-38.pages.dev/symbol/sym-2682/)
- [SYM 2678](https://vintage-rune-symbols-92.pages.dev/symbol/sym-2678/)
- [SWIMMING FISH RIGHT](https://gothic-bio-fonts-10.pages.dev/symbol/swimming-fish-right/)
- [ROTATED FLORAL HEART](https://vintage-angel-text-38.pages.dev/symbol/rotated-floral-heart/)
- [GAMING WEAPONS](https://vintage-angel-text-38.pages.dev/ru/gaming-weapons/)
- [LATIN CROSS FAITH](https://vintage-angel-text-38.pages.dev/symbol/latin-cross-faith/)
- [SYM 1D440](https://neon-matrix-symbols-57.pages.dev/symbol/sym-1d440/)
- [SYM 1D48B](https://clean-mono-fonts-64.pages.dev/symbol/sym-1d48b/)
- [SYM 1D44F](https://neon-tech-unicode-43.pages.dev/symbol/sym-1d44f/)
- [SYM 26C2](https://neon-matrix-symbols-57.pages.dev/symbol/sym-26c2/)
- [SYM 1D413](https://pastel-manga-symbols-57.pages.dev/symbol/sym-1d413/)
- [MUSIC WEATHER](https://cyber-clan-tags-69.pages.dev/es/music-weather/)
- [GAMING WEAPONS](https://vintage-script-symbols-65.pages.dev/pt/gaming-weapons/)
- [LATIN CROSS HEAVY](https://minimal-star-symbols-35.pages.dev/symbol/latin-cross-heavy/)
- [SYM 1F626](https://occult-rune-symbols-64.pages.dev/symbol/sym-1f626/)
- [SYM 2738](https://coquette-heart-text-40.pages.dev/symbol/sym-2738/)
- [SYM 2732](https://mecha-text-vault-91.pages.dev/symbol/sym-2732/)
- [SYM 26E9](https://baroque-text-decor-84.pages.dev/symbol/sym-26e9/)
- [SYM 1D416](https://vintage-scholar-text-15.pages.dev/symbol/sym-1d416/)
- [SYM 1F615](https://angelic-bio-symbols-59.pages.dev/symbol/sym-1f615/)
- [SYM 1F618](https://simple-line-kaomoji-30.pages.dev/symbol/sym-1f618/)
- [ROBLOX NAMES](https://vintage-angel-text-38.pages.dev/vi/roblox-names/)
- [RIGHT MATHEMATICAL WHITE SQUARE BRACKET](https://glitch-mecha-kaomoji-69.pages.dev/symbol/right-mathematical-white-square-bracket/)
- [SYM 1D420](https://cyberpunk-clan-tags-49.pages.dev/symbol/sym-1d420/)
- [SYM 26E3](https://gothic-bio-fonts-61.pages.dev/symbol/sym-26e3/)
- [SYM 26B5](https://sleek-mono-fonts-61.pages.dev/symbol/sym-26b5/)
- [WINGED ANGELIC COQUETTE HEART](https://scholarly-type-fonts-40.pages.dev/symbol/winged-angelic-coquette-heart/)
- [SYM 1F920](https://mecha-crosshair-symbols-38.pages.dev/symbol/sym-1f920/)
- [SYM 2632](https://ribbon-bow-unicode-18.pages.dev/symbol/sym-2632/)
- [ZODIAC CELESTIAL](https://minimal-star-symbols-74.pages.dev/ru/zodiac-celestial/)
- [SYM 268E](https://minimal-star-symbols-26.pages.dev/symbol/sym-268e/)
- [SYM 1F603](https://gothic-bio-fonts-10.pages.dev/symbol/sym-1f603/)
- [KAOMOJI](https://synthwave-text-vault-95.pages.dev/pt/kaomoji/)
- [SYM 1F979](https://minimal-star-symbols-43.pages.dev/symbol/sym-1f979/)
- [SYM 1D49B](https://anime-sparkle-text-91.pages.dev/symbol/sym-1d49b/)
- [SYM 2733](https://vintage-scholar-text-15.pages.dev/symbol/sym-2733/)
- [SYM 263A](https://synthwave-text-vault-95.pages.dev/symbol/sym-263a/)
- [SYM 1D439](https://ribbon-bow-unicode-18.pages.dev/symbol/sym-1d439/)
- [BEAMED EIGHTH NOTES](https://minimal-star-symbols-35.pages.dev/symbol/beamed-eighth-notes/)
- [SYM 1F62B](https://gothic-bio-fonts-61.pages.dev/symbol/sym-1f62b/)
- [SYM 1F63C](https://zen-unicode-hub-94.pages.dev/symbol/sym-1f63c/)
- [SYM 1F978](https://anime-sparkle-text-91.pages.dev/symbol/sym-1f978/)
- [PT](https://neon-tech-unicode-43.pages.dev/pt/)
- [ARROWS LINES](https://minimal-star-symbols-35.pages.dev/pt/arrows-lines/)
- [SYM 2722](https://coquette-aesthetic-symbols-72.pages.dev/symbol/sym-2722/)
- [LIBRA ZODIAC SCALES](https://minimal-star-symbols-40.pages.dev/symbol/libra-zodiac-scales/)
- [SYM 26C2](https://mecha-gamer-fonts-53.pages.dev/symbol/sym-26c2/)
- [ANGEL WINGS HEART](https://mecha-crosshair-symbols-38.pages.dev/symbol/angel-wings-heart/)
- [SYM 1F644](https://angelic-bio-symbols-59.pages.dev/symbol/sym-1f644/)
- [SYM 1D44D](https://clean-aesthetic-fonts-90.pages.dev/symbol/sym-1d44d/)
- [SYM 267D](https://vintage-scroll-text-23.pages.dev/symbol/sym-267d/)
- [NATURE FLOWERS](https://gothic-bio-fonts-10.pages.dev/ru/nature-flowers/)
- [SYM 2663](https://minimal-star-symbols-87.pages.dev/symbol/sym-2663/)
- [SYM 1D445](https://neon-tech-unicode-43.pages.dev/symbol/sym-1d445/)
- [SYM 2611](https://minimal-star-symbols-35.pages.dev/symbol/sym-2611/)
- [MUSIC WEATHER](https://gothic-bio-fonts-10.pages.dev/music-weather/)
- [SYM 1D435](https://scholar-rune-symbols-77.pages.dev/symbol/sym-1d435/)
- [SYM 2621](https://noir-poet-unicode-63.pages.dev/symbol/sym-2621/)
- [SHADOWED WHITE STAR](https://minimal-star-symbols-35.pages.dev/symbol/shadowed-white-star/)
- [SYM 1F61A](https://vintage-scroll-text-23.pages.dev/symbol/sym-1f61a/)
- [FREEFIRE NAMES](https://vintage-angel-text-38.pages.dev/es/freefire-names/)
- [SYM 1F642 200D 2195 FE0F](https://synthwave-text-vault-95.pages.dev/symbol/sym-1f642-200d-2195-fe0f/)
- [SYM 1F970](https://sleek-dot-symbols-31.pages.dev/symbol/sym-1f970/)
- [SYM 1F635](https://minimal-star-symbols-26.pages.dev/symbol/sym-1f635/)
- [SYM 2611](https://noir-poet-unicode-63.pages.dev/symbol/sym-2611/)
- [SYM 1F631](https://minimal-star-symbols-40.pages.dev/symbol/sym-1f631/)
- [SYM 1D44B](https://ribbon-bow-unicode-18.pages.dev/symbol/sym-1d44b/)
- [TIKTOK CAPTIONS](https://vintage-angel-text-38.pages.dev/vi/tiktok-captions/)
- [ZODIAC CELESTIAL](https://gothic-bio-fonts-10.pages.dev/zodiac-celestial/)
- [HOLLOW STAR](https://scholar-rune-symbols-77.pages.dev/symbol/hollow-star/)
- [SYM 26B7](https://neon-matrix-symbols-57.pages.dev/symbol/sym-26b7/)
- [BORDERS DIVIDERS](https://angelic-ribbon-text-78.pages.dev/vi/borders-dividers/)
- [SYM 1D490](https://alchemist-symbol-hub-29.pages.dev/symbol/sym-1d490/)
- [SYM 1D434](https://academic-latin-text-43.pages.dev/symbol/sym-1d434/)
- [FLOWER GIRL SMILE KAOMOJI](https://aesthetic-sparkle-text-48.pages.dev/symbol/flower-girl-smile-kaomoji/)
- [SYM 26B4](https://gothic-bio-fonts-81.pages.dev/symbol/sym-26b4/)
- [SYM 1D41D](https://kawaii-kaomoji-hub-89.pages.dev/symbol/sym-1d41d/)
- [SYM 1D442](https://chibi-emoticon-fonts-14.pages.dev/symbol/sym-1d442/)
- [SYM 2627](https://vintage-scholar-text-15.pages.dev/symbol/sym-2627/)
- [KAOMOJI](https://angelic-ribbon-text-78.pages.dev/pt/kaomoji/)
- [SYM 26C0](https://coquette-heart-text-40.pages.dev/symbol/sym-26c0/)
- [QUARTER MUSICAL NOTE](https://dark-poetry-symbols-18.pages.dev/symbol/quarter-musical-note/)
- [SYM 1D48C](https://daintystar-font-studio-48.pages.dev/symbol/sym-1d48c/)
- [SYM 1F92F](https://pastel-manga-symbols-57.pages.dev/symbol/sym-1f92f/)
- [SYM 26A5](https://cyber-clan-tags-69.pages.dev/symbol/sym-26a5/)
- [FLUTTERING BUTTERFLY](https://mecha-gamer-fonts-53.pages.dev/symbol/fluttering-butterfly/)
- [SYM 1F617](https://minimal-star-symbols-74.pages.dev/symbol/sym-1f617/)
- [SYM 2612](https://minimal-star-symbols-35.pages.dev/symbol/sym-2612/)
- [RIGHT HEAVY BRACKET BOX](https://gothic-bio-fonts-10.pages.dev/symbol/right-heavy-bracket-box/)
