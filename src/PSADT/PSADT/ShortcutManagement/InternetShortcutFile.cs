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
using PSADT.Interop;
using PSADT.Interop.ComTypes;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Shell;

namespace PSADT.ShortcutManagement
{
    /// <summary>
    /// Provides a managed wrapper around the Windows InternetShortcut COM interface.
    /// This class enables creating, loading, modifying, and saving Internet shortcut (.url) files.
    /// </summary>
    /// <remarks>
    /// This class wraps the <c>IUniformResourceLocatorW</c>, <c>IPersistFile</c>, <c>IPropertySetStorage</c>,
    /// and <c>IPropertyStorage</c> COM interfaces to provide access to URL shortcut properties.
    /// </remarks>
    public sealed class InternetShortcutFile : IDisposable
    {
        /// <summary>
        /// Creates a new Internet shortcut with the specified URL.
        /// </summary>
        /// <param name="url">The URL for the shortcut.</param>
        /// <returns>A new <see cref="InternetShortcutFile"/> instance with the URL set.</returns>
        public static InternetShortcutFile Create(Uri url)
        {
            ArgumentNullException.ThrowIfNull(url);
            return new() { Url = url };
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
        public static InternetShortcutFile Load(string filePath, Interop.STGM storageMode = Interop.STGM.STGM_READ)
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
                ((IPersistFile)internetShortcut).Load(filePath, (Windows.Win32.System.Com.STGM)storageMode);
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
            if (FilePath is not FileInfo currentFile)
            {
                throw new InvalidOperationException("No file path has been set. Use Save(string) to specify a path.");
            }
            Save(currentFile.FullName);
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
            if (IsReadOnly && string.Equals(Path.GetFullPath(filePath), FilePath?.FullName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot overwrite a shortcut file that was loaded with read-only access. Use Load(filePath, STGM.STGM_READWRITE) to enable modifications.");
            }
            ((IPersistFile)_internetShortcut).Save(filePath, true);
        }

        /// <summary>
        /// Gets the path of the currently loaded shortcut file.
        /// </summary>
        /// <value>The full path to the shortcut file, or <see langword="null"/> if no file has been loaded or saved.</value>
        public FileInfo? FilePath
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                ((IPersistFile)_internetShortcut).GetCurFile(out SafeCoTaskMemHandle? ppszFileName);
                using (ppszFileName)
                {
                    return ppszFileName?.ToStringUni() is string filePath
                        ? new(filePath)
                        : null;
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
        /// Gets or sets the display name for the Internet shortcut.
        /// </summary>
        public string? Name
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return GetStringProperty(PID_IS.PID_IS_NAME);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetStringProperty(PID_IS.PID_IS_NAME, value);
            }
        }

