# Security policy

Fluence.Wpf is a Windows WPF control library with native Win32 and DWM interop. This policy covers the library and its PowerShell module.

## Supported versions

Security fixes are considered for the current main branch and the latest tagged release. Older preview tags do not receive separate backports. Consult [GitHub releases](https://github.com/sintaxasn/Fluence.Wpf/releases) for the latest published version. The library's supported target frameworks are `net472`, `net8.0-windows10.0.26100.0`, and `net10.0-windows10.0.26100.0`.

## Report a vulnerability

Use GitHub's private vulnerability reporting from this repository's **Security** tab, or email [security@fluencewpf.com](mailto:security@fluencewpf.com). Do not include exploit details or secrets in public issues.

Include the affected version or commit, Windows version, target framework or PowerShell host, a minimal reproduction, expected and observed behavior, and the impact. If native interop or process termination is involved, include the relevant exception or crash details.

Visual defects and ordinary usage questions belong in [GitHub issues](https://github.com/sintaxasn/Fluence.Wpf/issues). See [SUPPORT.md](SUPPORT.md) for the information to include.
