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

## 🖥️ What's New in v4.2 RC2 - 2026-10-01

### Highlights

- `Show-ADTInstallationRestartPrompt` improvements:
  - Some parameters have been renamed to make their behaviour clearer (with previous names retained as aliases)
  - `-Force` switch support to show the prompt in Silent/NonInteractive mode
  - `-NoForceCloseApps` switch to omit the `/f` switch when calling `shutdown.exe` for more graceful restarts
  - Parameter sets fixed so that all UI options are still available when suppling `-AllowSilentRestart` (previously named `-SilentRestart`)
  - Default countdown durations can now be set in `config.psd1`.
- `New-ADTTemplate` can now rename the `Invoke-AppDeployToolkit` launcher files via `-LauncherName`, and omit content (Assets, Config, Extensions, Files, Module, Strings, SupportFiles) via `-ExcludeContent`.
- `Show-ADTInstallationPrompt` `-SecureInput` now returns the entered text as a `SecureString`.
- `Show-ADTInstallationWelcome` `-CheckDiskSpace` / `-RequiredDiskSpace` now work without an active deployment session.
- Group Policy now supports version-specific policy keys (`HKLM\SOFTWARE\Policies\PSAppDeployToolkit\4.2`), so 4.2-only settings such as Base64 assets don't break 4.0/4.1 deployments on the same device. The ADMX/ADML templates have been updated to match, and empty policy values can now clear values set in a deployment's config.
- New `PathsBasedOnSystemContext` config option to use the `NoAdminRights` paths whenever the caller isn't the LocalSystem account, rather than whenever the caller isn't an admin.
- Numerous fixes to the v3 compatibility layer (`AppDeployToolkitMain.ps1`) to better match v3 behaviour.
- Extensive new C# and Pester unit test coverage.

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
