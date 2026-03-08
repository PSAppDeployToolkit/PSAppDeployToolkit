/*
 * Copyright (C) 2026 Devicie Pty Ltd. All rights reserved.
 * 
 * This file is part of PSAppDeployToolkit. 
 * 
 * PSAppDeployToolkit is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, either version 3
 * of the License, or (at your option) any later version.
 * 
 * PSAppDeployToolkit is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * 
 * See the GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with PSAppDeployToolkit. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.PropertiesSystem;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.ShortcutManagement
{
    /// <summary>
    /// Provides a managed wrapper around the Windows Shell Link (shortcut) COM interface.
    /// This class enables creating, loading, modifying, and saving Windows shortcut (.lnk) files.
    /// </summary>
    /// <remarks>
    /// This class wraps the <c>IShellLinkW</c>, <c>IShellLinkDataList</c>, <c>IPersistFile</c>,
    /// and <c>IPropertyStore</c> COM interfaces to provide full access to shell link properties,
    /// flags, and extended properties such as AppUserModelID.
    /// </remarks>
    public sealed class ShellLinkFile : IDisposable
    {
        /// <summary>
        /// Creates a new, empty shell link.
        /// </summary>
        /// <returns>A new <see cref="ShellLinkFile"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ShellLinkFile New()
        {
            return new();
        }

        /// <summary>
        /// Creates a new shell link with the specified target path.
        /// </summary>
        /// <param name="targetPath">The target path for the shortcut.</param>
        /// <returns>A new <see cref="ShellLinkFile"/> instance with the target path set.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ShellLinkFile New(string targetPath)
        {
            return new() { TargetPath = targetPath };
        }

        /// <summary>
        /// Loads an existing shortcut file in read-only mode.
        /// </summary>
        /// <param name="filePath">The path to the shortcut file to load.</param>
        /// <returns>A new <see cref="ShellLinkFile"/> instance loaded from the specified file.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        /// <remarks>Use <see cref="Load(string, Interop.STGM)"/> with <see cref="Interop.STGM.STGM_READWRITE"/> if you need to modify the shortcut.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ShellLinkFile Load(string filePath)
        {
            return Load(filePath, Interop.STGM.STGM_READ);
        }

        /// <summary>
        /// Loads an existing shortcut file with the specified storage mode.
        /// </summary>
        /// <param name="filePath">The path to the shortcut file to load.</param>
        /// <param name="storageMode">The storage mode flags.</param>
        /// <returns>A new <see cref="ShellLinkFile"/> instance loaded from the specified file.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ShellLinkFile Load(string filePath, Interop.STGM storageMode)
        {
            return new(filePath, storageMode);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellLinkFile"/> class for creating a new shortcut.
        /// </summary>
        private ShellLinkFile()
        {
            _shellLink = (IShellLinkW)new ShellLink();
            _storageMode = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellLinkFile"/> class by loading an existing shortcut file.
        /// </summary>
        /// <param name="filePath">The path to the shortcut file to load.</param>
        /// <param name="storageMode">The storage mode flags.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        private ShellLinkFile(string filePath, Interop.STGM storageMode)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified shortcut file does not exist.", filePath);
            }
            IShellLinkW shellLink = (IShellLinkW)new ShellLink();
            try
            {
                ((IPersistFile)shellLink).Load(filePath, (STGM)storageMode);
                _shellLink = shellLink;
                _storageMode = storageMode;
            }
            catch
            {
                _ = Marshal.FinalReleaseComObject(shellLink);
                throw;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ShellLinkFile"/> class.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ~ShellLinkFile()
        {
            Dispose(false);
        }

        /// <summary>
        /// Saves the shortcut to the currently loaded file path.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no file path has been set or the file was opened read-only.</exception>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public void Save()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Cannot save a shortcut that was loaded with read-only access. Use Load(filePath, STGM.STGM_READWRITE) to enable modifications.");
            }
            if (FilePath is not string currentFile)
            {
                throw new InvalidOperationException("No file path has been set. Use Save(string) to specify a path.");
            }
            Save(currentFile);
        }

        /// <summary>
        /// Saves the shortcut to the specified file path.
        /// </summary>
        /// <param name="filePath">The path where the shortcut file should be saved.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to overwrite the source file that was opened read-only.</exception>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public void Save(string filePath)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            if (IsReadOnly && string.Equals(Path.GetFullPath(filePath), FilePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot overwrite a shortcut file that was loaded with read-only access. Use Load(filePath, STGM.STGM_READWRITE) to enable modifications.");
            }
            ((IPersistFile)_shellLink).Save(filePath, true);
        }

        /// <summary>
        /// Gets a value indicating whether the current storage mode is read-only, preventing any write operations.
        /// </summary>
        /// <remarks>Use this property to determine if modifications to the storage are allowed. When <see
        /// langword="true"/>, attempts to write or update the storage will not be permitted.</remarks>
        private bool IsReadOnly => _storageMode is Interop.STGM mode && (mode & (Interop.STGM.STGM_WRITE | Interop.STGM.STGM_READWRITE)) == 0;

        /// <summary>
        /// Gets a value indicating whether the current object can be saved.
        /// </summary>
        /// <remarks>This property returns <see langword="true"/> if the object is not in a read-only
        /// state. Use this property to determine if changes can be persisted.</remarks>
        public bool CanSave => !IsReadOnly;

        /// <summary>
        /// Gets the path of the currently loaded shortcut file.
        /// </summary>
        /// <value>The full path to the shortcut file, or <see langword="null"/> if no file has been loaded or saved.</value>
        public string? FilePath
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                ((IPersistFile)_shellLink).GetCurFile(out SafeCoTaskMemHandle? ppszFileName);
                using (ppszFileName)
                {
                    return ppszFileName?.ToStringUni();
                }
            }
        }

        /// <summary>
        /// Gets or sets the target path of the shortcut.
        /// </summary>
        /// <value>The path to the target file or folder that the shortcut points to.</value>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public string? TargetPath
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                Span<char> buffer = stackalloc char[(int)PInvoke.MAX_PATH]; buffer.Clear();
                _shellLink.GetPath(buffer, (uint)SLGP_FLAGS.SLGP_UNCPRIORITY);
                return buffer.ToStringUni();
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _shellLink.SetPath(value);
            }
        }

        /// <summary>
        /// Gets or sets the description (comment) of the shortcut.
        /// </summary>
        /// <value>The description text associated with the shortcut.</value>
        /// <remarks>
        /// First attempts to retrieve the description from <c>IPropertyStore</c> with <c>PKEY_Link_Comment</c>,
        /// which properly allocates the correct buffer size. Falls back to <c>IShellLinkW.GetDescription</c>
        /// if the property store value is not set, as some shortcuts only store the description in the
        /// shell link structure.
        /// </remarks>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public string? Description
        {
            get
            {
                // Try property store first (preferred, no buffer size issues).
                // Fall back to IShellLinkW.GetDescription if required/necessary.
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (GetStringProperty(in PInvoke.PKEY_Link_Comment) is string propertyValue)
                {
                    return propertyValue;
                }
                Span<char> buffer = stackalloc char[(int)PInvoke.INFOTIPSIZE]; buffer.Clear();
                _shellLink.GetDescription(buffer);
                return buffer.ToStringUni();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetStringProperty(in PInvoke.PKEY_Link_Comment, value);
        }

        /// <summary>
        /// Gets or sets the working directory for the shortcut's target.
        /// </summary>
        /// <value>The working directory path that will be set when the shortcut is activated.</value>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public string? WorkingDirectory
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                Span<char> buffer = stackalloc char[(int)PInvoke.MAX_PATH]; buffer.Clear();
                _shellLink.GetWorkingDirectory(buffer);
                return buffer.ToStringUni();
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _shellLink.SetWorkingDirectory(value);
            }
        }

        /// <summary>
        /// Gets or sets the command-line arguments for the shortcut.
        /// </summary>
        /// <value>The arguments passed to the target when the shortcut is activated.</value>
        /// <remarks>
        /// Uses <c>IPropertyStore</c> with <c>PKEY_Link_Arguments</c> as recommended by Microsoft
        /// for Windows 7 and later, which properly allocates the correct buffer size and avoids
        /// silent truncation that can occur with <c>IShellLinkW.GetArguments</c>.
        /// </remarks>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public string? Arguments
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetStringProperty(in PInvoke.PKEY_Link_Arguments);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetStringProperty(in PInvoke.PKEY_Link_Arguments, value);
        }

        /// <summary>
        /// Gets or sets the hotkey for the shortcut.
        /// </summary>
        /// <value>
        /// The hotkey combination as a string (e.g., "Ctrl+Shift+Alt+Q").
        /// Returns <see langword="null"/> if no hotkey is assigned.
        /// </value>
        /// <exception cref="ArgumentException">Thrown when the hotkey string format is invalid.</exception>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        /// <example>
        /// <code>
        /// // Set hotkey using WScript.Shell-compatible string format
        /// shortcut.Hotkey = "ALT+CTRL+F";
        /// shortcut.Hotkey = "Ctrl+Shift+Q";
        /// 
        /// // Read and display hotkey
        /// Console.WriteLine(shortcut.Hotkey); // Output: "Ctrl+Shift+Q"
        /// </code>
        /// </example>
        public string? Hotkey
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _shellLink.GetHotkey(out ushort hotkey);
                return hotkey > 0 ? ShortcutHotkey.FromValue(hotkey).ToString() : null;
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (string.IsNullOrWhiteSpace(value))
                {
                    _shellLink.SetHotkey(0);
                }
                else
                {
                    _shellLink.SetHotkey(ShortcutHotkey.Parse(value!).Value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the window show state for the shortcut's target.
        /// </summary>
        /// <value>The <see cref="SHOW_WINDOW_CMD"/> value indicating how the window should be shown.</value>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public Interop.SHOW_WINDOW_CMD WindowStyle
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _shellLink.GetShowCmd(out SHOW_WINDOW_CMD showCmd);
                return (Interop.SHOW_WINDOW_CMD)showCmd;
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _shellLink.SetShowCmd((SHOW_WINDOW_CMD)value);
            }
        }

        /// <summary>
        /// Gets or sets the icon location for the shortcut.
        /// </summary>
        /// <value>The path to the file containing the icon for the shortcut.</value>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public string? IconLocation
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                Span<char> buffer = stackalloc char[(int)PInvoke.MAX_PATH]; buffer.Clear();
                _shellLink.GetIconLocation(buffer, out _);
                return buffer.ToStringUni();
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _shellLink.SetIconLocation(value, IconIndex);
            }
        }

        /// <summary>
        /// Gets or sets the icon index within the icon location file.
        /// </summary>
        /// <value>The zero-based index of the icon within the file specified by <see cref="IconLocation"/>.</value>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public int IconIndex
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                Span<char> buffer = stackalloc char[(int)PInvoke.MAX_PATH]; buffer.Clear();
                _shellLink.GetIconLocation(buffer, out int iconIndex);
                return iconIndex;
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _shellLink.SetIconLocation(IconLocation, value);
            }
        }

        /// <summary>
        /// Gets or sets the Application User Model ID for the shortcut.
        /// </summary>
        /// <value>
        /// The AppUserModelID string that identifies the application for taskbar grouping.
        /// See <see href="https://docs.microsoft.com/windows/win32/shell/appids">Application User Model IDs</see>.
        /// </value>
        public string? AppUserModelId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetStringProperty(in PInvoke.PKEY_AppUserModel_ID);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetStringProperty(in PInvoke.PKEY_AppUserModel_ID, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the shortcut should be excluded from "Show in New Install" lists.
        /// </summary>
        public bool? AppUserModelExcludeFromShowInNewInstall
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetBoolProperty(in PInvoke.PKEY_AppUserModel_ExcludeFromShowInNewInstall);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetBoolProperty(in PInvoke.PKEY_AppUserModel_ExcludeFromShowInNewInstall, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item is a destination list separator.
        /// </summary>
        public bool? AppUserModelIsDestListSeparator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetBoolProperty(in PInvoke.PKEY_AppUserModel_IsDestListSeparator);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetBoolProperty(in PInvoke.PKEY_AppUserModel_IsDestListSeparator, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the application runs in dual mode (desktop and immersive).
        /// </summary>
        public bool? AppUserModelIsDualMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetBoolProperty(in PInvoke.PKEY_AppUserModel_IsDualMode);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetBoolProperty(in PInvoke.PKEY_AppUserModel_IsDualMode, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether pinning should be prevented.
        /// </summary>
        public bool? AppUserModelPreventPinning
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetBoolProperty(in PInvoke.PKEY_AppUserModel_PreventPinning);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetBoolProperty(in PInvoke.PKEY_AppUserModel_PreventPinning, value);
        }

        /// <summary>
        /// Gets or sets the relaunch command for the shortcut.
        /// </summary>
        public string? AppUserModelRelaunchCommand
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetStringProperty(in PInvoke.PKEY_AppUserModel_RelaunchCommand);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetStringProperty(in PInvoke.PKEY_AppUserModel_RelaunchCommand, value);
        }

        /// <summary>
        /// Gets or sets the relaunch display name resource for the shortcut.
        /// </summary>
        public string? AppUserModelRelaunchDisplayNameResource
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetStringProperty(in PInvoke.PKEY_AppUserModel_RelaunchDisplayNameResource);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetStringProperty(in PInvoke.PKEY_AppUserModel_RelaunchDisplayNameResource, value);
        }

        /// <summary>
        /// Gets or sets the relaunch icon resource for the shortcut.
        /// </summary>
        public string? AppUserModelRelaunchIconResource
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetStringProperty(in PInvoke.PKEY_AppUserModel_RelaunchIconResource);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetStringProperty(in PInvoke.PKEY_AppUserModel_RelaunchIconResource, value);
        }

        /// <summary>
        /// Gets or sets the start pin option for the shortcut.
        /// </summary>
        public uint? AppUserModelStartPinOption
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetUInt32Property(in PInvoke.PKEY_AppUserModel_StartPinOption);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetUInt32Property(in PInvoke.PKEY_AppUserModel_StartPinOption, value);
        }

        /// <summary>
        /// Gets or sets the Toast Activator CLSID for the shortcut.
        /// </summary>
        public Guid? AppUserModelToastActivatorClsid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetGuidProperty(in PInvoke.PKEY_AppUserModel_ToastActivatorCLSID);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetGuidProperty(in PInvoke.PKEY_AppUserModel_ToastActivatorCLSID, value);
        }

        /// <summary>
        /// Gets a value indicating whether the shortcut has an ID list.
        /// </summary>
        /// <remarks>This flag is automatically managed by the shell when the shortcut's PIDL is set.</remarks>
        public bool HasIdList => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_ID_LIST);

        /// <summary>
        /// Gets a value indicating whether the shortcut has link info.
        /// </summary>
        /// <remarks>This flag is automatically managed by the shell.</remarks>
        public bool HasLinkInfo => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_LINK_INFO);

        /// <summary>
        /// Gets a value indicating whether the shortcut has a name string (description).
        /// </summary>
        /// <remarks>This flag is automatically set when <see cref="Description"/> is assigned.</remarks>
        public bool HasName => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_NAME);

        /// <summary>
        /// Gets a value indicating whether the shortcut has a relative path.
        /// </summary>
        /// <remarks>This flag is automatically set when <see cref="SetRelativePath"/> is called.</remarks>
        public bool HasRelativePath => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_RELPATH);

        /// <summary>
        /// Gets a value indicating whether the shortcut has a working directory.
        /// </summary>
        /// <remarks>This flag is automatically set when <see cref="WorkingDirectory"/> is assigned.</remarks>
        public bool HasWorkingDirectory => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_WORKINGDIR);

        /// <summary>
        /// Gets a value indicating whether the shortcut has arguments.
        /// </summary>
        /// <remarks>This flag is automatically set when <see cref="Arguments"/> is assigned.</remarks>
        public bool HasArguments => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_ARGS);

        /// <summary>
        /// Gets a value indicating whether the shortcut has an icon location.
        /// </summary>
        /// <remarks>This flag is automatically set when <see cref="IconLocation"/> is assigned.</remarks>
        public bool HasIconLocation => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_ICONLOCATION);

        /// <summary>
        /// Gets a value indicating whether the shortcut uses Unicode strings.
        /// </summary>
        /// <remarks>This flag is automatically managed by the shell.</remarks>
        public bool IsUnicode => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_UNICODE);

        /// <summary>
        /// Gets or sets a value indicating whether link info should not be stored.
        /// </summary>
        public bool ForceNoLinkInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_FORCE_NO_LINKINFO);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_FORCE_NO_LINKINFO, value);
        }

        /// <summary>
        /// Gets a value indicating whether the shortcut contains expandable environment strings.
        /// </summary>
        /// <remarks>This flag is automatically set when environment variables are used in paths.</remarks>
        public bool HasExpandableStrings => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_EXP_SZ);

        /// <summary>
        /// Gets or sets a value indicating whether the target should run in a separate VDM (16-bit apps).
        /// </summary>
        public bool RunInSeparate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_RUN_IN_SEPARATE);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_RUN_IN_SEPARATE, value);
        }

        /// <summary>
        /// Gets a value indicating whether the shortcut has a Darwin ID (MSI advertised shortcut).
        /// </summary>
        /// <remarks>This flag is set by Windows Installer for advertised shortcuts.</remarks>
        public bool HasDarwinId => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_DARWINID);

        /// <summary>
        /// Gets or sets a value indicating whether the shortcut should run as a different user (Run as Administrator).
        /// </summary>
        public bool RunAsUser
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_RUNAS_USER);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_RUNAS_USER, value);
        }

        /// <summary>
        /// Gets a value indicating whether the shortcut has an expanded icon size.
        /// </summary>
        /// <remarks>This flag is automatically managed by the shell.</remarks>
        public bool HasExpandedIconSize => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_HAS_EXP_ICON_SZ);

        /// <summary>
        /// Gets or sets a value indicating whether PIDL alias should not be used.
        /// </summary>
        public bool NoPidlAlias
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_NO_PIDL_ALIAS);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_NO_PIDL_ALIAS, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether UNC path should be forced.
        /// </summary>
        public bool ForceUncName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_FORCE_UNCNAME);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_FORCE_UNCNAME, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the shortcut runs with the shim layer.
        /// </summary>
        public bool RunWithShimLayer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_RUN_WITH_SHIMLAYER);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_RUN_WITH_SHIMLAYER, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether link tracking should be disabled.
        /// </summary>
        public bool ForceNoLinkTrack
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_FORCE_NO_LINKTRACK);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_FORCE_NO_LINKTRACK, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether target metadata is enabled.
        /// </summary>
        public bool EnableTargetMetadata
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_ENABLE_TARGET_METADATA);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_ENABLE_TARGET_METADATA, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether link path tracking is disabled.
        /// </summary>
        public bool DisableLinkPathTracking
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_DISABLE_LINK_PATH_TRACKING);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_DISABLE_LINK_PATH_TRACKING, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether known folder relative tracking is disabled.
        /// </summary>
        public bool DisableKnownFolderRelativeTracking
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_DISABLE_KNOWNFOLDER_RELATIVE_TRACKING);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_DISABLE_KNOWNFOLDER_RELATIVE_TRACKING, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether known folder alias should not be used.
        /// </summary>
        public bool NoKnownFolderAlias
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_NO_KF_ALIAS);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_NO_KF_ALIAS, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether linking to a link is allowed.
        /// </summary>
        public bool AllowLinkToLink
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_ALLOW_LINK_TO_LINK);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_ALLOW_LINK_TO_LINK, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the path should be un-aliased on save.
        /// </summary>
        public bool UnaliasOnSave
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_UNALIAS_ON_SAVE);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_UNALIAS_ON_SAVE, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether environment path is preferred.
        /// </summary>
        public bool PreferEnvironmentPath
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_PREFER_ENVIRONMENT_PATH);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_PREFER_ENVIRONMENT_PATH, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether local ID list should be kept for UNC targets.
        /// </summary>
        public bool KeepLocalIdListForUncTarget
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFlag(SHELL_LINK_DATA_FLAGS.SLDF_KEEP_LOCAL_IDLIST_FOR_UNC_TARGET);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetFlag(SHELL_LINK_DATA_FLAGS.SLDF_KEEP_LOCAL_IDLIST_FOR_UNC_TARGET, value);
        }

        /// <summary>
        /// Sets the relative path for the shortcut.
        /// </summary>
        /// <param name="relativePath">The relative path to the target.</param>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public void SetRelativePath(string relativePath)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _shellLink.SetRelativePath(relativePath, 0);
        }

        /// <summary>
        /// Resolves the shortcut, finding the target if it has moved.
        /// </summary>
        /// <param name="hwnd">A handle to the parent window for any UI that may be displayed.</param>
        /// <param name="flags">Flags that control the resolution process.</param>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public void Resolve(nint hwnd, uint flags)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _shellLink.Resolve((HWND)hwnd, flags);
        }

        /// <summary>
        /// Gets a string property value from the property store.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <returns>The string value, or an empty string if the property is not set.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property has an unexpected type.</exception>
        private string? GetStringProperty(in PROPERTYKEY key)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PROPVARIANT propVariant = default;
            try
            {
                ((IPropertyStore)_shellLink).GetValue(in key, out propVariant);
                VARENUM vt = propVariant.Anonymous.Anonymous.vt;
                if (vt == VARENUM.VT_EMPTY)
                {
                    return null;
                }
                if (vt != VARENUM.VT_LPWSTR)
                {
                    throw new InvalidOperationException($"Property has unexpected type {vt}, expected VT_LPWSTR.");
                }
                PWSTR pwszVal = propVariant.Anonymous.Anonymous.Anonymous.pwszVal;
                if (pwszVal.IsNull())
                {
                    return null;
                }
                string pwszValStr = pwszVal.ToString();
                return pwszValStr.Length > 0 ? pwszValStr : null;
            }
            finally
            {
                _ = PInvoke.PropVariantClear(ref propVariant);
            }
        }

        /// <summary>
        /// Sets a string property value in the property store.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The string value to set, or <see langword="null"/> to clear the property.</param>
        private void SetStringProperty(in PROPERTYKEY key, string? value)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PROPVARIANT propVariant = default;
            try
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    unsafe
                    {
                        propVariant.Anonymous.Anonymous.Anonymous.pwszVal = new((char*)Marshal.StringToCoTaskMemUni(value));
                    }
                    propVariant.Anonymous.Anonymous.vt = VARENUM.VT_LPWSTR;
                }
                else
                {
                    propVariant.Anonymous.Anonymous.vt = VARENUM.VT_EMPTY;
                }
                IPropertyStore propertyStore = (IPropertyStore)_shellLink;
                propertyStore.SetValue(in key, in propVariant);
                propertyStore.Commit();
            }
            finally
            {
                _ = PInvoke.PropVariantClear(ref propVariant);
            }
        }

        /// <summary>
        /// Gets a boolean property value from the property store.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <returns>The boolean value, or <see langword="false"/> if the property is not set.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property has an unexpected type.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Using a ternary here is just messy.")]
        private bool? GetBoolProperty(in PROPERTYKEY key)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PROPVARIANT propVariant = default;
            try
            {
                ((IPropertyStore)_shellLink).GetValue(in key, out propVariant);
                VARENUM vt = propVariant.Anonymous.Anonymous.vt;
                if (vt == VARENUM.VT_EMPTY)
                {
                    return null;
                }
                if (vt != VARENUM.VT_BOOL)
                {
                    throw new InvalidOperationException($"Property has unexpected type {vt}, expected VT_BOOL.");
                }
                return propVariant.Anonymous.Anonymous.Anonymous.boolVal != 0;
            }
            finally
            {
                _ = PInvoke.PropVariantClear(ref propVariant);
            }
        }

        /// <summary>
        /// Sets a boolean property value in the property store.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The boolean value to set, or <see langword="null"/> to clear the property.</param>
        private void SetBoolProperty(in PROPERTYKEY key, bool? value)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PROPVARIANT propVariant = default;
            try
            {
                if (value.HasValue)
                {
                    propVariant.Anonymous.Anonymous.vt = VARENUM.VT_BOOL;
                    propVariant.Anonymous.Anonymous.Anonymous.boolVal = new VARIANT_BOOL((short)(value.Value ? -1 : 0));
                }
                else
                {
                    propVariant.Anonymous.Anonymous.vt = VARENUM.VT_EMPTY;
                }
                IPropertyStore propertyStore = (IPropertyStore)_shellLink;
                propertyStore.SetValue(in key, in propVariant);
                propertyStore.Commit();
            }
            finally
            {
                _ = PInvoke.PropVariantClear(ref propVariant);
            }
        }

        /// <summary>
        /// Gets a GUID property value from the property store.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <returns>The GUID value, or <see cref="Guid.Empty"/> if the property is not set.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property has an unexpected type.</exception>
        private Guid? GetGuidProperty(in PROPERTYKEY key)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PROPVARIANT propVariant = default;
            try
            {
                ((IPropertyStore)_shellLink).GetValue(in key, out propVariant);
                VARENUM vt = propVariant.Anonymous.Anonymous.vt;
                if (vt == VARENUM.VT_EMPTY)
                {
                    return null;
                }
                if (vt != VARENUM.VT_CLSID)
                {
                    throw new InvalidOperationException($"Property has unexpected type {vt}, expected VT_CLSID.");
                }
                unsafe
                {
                    if (propVariant.Anonymous.Anonymous.Anonymous.puuid is not null)
                    {
                        return *propVariant.Anonymous.Anonymous.Anonymous.puuid;
                    }
                }
                return null;
            }
            finally
            {
                _ = PInvoke.PropVariantClear(ref propVariant);
            }
        }

        /// <summary>
        /// Sets a GUID property value in the property store.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The GUID value to set, or <see langword="null"/> to clear the property.</param>
        private void SetGuidProperty(in PROPERTYKEY key, Guid? value)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PROPVARIANT propVariant = default;
            try
            {
                if (value.HasValue)
                {
                    unsafe
                    {
                        propVariant.Anonymous.Anonymous.Anonymous.puuid = (Guid*)Marshal.AllocCoTaskMem(sizeof(Guid));
                        *propVariant.Anonymous.Anonymous.Anonymous.puuid = value.Value;
                    }
                    propVariant.Anonymous.Anonymous.vt = VARENUM.VT_CLSID;
                }
                else
                {
                    propVariant.Anonymous.Anonymous.vt = VARENUM.VT_EMPTY;
                }
                IPropertyStore propertyStore = (IPropertyStore)_shellLink;
                propertyStore.SetValue(in key, in propVariant);
                propertyStore.Commit();
            }
            finally
            {
                _ = PInvoke.PropVariantClear(ref propVariant);
            }
        }

        /// <summary>
        /// Gets an unsigned integer property value from the property store.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <returns>The unsigned integer value, or 0 if the property is not set.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property has an unexpected type.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Using a ternary here is just messy.")]
        private uint? GetUInt32Property(in PROPERTYKEY key)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PROPVARIANT propVariant = default;
            try
            {
                ((IPropertyStore)_shellLink).GetValue(in key, out propVariant);
                VARENUM vt = propVariant.Anonymous.Anonymous.vt;
                if (vt == VARENUM.VT_EMPTY)
                {
                    return null;
                }
                if (vt != VARENUM.VT_UI4)
                {
                    throw new InvalidOperationException($"Property has unexpected type {vt}, expected VT_UI4.");
                }
                return propVariant.Anonymous.Anonymous.Anonymous.ulVal;
            }
            finally
            {
                _ = PInvoke.PropVariantClear(ref propVariant);
            }
        }

        /// <summary>
        /// Sets an unsigned integer property value in the property store.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The unsigned integer value to set, or <see langword="null"/> to clear the property.</param>
        private void SetUInt32Property(in PROPERTYKEY key, uint? value)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PROPVARIANT propVariant = default;
            try
            {
                if (value.HasValue)
                {
                    propVariant.Anonymous.Anonymous.vt = VARENUM.VT_UI4;
                    propVariant.Anonymous.Anonymous.Anonymous.ulVal = value.Value;
                }
                else
                {
                    propVariant.Anonymous.Anonymous.vt = VARENUM.VT_EMPTY;
                }
                IPropertyStore propertyStore = (IPropertyStore)_shellLink;
                propertyStore.SetValue(in key, in propVariant);
                propertyStore.Commit();
            }
            finally
            {
                _ = PInvoke.PropVariantClear(ref propVariant);
            }
        }

        /// <summary>
        /// Gets the shell link data flags.
        /// </summary>
        /// <returns>The current <see cref="SHELL_LINK_DATA_FLAGS"/> value.</returns>
        private SHELL_LINK_DATA_FLAGS GetFlags()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ((IShellLinkDataList)_shellLink).GetFlags(out uint flags);
            return (SHELL_LINK_DATA_FLAGS)flags;
        }

        /// <summary>
        /// Sets the shell link data flags.
        /// </summary>
        /// <param name="flags">The <see cref="SHELL_LINK_DATA_FLAGS"/> value to set.</param>
        private void SetFlags(SHELL_LINK_DATA_FLAGS flags)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ((IShellLinkDataList)_shellLink).SetFlags((uint)flags);
        }

        /// <summary>
        /// Gets or sets a flag in the shell link data flags.
        /// </summary>
        /// <param name="flag">The flag to get or set.</param>
        /// <returns><see langword="true"/> if the flag is set; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetFlag(SHELL_LINK_DATA_FLAGS flag)
        {
            return (GetFlags() & flag) != 0;
        }

        /// <summary>
        /// Sets or clears a flag in the shell link data flags.
        /// </summary>
        /// <param name="flag">The flag to set or clear.</param>
        /// <param name="value"><see langword="true"/> to set the flag; <see langword="false"/> to clear it.</param>
        private void SetFlag(SHELL_LINK_DATA_FLAGS flag, bool value)
        {
            SHELL_LINK_DATA_FLAGS flags = GetFlags();
            if (value)
            {
                flags |= flag;
            }
            else
            {
                flags &= ~flag;
            }
            SetFlags(flags);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ShellLinkFile"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true); GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ShellLinkFile"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_shellLink != null)
                    {
                        _ = Marshal.FinalReleaseComObject(_shellLink);
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The underlying IShellLinkW COM object.
        /// </summary>
        private readonly IShellLinkW _shellLink;

        /// <summary>
        /// Represents the storage mode used by the interop component, as defined by the Interop.STGM enumeration.
        /// </summary>
        /// <remarks>The storage mode determines how data is stored and accessed within the interop
        /// component. This field is initialized during construction and is intended for internal use only.</remarks>
        private readonly Interop.STGM? _storageMode;
    }
}
