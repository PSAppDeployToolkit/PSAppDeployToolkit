using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Windows.Foundation.Metadata;

namespace iNKORE.UI.WPF.Modern.Gallery.Helpers
{
    /// <summary>
    /// RootFrameNavigationHelper registers for standard mouse and keyboard
    /// shortcuts used to go back and forward. There should be only one
    /// RootFrameNavigationHelper per view, and it should be associated with the
    /// root frame.
    /// </summary>
    /// <example>
    /// To make use of RootFrameNavigationHelper, create an instance of the
    /// RootNavigationHelper such as in the constructor of your root page.
    /// <code>
    ///     public MyRootPage()
    ///     {
    ///         this.InitializeComponent();
    ///         this.rootNavigationHelper = new RootNavigationHelper(MyFrame);
    ///     }
    /// </code>
    /// </example>
    [Windows.Foundation.Metadata.WebHostHidden]
    public class RootFrameNavigationHelper
    {
        private Frame Frame { get; set; }
        private NavigationView CurrentNavView { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootNavigationHelper"/> class.
        /// </summary>
        /// <param name="rootFrame">A reference to the top-level frame.
        /// This reference allows for frame manipulation and to register navigation handlers.</param>
        public RootFrameNavigationHelper(Frame rootFrame, NavigationView currentNavView)
        {
            this.Frame = rootFrame;
            this.Frame.Navigated += (s, e) =>
            {
                // Update the Back button whenever a navigation occurs.
                UpdateBackButton();
            };
            this.CurrentNavView = currentNavView;

            CurrentNavView.BackRequested += NavView_BackRequested;
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            // don't go back if the nav pane is overlayed
            if (this.CurrentNavView.IsPaneOpen && (this.CurrentNavView.DisplayMode == NavigationViewDisplayMode.Compact || this.CurrentNavView.DisplayMode == NavigationViewDisplayMode.Minimal))
            {
                return false;
            }

            bool navigated = false;
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
                navigated = true;
            }

            return navigated;
        }

        private bool TryGoForward()
        {
            bool navigated = false;
            if (this.Frame.CanGoForward)
            {
                this.Frame.GoForward();
                navigated = true;
            }
            return navigated;
        }

        private void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = TryGoBack();
            }
        }

        private void UpdateBackButton()
        {
            this.CurrentNavView.IsBackEnabled = this.Frame.CanGoBack ? true : false;
        }
    }
}
