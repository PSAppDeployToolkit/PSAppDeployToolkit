//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class ListBoxPage : Page
    {
        private List<Tuple<string, FontFamily>> _fonts = new List<Tuple<string, FontFamily>>()
        {
            new Tuple<string, FontFamily>("Arial", new FontFamily("Arial")),
            new Tuple<string, FontFamily>("Comic Sans MS", new FontFamily("Comic Sans MS")),
            new Tuple<string, FontFamily>("Courier New", new FontFamily("Courier New")),
            new Tuple<string, FontFamily>("Segoe UI", new FontFamily("Segoe UI")),
            new Tuple<string, FontFamily>("Times New Roman", new FontFamily("Times New Roman"))
        };

        public List<Tuple<string, FontFamily>> Fonts
        {
            get { return _fonts; }
        }
        public ListBoxPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        private void ColorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string colorName = e.AddedItems[0].ToString();
            switch (colorName)
            {
                case "Yellow":
                    Control1Output.Fill = new SolidColorBrush(Colors.Yellow);
                    break;
                case "Green":
                    Control1Output.Fill = new SolidColorBrush(Colors.Green);
                    break;
                case "Blue":
                    Control1Output.Fill = new SolidColorBrush(Colors.Blue);
                    break;
                case "Red":
                    Control1Output.Fill = new SolidColorBrush(Colors.Red);
                    break;
            }
        }

        private void ListBox2_Loaded(object sender, RoutedEventArgs e)
        {
            ListBox2.SelectedIndex = 2;
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example2.CSharp = Example2CS;
        }

        public string Example1Xaml => $@"
<ListBox SelectionChanged=""ColorListBox_SelectionChanged"" MinWidth=""200"">
    <sys:String>Blue</sys:String>
    <sys:String>Green</sys:String>
    <sys:String>Red</sys:String>
    <sys:String>Yellow</sys:String>
</ListBox>
";

        public string Example2Xaml => $@"
<ListBox x:Name=""ListBox2""
    DataContext=""{{Binding RelativeSource={{RelativeSource Mode=FindAncestor, AncestorType={{x:Type ui:Page}}}}}}""
    DisplayMemberPath=""Item1""
    ItemsSource=""{{Binding Fonts}}""
    Loaded=""ListBox2_Loaded""
    SelectedValuePath=""Item2"" />
";

        public string Example2CS => $@"
private List<Tuple<string, FontFamily>> _fonts = new List<Tuple<string, FontFamily>>()
{{
    new Tuple<string, FontFamily>(""Arial"", new FontFamily(""Arial"")),
    new Tuple<string, FontFamily>(""Comic Sans MS"", new FontFamily(""Comic Sans MS"")),
    new Tuple<string, FontFamily>(""Courier New"", new FontFamily(""Courier New"")),
    new Tuple<string, FontFamily>(""Segoe UI"", new FontFamily(""Segoe UI"")),
    new Tuple<string, FontFamily>(""Times New Roman"", new FontFamily(""Times New Roman""))
}};

public List<Tuple<string, FontFamily>> Fonts
{{
    get {{ return _fonts; }}
}}
";

        #endregion

    }
}
