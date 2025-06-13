using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Helpers;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Controls
{
    /// <summary>
    /// Default styles for controls.
    /// </summary>
    public class XamlControlsResources : ResourceDictionary
    {
        /// <summary>
        /// Initializes a new instance of the XamlControlsResources class.
        /// </summary>
        public XamlControlsResources()
        {
            MergedDictionaries.Add(ThemeResources);
            MergedDictionaries.Add(ControlsResources);
            
            if(UISettingsResources != null)
            {
                MergedDictionaries.Add(UISettingsResources);
            }

            if (DesignMode.DesignModeEnabled)
            {
                _ = CompactResources;
            }

            if(_current == null)
            {
                _current = this;
            }
        }

        private static XamlControlsResources _current;
        public static XamlControlsResources Current
        {
            get { return _current; }
        }

        public bool UseCompactResources
        {
            get => _useCompactResources;
            set
            {
                if (_useCompactResources != value)
                {
                    _useCompactResources = value;

                    if (UseCompactResources)
                    {
                        MergedDictionaries.Add(CompactResources);
                    }
                    else
                    {
                        MergedDictionaries.Remove(CompactResources);
                    };
                }
            }
        }

        internal static ResourceDictionary ThemeResources
        {
            get
            {
                if (_themeResources == null)
                {
                    _themeResources = new ResourceDictionary { Source = PackUriHelper.GetAbsoluteUri("Themes/ThemeResources.xaml") };
                }
                return _themeResources;
            }
        }

        internal static ResourceDictionary ControlsResources
        {
            get
            {
                if (_controlsResources == null)
                {
                    _controlsResources = new ResourceDictionary { Source = PackUriHelper.GetAbsoluteUri("Themes/ControlsResources.xaml") };
                }
                return _controlsResources;
            }
        }

        internal static ResourceDictionary CompactResources
        {
            get
            {
                if (_compactResources == null)
                {
                    _compactResources = new ResourceDictionary { Source = PackUriHelper.GetAbsoluteUri("Themes/DensityStyles/Compact.xaml") };
                }
                return _compactResources;
            }
        }

        internal static ResourceDictionary UISettingsResources
        {
            get
            {
                try
                {
                    return _uiSettingsResources ??= new UISettingsResources();
                }
                catch
                {
                    return null;
                }
            }
        }

        private static ResourceDictionary _themeResources;
        private static ResourceDictionary _controlsResources;
        private static ResourceDictionary _compactResources;
        private static ResourceDictionary _uiSettingsResources;

        private bool _useCompactResources;
    }
}
