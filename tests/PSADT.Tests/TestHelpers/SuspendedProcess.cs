using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using PSADT.Interop;
using PSADT.Interop.SafeHandles;
using PSADT.SafeHandles;
using Windows.Win32.System.Threading;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// A process created suspended, so that it never runs, and terminated when the test is done with it.
    /// </summary>
    internal sealed class SuspendedProcess : IDisposable
    {
        /// <summary>
        /// Takes ownership of a created process.
        /// </summary>
        /// <param name="processInformation">The handles and identifier the process was created with.</param>
        private SuspendedProcess(in PROCESS_INFORMATION processInformation)
        {
            Handle = new(processInformation.hProcess, ownsHandle: true);
            thread = new(processInformation.hThread, ownsHandle: true);
            Id = processInformation.dwProcessId;
        }

        /// <summary>
        /// A handle to the process with every right to it.
        /// </summary>
        public SafeProcessHandle Handle { get; }

        /// <summary>
        /// The process's identifier.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Creates a suspended process as a child of this one.
        /// </summary>
        /// <returns>The created process.</returns>
        public static SuspendedProcess Start()
        {
            STARTUPINFOW startupInfo = new() { cb = (uint)Unsafe.SizeOf<STARTUPINFOW>() };
            Span<char> commandLine = CommandLine.ToCharArray();
            _ = NativeMethods.CreateProcess(ImagePath, ref commandLine, lpProcessAttributes: null, lpThreadAttributes: null, bInheritHandles: false, PROCESS_CREATION_FLAGS.CREATE_SUSPENDED, lpEnvironment: null, lpCurrentDirectory: null, in startupInfo, out PROCESS_INFORMATION processInformation);
            return new(in processInformation);
        }

        /// <summary>
        /// Creates a suspended process whose parent is the given process rather than this one.
        /// </summary>
        /// <param name="parent">The process to make the parent.</param>
        /// <returns>The created process.</returns>
        public static SuspendedProcess Start(SuspendedProcess parent)
        {
            // The attribute list points at the parent's handle rather than copying it, so it stays pinned until the process exists.
            using SafePinnedGCHandle parentHandle = SafePinnedGCHandle.Alloc([parent.Handle.DangerousGetHandle()]);
            using SafeProcThreadAttributeListHandle attributeList = SafeProcThreadAttributeListHandle.Alloc(1);
            _ = attributeList.Update(PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_PARENT_PROCESS, parentHandle.AsReadOnlySpan<byte>());
            STARTUPINFOEXW startupInfo = new() { lpAttributeList = (LPPROC_THREAD_ATTRIBUTE_LIST)attributeList.DangerousGetHandle() };
            startupInfo.StartupInfo.cb = (uint)Unsafe.SizeOf<STARTUPINFOEXW>();
            Span<char> commandLine = CommandLine.ToCharArray();
            _ = NativeMethods.CreateProcess(ImagePath, ref commandLine, lpProcessAttributes: null, lpThreadAttributes: null, bInheritHandles: false, PROCESS_CREATION_FLAGS.CREATE_SUSPENDED | PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT, lpEnvironment: null, lpCurrentDirectory: null, in startupInfo, out PROCESS_INFORMATION processInformation);
            return new(in processInformation);
        }

        /// <summary>
        /// Creates suspended processes until Windows gives one the given identifier, terminating the rest as it goes.
        /// </summary>
        /// <remarks>Windows gives out a freed identifier again only after many others. When measured, that took 700 to
        /// 2,900 processes, or 3 to 11 seconds.</remarks>
        /// <param name="processId">The identifier wanted, which must already be free.</param>
        /// <param name="timeout">How long to keep trying.</param>
        /// <returns>The process given the identifier, or <see langword="null"/> if none was in time.</returns>
        public static SuspendedProcess? StartWithId(uint processId, TimeSpan timeout)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                SuspendedProcess process = Start();
                if (process.Id == processId)
                {
                    return process;
                }
                process.Dispose();
            }
            return null;
        }

        /// <summary>
        /// Terminates the process and waits for it to go, which frees its identifier once nothing else holds it open.
        /// </summary>
        public void Dispose()
        {
            if (Handle.IsClosed)
            {
                return;
            }
            try
            {
                _ = NativeMethods.TerminateProcess(Handle, 1);
                _ = NativeMethods.WaitForSingleObject(Handle, 30_000);
            }
            finally
            {
                thread.Dispose();
                Handle.Dispose();
            }
        }

        /// <summary>
        /// The image every suspended process is created from, which never gets to run.
        /// </summary>
        private static readonly string ImagePath = Path.Join(Environment.SystemDirectory, "rundll32.exe");

        /// <summary>
        /// The command line every suspended process is created with.
        /// </summary>
        private static readonly string CommandLine = $"\"{ImagePath}\"\0";

        /// <summary>
        /// A handle to the process's initial thread, which is never resumed.
        /// </summary>
        private readonly SafeFileHandle thread;
    }
}
