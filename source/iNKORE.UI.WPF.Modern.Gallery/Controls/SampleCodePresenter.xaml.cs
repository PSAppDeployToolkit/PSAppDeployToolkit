using ColorCodeStandard;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using iNKORE.UI.WPF.Modern.Gallery.Helpers;
using SamplesCommon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace iNKORE.UI.WPF.Modern.Gallery.Controls
{
    /// <summary>
    /// SampleCodePresenter.xaml 的交互逻辑
    /// </summary>
    public partial class SampleCodePresenter : UserControl
    {
        public static readonly DependencyProperty CodeProperty = DependencyProperty.Register("Code", typeof(string), typeof(SampleCodePresenter), new PropertyMetadata(string.Empty, OnCodeSourceFilePropertyChanged));
        public string Code
        {
            get { return (string)GetValue(CodeProperty); }
            set { SetValue(CodeProperty, value); }
        }

        public static readonly DependencyProperty IsCSharpSampleProperty = DependencyProperty.Register("IsCSharpSample", typeof(bool), typeof(SampleCodePresenter), new PropertyMetadata(false, OnCodeSourceFilePropertyChanged));
        public bool IsCSharpSample
        {
            get { return (bool)GetValue(IsCSharpSampleProperty); }
            set { SetValue(IsCSharpSampleProperty, value); }
        }

        public static readonly DependencyProperty SubstitutionsProperty = DependencyProperty.Register("Substitutions", typeof(ObservableCollection<ControlExampleSubstitution>), typeof(SampleCodePresenter), new PropertyMetadata(null, OnSubstitutionsPropertyChanged));
        public ObservableCollection<ControlExampleSubstitution> Substitutions
        {
            get => (ObservableCollection<ControlExampleSubstitution>)GetValue(SubstitutionsProperty);
            set
            {
                if (value == null)
                {
                    ClearValue(SubstitutionsProperty);
                }
                else
                {
                    SetValue(SubstitutionsProperty, value);
                }
            }
        }

        public bool IsEmpty => string.IsNullOrEmpty(Code);

        private static Regex SubstitutionPattern = new Regex(@"\$\(([^\)]+)\)");

        public SampleCodePresenter()
        {
            InitializeComponent();

            CodePresenter.TextArea.SelectionBorder = new Pen(Brushes.Transparent, 0);
            CodePresenter.TextArea.SelectionCornerRadius = 0;
            CodePresenter.TextArea.SetResourceReference(TextArea.SelectionBrushProperty, ThemeKeys.TextControlSelectionHighlightColorKey);

            // Ensure caret never shows (keep selection & copy)
            HideCaretPermanently();
            CodePresenter.TextArea.GotFocus += (s,e)=> HideCaretPermanently();
            CodePresenter.TextArea.TextView.VisualLinesChanged += (s,e)=> HideCaretPermanently();
        }

        private void HideCaretPermanently()
        {
            // Make sure the editor can still be clicked for selection but caret invisible.
            var caret = CodePresenter.TextArea.Caret;
            caret.CaretBrush = Brushes.Transparent;
            // keep focus off the editor so IME/caret logic does not repaint a visible caret
            if (CodePresenter.IsFocused)
            {
                // Move focus to parent container (still allows mouse selection highlight within AvalonEdit)
                var parent = (DependencyObject)CodePresenter.Parent;
                while (parent != null && parent is not Control)
                {
                    parent = LogicalTreeHelper.GetParent(parent);
                }
                (parent as Control)?.Focusable.Equals(true);
            }
        }

        private static void OnSubstitutionsPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            if (target is SampleCodePresenter presenter)
            {
                presenter.RegisterSubstitutions();
            }
        }

        private static void OnCodeSourceFilePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            if (target is SampleCodePresenter presenter)
            {
                presenter.ReevaluateVisibility();
            }
        }

        private void ReevaluateVisibility()
        {
            if (string.IsNullOrEmpty(Code))
            {
                Visibility = Visibility.Collapsed;
            }
            else
            {
                Visibility = Visibility.Visible;
                GenerateSyntaxHighlightedContent();
                SampleHeader.Text = IsCSharpSample ? "C#" : "XAML";
            }
        }

        private void RegisterSubstitutions()
        {
            foreach (var substitution in Substitutions)
            {
                substitution.ValueChanged += OnValueChanged;
            }
        }

        private void SampleCodePresenter_Loaded(object sender, RoutedEventArgs e)
        {
            ReevaluateVisibility();
            SampleHeader.Text = IsCSharpSample ? "C#" : "XAML";

            FixAvalonEditScrolling();

            try
            {
                if (CodePresenter?.ContextMenu != null)
                {
                    // Adjust context menu to only show 'Copy' if there is a selection
                    CodePresenter.ContextMenu.Opened += (s, args) =>
                    {
                        var hasSelection = CodePresenter?.SelectionLength > 0;
                        foreach (var mi in CodePresenter.ContextMenu.Items.OfType<MenuItem>())
                        {
                            if (mi.Command == ApplicationCommands.Copy)
                            {
                                mi.Visibility = hasSelection == true ? Visibility.Visible : Visibility.Collapsed;
                            }
                            else if (mi.Command == ApplicationCommands.SelectAll)
                            {
                                mi.Visibility = Visibility.Visible;
                            }
                        }
                    };
                }
            }
            catch
            {
                // Exception can happen if the localization resources are not loaded, ignore it.
            }
        }

        private void CodePresenter_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateSyntaxHighlightedContent();
        }

        private void SampleCodePresenter_ActualThemeChanged(object sender, RoutedEventArgs e)
        {
            // If the theme has changed after the user has already opened the app (ie. via settings), then the new locally set theme will overwrite the colors that are set during Loaded.
            // Therefore we need to re-format the REB to use the correct colors.
            GenerateSyntaxHighlightedContent();
        }

        private void OnValueChanged(ControlExampleSubstitution sender, object e)
        {
            GenerateSyntaxHighlightedContent();
        }

        private void GenerateSyntaxHighlightedContent()
        {
            FormatAndRenderSampleFromString(Code, CodePresenter, IsCSharpSample ? Languages.CSharp : Languages.Xml);
        }


        private void FormatAndRenderSampleFromString(string sampleString, TextEditor presenter, ILanguage highlightLanguage)
        {
            var highlighterName = "";
            if (highlightLanguage == Languages.CSharp)
            {
                highlighterName = "C#";
            }
            else if (highlightLanguage == Languages.Xml)
            {
                highlighterName = "XML";
            }

            var highlighter = HighlightingManager.Instance.GetDefinition(highlighterName);
            if (presenter.SyntaxHighlighting != highlighter)
            presenter.SyntaxHighlighting = highlighter;
            
            var formattedText = StringHelper.RemoveLeadingAndTrailingEmptyLines(sampleString);
            if (formattedText != presenter.Text)
                presenter.Text = formattedText;

            presenter.Visibility = Visibility.Visible;
        }

        private void CopyCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(CodePresenter.Text);
                VisualStateManager.GoToState(this, "ConfirmationDialogVisible", false);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Unable to Perform Copy", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Automatically close teachingtip after 1 seconds
            this.RunOnUIThread(async () =>
            {
                await Task.Delay(1000);
                VisualStateManager.GoToState(this, "ConfirmationDialogHidden", false);
            });
        }

        private void FixAvalonEditScrolling()
        {
            var scv = CodePresenter.Template.FindName("PART_ScrollViewer", CodePresenter);
            if (scv is ScrollViewer PART_ScrollViewer)
            {
                // I don't know why AvalonEditor doesn't handle horizontal scrolling properly,
                // So we see horizontal scrolls as vertical scrolls and bubble it to the parent.

                PART_ScrollViewer.PreviewMouseWheel += (sender, e) =>
                {
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                    eventArg.Source = sender;
                    this.RaiseEvent(eventArg);
                };
            }
        }
    }
}
