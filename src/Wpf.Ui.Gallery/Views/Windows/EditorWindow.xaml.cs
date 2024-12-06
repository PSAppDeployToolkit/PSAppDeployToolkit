// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Gallery.ViewModels.Windows;

namespace Wpf.Ui.Gallery.Views.Windows;

public partial class EditorWindow
{
    public EditorWindowViewModel ViewModel { get; init; }

    public EditorWindow(EditorWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    /*
    internal class EditorDataStack : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int
            _line = 1,
            _character = 0,
            _progress = 80;

        private string _file = "Draft";

        public int Line
        {
            get => _line;
            set
            {
                if (value == _line)
                    return;
                _line = value;
                OnPropertyChanged(nameof(Line));
            }
        }

        public int Character
        {
            get => _character;
            set
            {
                if (value == _character)
                    return;
                _character = value;
                OnPropertyChanged(nameof(Character));
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                if (value == _progress)
                    return;
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        public string File
        {
            get => _file;
            set
            {
                if (value == _file)
                    return;
                _file = value;
                OnPropertyChanged(nameof(File));
            }
        }
    }

    private EditorDataStack DataStack = new();

    public string Line { get; set; } = "0";

    public EditorWindow(EditorWindowViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();

        DataContext = DataStack;
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item)
            return;

        string tag = item?.Tag as string ?? String.Empty;

        System.Diagnostics.Debug.WriteLine("DEBUG | Clicked: " + tag, "Wpf.Ui.Demo");

        switch (tag)
        {
            case "exit":
                Close();

                break;

            case "save":
                Save();

                break;

            case "open":
                Open();

                break;

            case "new_file":
                RootTextBox.Document = new();
                DataStack.File = "Draft";

                break;

            case "new_window":
                EditorWindow editorWindow = new(ViewModel);
                editorWindow.Owner = this;
                editorWindow.Show();

                break;

            case "word_wrap":
                RootSnackbar.Title = "Word wrapping changed!";
                RootSnackbar.Message = "Currently word wrapping is " + (item.IsChecked ? "Enabled" : "Disabled");
                RootSnackbar.Show();

                break;

            case "status_bar":
                RootStatusBar.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;

                break;

            default:
                ActionDialog.Show();

                break;
        }
    }

    private void Save()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            DataStack.File = openFileDialog.FileName;
            // Save
        }
    }

    private void Open()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            DataStack.File = openFileDialog.FileName;
            // Load
        }
    }

    private void UpdateLine()
    {
        TextPointer caretPosition = RootTextBox.CaretPosition;
        TextPointer p = RootTextBox.Document.ContentStart.GetLineStartPosition(0);

        RootTextBox.CaretPosition.GetLineStartPosition(-Int32.MaxValue, out int lineMoved);

        DataStack.Line = -lineMoved;
        DataStack.Character = Math.Max(p.GetOffsetToPosition(caretPosition) - 1, 0);
    }

    private void RootTextBox_OnGotFocus(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("DEBUG | Editor got focus", "Wpf.Ui.Demo.Editor");
        UpdateLine();
    }

    private void RootTextBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 2)
            return;
        System.Diagnostics.Debug.WriteLine("DEBUG | Editor mouse down", "Wpf.Ui.Demo.Editor");
        UpdateLine();
    }

    private void RootTextBox_OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("DEBUG | Editor key up", "Wpf.Ui.Demo.Editor");
        UpdateLine();
    }

    private void ActionDialog_OnButtonRightClick(object sender, RoutedEventArgs e)
    {
        ActionDialog.Hide();
    }
    */
}