        /// <summary>
        /// Gets or sets the working directory for the Internet shortcut.
        /// </summary>
        public string? WorkingDirectory
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return GetStringProperty(PID_IS.PID_IS_WORKINGDIR);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetStringProperty(PID_IS.PID_IS_WORKINGDIR, value);
            }
        }

        /// <summary>
        /// Gets or sets the hotkey for the Internet shortcut.
        /// </summary>
        public string? Hotkey
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                ushort? hotkeyValue = GetUInt16Property(PID_IS.PID_IS_HOTKEY);
                return hotkeyValue > 0 ? ShortcutHotkey.FromValue(hotkeyValue.Value).ToString() : null;
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetUInt16Property(PID_IS.PID_IS_HOTKEY, value is not null && !string.IsNullOrWhiteSpace(value)
                    ? ShortcutHotkey.Parse(value).ToUInt16()
                    : (ushort)0);
            }
        }

        /// <summary>
        /// Gets or sets the show command value for the Internet shortcut.
        /// </summary>
        public ShortcutWindowStyle? ShowCommand
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return (ShortcutWindowStyle?)GetInt32Property(PID_IS.PID_IS_SHOWCMD);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetInt32Property(PID_IS.PID_IS_SHOWCMD, (int?)value);
            }
        }

        /// <summary>
        /// Gets or sets the icon file path for the Internet shortcut.
        /// </summary>
        public Uri? IconFile
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return GetStringProperty(PID_IS.PID_IS_ICONFILE) is string iconFile
                    ? new(iconFile)
                    : null;
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetStringProperty(PID_IS.PID_IS_ICONFILE, value?.AbsolutePath);
            }
        }

        /// <summary>
        /// Gets or sets the icon index for the Internet shortcut.
        /// </summary>
        public int? IconIndex
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return IconFile is not null ? GetInt32Property(PID_IS.PID_IS_ICONINDEX) : null;
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (value is null && IconFile is not null)
                {
                    throw new InvalidOperationException("Cannot set IconIndex to null when IconFile is set.");
                }
                SetInt32Property(PID_IS.PID_IS_ICONINDEX, value);
            }
        }

        /// <summary>
        /// Gets or sets the What's New text for the Internet shortcut.
        /// </summary>
        public string? WhatsNew
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return GetStringProperty(PID_IS.PID_IS_WHATSNEW);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetStringProperty(PID_IS.PID_IS_WHATSNEW, value);
            }
        }

        /// <summary>
        /// Gets or sets the author for the Internet shortcut.
        /// </summary>
        public string? Author
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return GetStringProperty(PID_IS.PID_IS_AUTHOR);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetStringProperty(PID_IS.PID_IS_AUTHOR, value);
            }
        }

        /// <summary>
        /// Gets or sets the description for the Internet shortcut.
        /// </summary>
        public string? Description
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return GetStringProperty(PID_IS.PID_IS_DESCRIPTION);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetStringProperty(PID_IS.PID_IS_DESCRIPTION, value);
            }
        }

        /// <summary>
        /// Gets or sets the comment for the Internet shortcut.
        /// </summary>
        public string? Comment
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return GetStringProperty(PID_IS.PID_IS_COMMENT);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetStringProperty(PID_IS.PID_IS_COMMENT, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Internet shortcut has roamed.
        /// </summary>
        public bool? Roamed
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return GetBooleanProperty(PID_IS.PID_IS_ROAMED);
            }
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                SetBooleanProperty(PID_IS.PID_IS_ROAMED, value);
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
        /// Gets a boolean property value from the Internet Shortcut property set.
        /// </summary>
        /// <param name="propertyId">The property ID.</param>
        /// <returns>The property value, or <see langword="null"/> if not set.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Using if statements keeps type handling clearer here.")]
        private bool? GetBooleanProperty(PID_IS propertyId)
        {
            IPropertyStorage propertyStorage = OpenInternetShortcutPropertyStorage((uint)Interop.STGM.STGM_READ);
            try
            {
                PROPVARIANT[] propertyValues = [default];
                try
                {
                    PROPSPEC propertySpec = new()
                    {
                        ulKind = PROPSPEC_KIND.PRSPEC_PROPID,
                        Anonymous = new() { propid = (uint)propertyId }
                    };
                    propertyStorage.ReadMultiple([propertySpec], propertyValues);
                    VARENUM vt = propertyValues[0].Anonymous.Anonymous.vt;
                    if (vt == VARENUM.VT_EMPTY)
                    {
                        return null;
                    }
                    if (vt == VARENUM.VT_BOOL)
                    {
                        return propertyValues[0].Anonymous.Anonymous.Anonymous.boolVal != 0;
                    }
                    if (vt == VARENUM.VT_I4)
                    {
                        return propertyValues[0].Anonymous.Anonymous.Anonymous.lVal != 0;
                    }
                    if (vt == VARENUM.VT_UI4)
                    {
                        return propertyValues[0].Anonymous.Anonymous.Anonymous.ulVal != 0;
                    }
                    throw new InvalidOperationException($"Property has unexpected type {vt}, expected VT_BOOL, VT_I4, or VT_UI4.");
                }
                finally
                {
                    _ = NativeMethods.PropVariantClear(ref propertyValues[0]);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(propertyStorage);
            }
        }

        /// <summary>
        /// Sets a boolean property value in the Internet Shortcut property set.
        /// </summary>
        /// <param name="propertyId">The property ID.</param>
        /// <param name="value">The property value.</param>
        private void SetBooleanProperty(PID_IS propertyId, bool? value)
        {
            IPropertyStorage propertyStorage = OpenInternetShortcutPropertyStorage((uint)Interop.STGM.STGM_READWRITE);
            try
            {
                PROPVARIANT[] propertyValues = [default];
                try
                {
                    if (value.HasValue)
                    {
                        propertyValues[0].Anonymous.Anonymous.vt = VARENUM.VT_BOOL;
                        propertyValues[0].Anonymous.Anonymous.Anonymous.boolVal = new VARIANT_BOOL((short)(value.Value ? -1 : 0));
                    }
                    else
                    {
                        propertyValues[0].Anonymous.Anonymous.vt = VARENUM.VT_EMPTY;
                    }
                    PROPSPEC propertySpec = new()
                    {
                        ulKind = PROPSPEC_KIND.PRSPEC_PROPID,
                        Anonymous = new() { propid = (uint)propertyId }
                    };
                    propertyStorage.WriteMultiple([propertySpec], propertyValues, 2);
                    propertyStorage.Commit(0);
                }
                finally
                {
                    _ = NativeMethods.PropVariantClear(ref propertyValues[0]);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(propertyStorage);
            }
        }

        /// <summary>
        /// Gets a property value from the Internet Shortcut property set.
        /// </summary>
        /// <param name="propertyId">The property ID.</param>
        /// <returns>The property value, or <see langword="null"/> if not set.</returns>
        private string? GetStringProperty(PID_IS propertyId)
        {
            IPropertyStorage propertyStorage = OpenInternetShortcutPropertyStorage((uint)Interop.STGM.STGM_READ);
            try
            {
                PROPVARIANT[] propertyValues = [default];
                try
                {
                    PROPSPEC propertySpec = new()
                    {
                        ulKind = PROPSPEC_KIND.PRSPEC_PROPID,
                        Anonymous = new() { propid = (uint)propertyId }
                    };
                    propertyStorage.ReadMultiple([propertySpec], propertyValues);
                    VARENUM vt = propertyValues[0].Anonymous.Anonymous.vt;
                    if (vt == VARENUM.VT_EMPTY)
                    {
                        return null;
                    }
                    if (vt == VARENUM.VT_BSTR)
                    {
                        nint bstrVal;
                        unsafe
                        {
                            bstrVal = (nint)propertyValues[0].Anonymous.Anonymous.Anonymous.bstrVal.Value;
                        }
                        if (bstrVal == 0)
                        {
                            return null;
                        }
                        string? bstrValStr = Marshal.PtrToStringBSTR(bstrVal);
                        return !string.IsNullOrWhiteSpace(bstrValStr) ? bstrValStr : null;
                    }
                    if (vt != VARENUM.VT_LPWSTR)
                    {
                        throw new InvalidOperationException($"Property has unexpected type {vt}, expected VT_LPWSTR or VT_BSTR.");
                    }
                    PWSTR pwszVal = propertyValues[0].Anonymous.Anonymous.Anonymous.pwszVal;
                    if (pwszVal.IsNull())
                    {
                        return null;
                    }
                    string pwszValStr = pwszVal.ToString();
                    return pwszValStr.Length > 0 ? pwszValStr : null;
                }
                finally
                {
                    _ = NativeMethods.PropVariantClear(ref propertyValues[0]);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(propertyStorage);
            }
        }

        /// <summary>
        /// Sets a property value in the Internet Shortcut property set.
        /// </summary>
        /// <param name="propertyId">The property ID.</param>
        /// <param name="value">The property value.</param>
        private void SetStringProperty(PID_IS propertyId, string? value)
        {
            IPropertyStorage propertyStorage = OpenInternetShortcutPropertyStorage((uint)Interop.STGM.STGM_READWRITE);
            try
            {
                PROPVARIANT[] propertyValues = [default];
                try
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        unsafe
                        {
                            propertyValues[0].Anonymous.Anonymous.Anonymous.pwszVal = new((char*)Marshal.StringToCoTaskMemUni(value));
                        }
                        propertyValues[0].Anonymous.Anonymous.vt = VARENUM.VT_LPWSTR;
                    }
                    else
                    {
                        propertyValues[0].Anonymous.Anonymous.vt = VARENUM.VT_EMPTY;
                    }
                    PROPSPEC propertySpec = new()
                    {
                        ulKind = PROPSPEC_KIND.PRSPEC_PROPID,
                        Anonymous = new() { propid = (uint)propertyId }
                    };
                    propertyStorage.WriteMultiple([propertySpec], propertyValues, 2);
                    propertyStorage.Commit(0);
                }
                finally
                {
                    _ = NativeMethods.PropVariantClear(ref propertyValues[0]);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(propertyStorage);
            }
        }

        /// <summary>
        /// Gets a property value from the Internet Shortcut property set.
        /// </summary>
        /// <param name="propertyId">The property ID.</param>
        /// <returns>The property value, or 0 if not set.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Using a switch expression here causes IDE0072 in this project configuration.")]
        private int? GetInt32Property(PID_IS propertyId)
        {
            IPropertyStorage propertyStorage = OpenInternetShortcutPropertyStorage((uint)Interop.STGM.STGM_READ);
            try
            {
                PROPVARIANT[] propertyValues = [default];
                try
                {
                    PROPSPEC propertySpec = new()
                    {
                        ulKind = PROPSPEC_KIND.PRSPEC_PROPID,
                        Anonymous = new() { propid = (uint)propertyId }
                    };
                    propertyStorage.ReadMultiple([propertySpec], propertyValues);
                    VARENUM vt = propertyValues[0].Anonymous.Anonymous.vt;
                    if (vt == VARENUM.VT_EMPTY)
                    {
                        return null;
                    }
                    if (vt == VARENUM.VT_I4)
                    {
                        return propertyValues[0].Anonymous.Anonymous.Anonymous.lVal;
                    }
                    if (vt == VARENUM.VT_UI4)
                    {
                        return (int)propertyValues[0].Anonymous.Anonymous.Anonymous.ulVal;
                    }
                    throw new InvalidOperationException($"Property has unexpected type {vt}, expected VT_I4 or VT_UI4.");
                }
                finally
                {
                    _ = NativeMethods.PropVariantClear(ref propertyValues[0]);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(propertyStorage);
            }
        }

        /// <summary>
        /// Gets an unsigned 16-bit property value from the Internet Shortcut property set.
        /// </summary>
        /// <param name="propertyId">The property ID.</param>
        /// <returns>The property value, or <see langword="null"/> if not set.</returns>
        private ushort? GetUInt16Property(PID_IS propertyId)
        {
            IPropertyStorage propertyStorage = OpenInternetShortcutPropertyStorage((uint)Interop.STGM.STGM_READ);
            try
            {
                PROPVARIANT[] propertyValues = [default];
                try
                {
                    PROPSPEC propertySpec = new()
                    {
                        ulKind = PROPSPEC_KIND.PRSPEC_PROPID,
                        Anonymous = new() { propid = (uint)propertyId }
                    };
                    propertyStorage.ReadMultiple([propertySpec], propertyValues);
                    VARENUM vt = propertyValues[0].Anonymous.Anonymous.vt;
                    if (vt == VARENUM.VT_EMPTY)
                    {
                        return null;
                    }
                    if (vt == VARENUM.VT_UI2)
                    {
                        return propertyValues[0].Anonymous.Anonymous.Anonymous.uiVal;
                    }
                    if (vt == VARENUM.VT_I2)
                    {
                        short value = propertyValues[0].Anonymous.Anonymous.Anonymous.iVal;
                        return value >= 0 ? (ushort)value : throw new InvalidOperationException("Property has a negative VT_I2 value, expected an unsigned 16-bit value.");
                    }
                    if (vt == VARENUM.VT_I4)
                    {
                        int value = propertyValues[0].Anonymous.Anonymous.Anonymous.lVal;
                        return value is >= ushort.MinValue and <= ushort.MaxValue
                            ? (ushort)value
                            : throw new InvalidOperationException($"Property value {value} is outside the UInt16 range.");
                    }
                    if (vt == VARENUM.VT_UI4)
                    {
                        uint value = propertyValues[0].Anonymous.Anonymous.Anonymous.ulVal;
                        return value <= ushort.MaxValue
                            ? (ushort)value
                            : throw new InvalidOperationException($"Property value {value} is outside the UInt16 range.");
                    }
                    throw new InvalidOperationException($"Property has unexpected type {vt}, expected VT_UI2, VT_I2, VT_I4, or VT_UI4.");
                }
                finally
                {
                    _ = NativeMethods.PropVariantClear(ref propertyValues[0]);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(propertyStorage);
            }
        }

        /// <summary>
        /// Sets a property value in the Internet Shortcut property set.
        /// </summary>
        /// <param name="propertyId">The property ID.</param>
        /// <param name="value">The property value.</param>
        private void SetInt32Property(PID_IS propertyId, int? value)
        {
            IPropertyStorage propertyStorage = OpenInternetShortcutPropertyStorage((uint)Interop.STGM.STGM_READWRITE);
            try
            {
                PROPVARIANT[] propertyValues = [default];
                try
                {
                    if (value is not null)
                    {
                        propertyValues[0].Anonymous.Anonymous.vt = VARENUM.VT_I4;
                        propertyValues[0].Anonymous.Anonymous.Anonymous.lVal = value.Value;
                    }
                    else
                    {
                        propertyValues[0].Anonymous.Anonymous.vt = VARENUM.VT_EMPTY;
                    }
                    PROPSPEC propertySpec = new()
                    {
                        ulKind = PROPSPEC_KIND.PRSPEC_PROPID,
                        Anonymous = new() { propid = (uint)propertyId }
                    };
                    propertyStorage.WriteMultiple([propertySpec], propertyValues, 2);
                    propertyStorage.Commit(0);
                }
                finally
                {
                    _ = NativeMethods.PropVariantClear(ref propertyValues[0]);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(propertyStorage);
            }
        }

        /// <summary>
        /// Sets an unsigned 16-bit property value in the Internet Shortcut property set.
        /// </summary>
        /// <param name="propertyId">The property ID.</param>
        /// <param name="value">The property value.</param>
        private void SetUInt16Property(PID_IS propertyId, ushort? value)
        {
            IPropertyStorage propertyStorage = OpenInternetShortcutPropertyStorage((uint)Interop.STGM.STGM_READWRITE);
            try
            {
                PROPVARIANT[] propertyValues = [default];
                try
                {
                    if (value is not null)
                    {
                        propertyValues[0].Anonymous.Anonymous.vt = VARENUM.VT_UI2;
                        propertyValues[0].Anonymous.Anonymous.Anonymous.uiVal = value.Value;
                    }
                    else
                    {
                        propertyValues[0].Anonymous.Anonymous.vt = VARENUM.VT_EMPTY;
                    }
                    PROPSPEC propertySpec = new()
                    {
                        ulKind = PROPSPEC_KIND.PRSPEC_PROPID,
                        Anonymous = new() { propid = (uint)propertyId }
                    };
                    propertyStorage.WriteMultiple([propertySpec], propertyValues, 2);
                    propertyStorage.Commit(0);
                }
                finally
                {
                    _ = NativeMethods.PropVariantClear(ref propertyValues[0]);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(propertyStorage);
            }
        }

        /// <summary>
        /// Opens the Internet Shortcut property storage.
        /// </summary>
        /// <param name="mode">The storage mode.</param>
        /// <returns>The opened property storage.</returns>
        private IPropertyStorage OpenInternetShortcutPropertyStorage(uint mode)
        {
            ((IPropertySetStorage)_internetShortcut).Open(in PInvoke.FMTID_Intshcut, mode, out IPropertyStorage propertyStorage);
            return propertyStorage;
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
        /// Gets a value indicating whether the current object can be saved.
        /// </summary>
        /// <remarks>This property returns <see langword="true"/> if the object is not in a read-only
        /// state. Use this property to determine if changes can be persisted.</remarks>
        internal bool CanSave => !IsReadOnly;

        /// <summary>
        /// Gets a value indicating whether the current storage mode is read-only, preventing any write operations.
        /// </summary>
        private bool IsReadOnly => _storageMode is Interop.STGM mode && (mode & (Interop.STGM.STGM_WRITE | Interop.STGM.STGM_READWRITE)) == 0;

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
        private readonly Interop.STGM? _storageMode;

    }
}
