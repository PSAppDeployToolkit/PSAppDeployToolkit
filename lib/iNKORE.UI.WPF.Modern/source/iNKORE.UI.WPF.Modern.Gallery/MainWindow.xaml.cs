using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using iNKORE.UI.WPF.Modern.Gallery.Common;
using iNKORE.UI.WPF.Modern.Gallery.DataModel;
using iNKORE.UI.WPF.Modern.Gallery.Helpers;
using iNKORE.UI.WPF.Modern.Gallery.Properties;
using SamplesCommon;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Threading;

namespace iNKORE.UI.WPF.Modern.Gallery
{
    public partial class MainWindow : Window
    {
        public static MainWindow Current;

        public MainWindow()
        {
            Current = this;
            InitializeComponent();
            InitialzeApp();
        }

        private async void InitialzeApp()
        {
            await Task.Delay(1);
            Common.WindowHelper.TrackWindow(this);
            ThemeHelper.Initialize();
            await ControlInfoDataSource.Instance.GetRealmsAsync();
            SubscribeToResourcesChanged();

            new Thread(() =>
            {
                Thread.Sleep(2000);
                DispatcherHelper.RunOnMainThread(() =>
                {
                    NavigationRootPage NavigationRootPage = new NavigationRootPage();
                    Content = NavigationRootPage;
                    BackdropHelper.Apply(this, BackdropType.Mica, true);
                });
            }).Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            DispatcherHelper.RunOnMainThread(() =>
            {
                if (this == Application.Current.MainWindow)
                {
                    this.SetPlacement(Settings.Default.MainWindowPlacement);
                }
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!e.Cancel)
            {
                DispatcherHelper.RunOnMainThread(() =>
                {
                    if (this == Application.Current.MainWindow)
                    {
                        Settings.Default.MainWindowPlacement = this.GetPlacement();
                        Settings.Default.Save();
                    }
                });
            }
        }

        /*protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == FocusManager.FocusedElementProperty)
            {
                Debug.WriteLine("FocusedElement: " + e.NewValue);
            }
        }*/

        [Conditional("DEBUG")]
        private void SubscribeToResourcesChanged()
        {
            Type t = typeof(FrameworkElement);
            EventInfo ei = t.GetEvent("ResourcesChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            Type tDelegate = ei.EventHandlerType;
            MethodInfo h = GetType().GetMethod(nameof(OnResourcesChanged), BindingFlags.NonPublic | BindingFlags.Instance);
            Delegate d = Delegate.CreateDelegate(tDelegate, this, h);
            MethodInfo addHandler = ei.GetAddMethod(true);
            object[] addHandlerArgs = { d };
            addHandler.Invoke(this, addHandlerArgs);
        }

        private void OnResourcesChanged(object sender, EventArgs e)
        {
        }
    }
}
