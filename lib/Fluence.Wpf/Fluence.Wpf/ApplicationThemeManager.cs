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
using System.Windows.Media;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Theming;

namespace Fluence.Wpf
{
    /// <summary>
    /// Manages Fluence.Wpf theme resource dictionaries, accent coordination, and runtime theme changes.
    /// </summary>
    /// <remarks>
    /// <see cref="Apply"/> delegates to the internal theme engine, which publishes one computed
    /// colors-plus-brushes dictionary at slot [0] and replaces it on every change.
    /// </remarks>
    public static class ApplicationThemeManager
    {
        /// <summary>
        /// Gets the currently requested theme (may be <see cref="ApplicationTheme.Auto"/>).
        /// </summary>
        public static ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.Auto;

        /// <summary>
        /// Gets the currently requested backdrop type.
        /// </summary>
        public static BackdropType CurrentBackdrop { get; private set; } = BackdropType.Auto;

        /// <summary>
        /// Gets the concrete theme (Light, Dark, or HighContrast) that was resolved and applied during
        /// the most recent theme pipeline run (that is, the last call to <see cref="Apply"/> or to any
        /// <see cref="ApplicationAccentColorManager"/> apply method
        /// (<see cref="ApplicationAccentColorManager.ApplySystemAccent"/>,
        /// <see cref="ApplicationAccentColorManager.ApplyApplicationAccent"/>, or
        /// <see cref="ApplicationAccentColorManager.ApplyCustomAccent"/>), whichever ran last. When
        /// <see cref="CurrentTheme"/> is <see cref="ApplicationTheme.Auto"/>, this reflects the OS theme
        /// at the time of that last pipeline run; it does not update automatically when the OS theme
        /// changes without a subsequent pipeline run. Before the first pipeline run, this property returns
        /// <see cref="ApplicationTheme.Light"/> as the pre-initialization default.
        /// </summary>
        public static ApplicationTheme ResolvedTheme => FluenceThemeEngine.ResolvedTheme;

        /// <summary>
        /// Gets a value indicating whether the Windows system (window-chrome) color mode is currently Dark.
        /// Reflects the live registry value; independent of <see cref="CurrentTheme"/>.
        /// </summary>
        public static bool IsSystemInDarkMode => !RegistryHelper.GetSystemUsesLightTheme();

        /// <summary>
        /// Gets a value indicating whether the Windows app color mode is currently Dark.
        /// Reflects the live registry value; independent of <see cref="CurrentTheme"/>.
        /// </summary>
        public static bool IsAppInDarkMode => !RegistryHelper.GetAppsUseLightTheme();

        /// <summary>
        /// Raised after a theme or accent change has been applied.
        /// </summary>
        public static event EventHandler<ThemeChangedEventArgs>? Changed;

        /// <summary>
        /// Initializes the theme resource stack or applies a later theme change.
        /// </summary>
        /// <param name="theme">The requested application theme. Use <see cref="ApplicationTheme.Auto"/> to follow Windows app theme settings.</param>
        /// <param name="backdrop">The requested window backdrop policy retained for <see cref="CurrentBackdrop"/> consumers.</param>
        /// <param name="updateAccent">Accepted for signature compatibility; the single pipeline always rebuilds the computed dictionary using the current accent intent.</param>
        /// <remarks>
        /// The first call seeds the three resource slots ([0] computed colors and brushes, [1] Typography,
        /// [2] Generic). Later calls replace the computed slot so <c>DynamicResource</c> consumers re-resolve.
        /// </remarks>
        public static void Apply(ApplicationTheme theme, BackdropType backdrop = BackdropType.Auto, bool updateAccent = true)
        {
            if (_isApplying)
            {
                return;
            }
            _isApplying = true;
            try
            {
                ApplicationAccentColorManager.EnsureInitialized();
                TabKeyboardNavigation.EnsureRegistered();
                CurrentTheme = theme;
                CurrentBackdrop = backdrop;
                _ = updateAccent; // retained for signature compatibility; the pipeline always rebuilds.
                FluenceThemeEngine.Apply(theme);
                OnChanged(FluenceThemeEngine.ResolvedTheme);
            }
            finally
            {
                _isApplying = false;
            }
        }

        /// <summary>
        /// Re-applies with <see cref="ApplicationTheme.Auto"/> to pick up system changes.
        /// </summary>
        public static void ApplySystemTheme()
        {
            Apply(ApplicationTheme.Auto, CurrentBackdrop);
        }

        internal static ApplicationTheme ResolveTheme(ApplicationTheme theme)
        {
            return ThemeResolver.Resolve(theme);
        }

        internal static ApplicationTheme GetResolvedTheme()
        {
            return ResolveTheme(CurrentTheme);
        }

        private static void OnChanged(ApplicationTheme resolvedTheme)
        {
            if (Changed is not null)
            {
                Color accent = ApplicationAccentColorManager.SystemAccentColor;
                Changed(sender: null, new ThemeChangedEventArgs(resolvedTheme, accent));
            }
        }

        internal static void ResetForTesting()
        {
            CurrentTheme = ApplicationTheme.Auto;
            CurrentBackdrop = BackdropType.Auto;
            _isApplying = false;
            FluenceThemeEngine.ResetForTesting();
        }

        /*
         * Slot model in Application.Current.Resources.MergedDictionaries after the first Apply:
         *   [0] Computed colors + brushes - REPLACED on every theme/accent change
         *   [1] Typography.xaml           - static, theme-independent
         *   [2] Generic.xaml              - static, control templates
         * The computed slot is built by FluenceThemeEngine.
         */

        // Flag to prevent re-entrant calls to Apply().
        private static bool _isApplying;
    }
}
