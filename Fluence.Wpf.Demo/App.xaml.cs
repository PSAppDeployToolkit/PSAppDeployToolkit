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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fluence.Wpf.Demo
{
    public partial class App : Application
    {
        private const string IconsPageTitle = "Icons";
        private const string SmokeTestArgument = "--smoke-test";
        private static readonly Uri DemoSharedStylesUri = new(
            "/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml",
            UriKind.Relative);

        /// <summary>
        /// Application entry point. Initializes the theme engine, applies the system accent
        /// color, merges shared demo styles, and shows the main gallery window. The startup
        /// sequence is: apply theme (seeds all three resource-dictionary slots) -&gt; apply accent
        /// -&gt; load demo shared styles -&gt; show window.
        /// </summary>
        /// <param name="e">The startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica);
            ApplicationAccentColorManager.ApplySystemAccent();
            LoadDemoSharedStyles();

            MainWindow mainWindow = new();
            MainWindow = mainWindow;
            mainWindow.Show();

            // Headless self-test used by CI and the screenshot harness; not part of the normal
            // app flow - safe to ignore when learning the startup sequence.
            if (IsSmokeTest(e.Args))
            {
                _ = Dispatcher.BeginInvoke(new Action(delegate { RunSmokeTest(mainWindow); }), DispatcherPriority.ApplicationIdle);
            }
        }

        private static void RunSmokeTest(MainWindow mainWindow)
        {
            foreach (DemoNavigationItem item in DemoNavigationCatalog.Items)
            {
                mainWindow.NavigateTo(item.Title);
                mainWindow.UpdateLayout();
                DrainDispatcher(mainWindow.Dispatcher);
                if (string.Equals(item.Title, IconsPageTitle, StringComparison.Ordinal))
                {
                    RealizeIconsList(mainWindow);
                }

                ExerciseSmokePageThemes(mainWindow);
            }

            mainWindow.NavigateTo("settings");
            mainWindow.UpdateLayout();
            DrainDispatcher(mainWindow.Dispatcher);
            ExerciseSmokePageThemes(mainWindow);
            ExerciseSmokeChrome(mainWindow);

            ApplicationThemeManager.Apply(ApplicationTheme.HighContrast);
            ApplicationThemeManager.Apply(ApplicationTheme.Light);

            mainWindow.Close();
        }

        private static void ExerciseSmokePageThemes(MainWindow mainWindow)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
            mainWindow.UpdateLayout();
            DrainDispatcher(mainWindow.Dispatcher);

            ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            mainWindow.UpdateLayout();
            DrainDispatcher(mainWindow.Dispatcher);
        }

        private static void ExerciseSmokeChrome(MainWindow mainWindow)
        {
            ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xC3, 0x00, 0x52));
            mainWindow.UpdateLayout();
            DrainDispatcher(mainWindow.Dispatcher);

            ApplySmokeBackdrop(mainWindow, BackdropType.Mica);
            ApplySmokeBackdrop(mainWindow, BackdropType.Acrylic);
            ApplySmokeBackdrop(mainWindow, BackdropType.Tabbed);
            ApplySmokeBackdrop(mainWindow, BackdropType.None);

            ApplicationAccentColorManager.ApplySystemAccent();
        }

        private static void ApplySmokeBackdrop(MainWindow mainWindow, BackdropType backdrop)
        {
            mainWindow.SystemBackdropType = backdrop;
            ApplicationThemeManager.Apply(ApplicationTheme.Light, backdrop);
            mainWindow.UpdateLayout();
            DrainDispatcher(mainWindow.Dispatcher);
        }

        private static void LoadDemoSharedStyles()
        {
            Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = DemoSharedStylesUri });
        }

        private static void RealizeIconsList(DependencyObject root)
        {
            ListView list = FindVisualChildByName<ListView>(root, "IconCatalogList") ?? throw new InvalidOperationException("The Icons page did not create IconCatalogList.");
            _ = list.ApplyTemplate();
            list.UpdateLayout();
            if (list.Items.Count is 0)
            {
                throw new InvalidOperationException("The Icons page did not load any icon rows.");
            }

            list.ScrollIntoView(list.Items[0]);
            list.UpdateLayout();

            if (list.ItemContainerGenerator.ContainerFromIndex(0) is not FrameworkElement firstContainer)
            {
                throw new InvalidOperationException("The Icons page did not realize the first icon row.");
            }

            _ = firstContainer.ApplyTemplate();
            firstContainer.UpdateLayout();

            int lastIndex = list.Items.Count - 1;
            list.ScrollIntoView(list.Items[lastIndex]);
            list.UpdateLayout();
            DrainDispatcher(list.Dispatcher);

            if (list.ItemContainerGenerator.ContainerFromIndex(lastIndex) is not FrameworkElement lastContainer)
            {
                throw new InvalidOperationException("The Icons page did not realize the final icon row after scrolling.");
            }

            _ = lastContainer.ApplyTemplate();
            lastContainer.UpdateLayout();
        }

        private static T? FindVisualChildByName<T>(DependencyObject root, string name)
            where T : FrameworkElement
        {
            if (root is null)
            {
                return null;
            }

            if (root is T rootElement && string.Equals(rootElement.Name, name, StringComparison.Ordinal))
            {
                return rootElement;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                T? match = FindVisualChildByName<T>(VisualTreeHelper.GetChild(root, i), name);
                if (match is not null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void DrainDispatcher(Dispatcher dispatcher)
        {
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(static delegate { }));
        }

        private static bool IsSmokeTest(string[] args)
        {
            if (args is null)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], SmokeTestArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
