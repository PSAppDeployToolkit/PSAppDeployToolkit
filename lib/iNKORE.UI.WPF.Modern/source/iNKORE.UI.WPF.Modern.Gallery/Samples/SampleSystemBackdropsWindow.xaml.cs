using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using iNKORE.UI.WPF.Modern.Helpers;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.UI.Xaml;

namespace iNKORE.UI.WPF.Modern.Gallery.Samples
{
    /// <summary>
    /// SampleSystemBackdropsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SampleSystemBackdropsWindow : Window
    {

        public enum BackdropPickerMode
        {
            Full,        // None, Mica, MicaAlt (Tabbed), Acrylic
            MicaOnly,    // Mica, MicaAlt (Tabbed), None
            AcrylicOnly  // Acrylic, None
        }

        private readonly BackdropPickerMode _mode;

        public SampleSystemBackdropsWindow()
            : this(BackdropPickerMode.Full)
        {
        }

        public SampleSystemBackdropsWindow(BackdropPickerMode mode)
        {
            InitializeComponent();

            _mode = mode;
            PopulateBackdropCombo();

            cbBackdrop.SelectedIndex = 0;
            cbTheme.SelectedIndex    = 0;
        }

        private void PopulateBackdropCombo()
        {
            cbBackdrop.Items.Clear();

            BackdropType[] baseList = _mode switch
            {
                BackdropPickerMode.MicaOnly    => new[] { BackdropType.Mica, BackdropType.Tabbed },
                BackdropPickerMode.AcrylicOnly => new[] { BackdropType.Acrylic },
                BackdropPickerMode.Full        => new[] { BackdropType.Mica, BackdropType.Tabbed, BackdropType.Acrylic },
                _ => throw new InvalidOperationException($"Unknown mode {_mode}")
            };

            foreach (var b in baseList)
            {
                string text = b == BackdropType.Tabbed ? "MicaAlt" : b.ToString();
                cbBackdrop.Items.Add(new ComboBoxItem
                {
                    Content = text,
                    Tag     = b
                });
            }

            cbBackdrop.Items.Add(new ComboBoxItem
            {
                Content = BackdropType.None.ToString(),
                Tag     = BackdropType.None
            });
        }

        private void CbBackdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbBackdrop.SelectedItem is not ComboBoxItem item ||
                !Enum.TryParse(item.Tag?.ToString(), out BackdropType type))
                return;

            if (OSVersionHelper.IsWindows11OrGreater)
            {
                WindowHelper.SetSystemBackdropType(this, type);
            }
            else if (OSVersionHelper.IsWindows10OrGreater)
            {
                bool acrylic = (type == BackdropType.Acrylic);
                WindowHelper.SetSystemBackdropType(this,
                    acrylic ? BackdropType.Acrylic : BackdropType.None);
            }
            else if (OSVersionHelper.IsWindowsVistaOrGreater)
            {
                WindowHelper.SetUseAeroBackdrop(this, type != BackdropType.None);
            }
        }

        private void CbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTheme.SelectedItem is not ComboBoxItem item)
                return;

            string selected = item.Content?.ToString() ?? "Use system setting";

            switch (selected)
            {
                case "Light":
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                    break;
                case "Dark":
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    break;
                case "Use system setting":
                default:
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    break;
            }
        }
    }
}
