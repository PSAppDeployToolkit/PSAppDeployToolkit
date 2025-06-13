using iNKORE.UI.WPF.Modern.Media.Animation;
using SamplesCommon.SamplePages;
using System.Windows;
using System.Windows.Controls;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;
using SamplePages = SamplesCommon.SamplePages;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public sealed partial class PageTransitionPage : Page
    {
        private NavigationTransitionInfo _transitionInfo = null;

        public PageTransitionPage()
        {
            InitializeComponent();

            ContentFrame.Navigate(typeof(SamplePage1));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private SamplePage1 _page1 = new SamplePage1();
        private SamplePage2 _page2 = new SamplePage2();

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {

            Page pageToNavigateTo = ContentFrame.BackStackDepth % 2 == 1 ? _page1 : _page2;

            if (_transitionInfo == null)
            {
                // Default behavior, no transition set or used.
                ContentFrame.Navigate(pageToNavigateTo, null);
            }
            else
            {
                // Explicit transition info used.
                ContentFrame.Navigate(pageToNavigateTo, null, _transitionInfo);
            }
        }

        private void BackwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContentFrame.BackStackDepth > 0)
            {
                ContentFrame.GoBack();
            }
        }

        private void TransitionRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var senderTransitionString = (sender as RadioButton).Content.ToString();
            if (senderTransitionString != "Default")
            {
                if (senderTransitionString == "Entrance")
                {
                    _transitionInfo = new EntranceNavigationTransitionInfo();
                }
                else if (senderTransitionString == "DrillIn")
                {
                    _transitionInfo = new DrillInNavigationTransitionInfo();
                }
                else if (senderTransitionString == "Suppress")
                {
                    _transitionInfo = new SuppressNavigationTransitionInfo();
                }
                else if (senderTransitionString == "Slide from Right")
                {
                    _transitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };
                }
                else if (senderTransitionString == "Slide from Left")
                {
                    _transitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft };
                }
            }
            else
            {
                _transitionInfo = null;
            }

            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example1.CSharp = Example1CS;
        }

        public string Example1Xaml => $@"
<ui:Frame x:Name=""ContentFrame""/>
";

        public string Example1CS => $@"
private void ForwardButton_Click(object sender, RoutedEventArgs e)
{{

    Page pageToNavigateTo = ContentFrame.BackStackDepth % 2 == 1 ? _page1 : _page2;
 
{(_transitionInfo == null ? $@"
    // Default behavior, no transition set or used.
    ContentFrame.Navigate(pageToNavigateTo, null);
" : $@"
    // Explicit transition info used.
    ContentFrame.Navigate(pageToNavigateTo, null, new {_transitionInfo.GetType().Name}());
")}
}}

private void BackwardButton_Click(object sender, RoutedEventArgs e)
{{
    if (ContentFrame.BackStackDepth > 0)
    {{
        ContentFrame.GoBack();
    }}
}}
";

        #endregion
    }
}
