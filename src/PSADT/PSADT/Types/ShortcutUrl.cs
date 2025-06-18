namespace PSADT.Types
{
    /// <summary>
    /// Represents a URL shortcut.
    /// </summary>
    public sealed record ShortcutUrl(string? path = null, string? targetPath = null, string? iconIndex = null, string? iconLocation = null) : ShortcutBase(path, targetPath, iconIndex, iconLocation);
}
