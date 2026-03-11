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
using PSADT.Interop.ComTypes;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace PSADT.ShortcutManagement
{
    /// <summary>
    /// Provides a managed wrapper around the Windows InternetShortcut COM interface.
    /// This class enables creating, loading, modifying, and saving Internet shortcut (.url) files.
    /// </summary>
    /// <remarks>
    /// This class wraps the <c>IUniformResourceLocatorW</c> and <c>IPersistFile</c> COM interfaces
    /// to provide access to URL shortcut properties.
    /// </remarks>
    public sealed class InternetShortcutFile : IDisposable
    {
        /// <summary>
        /// Creates a new, empty Internet shortcut.
        /// </summary>
        /// <returns>A new <see cref="InternetShortcutFile"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InternetShortcutFile New()
        {
            return new();
        }

        /// <summary>
        /// Creates a new Internet shortcut with the specified URL.
        /// </summary>
        /// <param name="url">The URL for the shortcut.</param>
        /// <returns>A new <see cref="InternetShortcutFile"/> instance with the URL set.</returns>
        public static InternetShortcutFile New(Uri url)
        {
            ArgumentNullException.ThrowIfNull(url);
            return new() { Url = url };
        }

        /// <summary>
        /// Loads an existing Internet shortcut file in read-only mode.
        /// </summary>
        /// <param name="filePath">The path to the shortcut file to load.</param>
        /// <returns>A new <see cref="InternetShortcutFile"/> instance loaded from the specified file.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        /// <remarks>Use <see cref="Load(string, Interop.STGM)"/> with <see cref="Interop.STGM.STGM_READWRITE"/> if you need to modify the shortcut.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InternetShortcutFile Load(string filePath)
        {
            return Load(filePath, Interop.STGM.STGM_READ);
        }

        /// <summary>
        /// Loads an existing Internet shortcut file with the specified storage mode.
        /// </summary>
        /// <param name="filePath">The path to the shortcut file to load.</param>
        /// <param name="storageMode">The storage mode flags.</param>
        /// <returns>A new <see cref="InternetShortcutFile"/> instance loaded from the specified file.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InternetShortcutFile Load(string filePath, Interop.STGM storageMode)
        {
            return new(filePath, storageMode);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InternetShortcutFile"/> class for creating a new shortcut.
        /// </summary>
        private InternetShortcutFile()
        {
            _internetShortcut = new IUniformResourceLocatorW();
            _storageMode = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InternetShortcutFile"/> class by loading an existing shortcut file.
        /// </summary>
        /// <param name="filePath">The path to the shortcut file to load.</param>
        /// <param name="storageMode">The storage mode flags.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        private InternetShortcutFile(string filePath, Interop.STGM storageMode)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified shortcut file does not exist.", filePath);
            }
            IUniformResourceLocatorW internetShortcut = new();
            try
            {
                ((IPersistFile)internetShortcut).Load(filePath, (STGM)storageMode);
                _internetShortcut = internetShortcut;
                _storageMode = storageMode;
            }
            catch
            {
                _ = Marshal.FinalReleaseComObject(internetShortcut);
                throw;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="InternetShortcutFile"/> class.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ~InternetShortcutFile()
        {
            Dispose(false);
        }

        /// <summary>
        /// Saves the Internet shortcut to the currently loaded file path.
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
        /// Saves the Internet shortcut to the specified file path.
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
            ((IPersistFile)_internetShortcut).Save(filePath, true);
        }

        /// <summary>
        /// Gets a value indicating whether the current storage mode is read-only, preventing any write operations.
        /// </summary>
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
                ((IPersistFile)_internetShortcut).GetCurFile(out SafeCoTaskMemHandle? ppszFileName);
                using (ppszFileName)
                {
                    return ppszFileName?.ToStringUni();
                }
            }
        }

        /// <summary>
        /// Gets or sets the URL of the Internet shortcut.
        /// </summary>
        /// <value>The URL that the shortcut points to.</value>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public Uri? Url
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _internetShortcut.GetURL(out string? url);
                return url is not null ? new(url) : null;
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                ArgumentNullException.ThrowIfNull(value);
                _internetShortcut.SetURL(value.AbsoluteUri, 0);
            }
        }

        /// <summary>
        /// Opens the URL using the default handler.
        /// </summary>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke()
        {
            Invoke(null, default, Interop.IURL_INVOKECOMMAND_FLAGS.IURL_INVOKECOMMAND_FL_USE_DEFAULT_VERB);
        }

        /// <summary>
        /// Opens the URL using the specified verb.
        /// </summary>
        /// <param name="verb">The verb to invoke (e.g., "open"). Pass <see langword="null"/> for the default verb.</param>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(string? verb)
        {
            Invoke(verb, default, verb is null ? Interop.IURL_INVOKECOMMAND_FLAGS.IURL_INVOKECOMMAND_FL_USE_DEFAULT_VERB : 0);
        }

        /// <summary>
        /// Opens the URL using the specified verb and options.
        /// </summary>
        /// <param name="verb">The verb to invoke (e.g., "open"). Pass <see langword="null"/> for the default verb.</param>
        /// <param name="hwndParent">A handle to the parent window for any UI that may be displayed.</param>
        /// <param name="flags">Flags that control the invocation behavior.</param>
        /// <exception cref="COMException">Thrown when the COM operation fails.</exception>
        public void Invoke(string? verb, nint hwndParent, Interop.IURL_INVOKECOMMAND_FLAGS flags)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            unsafe
            {
                fixed (char* pVerb = verb)
                {
                    URLINVOKECOMMANDINFOW commandInfo = new()
                    {
                        dwcbSize = (uint)sizeof(URLINVOKECOMMANDINFOW),
                        dwFlags = (uint)flags,
                        hwndParent = (HWND)hwndParent,
                        pcszVerb = pVerb
                    };
                    _internetShortcut.InvokeCommand(in commandInfo);
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="InternetShortcutFile"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="InternetShortcutFile"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_internetShortcut != null)
                    {
                        _ = Marshal.FinalReleaseComObject(_internetShortcut);
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
        /// The underlying IUniformResourceLocatorW COM object.
        /// </summary>
        private readonly IUniformResourceLocatorW _internetShortcut;

        /// <summary>
        /// The storage mode used when loading the shortcut file.
        /// </summary>
        /// <remarks>
        /// This is <see langword="null"/> for newly created shortcuts (via <see cref="New()"/> or <see cref="New(Uri)"/>),
        /// indicating they can be saved to any path. For loaded shortcuts, this reflects the access mode used during loading.
        /// </remarks>
        private readonly Interop.STGM? _storageMode;
    }
}
