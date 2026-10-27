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
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FluenceStarterApp.Controls;

/// <summary>
/// Displays a bundled color Settings category image at the current monitor DPI.
/// </summary>
public sealed class SettingsCategoryImage : Image
{
    private static readonly int[] AvailableScales = [100, 125, 150, 200, 400];
    private static readonly Dictionary<(string Category, int Scale), BitmapImage> Images = [];

    /// <summary>Identifies which of the four starter categories to display.</summary>
    public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(
        nameof(Category),
        typeof(string),
        typeof(SettingsCategoryImage),
        new PropertyMetadata(null, OnCategoryChanged));

    /// <summary>Gets or sets General, Appearance, Notifications, or About.</summary>
    public string? Category
    {
        get => (string?)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    /// <summary>Creates an image that follows monitor DPI changes.</summary>
    public SettingsCategoryImage()
    {
        Loaded += (_, _) => RefreshSource();
    }

    /// <inheritdoc />
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        RefreshSource(newDpi.DpiScaleX);
    }

    private static void OnCategoryChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((SettingsCategoryImage)dependencyObject).RefreshSource();
    }

    private void RefreshSource(double? dpiScale = null)
    {
        string? category = Category;
        if (category is null)
        {
            Source = null;
            return;
        }

        if (category is not ("General" or "Appearance" or "Notifications" or "About"))
        {
            throw new ArgumentOutOfRangeException(nameof(Category), category, "Unknown Settings category.");
        }

        int scale = SelectScale((dpiScale ?? VisualTreeHelper.GetDpi(this).DpiScaleX) * 100);
        (string Category, int Scale) key = (category, scale);
        if (!Images.TryGetValue(key, out BitmapImage? bitmap))
        {
            string assemblyName = GetType().Assembly.GetName().Name
                ?? throw new InvalidOperationException("The application assembly has no name.");
            Uri resource = new($"pack://application:,,,/{assemblyName};component/Assets/SettingsIcons/{category}-scale-{scale}.png", UriKind.Absolute);
            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = resource;
            bitmap.EndInit();
            bitmap.Freeze();
            Images.Add(key, bitmap);
        }

        Source = bitmap;
    }

    private static int SelectScale(double percent)
    {
        int selected = AvailableScales[0];
        foreach (int candidate in AvailableScales)
        {
            if (Math.Abs(candidate - percent) < Math.Abs(selected - percent))
            {
                selected = candidate;
            }
        }

        return selected;
    }
}
