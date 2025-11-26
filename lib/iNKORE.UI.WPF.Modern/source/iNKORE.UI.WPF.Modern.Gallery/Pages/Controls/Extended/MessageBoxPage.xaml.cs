using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using Page = iNKORE.UI.WPF.Modern.Controls.Page;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Common;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System.Media;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Extended
{
    /// <summary>
    /// BorderPage.xaml 的交互逻辑
    /// </summary>
    public partial class MessageBoxPage : Page
    {
        public MessageBoxPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RadioButtons_DefaultBackdropStyle.SelectedItem = MessageBox.DefaultBackdropType;
            UpdateExampleCode();
        }

        public static readonly string UsingMessageBoxDirective = "using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;";

        #region Example 1

        static readonly MessageBoxPayload Msg1 = new MessageBoxPayload()
        {
            Message = "But nobody came.",
        };

        private void Button_ShowMsg1_Click(object sender, RoutedEventArgs e)
        {
            Msg1.Show();
        }

        public string Example1CS => Msg1.ToCode();

        #endregion

        #region Example 2

        static readonly MessageBoxPayload Msg2Warning = new MessageBoxPayload()
        {
            Message = "IN THIS WORLD, IT'S KILL OR BE KILLED.",
            Title = "Your Best Friend",
            Image = new MsgBoxIcon(MessageBoxImage.Warning),
        };

        static readonly MessageBoxPayload Msg2Info = new MessageBoxPayload()
        {
            Message = "\"Friendship\" is just a hot person's way of making you their slave.",
            Title = "You Wanna Find Out",
            Image = new MsgBoxIcon(MessageBoxImage.Information)
        };

        static readonly MessageBoxPayload Msg2Question = new MessageBoxPayload()
        {
            Message = "Are you sure to destroy the world with this nuclear bomb? You're gonna pay for it someday.",
            Title = "Wanna Have A Bad Time?",
            Image = new MsgBoxIcon(MessageBoxImage.Information),
            Button = MessageBoxButton.YesNo
        };

        static readonly MessageBoxPayload Msg2Error = new MessageBoxPayload()
        {
            Message = "You can't understand how this feels. Knowing that one day, without any warning, it's all going to be reset.",
            Title = "The End",
            Image = new MsgBoxIcon(MessageBoxImage.Error)
        };

        static readonly MessageBoxPayload Msg2FontIconData = new MessageBoxPayload()
        {
            Message = "You never gained any LOVE, but you gained love. Does that even make sense?",
            Title = "What is LOVE?",
            Image = new MsgBoxIcon(SegoeFluentIcons.Heart, "SegoeFluentIcons.Heart"),
        };

        MessageBoxPayload Msg2ImageIconSource => new MessageBoxPayload()
        {
            Message = "iNKORE Open Source Products - iNKORE.UI.WPF.Modern - Make Windows Presentation Foundation Great Again!",
            Title = "iNKORE Open Source",
            Image = new MsgBoxIcon(new ImageIconSource() { ImageSource = (BitmapImage)this.Resources["WpfLibraryLogo"], DefaultSize = new Size(32, 32) },
                "new ImageIconSource() { ImageSource = new Uri(\"...\"), DefaultSize = new Size(32, 32) }"),
        };


        private void Button_ShowMsg2_Warning_Click(object sender, RoutedEventArgs e)
        {
            Msg2Warning.Show();
        }

        private void Button_ShowMsg2_Info_Click(object sender, RoutedEventArgs e)
        {
            Msg2Info.Show();
        }

        private void Button_ShowMsg2_Question_Click(object sender, RoutedEventArgs e)
        {
            Msg2Question.Show();
        }

        private void Button_ShowMsg2_FontIconData_Click(object sender, RoutedEventArgs e)
        {
            Msg2FontIconData.Show();
        }

        private void Button_ShowMsg2_Error_Click(object sender, RoutedEventArgs e)
        {
            Msg2Error.Show();
        }

        private void Button_ShowMsg2_IconSource_Click(object sender, RoutedEventArgs e)
        {
            Msg2ImageIconSource.Show();
        }

        MessageBoxPayload[] Example2Data =>
        [
            Msg2Warning, Msg2Info, Msg2Question, Msg2Error, Msg2FontIconData, this.Msg2ImageIconSource
        ];

        public string Example2CS => string.Join("\n", Example2Data.Select(data => data.ToCode()));


        #endregion

        #region Example 3

        static readonly MessageBoxPayload Msg3YesNo = new MessageBoxPayload()
        {
            Message = "This ghost keeps saying \"Z\" out loud repeatedly, pretending to sleep. Do you want to move it with force?",
            Title = "The Ruins",
            Image = new MsgBoxIcon(MessageBoxImage.Question),
            Button = MessageBoxButton.YesNo
        };

        static readonly MessageBoxPayload Msg3YesNoCancel = new MessageBoxPayload()
        {
            Message = "Do you want to save the changes you made to this file?",
            Title = "Macrohard Windoze",
            Image = new MsgBoxIcon(MessageBoxImage.Question),
            Button = MessageBoxButton.YesNoCancel
        };

        static readonly MessageBoxPayload Msg3OKCancel = new MessageBoxPayload()
        {
            Message = "If by any chance you have any unfinished business, please do what you must.",
            Title = "The Is It",
            Image = new MsgBoxIcon(MessageBoxImage.Question),
            Button = MessageBoxButton.OKCancel
        };

        private void displayExample3Result(MessageBoxResult result)
        {
            TextBlock_Example3_Result.Text = "You clicked " + result.ToString();
        }


        private void Button_ShowMsg3_YesNo_Click(object sender, RoutedEventArgs e)
        {
            displayExample3Result(Msg3YesNo.Show());
        }

        private void Button_ShowMsg3_YesNoCancel_Click(object sender, RoutedEventArgs e)
        {
            displayExample3Result(Msg3YesNoCancel.Show());
        }

        private void Button_ShowMsg3_OKCancel_Click(object sender, RoutedEventArgs e)
        {
            displayExample3Result(Msg3OKCancel.Show());
        }

        static MessageBoxPayload[] Example3Data =
        [
            Msg3YesNo, Msg3YesNoCancel, Msg3OKCancel
        ];

        public string Example3CS => string.Join("\n", Example3Data.Select(data => data.ToCode()));

        #endregion

        #region Example 4

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RadioButtons_DefaultBackdropStyle.SelectedItem == null) return;

            MessageBox.DefaultBackdropType = Enum.Parse<BackdropType>(RadioButtons_DefaultBackdropStyle.SelectedItem.ToString());
            UpdateExampleCode();
        }

        public string Example4CS => $@"
