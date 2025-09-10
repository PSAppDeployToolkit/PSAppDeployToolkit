namespace PSADT.Types
{
    /// <summary>
    /// Represents a URL shortcut.
    /// </summary>
    public sealed class ShortcutUrl(string path, string targetPath, string? iconLocation, string? iconIndex) : ShortcutBase(path, targetPath, iconLocation, iconIndex);
}
