# ![PSAppDeployToolkit](https://github.com/user-attachments/assets/acfafa06-75ef-4988-aea6-5711fd9b6fc4)

![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/psappdeploytoolkit?logoSize=auto&label=PowerShell%20Gallery)
![GitHub](https://img.shields.io/github/downloads/psappdeploytoolkit/psappdeploytoolkit/total?label=GitHub)
![Main Branch Status](https://img.shields.io/github/check-runs/psappdeploytoolkit/psappdeploytoolkit/main?label=main)
![Develop Branch Status](https://img.shields.io/github/check-runs/psappdeploytoolkit/psappdeploytoolkit/develop?label=develop)
![#psappdeploytoolkit Discord Chat](https://img.shields.io/discord/618712310185197588?label=Discord%20Chat)

PSAppDeployToolkit is a framework for deploying applications in a business / corporate environment. It provides a set of well-defined functions for common application deployment tasks, as well as user interface elements for end user interaction during a deployment. It simplifies the complex scripting challenges of deploying applications in the enterprise, provides a consistent deployment experience for your end users and as a result of this, improves the overall success rate of your deployments.

### Features

PSAppDeployToolkit allows you to encapsulate a typical Windows Installer MSI or Setup executable to provide it with enhanced capabilities.

- Validate prerequisites such as dependencies on minimum software versions
- Ensure that in-use applications are closed and prevent reopening during the deployment
- Check with the user if now is a good time to start an install and allow them to defer
- Uninstall existing applications and perform clean up operations
- Capture any important settings that may be required for an upgrade or migration
- Run the installation silently and capture logs in the event of an issue
- Run post-installation configuration tasks to customize for your environment
- Prompt the user to restart their computer if required, immediately, on a timer and with a deadline

## Screenshots

<img src="https://github.com/user-attachments/assets/e74f905a-9999-480a-90ba-78f1dfcd41f9" width="49%" height="49%">
<img src="https://github.com/user-attachments/assets/41299581-3b63-49f0-a9de-6852c1c17257" width="49%" height="49%"">

## What's New in v4

- Modern Fluent user interface
- Digitally signed PowerShell module
- All C# code is now compiled
- Codebase completely refactored and optimized
- Complete removal of VBScript code
- Strongly typed and defined object types, no more PSCustomObjects, etc
- Defensively coded to ensure security and reliability
- Now provides PowerShell 7 and ARM support
- Extensions supported as supplemental modules
- Custom action support for extensions on deployment start/finish
- Support for overriding config via the registry
- Backwards-compatibility with v3 deployment scripts

## Getting Started / Downloading

- [Getting Started Guidance](https://psappdeploytoolkit.com/docs/getting-started/download)
- [PowerShell Gallery](https://www.powershellgallery.com/packages/PSAppDeployToolkit)
- [GitHub Latest Release](https://github.com/psappdeploytoolkit/psappdeploytoolkit/releases)

## Important Links

### PSAppDeployToolkit

- [Homepage](https://psappdeploytoolkit.com)
- [Latest News](https://psappdeploytoolkit.com/blog)

- [Documentation](https://psappdeploytoolkit.com/docs)
- [Function & Variable References](https://psappdeploytoolkit.com/docs/reference)
- [GitHub Latest Release](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/releases)

### Community

- [Discourse Forum](https://discourse.psappdeploytoolkit.com/)
- [Discord Chat](https://discord.com/channels/618712310185197588/627204361545842688)
- [Reddit](https://reddit.com/r/psadt)

### GitHub

- [Issues](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues)
- [Security Policy](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/security)
- [Contributer Guidelines](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/.github/CONTRIBUTING.md)

## License

The PowerShell App Deployment Tool is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more details.
