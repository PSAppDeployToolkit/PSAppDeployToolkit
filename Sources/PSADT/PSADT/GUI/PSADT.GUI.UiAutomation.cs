using System;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using PSADT.PInvoke;
using PSADT.OperatingSystem;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.GUI
{
    /// <summary>
    /// Provides methods for interacting with UI automation on Windows.
    /// </summary>
    public static class UiAutomation
    {
        /// <summary>
        /// Enumerates all top-level windows on the screen.
        /// </summary>
        /// <returns>A list of window handles.</returns>
        public static List<IntPtr> EnumWindows()
        {
            List<IntPtr> windows = new List<IntPtr>();
            GCHandle hItems = GCHandle.Alloc(windows);

            try
            {
                IntPtr lItems = GCHandle.ToIntPtr(hItems);
                if (!NativeMethods.EnumWindows(EnumWindowsProc, ref lItems))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to enumerate windows. Error code: {errorCode}");
                }
            }
            finally
            {
                if (hItems.IsAllocated)
                {
                    hItems.Free();
                }
            }
            return windows;
        }

        /// <summary>
        /// Callback function to enumerate windows.
        /// </summary>
        /// <param name="hWnd">A handle to a window.</param>
        /// <param name="lItems">A pointer to the application-defined value.</param>
        /// <returns>True to continue enumeration; false to stop.</returns>
        private static bool EnumWindowsProc(IntPtr hWnd, ref IntPtr lItems)
        {
            if (hWnd != IntPtr.Zero)
            {
                GCHandle hItems = GCHandle.FromIntPtr(lItems);
                if (hItems.Target is List<IntPtr> items)
                {
                    items.Add(hWnd);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Enumerates child windows of a specified parent window.
        /// </summary>
        /// <param name="parentWindowHandle">The handle of the parent window.</param>
        /// <returns>A list of child window handles.</returns>
        public static List<IntPtr> EnumChildWindows(IntPtr parentWindowHandle)
        {
            List<IntPtr> childWindows = new List<IntPtr>();
            GCHandle gch = GCHandle.Alloc(childWindows);

            try
            {
                NativeMethods.EnumChildWindows(parentWindowHandle, (hWnd, lParam) =>
                {
                    GCHandle gchList = GCHandle.FromIntPtr(lParam);
                    if (gchList.Target is List<IntPtr> list)
                    {
                        list.Add(hWnd);
                    }
                    return true;
                }, GCHandle.ToIntPtr(gch));
            }
            finally
            {
                if (gch.IsAllocated)
                {
                    gch.Free();
                }
            }

            return childWindows;
        }

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <returns>The window text.</returns>
        public static string GetWindowText(IntPtr hWnd)
        {
            int textLength = NativeMethods.GetWindowTextLength(hWnd);
            if (textLength > 0)
            {
                char[] buffer = new char[textLength + 1];
                if (NativeMethods.GetWindowText(hWnd, buffer, buffer.Length) > 0)
                {
                    return new string(buffer).TrimEnd('\0');
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Finds a window by its title and process ID using a specified string comparison mode.
        /// </summary>
        /// <param name="partialTitle">The partial title to search for.</param>
        /// <param name="comparisonMode">The comparison mode to use: StartsWith, Contains, or EndsWith.</param>
        /// <param name="processId">The process ID to filter windows by. Pass 0 to search across all processes.</param>
        /// <returns>The handle of the window if found; otherwise, IntPtr.Zero.</returns>
        public static IntPtr FindWindowByPartialTitle(string partialTitle, StringComparisonMode comparisonMode, int processId = 0)
        {
            List<IntPtr> windows = EnumWindows();

            foreach (var windowHandle in windows)
            {
                int threadId = NativeMethods.GetWindowThreadProcessId(windowHandle, out int windowProcessId);
                if (threadId == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to retrieve the process ID for window handle {windowHandle}. Error code: {errorCode}");
                }

                if (processId == 0 || windowProcessId == processId)
                {
                    string windowTitle = GetWindowText(windowHandle);

                    switch (comparisonMode)
                    {
                        case StringComparisonMode.StartsWith:
                            if (windowTitle.StartsWith(partialTitle, StringComparison.OrdinalIgnoreCase))
                            {
                                return windowHandle;
                            }
                            break;

                        case StringComparisonMode.Contains:
                            if (windowTitle.IndexOf(partialTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return windowHandle;
                            }
                            break;

                        case StringComparisonMode.EndsWith:
                            if (windowTitle.EndsWith(partialTitle, StringComparison.OrdinalIgnoreCase))
                            {
                                return windowHandle;
                            }
                            break;
                    }
                }
            }

            return IntPtr.Zero;
        }


        /// <summary>
        /// Brings the specified window to the foreground.
        /// </summary>
        /// <param name="windowHandle">A handle to the window.</param>
        /// <returns>True if the window was brought to the foreground; otherwise, false.</returns>
        public static bool BringWindowToFront(IntPtr windowHandle)
        {
            if (NativeMethods.IsIconic(windowHandle))
            {
                NativeMethods.ShowWindow(windowHandle, ShowWindowEnum.Restore);
            }

            IntPtr currentForegroundWindow = NativeMethods.GetForegroundWindow();
            int currentThreadId = NativeMethods.GetCurrentThreadId();
            int windowThreadProcessId = NativeMethods.GetWindowThreadProcessId(currentForegroundWindow, out _);

            if (!NativeMethods.AttachThreadInput(windowThreadProcessId, currentThreadId, true))
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to attach thread input. Error code: {errorCode}");
            }

            try
            {
                if (!NativeMethods.BringWindowToTop(windowHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to bring window to top. Error code: {errorCode}");
                }

                if (!NativeMethods.SetForegroundWindow(windowHandle) ||
                    NativeMethods.SetActiveWindow(windowHandle) == IntPtr.Zero ||
                    NativeMethods.SetFocus(windowHandle) == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to set window state. Error code: {errorCode}");
                }

                return true;
            }
            finally
            {
                NativeMethods.AttachThreadInput(windowThreadProcessId, currentThreadId, false);
            }
        }

        /// <summary>
        /// Waits for a window to be ready (enabled and visible) within a specified timeout.
        /// </summary>
        /// <param name="windowHandle">The handle of the window to check.</param>
        /// <param name="timeoutMilliseconds">The maximum time to wait in milliseconds.</param>
        /// <returns>True if the window becomes ready within the timeout; otherwise, false.</returns>
        public static bool WaitForWindowToBeReady(IntPtr windowHandle, int timeoutMilliseconds)
        {
            const int delay = 100; // Polling interval in milliseconds
            int elapsed = 0;

            while (elapsed < timeoutMilliseconds)
            {
                if (NativeMethods.IsWindowEnabled(windowHandle) && NativeMethods.IsWindowVisible(windowHandle))
                {
                    return true;
                }

                Thread.Sleep(delay);
                elapsed += delay;
            }

            return false; // Timeout
        }

        /// <summary>
        /// Gets the process ID of the specified window.
        /// </summary>
        /// <param name="windowHandle">A handle to the window.</param>
        /// <returns>The process ID.</returns>
        public static int GetWindowThreadProcessId(IntPtr windowHandle)
        {
            if (NativeMethods.GetWindowThreadProcessId(windowHandle, out int processID) == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to get window thread process ID. Error code: {errorCode}");
            }
            return processID;
        }

        /// <summary>
        /// Gets the specified value of a window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nIndex">The zero-based offset to the value to be retrieved.</param>
        /// <returns>The value at the specified offset.</returns>
        public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 4
                ? NativeMethods.GetWindowLong32(hWnd, nIndex)
                : NativeMethods.GetWindowLongPtr64(hWnd, nIndex);
        }

        /// <summary>
        /// Gets the user notification state.
        /// </summary>
        /// <returns>The user notification state.</returns>
        public static string GetUserNotificationState()
        {
            if (NativeMethods.SHQueryUserNotificationState(out QUERY_USER_NOTIFICATION_STATE state) != 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to query user notification state. Error code: {errorCode}");
            }
            return state.ToString();
        }

        /// <summary>
        /// Retrieves the text of a specified control.
        /// </summary>
        /// <param name="hWnd">The handle of the control.</param>
        /// <returns>The text of the control.</returns>
        public static string GetControlText(IntPtr hWnd)
        {
            int length = NativeMethods.GetWindowTextLength(hWnd) + 1;
            char[] buffer = new char[length];
            NativeMethods.GetWindowText(hWnd, buffer, length);
            return new string(buffer).TrimEnd('\0');
        }

        /// <summary>
        /// Sets focus to a specified control.
        /// </summary>
        /// <param name="hWnd">The handle of the control.</param>
        public static void SetFocusToControl(IntPtr hWnd)
        {
            NativeMethods.SetFocus(hWnd);
        }

        /// <summary>
        /// Sends a click message to a specified control.
        /// </summary>
        /// <param name="hWnd">The handle of the control.</param>
        public static void ClickControl(IntPtr hWnd)
        {
            NativeMethods.SendMessage(hWnd, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Simulates a click on a WPF <see cref="ButtonBase"/> control.
        /// </summary>
        /// <param name="button">The <see cref="ButtonBase"/> control to click.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="button"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the automation peer cannot be created or the button does not support the Invoke pattern.</exception>
        public static void PerformButtonClick(ButtonBase button)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button), "Button cannot be null.");
            }

            ButtonBaseAutomationPeer peer = FrameworkElementAutomationPeer.CreatePeerForElement(button) as ButtonBaseAutomationPeer
                ?? throw new InvalidOperationException("Unable to create automation peer for the button.");

            IInvokeProvider invoker = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider
                ?? throw new InvalidOperationException("The button does not support the Invoke pattern.");

            invoker.Invoke();
        }

        /// <summary>
        /// Finds a window by its title, finds a control within that window by its text, and clicks the control.
        /// </summary>
        /// <param name="windowTitle">The partial title of the window to search for.</param>
        /// <param name="windowComparisonMode">The comparison mode to use for the window title: StartsWith, Contains, or EndsWith.</param>
        /// <param name="controlText">The text of the control to search for.</param>
        /// <param name="controlComparisonMode">The comparison mode to use for the control text: StartsWith, Contains, or EndsWith.</param>
        /// <param name="timeoutMilliseconds">The maximum time to wait in milliseconds.</param>
        public static void FindWindowAndClickControl(string windowTitle, StringComparisonMode windowComparisonMode, string controlText, StringComparisonMode controlComparisonMode, int timeoutMilliseconds = 5000)
        {
            IntPtr windowHandle = IntPtr.Zero;
            DateTime endTime = DateTime.Now.AddMilliseconds(timeoutMilliseconds);

            while (DateTime.Now < endTime && windowHandle == IntPtr.Zero)
            {
                windowHandle = FindWindowByPartialTitle(windowTitle, windowComparisonMode);
                if (windowHandle == IntPtr.Zero)
                {
                    Thread.Sleep(100);
                }
            }

            if (windowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Window with title '{windowTitle}' not found.");
            }

            if (!WaitForWindowToBeReady(windowHandle, timeoutMilliseconds))
            {
                throw new InvalidOperationException($"Window with handle {windowHandle} is not ready.");
            }

            List<IntPtr> childWindows = EnumChildWindows(windowHandle);
            IntPtr controlHandle = IntPtr.Zero;

            foreach (var childHandle in childWindows)
            {
                string childText = GetControlText(childHandle);

                switch (controlComparisonMode)
                {
                    case StringComparisonMode.StartsWith:
                        if (childText.StartsWith(controlText, StringComparison.OrdinalIgnoreCase))
                        {
                            controlHandle = childHandle;
                        }
                        break;

                    case StringComparisonMode.Contains:
                        if (childText.IndexOf(controlText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            controlHandle = childHandle;
                        }
                        break;

                    case StringComparisonMode.EndsWith:
                        if (childText.EndsWith(controlText, StringComparison.OrdinalIgnoreCase))
                        {
                            controlHandle = childHandle;
                        }
                        break;
                }

                if (controlHandle != IntPtr.Zero)
                {
                    break;
                }
            }

            if (controlHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Control with text '{controlText}' not found in window '{windowTitle}'.");
            }

            try
            {
                ClickControl(controlHandle);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to click control with handle {controlHandle}.", ex);
            }
        }

        /// <summary>
        /// Destroys the specified menu and frees any memory that the menu occupies.
        /// </summary>
        /// <param name="hMenu">A handle to the menu to be destroyed.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the DestroyMenu operation fails.</exception>
        public static bool DestroyMenu(IntPtr hMenu)
        {
            if (hMenu == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hMenu), "The menu handle cannot be null.");
            }

            bool result = NativeMethods.DestroyMenu(hMenu);

            if (!result)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to destroy the menu. Error code: {errorCode}");
            }

            return result;
        }

        /// <summary>
        /// Attempts to set the current process as DPI aware.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the process was successfully set as DPI aware; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the operation fails to set the process as DPI aware.
        /// </exception>
        /// <remarks>
        /// Setting a process as DPI aware improves its rendering and scaling behavior on high-DPI displays.
        /// This method wraps the native <see cref="SetProcessDPIAware"/> function, which does not set last error codes for failure.
        /// </remarks>
        public static bool TrySetProcessDPIAware()
        {
            if (!NativeMethods.SetProcessDPIAware())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to set the DPI awareness level for the current process.
        /// </summary>
        /// <param name="dpiAwareness">
        /// The DPI awareness level to set for the process. Use one of the predefined <see cref="PROCESS_DPI_AWARENESS"/> values.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if the provided <paramref name="dpiAwareness"/> value is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the operation fails to set the DPI awareness level for the process.
        /// </exception>
        /// <remarks>
        /// Setting the DPI awareness level affects how the current process interacts with the system's DPI settings. 
        /// It can improve the rendering quality of the application's UI on high-DPI monitors or modify the scaling behavior of various UI elements.
        /// <para>
        /// This method will throw an <see cref="InvalidOperationException"/> if the system is unable to change the DPI awareness level.
        /// </para>
        /// </remarks>
        public static void SetProcessDpiAwareness(PROCESS_DPI_AWARENESS dpiAwareness)
        {
            if (!Enum.IsDefined(typeof(PROCESS_DPI_AWARENESS), dpiAwareness))
            {
                throw new ArgumentException("Invalid DPI awareness level provided.", nameof(dpiAwareness));
            }

            if (!NativeMethods.SetProcessDpiAwareness(dpiAwareness))
            {
                ErrorHandler.ThrowSystemError("Failed to set DPI awareness level for the process.", SystemErrorType.Win32);
            }
        }

        /// <summary>
        /// Attempts to set the DPI awareness context for the current process.
        /// </summary>
        /// <param name="dpiContext">
        /// The DPI awareness context to set for the process. Use one of the predefined <see cref="DPI_AWARENESS_CONTEXT"/> values.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if the provided <paramref name="dpiContext"/> is invalid (i.e., <see cref="IntPtr.Zero"/> is <c>true</c>).
        /// </exception>
        /// <exception cref="Win32Exception">
        /// Thrown if the operation fails to set the DPI awareness context, with additional information from the system error code.
        /// </exception>
        /// <remarks>
        /// Setting the DPI awareness context affects how the current process interacts with the system's DPI settings. This can improve
        /// the rendering quality of the application's UI on high-DPI monitors or change the scaling behavior for elements such as menus,
        /// windows, and controls.
        /// The function will throw a <see cref="Win32Exception"/> if the underlying native method call fails.
        /// </remarks>

        public static void SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT dpiContext)
        {
            if (dpiContext.IsNull)
            {
                throw new ArgumentException("Invalid DPI awareness context provided.", nameof(dpiContext));
            }

            if (!NativeMethods.SetProcessDpiAwarenessContext(dpiContext))
            {
                ErrorHandler.ThrowSystemError("Failed to set DPI awareness context.", SystemErrorType.Win32);
            }
        }

        /// <summary>
        /// Sets the appropriate DPI awareness for the current process based on the operating system version.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the appropriate DPI awareness setting could not be applied.</exception>
        /// <remarks>
        /// This method will check the operating system version and apply the most advanced DPI awareness setting supported by the system.
        /// It will attempt to use Per Monitor DPI Awareness v2 for Windows 10 (version 15063 and later), fallback to earlier versions for
        /// Windows 8.1 and above, and finally to older APIs for Windows 7 and Vista.
        /// </remarks>
        public static void SetProcessDpiAwarenessForOSVersion()
        {
            OSVersionInfo osVersionInfo = new OSVersionInfo();

            if (osVersionInfo.Version >= new Version(10, 0, 15063)) // Windows 10, Creators Update (Version 1703) and later
            {
                SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            }
            else if (osVersionInfo.Version >= new Version(10, 0, 14393)) // Windows 10, Anniversary Update (Version 1607)
            {
                SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE);
            }
            else if (osVersionInfo.Version >= new Version(6, 3, 9600)) // Windows 8.1
            {
                SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
            }
            else if (osVersionInfo.Version >= new Version(6, 0, 6000)) // Windows Vista or Windows 7
            {
                if (!TrySetProcessDPIAware())
                {
                    throw new InvalidOperationException("Failed to set DPI awareness to Process DPI Aware.");
                }
            }
            else
            {
                throw new NotSupportedException("The current operating system version does not support any known DPI awareness APIs.");
            }
        }
    }
}
