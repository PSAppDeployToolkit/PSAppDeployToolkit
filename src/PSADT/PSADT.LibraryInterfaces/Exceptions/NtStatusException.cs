using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.System.LibraryLoader;

namespace PSADT.LibraryInterfaces.Exceptions
{
    /// <summary>
    /// The exception that is thrown for NTSTATUS error codes returned from Windows NT native API calls.
    /// </summary>
    [Serializable]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "NTSTATUS value is required for this exception type")]
    internal class NtStatusException : ExternalException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NtStatusException"/> class with the specified NTSTATUS code and a custom message.
        /// </summary>
        /// <param name="ntStatus">The NTSTATUS code that caused the exception.</param>
        /// <param name="message">A custom message that describes the error.</param>
        internal NtStatusException(NTSTATUS ntStatus, string? message = null) : base(message ?? GetMessageForNtStatus(ntStatus))
        {
            HResult = ExceptionUtilities.HRESULT_FROM_NT(ntStatus).Value;
            NtStatus = ntStatus.Value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtStatusException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
#if NET8_0_OR_GREATER
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
        protected NtStatusException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            NtStatus = info.GetInt32(nameof(NtStatus));
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
#if NET8_0_OR_GREATER
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(NtStatus), NtStatus);
        }

        /// <summary>
        /// Gets the NTSTATUS value as a CsWin32 NTSTATUS struct.
        /// </summary>
        public int NtStatus { get; }

        /// <summary>
        /// Gets the error message for the specified NTSTATUS code using FormatMessage with ntdll.dll.
        /// </summary>
        /// <param name="ntStatus">The NTSTATUS code to get the message for.</param>
        /// <returns>The error message corresponding to the NTSTATUS code.</returns>
        private static string GetMessageForNtStatus(NTSTATUS ntStatus)
        {
            // Use FormatMessage with ntdll.dll to get the error message for the NTSTATUS code. This will provide the most accurate and descriptive message for the error code.
            // https://learn.microsoft.com/en-us/windows-hardware/drivers/install/devprop-type-ntstatus#retrieving-the-descriptive-text-for-a-ntstatus-error-code-value
            string messageSuffix = $"(Exception from NTSTATUS: 0x{ntStatus.Value:X8}{(NtStatusValueNameMap.TryGetValue(ntStatus, out string? ntStatusName) ? $" ({ntStatusName})" : null)})";
            Span<char> buffer = new char[short.MaxValue];
            try
            {
                return $"{Regex.Replace(buffer.Slice(0, (int)Kernel32.FormatMessage(FormatMessageFlags, NtDllHandle, unchecked((uint)ntStatus.Value), buffer)).ToString(), @"\{.+\}", string.Empty).Trim().TrimEnd('.')}. {messageSuffix}";
            }
            catch (Win32Exception)
            {
                // That failed... Try to base the error on a Win32Exception message if possible (i.e. not ERROR_MR_MID_NOT_FOUND).
                if (ExceptionUtilities.WIN32_FROM_NT(ntStatus) is WIN32_ERROR win32Error)
                {
                    // Use the Win32Exception message only if it's valid.
                    if (ExceptionUtilities.GetMessageForWin32Error(win32Error, true) is string message && !string.IsNullOrWhiteSpace(message) && !message.StartsWith("Unknown error"))
                    {
                        return $"{message} {messageSuffix}";
                    }
                }

                // Fallback message if any of the above fails.
                return $"The requested operation failed. {messageSuffix}";
            }
        }

        /// <summary>
        /// Cached handle to ntdll.dll for FormatMessage calls.
        /// </summary>
        private static readonly FreeLibrarySafeHandle NtDllHandle = Kernel32.LoadLibraryEx("ntdll.dll", LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_SEARCH_SYSTEM32);

        /// <summary>
        /// Provides a read-only mapping of NTSTATUS enumeration values to their corresponding field names.
        /// </summary>
        /// <remarks>This dictionary is initialized using reflection to retrieve all public static fields
        /// of the NTSTATUS enumeration, allowing for efficient lookup of the string representation of NTSTATUS values.
        /// This can be useful for debugging, logging, or displaying human-readable status codes.</remarks>
        private static readonly ReadOnlyDictionary<NTSTATUS, string> NtStatusValueNameMap = new(typeof(NTSTATUS).GetFields(BindingFlags.NonPublic | BindingFlags.Static).Where(static field => field.Name != "STATUS_SUCCESS").GroupBy(static field => (NTSTATUS)field.GetValue(null)!).ToDictionary(static g => g.Key, static g => g.First().Name));

        /// <summary>
        /// Defines the options for formatting messages retrieved from the system or a specified module.
        /// </summary>
        /// <remarks>This field combines multiple formatting options to control how messages are formatted
        /// when retrieved. It is used in conjunction with the FormatMessage function to specify the source of the
        /// message and how it should be processed.</remarks>
        private const FORMAT_MESSAGE_OPTIONS FormatMessageFlags = FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_FROM_HMODULE | FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_IGNORE_INSERTS;
    }
}
