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
