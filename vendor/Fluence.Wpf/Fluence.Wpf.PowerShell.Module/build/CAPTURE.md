# PowerShell screenshot capture

These PNGs show actual WPF controls rendered from the current PowerShell module by
`Fluence.Wpf.PowerShell.Module/build/Capture-Documentation.ps1`. The script loads the staged module
assemblies and opens each scene in a separate Windows PowerShell STA process.

The published captures use WPF `RenderTargetBitmap`. The module's Mica window surface is
transparent to offscreen rendering, so the script composites the window over the library's
`ApplicationBackgroundBrush`. The images show the control templates, title bar text and icon,
theme colors, accent colors, and selected states. They do not include the desktop, native DWM
backdrop, or a DWM shadow. The PNG outside the WPF window has no transparent shadow region.
The Windows screen capture API returned `The handle is invalid` in the capture host, including
when invoked through the standard screenshot helper. A desktop capture on an interactive Windows
session can use the script's `-CaptureMode Desktop` to record the real shadow if DWM draws it.

To regenerate all images after building and staging the module:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Fluence.Wpf.PowerShell.Module/build/Capture-Documentation.ps1
```

Scenes include message (normal and warning), form, image dialog, single and multiple list
selection, determinate and indeterminate progress, restart prompt, XAML hosted window, and purple
and green custom accents. Light and dark pairs are provided where the control benefits from a
direct comparison. The file name identifies the scene and theme.
