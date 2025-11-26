// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Controls.UserControls
{
    public sealed partial class TypographyControl : UserControl
    {
        public TypographyControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ExampleProperty = 
            DependencyProperty.Register(nameof(Example), typeof(string), typeof(TypographyControl), new PropertyMetadata(""));

        public string Example
        {
            get => (string)GetValue(ExampleProperty);
            set => SetValue(ExampleProperty, value);
        }

        public static readonly DependencyProperty WeightProperty = 
            DependencyProperty.Register(nameof(Weight), typeof(string), typeof(TypographyControl), new PropertyMetadata(""));

        public string Weight
        {
            get => (string)GetValue(WeightProperty);
            set => SetValue(WeightProperty, value);
        }

        public static readonly DependencyProperty VariableFontProperty = 
            DependencyProperty.Register(nameof(VariableFont), typeof(string), typeof(TypographyControl), new PropertyMetadata(""));

        public string VariableFont
        {
            get => (string)GetValue(VariableFontProperty);
            set => SetValue(VariableFontProperty, value);
        }

        public static readonly DependencyProperty SizeLineHeightProperty = 
            DependencyProperty.Register(nameof(SizeLineHeight), typeof(string), typeof(TypographyControl), new PropertyMetadata(""));

        public string SizeLineHeight
        {
            get => (string)GetValue(SizeLineHeightProperty);
            set => SetValue(SizeLineHeightProperty, value);
        }

        public static readonly DependencyProperty ExampleStyleProperty = 
            DependencyProperty.Register(nameof(ExampleStyle), typeof(Style), typeof(TypographyControl), new PropertyMetadata(null));

        public Style ExampleStyle
        {
            get => (Style)GetValue(ExampleStyleProperty);
            set => SetValue(ExampleStyleProperty, value);
        }

        public static readonly DependencyProperty ResourceNameProperty = 
            DependencyProperty.Register(nameof(ResourceName), typeof(string), typeof(TypographyControl), new PropertyMetadata("", ResourceNameProperty_ValueChanged));

        private static void ResourceNameProperty_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TypographyControl sender)
            {
                sender.Usage = $"Style=\"{{StaticResource {{x:Static ui:ThemeKeys.{e.NewValue}Key}}}}\"";
            }
        }

        public string ResourceName
        {
            get => (string)GetValue(ResourceNameProperty);
            set => SetValue(ResourceNameProperty, value);
        }

        public static readonly DependencyProperty UsageProperty = 
            DependencyProperty.Register(nameof(Usage), typeof(string), typeof(TypographyControl), new PropertyMetadata(""));

        public string Usage
        {
            get => (string)GetValue(UsageProperty);
            set => SetValue(UsageProperty, value);
        }


        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Usage))
                {
                    Clipboard.SetText(Usage);
                    
                    // Show confirmation animation
                    VisualStateManager.GoToState(this, "ConfirmationDialogVisible", true);
                    
                    // Hide confirmation after 2 seconds
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        VisualStateManager.GoToState(this, "ConfirmationDialogHidden", true);
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                // Handle clipboard access errors silently
                System.Diagnostics.Debug.WriteLine($"Failed to copy to clipboard: {ex.Message}");
            }
        }
    }
}