MessageBox.DefaultBackdropType = BackdropType.{MessageBox.DefaultBackdropType};
";


        #endregion

        #region Example 5

        private static MsgBoxIcon[] _example5_preIcons =
        [
            new MsgBoxIcon(),
            new MsgBoxIcon(MessageBoxImage.Error),
            new MsgBoxIcon(MessageBoxImage.Question),
            new MsgBoxIcon(MessageBoxImage.Warning),
            new MsgBoxIcon(MessageBoxImage.Information),

            new MsgBoxIcon(SegoeFluentIcons.Heart, "SegoeFluentIcons.Heart"),
            new MsgBoxIcon(SegoeFluentIcons.HeartBroken, "SegoeFluentIcons.HeartBroken"),
            new MsgBoxIcon(SegoeFluentIcons.Home, "SegoeFluentIcons.Home"),
            new MsgBoxIcon(SegoeFluentIcons.Settings, "SegoeFluentIcons.Settings"),
            new MsgBoxIcon(SegoeFluentIcons.Shield, "SegoeFluentIcons.Shield"),
            new MsgBoxIcon(SegoeFluentIcons.SpecialEffectSize, "SegoeFluentIcons.SpecialEffectSize"),
            new MsgBoxIcon(SegoeFluentIcons.Emoji2, "SegoeFluentIcons.Emoji2"),
        ];

        public MsgBoxIcon[] Example5_IconSelects => _example5_preIcons;


        public static Dictionary<string, SystemSound> _example5_SoundMap = new Dictionary<string, SystemSound>()
        {
            { "Asterisk", SystemSounds.Asterisk },
            { "Beep", SystemSounds.Beep },
            { "Exclamation", SystemSounds.Exclamation },
            { "Hand", SystemSounds.Hand },
            { "Question", SystemSounds.Question },
        };

        public ObservableCollection<string> Example5_SoundSelects => new ObservableCollection<string>(_example5_SoundMap.Keys);

        private void TextBox_Example5_Message_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExampleCode();
        }


        private void TextBox_Example5_Title_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ComboBox_Example5_Image_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ComboBox_Example5_Button_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ComboBox_Example5_Sound_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ComboBox_Example5_DefaultButton_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private static string toHardcodeString(string val)
        {
            return val.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\"", "\\\"");
        }

        public string Example5CS => $@"
