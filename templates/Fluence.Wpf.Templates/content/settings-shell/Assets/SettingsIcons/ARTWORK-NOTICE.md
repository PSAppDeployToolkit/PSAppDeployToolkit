# Settings category artwork

The four category images are bundled WPF resources. They are loaded from this application's assembly at runtime; no Windows App SDK component or icon extraction is used by the application.

| Image prefix | Source | Terms |
| --- | --- | --- |
| `General` | `ContrastStandard/System.png` in the installed Windows 11 `Windows.UI.SettingsAppThreshold.pri`, OS build 26220 | Microsoft Windows artwork. No explicit redistribution grant was found during the asset research. These images are **not** covered by the MIT notice below or by the BSD license on the sample code. |
| `Appearance` | `ContrastStandard/Personalize.png` in the same installed Windows Settings PRI | Same provenance and limitation as `General`. |
| `Notifications` | [Microsoft Fluent Emoji Bell Color SVG](https://github.com/microsoft/fluentui-emoji/blob/1ffb34c752ecf5d402f04cfb4b392c77f57c54bc/assets/Bell/Color/bell_color.svg) | Rendered PNG derivative of Microsoft Fluent Emoji under the accompanying MIT notice. |
| `About` | [Microsoft Fluent Emoji Information Color SVG](https://github.com/microsoft/fluentui-emoji/blob/1ffb34c752ecf5d402f04cfb4b392c77f57c54bc/assets/Information/Color/information_color.svg) | Rendered PNG derivative of Microsoft Fluent Emoji under the accompanying MIT notice. |

Each prefix has native 20, 25, 30, 40, and 80 pixel PNGs for 100%, 125%, 150%, 200%, and 400% display scale. The two Windows images retain the exact bytes extracted from the OS resource at each scale. The two Fluent Emoji images were rendered from the pinned source SVGs at each size during template development.

These category assets are separate from the monochrome setting row glyphs. The title bar has no bundled icon. If you redistribute a generated application, assess the rights to its two Windows Settings images for your distribution context.
