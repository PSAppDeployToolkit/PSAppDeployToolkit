using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// TextBlockPage.xaml 的交互逻辑
    /// </summary>
    public partial class TextBlockPage : Page
    {
        public TextBlockPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
        }

        public string Example1Xaml => $@"
<TextBlock Text=""I am a TextBlock."" />
";

        public string Example2Xaml => $@"
<TextBlock Style=""{{StaticResource CustomTextBlockStyle}}"" Text=""I am a styled TextBlock."" />
";

        public string Example3Xaml => $@"
<TextBlock Foreground=""CornflowerBlue"" TextWrapping=""Wrap""
    FontFamily=""Arial"" FontSize=""24"" FontStyle=""Italic"" 
    Text=""I am super excited to be here!"" />
";

        public string Example4Xaml => $@"
<TextBlock>
    <Run FontFamily=""Times New Roman"" Foreground=""DarkGray"">Text in a TextBlock doesn't have to be a simple string.</Run>
    <LineBreak />
    <Span>
        Text can be<Bold>bold</Bold>,
        <Italic>italic</Italic>,
        or<Underline>underlined</Underline>.
    </Span>
</TextBlock>
";

        #endregion

    }
}
