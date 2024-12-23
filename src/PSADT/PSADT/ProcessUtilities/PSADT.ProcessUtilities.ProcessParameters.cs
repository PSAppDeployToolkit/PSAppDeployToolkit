using System;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.PInvokes;

namespace PSADT.ProcessUtilities
{
    public static class ProcessParameters
    {
        internal static string? GetProcessParametersString(uint processId, PEB_OFFSET offset)
        {
            using var processHandle = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
                false,
                processId);

            if (processHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            bool isTargetWow64 = IsWow64Process(processHandle);
            bool isTarget64BitProcess = Environment.Is64BitOperatingSystem && !isTargetWow64;

            // All offset values below have been tested on Windows 7, 8, and 10 only.
            long processParametersOffset = isTarget64BitProcess ? 0x20 : 0x10;
            long stringOffset = offset switch
            {
                PEB_OFFSET.CurrentDirectory => isTarget64BitProcess ? 0x38 : 0x24,
                PEB_OFFSET.CommandLine => isTarget64BitProcess ? 0x70 : 0x40,
                _ => throw new ArgumentException("Invalid PEB offset", nameof(offset))
            };

            try
            {
                long pebAddress = 0;

                if (isTargetWow64)
                {
                    var pbi = IntPtr.Zero;
                    int status = NativeMethods.NtQueryInformationProcess(
                        processHandle,
                        PROCESSINFOCLASS.ProcessWow64Information,
                        ref pbi,
                        (uint)IntPtr.Size,
                        out _);

                    if (status != 0)
                        return null;

                    pebAddress = pbi.ToInt64();
                    var pp = new SafeHGlobalHandle(IntPtr.Size);

                    try
                    {
                        if (!NativeMethods.ReadProcessMemory(processHandle, pebAddress + processParametersOffset, ref pp, IntPtr.Size, out _))
                            return null;
                    }
                    finally
                    {
                        pp.Dispose();
                    }

                    var us = new UNICODE_STRING_32();
                    if (!NativeMethods.ReadProcessMemory(processHandle, (long)pp.DangerousGetHandle() + stringOffset, ref us, Marshal.SizeOf(us), out _))
                        return null;

                    if (us.Buffer == 0 || us.Length == 0)
                        return null;

                    var stringBuilder = new StringBuilder(us.Length / 2);
                    if (!NativeMethods.ReadProcessMemory(processHandle, us.Buffer, stringBuilder, us.Length, out _))
                        return null;

                    return RemoveTrailingBackslash(stringBuilder.ToString());
                }
                else if (Environment.Is64BitOperatingSystem != Environment.Is64BitProcess)
                {
                    var pbi = new PROCESS_BASIC_INFORMATION_WOW64();
                    int status = NativeMethods.NtWow64QueryInformationProcess64(
                        processHandle,
                        PROCESSINFOCLASS.ProcessBasicInformation,
                        ref pbi,
                        (uint)Marshal.SizeOf(pbi),
                        out _);

                    if (status != 0)
                        return null;

                    pebAddress = pbi.PebBaseAddress;

                    long pp = 0;
                    status = NativeMethods.NtWow64ReadVirtualMemory64(processHandle, pebAddress + processParametersOffset, ref pp, Marshal.SizeOf(pp), out _);
                    if (status != 0)
                        return null;

                    var us = new UNICODE_STRING_WOW64();
                    status = NativeMethods.NtWow64ReadVirtualMemory64(processHandle, pp + stringOffset, ref us, Marshal.SizeOf(us), out _);
                    if (status != 0)
                        return null;

                    if (us.Buffer == 0 || us.Length == 0)
                        return null;

                    var stringBuilder = new StringBuilder(us.Length / 2);
                    status = NativeMethods.NtWow64ReadVirtualMemory64(processHandle, us.Buffer, stringBuilder, us.Length, out _);
                    if (status != 0)
                        return null;

                    return RemoveTrailingBackslash(stringBuilder.ToString());
                }
                else
                {
                    var pbi = new PROCESS_BASIC_INFORMATION();
                    int status = NativeMethods.NtQueryInformationProcess(
                        processHandle,
                        PROCESSINFOCLASS.ProcessBasicInformation,
                        ref pbi,
                        (uint)Marshal.SizeOf(pbi),
                        out _);

                    if (status != 0)
                        return null;

                    pebAddress = pbi.PebBaseAddress.ToInt64();
                    var pp = new SafeHGlobalHandle(IntPtr.Size);
                    
                    try
                    {
                        if (!NativeMethods.ReadProcessMemory(processHandle, pebAddress + processParametersOffset, ref pp, IntPtr.Size, out _))
                            return null;
                    }
                    finally
                    {
                        pp.Dispose();
                    }

                    var us = new UNICODE_STRING();
                    if (!NativeMethods.ReadProcessMemory(processHandle, (long)pp.DangerousGetHandle() + stringOffset, ref us, Marshal.SizeOf(us), out _))
                        return null;

                    if (us.Buffer == IntPtr.Zero || us.Length == 0)
                        return null;

                    var stringBuilder = new StringBuilder(us.Length / 2);
                    if (!NativeMethods.ReadProcessMemory(processHandle, us.Buffer.ToInt64(), stringBuilder, us.Length, out _))
                        return null;

                    return RemoveTrailingBackslash(stringBuilder.ToString());
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the working directory of a process by its process ID.
        /// </summary>
        /// <param name="processId">The process ID to query.</param>
        /// <returns>The working directory path, or an empty string if it cannot be retrieved.</returns>
        public static string GetWorkingDirectory(uint processId)
        {
            try
            {
                using var processHandle = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
                    false,
                    processId);

                if (processHandle.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return GetProcessParametersString(processId, PEB_OFFSET.CurrentDirectory) ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the command line of a process by its process ID.
        /// </summary>
        /// <param name="processId">The process ID to query.</param>
        /// <returns>The command line string, or an empty string if it cannot be retrieved.</returns>
        public static string GetCommandLine(uint processId)
        {
            try
            {
                using var processHandle = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
                    false,
                    processId);

                if (processHandle.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return GetProcessParametersString(processId, PEB_OFFSET.CommandLine) ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static bool IsWow64Process(SafeProcessHandle processHandle)
        {
            if (Environment.OSVersion.Version.Major < 5 ||
                (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor < 1))
                return false;

            return NativeMethods.IsWow64Process(processHandle, out bool isWow64) && isWow64;
        }

        internal static string RemoveTrailingBackslash(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            return path.TrimEnd('\\');
        }
    }
}