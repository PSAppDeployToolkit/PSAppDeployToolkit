using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.Collections;

namespace PSADT.DeviceManagement
{
    /// <summary>
    /// Represents information about reboot and pending operations on the system.
    /// </summary>
    public sealed record class RebootInfo
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
            ArgumentException.ThrowIfNullOrWhiteSpace(computerName);
            ComputerName = computerName;
            LastBootUpTime = lastBootUpTime;
            IsSystemRebootPending = isSystemRebootPending;
            IsCBServicingRebootPending = isCBServicingRebootPending;
            IsWindowsUpdateRebootPending = isWindowsUpdateRebootPending;
            IsSCCMClientRebootPending = isSCCMClientRebootPending;
            IsIntuneClientRebootPending = isIntuneClientRebootPending;
            IsAppVRebootPending = isAppVRebootPending;
            IsFileRenameRebootPending = isFileRenameRebootPending;
            PendingFileRenameOperationsValue = new ValueList<string>(pendingFileRenameOperations?.Count > 0 ? [.. pendingFileRenameOperations] : []);
            ErrorMsgValue = new ValueList<string>([.. errorMsg]);
        }

        /// <summary>
        /// Returns a value indicating whether any reboot is pending.
        /// </summary>
        /// <remarks>Every source this type reads is taken into account, including both management clients. A source
        /// that could not be read reports <see langword="null"/> rather than <see langword="false"/>, and an unread
        /// source is not a reason to say no reboot is pending - so those are treated as no answer and the remaining
        /// sources decide.</remarks>
        /// <returns>True if any reboot is pending; otherwise false.</returns>
        public bool HasPendingReboot()
        {
            return IsSystemRebootPending || IsCBServicingRebootPending || IsWindowsUpdateRebootPending || IsSCCMClientRebootPending is true || IsIntuneClientRebootPending is true || IsAppVRebootPending || IsFileRenameRebootPending is true;
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
        /// <remarks>Held as a <see cref="ValueList{T}"/> so that this record compares by the list's contents. Every collection the
        /// framework offers compares by reference, so holding one directly would make two descriptions of the same
        /// thing unequal however alike they were.</remarks>
        public IReadOnlyList<string> PendingFileRenameOperations => new ReadOnlyCollection<string>([.. PendingFileRenameOperationsValue]);

        /// <summary>
        /// Gets the error messages related to reboot operations.
        /// </summary>
        public IReadOnlyList<string> ErrorMsg => new ReadOnlyCollection<string>([.. ErrorMsgValue]);

        /// <summary>
        /// The list recorded for <see cref="PendingFileRenameOperations"/>.
        /// </summary>
        private readonly ValueList<string> PendingFileRenameOperationsValue;

        /// <summary>
        /// The list recorded for <see cref="ErrorMsg"/>.
        /// </summary>
        private readonly ValueList<string> ErrorMsgValue;
    }
}
