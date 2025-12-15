using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSADT.Types
{
    /// <summary>
    /// Represents information about reboot and pending operations on the system.
    /// </summary>
    public sealed record RebootInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RebootInfo"/> struct with the specified values.
        /// </summary>
        /// <param name="computerName">The name of the computer.</param>
        /// <param name="lastBootUpTime">The last boot-up time of the system.</param>
        /// <param name="isSystemRebootPending">Indicates if a system reboot is pending.</param>
        /// <param name="isCBServicingRebootPending">Indicates if Component-Based Servicing (CBS) requires a reboot.</param>
        /// <param name="isWindowsUpdateRebootPending">Indicates if a Windows Update reboot is pending.</param>
        /// <param name="isSCCMClientRebootPending">Indicates if the SCCM client requires a reboot.</param>
        /// <param name="isIntuneClientRebootPending">Indicates if the Intune Management Extension client requires a reboot.</param>
        /// <param name="isAppVRebootPending">Indicates if an App-V client requires a reboot.</param>
        /// <param name="isFileRenameRebootPending">Indicates if file rename operations require a reboot.</param>
        /// <param name="pendingFileRenameOperations">A list of pending file rename operations.</param>
        /// <param name="errorMsg">The error messages related to reboot operations.</param>
        public RebootInfo(
            string computerName,
            DateTime lastBootUpTime,
            bool isSystemRebootPending,
            bool isCBServicingRebootPending,
            bool isWindowsUpdateRebootPending,
            bool? isSCCMClientRebootPending,
            bool? isIntuneClientRebootPending,
            bool isAppVRebootPending,
            bool? isFileRenameRebootPending,
            IReadOnlyList<string>? pendingFileRenameOperations,
            IReadOnlyList<string> errorMsg)
        {
            ComputerName = !string.IsNullOrWhiteSpace(computerName) ? computerName : throw new ArgumentNullException("Computer name cannot be null or empty.", (Exception?)null);
            LastBootUpTime = lastBootUpTime;
            IsSystemRebootPending = isSystemRebootPending;
            IsCBServicingRebootPending = isCBServicingRebootPending;
            IsWindowsUpdateRebootPending = isWindowsUpdateRebootPending;
            IsSCCMClientRebootPending = isSCCMClientRebootPending;
            IsIntuneClientRebootPending = isIntuneClientRebootPending;
            IsAppVRebootPending = isAppVRebootPending;
            IsFileRenameRebootPending = isFileRenameRebootPending;
            PendingFileRenameOperations = new ReadOnlyCollection<string>(pendingFileRenameOperations?.Count > 0 ? [.. pendingFileRenameOperations] : []);
            ErrorMsg = new ReadOnlyCollection<string>([.. errorMsg]);
        }

        /// <summary>
        /// Returns a value indicating whether any reboot is pending.
        /// </summary>
        /// <returns>True if any reboot is pending; otherwise false.</returns>
        public bool HasPendingReboot()
        {
            return IsSystemRebootPending || IsCBServicingRebootPending || IsWindowsUpdateRebootPending || IsSCCMClientRebootPending == true || IsAppVRebootPending || IsFileRenameRebootPending == true;
        }

        /// <summary>
        /// Gets the name of the computer.
        /// </summary>
        public string ComputerName { get; }

        /// <summary>
        /// Gets the last boot-up time of the system.
        /// </summary>
        public DateTime LastBootUpTime { get; }

        /// <summary>
        /// Gets a value indicating whether a system reboot is pending.
        /// </summary>
        public bool IsSystemRebootPending { get; }

        /// <summary>
        /// Gets a value indicating whether Component-Based Servicing (CBS) requires a reboot.
        /// </summary>
        public bool IsCBServicingRebootPending { get; }

        /// <summary>
        /// Gets a value indicating whether a Windows Update reboot is pending.
        /// </summary>
        public bool IsWindowsUpdateRebootPending { get; }

        /// <summary>
        /// Gets a value indicating whether the SCCM client requires a reboot.
        /// </summary>
        public bool? IsSCCMClientRebootPending { get; }

        /// <summary>
        /// Gets a value indicating whether the Intune Management Extension client requires a reboot.
        /// </summary>
        public bool? IsIntuneClientRebootPending { get; }

        /// <summary>
        /// Gets a value indicating whether an App-V client requires a reboot.
        /// </summary>
        public bool IsAppVRebootPending { get; }

        /// <summary>
        /// Gets a value indicating whether file rename operations require a reboot.
        /// </summary>
        public bool? IsFileRenameRebootPending { get; }

        /// <summary>
        /// Gets the list of pending file rename operations.
        /// </summary>
        public IReadOnlyList<string> PendingFileRenameOperations { get; }

        /// <summary>
        /// Gets the error messages related to reboot operations.
        /// </summary>
        public IReadOnlyList<string> ErrorMsg { get; }
    }
}
