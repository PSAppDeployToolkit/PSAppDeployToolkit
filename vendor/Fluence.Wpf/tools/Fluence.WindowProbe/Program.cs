/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf;

namespace Fluence.WindowProbe
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length is 1 && string.Equals(args[0], "--help", StringComparison.Ordinal))
            {
                Console.WriteLine(ProbeOptions.Usage);
                return 0;
            }

            ProbeOptions options;
            try
            {
                options = ProbeOptions.Parse(args);
            }
            catch (ArgumentException exception)
            {
                Console.Error.WriteLine(exception.Message);
                Console.Error.WriteLine(ProbeOptions.Usage);
                return 2;
            }

            RenderOptions.ProcessRenderMode = string.Equals(options.RenderMode, "software-only", StringComparison.Ordinal) ? RenderMode.SoftwareOnly : RenderMode.Default;
            // Records are written synchronously when their lifecycle or action callback runs.
#pragma warning disable MA0045
            using StreamWriter writer = new(new FileStream(options.Output, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };
            Application app = new() { ShutdownMode = ShutdownMode.OnMainWindowClose };
            if (!string.Equals(options.Mode, "wpf-fluent", StringComparison.Ordinal))
            {
                ApplicationThemeManager.Apply(string.Equals(options.Theme, "Dark", StringComparison.Ordinal) ? ApplicationTheme.Dark : ApplicationTheme.Light, ToBackdrop(options.Backdrop));
            }

            Window window = ProbeWindowFactory.Create(options);
            NativeSnapshot snapshot = new(options, window);
            IntPtr hwnd = IntPtr.Zero;
            void Record(string phase)
            {
                ProbeRecord entry = snapshot.Capture(hwnd, phase);
                DataContractJsonSerializer serializer = new(typeof(ProbeRecord));
                using MemoryStream buffer = new();
                serializer.WriteObject(buffer, entry);
                writer.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
            }

            ProbeWindowFactory.RecordAction = (name, action) =>
            {
                Record("before-action:" + name);
                action();
                _ = window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    if (hwnd != IntPtr.Zero)
                    {
                        Record("after-action:" + name);
                    }
                }));
            };
            window.SourceInitialized += (_, _) =>
            {
                hwnd = new WindowInteropHelper(window).Handle;
                Record("SourceInitialized-event");
            };
            window.Loaded += (_, _) =>
            {
                Record("Loaded");
                if (options.AutoCloseMs > 0)
                {
                    DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(options.AutoCloseMs) };
                    timer.Tick += (_, _) => { timer.Stop(); window.Close(); };
                    timer.Start();
                }
            };
            window.ContentRendered += (_, _) => Record("ContentRendered");
            window.Activated += (_, _) => Record("Activated");
            void OnFirstRendering(object? sender, EventArgs eventArgs)
            {
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                CompositionTarget.Rendering -= OnFirstRendering;
                Record("first-Rendering-callback");
            }

            CompositionTarget.Rendering += OnFirstRendering;
            window.Closed += (_, _) =>
            {
                hwnd = IntPtr.Zero;
                Record("after-close");
            };
            Record("before-Show");
            app.MainWindow = window;
            window.Show();
            Record("after-Show");
            return app.Run();
#pragma warning restore MA0045
        }

        private static WindowBackdropType ToBackdrop(string backdrop)
        {
            return backdrop switch
            {
                "Mica" => WindowBackdropType.Mica,
                "Acrylic" => WindowBackdropType.Acrylic,
                "Tabbed" => WindowBackdropType.Tabbed,
                _ => WindowBackdropType.None,
            };
        }
    }
}
