using iNKORE.UI.WPF.Modern.Common;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using static iNKORE.UI.WPF.Modern.Controls.LocalizedDialogCommands;
using System.Diagnostics;
using iNKORE.UI.WPF.Helpers;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public partial class MessageBox : Window
    {
        private MessageBoxResult? _result;
        public MessageBoxResult Result
        {
            get { return _result ?? MessageBoxResult.None; }
        }



        public Button OKButton { get; private set; }
        public Button YesButton { get; private set; }
        public Button NoButton { get; private set; }
        public Button CancelButton { get; private set; }
        public Border Border_UpperBackground { get; private set; }

        public static BackdropType DefaultBackdropType { get; set; } = BackdropType.Mica;

        public static bool MakeSound { get; set; } = true;

        static MessageBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MessageBox), new FrameworkPropertyMetadata(typeof(MessageBox)));
        }

        public static readonly DependencyPropertyDescriptor SystemBackdropTypeProperty_Descriptor = DependencyPropertyDescriptor.FromProperty(WindowHelper.SystemBackdropTypeProperty, typeof(MessageBox));

        public MessageBox()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, new ExecutedRoutedEventHandler(ExecuteCopy)));

            SetValue(TemplateSettingsPropertyKey, new MessageBoxTemplateSettings());
            var handler = new RoutedEventHandler((sender, e) => ApplyDarkMode());
            ThemeManager.AddActualThemeChangedHandler(this, handler);

            Loaded += On_Loaded;

            SystemBackdropTypeProperty_Descriptor.AddValueChanged(this, SystemBackdropTypeProperty_ValueChanged);
            ThemeManager.AddActualThemeChangedHandler(this, ThemeManager_AddActualThemeChanged);
        }

        private void ExecuteCopy(object sender, ExecutedRoutedEventArgs e)
        {
            const string longlines = "---------------------------";
            StringBuilder sb = new();
            sb.Append(longlines);
            sb.AppendLine();
            sb.Append(Caption);
            sb.AppendLine();
            sb.Append(longlines);
            sb.AppendLine();
            sb.Append(Content);
            sb.AppendLine();
            sb.Append(longlines);
            sb.AppendLine();
            //switch (MessageBoxButtons)
            //{
            //    case MessageBoxButton.OK:
            //        sb.Append(OKButtonText);
            //        break;
            //    case MessageBoxButton.OKCancel:
            //        sb.Append(OKButtonText + "     " + CancelButtonText);
            //        break;
            //    case MessageBoxButton.YesNo:
            //        sb.Append(YesButtonText + "     " + NoButtonText);
            //        break;
            //    case MessageBoxButton.YesNoCancel:
            //        sb.Append(YesButtonText + "     " + NoButtonText + "     " + CancelButtonText);
            //        break;
            //}

            bool isFirstButtonLoaded = true;
            var buttons = new Button[]
            {
                OKButton,
                YesButton,
                NoButton,
                CancelButton,
            };

            foreach(var button in buttons)
            {
                if(button.Visibility == Visibility.Visible)
                {
                    if (!isFirstButtonLoaded)
                    {
                        sb.Append("     ");
                    }

                    sb.Append(button.Content.ToString());
                    isFirstButtonLoaded = false;
                }
            }

            sb.AppendLine();
            sb.Append(longlines);

            try
            {
                new UIPermission(UIPermissionClipboard.AllClipboard).Demand();
                //Clipboard.SetText(sb.ToString());
                ClipboardEx.SetText(sb.ToString());
            }
            catch (SecurityException)
            {
                if (Debugger.IsAttached)
                {
                    throw;
                }
            }
        }

        private void ThemeManager_AddActualThemeChanged(object sender, RoutedEventArgs e)
        {
            if(WindowHelper.GetSystemBackdropType(this) != BackdropType.None)
            {
                if(ThemeManager.GetActualTheme(this) == ElementTheme.Dark)
                {
                    BackdropHelper.ApplyDarkMode(this);
                }
                else
                {
                    BackdropHelper.RemoveDarkMode(this);
                }
            }
        }

        private void SystemBackdropTypeProperty_ValueChanged(object sender, EventArgs e)
        {
            var backdrop = WindowHelper.GetSystemBackdropType(this);

            if(this.ReadLocalValue(BackgroundProperty) == DependencyProperty.UnsetValue)
            {
                if (backdrop == BackdropType.None || !backdrop.IsSupported())
                {
                    this.SetResourceReference(BackgroundProperty, ThemeKeys.ContentDialogBackgroundKey);
                    Border_UpperBackground?.SetResourceReference(BackgroundProperty, ThemeKeys.ContentDialogTopOverlayKey);
                }
                else
                {
                    this.Background = Brushes.Transparent;
                    Border_UpperBackground?.SetResourceReference(BackgroundProperty, ThemeKeys.LayerOnAcrylicFillColorDefaultBrushKey);
                }
            }
        }

        #region SystemSoundOnLoaded

        public static readonly DependencyProperty SystemSoundOnLoadedProperty =
            DependencyProperty.Register(
                nameof(SystemSoundOnLoaded),
                typeof(SystemSound),
                typeof(MessageBox));

        public SystemSound SystemSoundOnLoaded
        {
            get => (SystemSound)GetValue(SystemSoundOnLoadedProperty);
            set => SetValue(SystemSoundOnLoadedProperty, value);
        }

        #endregion


        #region Caption

        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(
                nameof(Caption),
                typeof(object),
                typeof(MessageBox));

        public object Caption
        {
            get => GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        #endregion

        #region CaptionTemplate

        public static readonly DependencyProperty CaptionTemplateProperty =
            DependencyProperty.Register(
                nameof(CaptionTemplate),
                typeof(DataTemplate),
                typeof(MessageBox));

        public DataTemplate CaptionTemplate
        {
            get => (DataTemplate)GetValue(CaptionTemplateProperty);
            set => SetValue(CaptionTemplateProperty, value);
        }

        #endregion

        #region OKButtonText

        public static readonly DependencyProperty OKButtonTextProperty =
            DependencyProperty.Register(
                nameof(OKButtonText),
                typeof(string),
                typeof(MessageBox),
                new PropertyMetadata(string.Empty, OnButtonTextChanged));

        public string OKButtonText
        {
            get => (string)GetValue(OKButtonTextProperty);
            set => SetValue(OKButtonTextProperty, value);
        }

        #endregion

        #region OKButtonCommand

        public static readonly DependencyProperty OKButtonCommandProperty =
            DependencyProperty.Register(
                nameof(OKButtonCommand),
                typeof(ICommand),
                typeof(MessageBox),
                null);

        public ICommand OKButtonCommand
        {
            get => (ICommand)GetValue(OKButtonCommandProperty);
            set => SetValue(OKButtonCommandProperty, value);
        }

        #endregion

        #region OKButtonCommandParameter

        public static readonly DependencyProperty OKButtonCommandParameterProperty =
            DependencyProperty.Register(
                nameof(OKButtonCommandParameter),
                typeof(object),
                typeof(MessageBox),
                null);

        public object OKButtonCommandParameter
        {
            get => GetValue(OKButtonCommandParameterProperty);
            set => SetValue(OKButtonCommandParameterProperty, value);
        }

        #endregion

        #region OKButtonStyle

        public static readonly DependencyProperty OKButtonStyleProperty =
            DependencyProperty.Register(
                nameof(OKButtonStyle),
                typeof(Style),
                typeof(MessageBox),
                null);

        public Style OKButtonStyle
        {
            get => (Style)GetValue(OKButtonStyleProperty);
            set => SetValue(OKButtonStyleProperty, value);
        }

        #endregion

        #region YesButtonText

        public static readonly DependencyProperty YesButtonTextProperty =
            DependencyProperty.Register(
                nameof(YesButtonText),
                typeof(string),
                typeof(MessageBox),
                new PropertyMetadata(string.Empty, OnButtonTextChanged));

        public string YesButtonText
        {
            get => (string)GetValue(YesButtonTextProperty);
            set => SetValue(YesButtonTextProperty, value);
        }

        #endregion

        #region YesButtonCommand

        public static readonly DependencyProperty YesButtonCommandProperty =
            DependencyProperty.Register(
                nameof(YesButtonCommand),
                typeof(ICommand),
                typeof(MessageBox),
                null);

        public ICommand YesButtonCommand
        {
            get => (ICommand)GetValue(YesButtonCommandProperty);
            set => SetValue(YesButtonCommandProperty, value);
        }

        #endregion

        #region YesButtonCommandParameter

        public static readonly DependencyProperty YesButtonCommandParameterProperty =
            DependencyProperty.Register(
                nameof(YesButtonCommandParameter),
                typeof(object),
                typeof(MessageBox),
                null);

        public object YesButtonCommandParameter
        {
            get => GetValue(YesButtonCommandParameterProperty);
            set => SetValue(YesButtonCommandParameterProperty, value);
        }

        #endregion

        #region YesButtonStyle

        public static readonly DependencyProperty YesButtonStyleProperty =
            DependencyProperty.Register(
                nameof(YesButtonStyle),
                typeof(Style),
                typeof(MessageBox),
                null);

        public Style YesButtonStyle
        {
            get => (Style)GetValue(YesButtonStyleProperty);
            set => SetValue(YesButtonStyleProperty, value);
        }

        #endregion

        #region NoButtonText

        public static readonly DependencyProperty NoButtonTextProperty =
            DependencyProperty.Register(
                nameof(NoButtonText),
                typeof(string),
                typeof(MessageBox),
                new PropertyMetadata(string.Empty, OnButtonTextChanged));

        public string NoButtonText
        {
            get => (string)GetValue(NoButtonTextProperty);
            set => SetValue(NoButtonTextProperty, value);
        }

        #endregion

        #region NoButtonCommand

        public static readonly DependencyProperty NoButtonCommandProperty =
            DependencyProperty.Register(
                nameof(NoButtonCommand),
                typeof(ICommand),
                typeof(MessageBox),
                null);

        public ICommand NoButtonCommand
        {
            get => (ICommand)GetValue(NoButtonCommandProperty);
            set => SetValue(NoButtonCommandProperty, value);
        }

        #endregion

        #region NoButtonCommandParameter

        public static readonly DependencyProperty NoButtonCommandParameterProperty =
            DependencyProperty.Register(
                nameof(NoButtonCommandParameter),
                typeof(object),
                typeof(MessageBox),
                null);

        public object NoButtonCommandParameter
        {
            get => GetValue(NoButtonCommandParameterProperty);
            set => SetValue(NoButtonCommandParameterProperty, value);
        }

        #endregion

        #region NoButtonStyle

        public static readonly DependencyProperty NoButtonStyleProperty =
            DependencyProperty.Register(
                nameof(NoButtonStyle),
                typeof(Style),
                typeof(MessageBox),
                null);

        public Style NoButtonStyle
        {
            get => (Style)GetValue(NoButtonStyleProperty);
            set => SetValue(NoButtonStyleProperty, value);
        }

        #endregion

        #region CancelButtonText

        public static readonly DependencyProperty CancelButtonTextProperty =
            DependencyProperty.Register(
                nameof(CancelButtonText),
                typeof(string),
                typeof(MessageBox),
                new PropertyMetadata(string.Empty, OnButtonTextChanged));

        public string CancelButtonText
        {
            get => (string)GetValue(CancelButtonTextProperty);
            set => SetValue(CancelButtonTextProperty, value);
        }

        #endregion

        #region CancelButtonCommand

        public static readonly DependencyProperty CancelButtonCommandProperty =
            DependencyProperty.Register(
                nameof(CancelButtonCommand),
                typeof(ICommand),
                typeof(MessageBox),
                null);

        public ICommand CancelButtonCommand
        {
            get => (ICommand)GetValue(CancelButtonCommandProperty);
            set => SetValue(CancelButtonCommandProperty, value);
        }

        #endregion

        #region CancelButtonCommandParameter

        public static readonly DependencyProperty CancelButtonCommandParameterProperty =
            DependencyProperty.Register(
                nameof(CancelButtonCommandParameter),
                typeof(object),
                typeof(MessageBox),
                null);

        public object CancelButtonCommandParameter
        {
            get => GetValue(CancelButtonCommandParameterProperty);
            set => SetValue(CancelButtonCommandParameterProperty, value);
        }

        #endregion

        #region CancelButtonStyle

        public static readonly DependencyProperty CancelButtonStyleProperty =
            DependencyProperty.Register(
                nameof(CancelButtonStyle),
                typeof(Style),
                typeof(MessageBox),
                null);

        public Style CancelButtonStyle
        {
            get => (Style)GetValue(CancelButtonStyleProperty);
            set => SetValue(CancelButtonStyleProperty, value);
        }

        #endregion

        #region CornerRadius

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(MessageBox));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region IconSource

        public IconSource IconSource
        {
            get => (IconSource)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                nameof(IconSource),
                typeof(IconSource),
                typeof(MessageBox),
                new PropertyMetadata(OnIconSourcePropertyChanged));

        private static void OnIconSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((MessageBox)sender).OnIconSourcePropertyChanged(args);
        }

        #endregion

        #region MessageBoxButtons

        public MessageBoxButton MessageBoxButtons
        {
            get => (MessageBoxButton)GetValue(MessageBoxButtonsProperty);
            set => SetValue(MessageBoxButtonsProperty, value);
        }

        public static readonly DependencyProperty MessageBoxButtonsProperty =
            DependencyProperty.Register(
                nameof(MessageBoxButtons),
                typeof(MessageBoxButton),
                typeof(MessageBox),
                new PropertyMetadata(OnMessageBoxButtonsPropertyChanged));

        private static void OnMessageBoxButtonsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((MessageBox)sender).UpdateMessageBoxButtonState();
        }

        #endregion

        #region DefaultResult

        public MessageBoxResult? DefaultResult
        {
            get => (MessageBoxResult?)GetValue(DefaultResultProperty);
            set => SetValue(DefaultResultProperty, value);
        }

        public static readonly DependencyProperty DefaultResultProperty =
            DependencyProperty.Register(
                nameof(DefaultResult),
                typeof(MessageBoxResult?),
                typeof(MessageBox));

        #endregion

        #region TemplateSettings

        private static readonly DependencyPropertyKey TemplateSettingsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(TemplateSettings),
                typeof(MessageBoxTemplateSettings),
                typeof(MessageBox),
                null);

        public static readonly DependencyProperty TemplateSettingsProperty =
            TemplateSettingsPropertyKey.DependencyProperty;

        public MessageBoxTemplateSettings TemplateSettings
        {
            get => (MessageBoxTemplateSettings)GetValue(TemplateSettingsProperty);
        }

        #endregion

        public event TypedEventHandler<MessageBox, MessageBoxOpenedEventArgs> Opened;

        public new event TypedEventHandler<MessageBox, MessageBoxClosingEventArgs> Closing;

        public new event TypedEventHandler<MessageBox, MessageBoxClosedEventArgs> Closed;

        public event TypedEventHandler<MessageBox, MessageBoxButtonClickEventArgs> OKButtonClick;

        public event TypedEventHandler<MessageBox, MessageBoxButtonClickEventArgs> YesButtonClick;

        public event TypedEventHandler<MessageBox, MessageBoxButtonClickEventArgs> NoButtonClick;

        public event TypedEventHandler<MessageBox, MessageBoxButtonClickEventArgs> CancelButtonClick;

        private static void OnButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MessageBox)d).UpdateButtonTextState();
        }

        void OnIconSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue is IconSource iconSource)
            {
                TemplateSettings.IconElement = iconSource.CreateIconElement();
            }
            else
            {
                TemplateSettings.ClearValue(MessageBoxTemplateSettings.IconElementProperty);
            }
            UpdateIconState();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (OKButton != null)
            {
                OKButton.Click -= OnButtonClick;
            }

            if (YesButton != null)
            {
                YesButton.Click -= OnButtonClick;
            }

            if (NoButton != null)
            {
                NoButton.Click -= OnButtonClick;
            }

            if (CancelButton != null)
            {
                CancelButton.Click -= OnButtonClick;
            }

            OKButton = GetTemplateChild(nameof(OKButton)) as Button;
            YesButton = GetTemplateChild(nameof(YesButton)) as Button;
            NoButton = GetTemplateChild(nameof(NoButton)) as Button;
            CancelButton = GetTemplateChild(nameof(CancelButton)) as Button;
            Border_UpperBackground = GetTemplateChild(nameof(Border_UpperBackground)) as Border;

            if (OKButton != null)
            {
                OKButton.Click += OnButtonClick;
            }

            if (YesButton != null)
            {
                YesButton.Click += OnButtonClick;
            }

            if (NoButton != null)
            {
                NoButton.Click += OnButtonClick;
            }

            if (CancelButton != null)
            {
                CancelButton.Click += OnButtonClick;
                CancelButton.IsCancel = true;
            }

            UpdateIconState();
            UpdateMessageState();
            UpdateButtonTextState();
            UpdateMessageBoxButtonState();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            InvalidateMeasure();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender == OKButton)
            {
                HandleButtonClick(
                    OKButtonClick,
                    OKButtonCommand,
                    OKButtonCommandParameter,
                    MessageBoxResult.OK);
            }
            else if (sender == YesButton)
            {
                HandleButtonClick(
                    YesButtonClick,
                    YesButtonCommand,
                    YesButtonCommandParameter,
                    MessageBoxResult.Yes);
            }
            else if (sender == NoButton)
            {
                HandleButtonClick(
                    NoButtonClick,
                    NoButtonCommand,
                    NoButtonCommandParameter,
                    MessageBoxResult.No);
            }
            else if (sender == CancelButton)
            {
                HandleButtonClick(
                    CancelButtonClick,
                    CancelButtonCommand,
                    CancelButtonCommandParameter,
                    MessageBoxResult.Cancel);
            }
        }

        private void HandleButtonClick(
            TypedEventHandler<MessageBox, MessageBoxButtonClickEventArgs> handler,
            ICommand command,
            object commandParameter,
            MessageBoxResult result)
        {
            if (handler != null)
            {
                var args = new MessageBoxButtonClickEventArgs();

                var deferral = new MessageBoxButtonClickDeferral(() =>
                {
                    if (!args.Cancel)
                    {
                        TryExecuteCommand(command, commandParameter);
                        Close(result);
                    }
                });

                args.SetDeferral(deferral);

                args.IncrementDeferralCount();
                handler(this, args);
                args.DecrementDeferralCount();
            }
            else
            {
                TryExecuteCommand(command, commandParameter);
                Close(result);
            }
        }

        private void UpdateButtonTextState()
        {
            var templateSettings = TemplateSettings;
            templateSettings.OKButtonText = string.IsNullOrEmpty(OKButtonText) ? GetString(DialogBoxCommand.IDOK) : OKButtonText;
            templateSettings.YesButtonText = string.IsNullOrEmpty(YesButtonText) ? GetString(DialogBoxCommand.IDYES) : YesButtonText;
            templateSettings.NoButtonText = string.IsNullOrEmpty(NoButtonText) ? GetString(DialogBoxCommand.IDNO) : NoButtonText;
            templateSettings.CancelButtonText = string.IsNullOrEmpty(CancelButtonText) ? GetString(DialogBoxCommand.IDCANCEL) : CancelButtonText;
        }

        private void UpdateMessageState()
        {
            string stateName = Caption == null || (Caption is string && string.IsNullOrEmpty((string)Caption)) ? TitleCollapsedState : TitleVisibleState;
            VisualStateManager.GoToState(this, stateName, true);
        }

        private void UpdateIconState()
        {
            string stateName = TemplateSettings.IconElement == null ? IconCollapsedState : IconVisibleState;
            VisualStateManager.GoToState(this, stateName, true);
        }

        private void UpdateMessageBoxButtonState()
        {
            string stateName;

            MessageBoxButton button = MessageBoxButtons;

            switch (button)
            {
                case MessageBoxButton.OK:
                    stateName = OKVisibleState;
                    if (OKButton != null) { OKButton.Focus(); }
                    break;
                case MessageBoxButton.OKCancel:
                    stateName = OKCancelVisibleState;
                    if (OKButton != null) { OKButton.Focus(); }
                    break;
                case MessageBoxButton.YesNoCancel:
                    stateName = YesNoCancelVisibleState;
                    if (YesButton != null) { YesButton.Focus(); }
                    break;
                case MessageBoxButton.YesNo:
                    stateName = YesNoVisibleState;
                    if (YesButton != null) { YesButton.Focus(); }
                    break;
                default:
                    stateName = OKVisibleState;
                    if (OKButton != null) { OKButton.Focus(); }
                    break;
            }

            VisualStateManager.GoToState(this, stateName, true);

            if(_result == null)
            {
                switch (button)
                {
                    case MessageBoxButton.OK:
                        stateName = OKAsDefaultButtonState;
                        break;
                    case MessageBoxButton.OKCancel:
                        stateName = OKAsDefaultButtonState;
                        break;
                    case MessageBoxButton.YesNoCancel:
                        stateName = YesAsDefaultButtonState;
                        break;
                    case MessageBoxButton.YesNo:
                        stateName = YesAsDefaultButtonState;
                        break;
                    default:
                        stateName = OKAsDefaultButtonState;
                        break;
                }
            }
            else
            {
                switch (_result.Value)
                {
                    case MessageBoxResult.OK:
                        stateName = OKAsDefaultButtonState;
                        break;
                    case MessageBoxResult.Cancel:
                        stateName = CancelAsDefaultButtonState;
                        break;
                    case MessageBoxResult.Yes:
                        stateName = YesAsDefaultButtonState;
                        break;
                    case MessageBoxResult.No:
                        stateName = NoAsDefaultButtonState;
                        break;
                    default:
                        stateName = NoneAsDefaultButtonState;
                        break;
                }
            }

            VisualStateManager.GoToState(this, stateName, true);
        }

        /// <summary>
        /// Opens a Message Box and returns only when the newly opened window is closed.
        /// </summary>
        /// <returns>A <see cref="MessageBoxResult"/> value that specifies which message box button is clicked by the user.</returns>
        public new MessageBoxResult ShowDialog()
        {
            base.ShowDialog();
            return Result;
        }

        public void Close(MessageBoxResult result)
        {
            var closing = Closing;
            if (closing != null)
            {
                var args = new MessageBoxClosingEventArgs(result);

                var deferral = new MessageBoxClosingDeferral(() =>
                {
                    if (!args.Cancel)
                    {
                        _result = result;
                        Close();
                        Closed?.Invoke(this, new MessageBoxClosedEventArgs(result));
                    }
                });

                args.SetDeferral(deferral);

                args.IncrementDeferralCount();
                closing(this, args);
                args.DecrementDeferralCount();
            }
            else
            {
                _result = result;
                Close();
                Closed?.Invoke(this, new MessageBoxClosedEventArgs(result));
            }
        }

        private void On_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyDarkMode();
            this.RemoveTitleBar();
            Opened?.Invoke(this, new MessageBoxOpenedEventArgs());

            if (DefaultBackdropType == BackdropType.None || BackdropHelper.IsSupported(DefaultBackdropType))
            {
                WindowHelper.SetSystemBackdropType(this, DefaultBackdropType);
            }

            ThemeManager_AddActualThemeChanged(sender, e);
            SystemBackdropTypeProperty_ValueChanged(sender, e);

            SystemSoundOnLoaded?.Play();
        }

        private static void TryExecuteCommand(ICommand command, object parameter)
        {
            if (command != null && command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }

        private void ApplyDarkMode()
        {
            var theme = ThemeManager.GetActualTheme(this);

            bool IsDark(ElementTheme theme)
            {
                return theme == ElementTheme.Default
                    ? ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark
                    : theme == ElementTheme.Dark;
            }

            if (IsDark(theme))
            {
                BackdropHelper.ApplyDarkMode(this);
            }
            else
            {
                this.RemoveDarkMode();
            }
        }

        private const string OKVisibleState = "OKVisible";
        private const string OKCancelVisibleState = "OKCancelVisible";
        private const string YesNoCancelVisibleState = "YesNoCancelVisible";
        private const string YesNoVisibleState = "YesNoVisible";

        private const string OKAsDefaultButtonState = "OKAsDefaultButton";
        private const string YesAsDefaultButtonState = "YesAsDefaultButton";
        private const string CancelAsDefaultButtonState = "CancelAsDefaultButton";
        private const string NoAsDefaultButtonState = "NoAsDefaultButton";
        private const string NoneAsDefaultButtonState = "NoneAsDefaultButton";

        private const string IconVisibleState = "IconVisible";
        private const string IconCollapsedState = "IconCollapsed";

        private const string TitleVisibleState = "TitleVisible";
        private const string TitleCollapsedState = "TitleCollapsed";
    }
}