MessageBox.Show
(
    ""{toHardcodeString(TextBox_Example5_Message.Text)}"",
    ""{toHardcodeString(TextBox_Example5_Title.Text)}"",
    MessageBoxButton.{ComboBox_Example5_Button.SelectedItem},
    {ComboBox_Example5_Image.SelectedItem},
    MessageBoxResult.{ComboBox_Example5_DefaultButton.SelectedItem},
    SystemSounds.{ComboBox_Example5_Sound.SelectedItem}
);
";


        #endregion

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.CSharp = Example1CS;
            Example2.CSharp = Example2CS;
            Example3.CSharp = Example3CS;
            Example4.CSharp = Example4CS;
            Example5.CSharp = Example5CS;
        }


        #endregion

        private void Button_ShowMsg5_Click(object sender, RoutedEventArgs e)
        {
            var msg = TextBox_Example5_Message.Text;
            var title = TextBox_Example5_Title.Text;
            var button = (MessageBoxButton)ComboBox_Example5_Button.SelectedItem;
            var image = (MsgBoxIcon)ComboBox_Example5_Image.SelectedItem;
            var defaultButton = (MessageBoxResult)ComboBox_Example5_DefaultButton.SelectedItem;
            var sound = _example5_SoundMap[(string)ComboBox_Example5_Sound.SelectedItem];
            var win = Application.Current.Windows.Cast<Window>()
                .FirstOrDefault(window => window.IsActive && window.ShowActivated);

            MessageBoxResult result;

            switch (image.Type)
            {
                case MsgBoxIconType.Preset:
                case MsgBoxIconType.None:
                    result = MessageBox.Show(win, msg, title, button, image.Value_Preset, defaultButton, sound);
                    break;
                case MsgBoxIconType.FontIcon:
                    result = MessageBox.Show(win, msg, title, button, image.Value_FontIcon, defaultButton, sound);
                    break;
                case MsgBoxIconType.IconSource:
                    result = MessageBox.Show(win, msg, title, button, image.Value_IconSource, defaultButton, sound);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("This should never happen!!");
            }

            TextBlock_Example5_Result.Text = "You clicked " + result.ToString();
        }
    }

    // This needs to be public for xaml binding purposes
    public class MessageBoxPayload
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public MessageBoxButton Button { get; set; } = MessageBoxButton.OK;
        public MsgBoxIcon Image { get; set; } = MsgBoxIcon.None;

        public MessageBoxResult Show()
        {
            switch (Image.Type)
            {
                case MsgBoxIconType.Preset:
                    return MessageBox.Show(Message, Title, Button, Image.Value_Preset);
                case MsgBoxIconType.FontIcon:
                    return MessageBox.Show(Message, Title, Button, Image.Value_FontIcon, null, SystemSounds.Beep);
                case MsgBoxIconType.IconSource:
                    return MessageBox.Show(Message, Title, Button, Image.Value_IconSource, null, SystemSounds.Beep);
                default:
                    return MessageBox.Show(Message, Title, Button);
            }
        }

        public string ToCode()
        {
            var method = "MessageBox.Show";
            var c_msg = Message.Replace("\"", "\\\"");
            if (!string.IsNullOrEmpty(Message) && string.IsNullOrEmpty(Title) && Button == MessageBoxButton.OK && Image.IsEmpty)
            {
                return method + $"(\"{c_msg}\");";
            }
            else if (Button == MessageBoxButton.OK && Image.IsEmpty)
            {
                return method + $"(\"{c_msg}\", \"{Title}\");";
            }
            else if (Image.IsEmpty)
            {
                return method + $"(\"{c_msg}\", \"{Title}\", MessageBoxButton.{Button});";
            }
            else
            {
                return method + $"(\"{c_msg}\", \"{Title}\", MessageBoxButton.{Button}, {Image});";
            }
        }
    }

    public struct MsgBoxIcon
    {
        public MsgBoxIconType Type { get; private set; }

        public MessageBoxImage Value_Preset { get; private set; } = MessageBoxImage.None;
        public FontIconData Value_FontIcon { get; private set; }
        public string Value_FontIconInput { get; private set; }
        public IconSource Value_IconSource { get; private set; }
        public string Value_IconSourceInput { get; private set; }

        public bool IsEmpty => Type == MsgBoxIconType.None || (Type == MsgBoxIconType.Preset && Value_Preset == MessageBoxImage.None);

        public MsgBoxIcon()
        {
            Type = MsgBoxIconType.None;
        }

        public MsgBoxIcon(MessageBoxImage preset)
        {
            Type = MsgBoxIconType.Preset;
            Value_Preset = preset;
        }

        public MsgBoxIcon(FontIconData fontIcon, string input)
        {
            Type = MsgBoxIconType.FontIcon;
            Value_FontIcon = fontIcon;
            Value_FontIconInput = input;
        }

        public MsgBoxIcon(IconSource iconSource, string input)
        {
            Type = MsgBoxIconType.IconSource;
            Value_IconSource = iconSource;
            Value_IconSourceInput = input;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case MsgBoxIconType.Preset:
                    return "MessageBoxImage." + Value_Preset.ToString();
                case MsgBoxIconType.FontIcon:
                    return Value_FontIconInput;
                case MsgBoxIconType.IconSource:
                    return Value_IconSourceInput;
                default:
                    return "MessageBoxImage.None";
            }
        }

        public static MsgBoxIcon None = new MsgBoxIcon();
    }

    public enum MsgBoxIconType
    {
        Preset,
        None,
        FontIcon,
        IconSource
    }
}
