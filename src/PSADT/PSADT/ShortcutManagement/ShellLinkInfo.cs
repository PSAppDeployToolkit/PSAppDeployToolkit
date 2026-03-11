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
using PSADT.Interop;

namespace PSADT.ShortcutManagement
{
    /// <summary>
    /// Represents an immutable snapshot of a <see cref="ShellLinkFile"/>.
    /// </summary>
    public sealed record ShellLinkInfo
    {
        /// <summary>
        /// Retrieves information about a Windows shell link (shortcut) from the specified file.
        /// </summary>
        /// <remarks>Use this method to access properties and metadata of a shortcut file, such as its
        /// target path, arguments, and icon. The method opens and reads the file, returning a structured representation
        /// for further inspection.</remarks>
        /// <param name="filePath">The path to the shell link (.lnk) file to be loaded. Cannot be null or empty.</param>
        /// <returns>A ShellLinkInfo object containing details about the shell link represented by the specified file.</returns>
        public static ShellLinkInfo Get(string filePath)
        {
            using ShellLinkFile shellLink = ShellLinkFile.Load(filePath);
            return new(shellLink);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellLinkInfo"/> class from a <see cref="ShellLinkFile"/>.
        /// </summary>
        /// <param name="shellLink">The shell link to snapshot.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="shellLink"/> is null.</exception>
        internal ShellLinkInfo(ShellLinkFile shellLink)
        {
            ArgumentNullException.ThrowIfNull(shellLink);
            FilePath = shellLink.FilePath ?? throw new ArgumentNullException(nameof(shellLink), "The provided Shell Link does not have a valid file path.");
            TargetPath = shellLink.TargetPath;
            Description = shellLink.Description;
            WorkingDirectory = shellLink.WorkingDirectory;
            Arguments = shellLink.Arguments;
            Hotkey = shellLink.Hotkey;
            WindowStyle = shellLink.WindowStyle;
            IconLocation = shellLink.IconLocation;
            IconIndex = shellLink.IconIndex;
            AppUserModelId = shellLink.AppUserModelId;
            AppUserModelExcludeFromShowInNewInstall = shellLink.AppUserModelExcludeFromShowInNewInstall;
            AppUserModelIsDestListSeparator = shellLink.AppUserModelIsDestListSeparator;
            AppUserModelIsDualMode = shellLink.AppUserModelIsDualMode;
            AppUserModelPreventPinning = shellLink.AppUserModelPreventPinning;
            AppUserModelRelaunchCommand = shellLink.AppUserModelRelaunchCommand;
            AppUserModelRelaunchDisplayNameResource = shellLink.AppUserModelRelaunchDisplayNameResource;
            AppUserModelRelaunchIconResource = shellLink.AppUserModelRelaunchIconResource;
            AppUserModelStartPinOption = shellLink.AppUserModelStartPinOption;
            AppUserModelToastActivatorClsid = shellLink.AppUserModelToastActivatorClsid;
            HasIdList = shellLink.HasIdList;
            HasLinkInfo = shellLink.HasLinkInfo;
            HasName = shellLink.HasName;
            HasRelativePath = shellLink.HasRelativePath;
            HasWorkingDirectory = shellLink.HasWorkingDirectory;
            HasArguments = shellLink.HasArguments;
            HasIconLocation = shellLink.HasIconLocation;
            IsUnicode = shellLink.IsUnicode;
            ForceNoLinkInfo = shellLink.ForceNoLinkInfo;
            HasExpandableStrings = shellLink.HasExpandableStrings;
            RunInSeparate = shellLink.RunInSeparate;
            HasDarwinId = shellLink.HasDarwinId;
            RunAsUser = shellLink.RunAsUser;
            HasExpandedIconSize = shellLink.HasExpandedIconSize;
            NoPidlAlias = shellLink.NoPidlAlias;
            ForceUncName = shellLink.ForceUncName;
            RunWithShimLayer = shellLink.RunWithShimLayer;
            ForceNoLinkTrack = shellLink.ForceNoLinkTrack;
            EnableTargetMetadata = shellLink.EnableTargetMetadata;
            DisableLinkPathTracking = shellLink.DisableLinkPathTracking;
            DisableKnownFolderRelativeTracking = shellLink.DisableKnownFolderRelativeTracking;
            NoKnownFolderAlias = shellLink.NoKnownFolderAlias;
            AllowLinkToLink = shellLink.AllowLinkToLink;
            UnaliasOnSave = shellLink.UnaliasOnSave;
            PreferEnvironmentPath = shellLink.PreferEnvironmentPath;
            KeepLocalIdListForUncTarget = shellLink.KeepLocalIdListForUncTarget;
        }

        /// <summary>
        /// Gets the path of the currently loaded shortcut file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the target path of the shortcut.
        /// </summary>
        public string? TargetPath { get; }

        /// <summary>
        /// Gets the description of the shortcut.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the working directory for the shortcut target.
        /// </summary>
        public string? WorkingDirectory { get; }

        /// <summary>
        /// Gets the command-line arguments for the shortcut target.
        /// </summary>
        public string? Arguments { get; }

        /// <summary>
        /// Gets the hotkey for the shortcut.
        /// </summary>
        public string? Hotkey { get; }

        /// <summary>
        /// Gets the window show state for the shortcut target.
        /// </summary>
        public SHOW_WINDOW_CMD WindowStyle { get; }

        /// <summary>
        /// Gets the icon location for the shortcut.
        /// </summary>
        public string? IconLocation { get; }

        /// <summary>
        /// Gets the icon index in the icon location file.
        /// </summary>
        public int IconIndex { get; }

        /// <summary>
        /// Gets the AppUserModel ID for the shortcut.
        /// </summary>
        public string? AppUserModelId { get; }

        /// <summary>
        /// Gets whether the shortcut is excluded from "Show in New Install" lists.
        /// </summary>
        public bool? AppUserModelExcludeFromShowInNewInstall { get; }

        /// <summary>
        /// Gets whether the shortcut is a destination list separator.
        /// </summary>
        public bool? AppUserModelIsDestListSeparator { get; }

        /// <summary>
        /// Gets whether the application runs in dual mode.
        /// </summary>
        public bool? AppUserModelIsDualMode { get; }

        /// <summary>
        /// Gets whether pinning is prevented for the shortcut.
        /// </summary>
        public bool? AppUserModelPreventPinning { get; }

        /// <summary>
        /// Gets the relaunch command for the shortcut.
        /// </summary>
        public string? AppUserModelRelaunchCommand { get; }

        /// <summary>
        /// Gets the relaunch display name resource for the shortcut.
        /// </summary>
        public string? AppUserModelRelaunchDisplayNameResource { get; }

        /// <summary>
        /// Gets the relaunch icon resource for the shortcut.
        /// </summary>
        public string? AppUserModelRelaunchIconResource { get; }

        /// <summary>
        /// Gets the start pin option for the shortcut.
        /// </summary>
        public uint? AppUserModelStartPinOption { get; }

        /// <summary>
        /// Gets the toast activator CLSID for the shortcut.
        /// </summary>
        public Guid? AppUserModelToastActivatorClsid { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut has an ID list.
        /// </summary>
        public bool HasIdList { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut has link info.
        /// </summary>
        public bool HasLinkInfo { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut has a name.
        /// </summary>
        public bool HasName { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut has a relative path.
        /// </summary>
        public bool HasRelativePath { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut has a working directory.
        /// </summary>
        public bool HasWorkingDirectory { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut has arguments.
        /// </summary>
        public bool HasArguments { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut has an icon location.
        /// </summary>
        public bool HasIconLocation { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut uses Unicode strings.
        /// </summary>
        public bool IsUnicode { get; }

        /// <summary>
        /// Gets a value indicating whether link info should not be stored.
        /// </summary>
        public bool ForceNoLinkInfo { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut contains expandable strings.
        /// </summary>
        public bool HasExpandableStrings { get; }

        /// <summary>
        /// Gets a value indicating whether the target runs in a separate VDM.
        /// </summary>
        public bool RunInSeparate { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut has a Darwin ID.
        /// </summary>
        public bool HasDarwinId { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut runs as a different user.
        /// </summary>
        public bool RunAsUser { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut has an expanded icon size.
        /// </summary>
        public bool HasExpandedIconSize { get; }

        /// <summary>
        /// Gets a value indicating whether PIDL alias is disabled.
        /// </summary>
        public bool NoPidlAlias { get; }

        /// <summary>
        /// Gets a value indicating whether UNC path is forced.
        /// </summary>
        public bool ForceUncName { get; }

        /// <summary>
        /// Gets a value indicating whether the shortcut runs with shim layer.
        /// </summary>
        public bool RunWithShimLayer { get; }

        /// <summary>
        /// Gets a value indicating whether link tracking is disabled.
        /// </summary>
        public bool ForceNoLinkTrack { get; }

        /// <summary>
        /// Gets a value indicating whether target metadata is enabled.
        /// </summary>
        public bool EnableTargetMetadata { get; }

        /// <summary>
        /// Gets a value indicating whether link path tracking is disabled.
        /// </summary>
        public bool DisableLinkPathTracking { get; }

        /// <summary>
        /// Gets a value indicating whether known folder relative tracking is disabled.
        /// </summary>
        public bool DisableKnownFolderRelativeTracking { get; }

        /// <summary>
        /// Gets a value indicating whether known folder alias should not be used.
        /// </summary>
        public bool NoKnownFolderAlias { get; }

        /// <summary>
        /// Gets a value indicating whether linking to a link is allowed.
        /// </summary>
        public bool AllowLinkToLink { get; }

        /// <summary>
        /// Gets a value indicating whether paths should be un-aliased on save.
        /// </summary>
        public bool UnaliasOnSave { get; }

        /// <summary>
        /// Gets a value indicating whether environment paths are preferred.
        /// </summary>
        public bool PreferEnvironmentPath { get; }

        /// <summary>
        /// Gets a value indicating whether local ID lists are kept for UNC targets.
        /// </summary>
        public bool KeepLocalIdListForUncTarget { get; }
    }
}
