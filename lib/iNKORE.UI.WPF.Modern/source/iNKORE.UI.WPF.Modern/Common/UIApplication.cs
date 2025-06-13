using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Common
{
    public class UIApplication
    {
        private readonly Application _application;
        public bool IsApplication => _application is not null;
        public UIApplication(Application application)
        {
            _application = application;
        }

        private ResourceDictionary _resources;
        public ResourceDictionary Resources
        {
            get
            {
                if (_resources is null)
                {
                    _resources = new ResourceDictionary();
                }

                if (ThemeResources.Current != null && !_resources.MergedDictionaries.Contains(ThemeResources.Current))
                    _resources.MergedDictionaries.Add(ThemeResources.Current);
                if (XamlControlsResources.Current != null && !_resources.MergedDictionaries.Contains(XamlControlsResources.Current))
                    _resources.MergedDictionaries.Add(XamlControlsResources.Current);

                return _application?.Resources ?? _resources;
            }
            set
            {
                if (_application is not null)
                    _application.Resources = value;
                _resources = value;
            }
        }

        private Window _mainWindow;
        public Window MainWindow
        {
            get
            {
                return _application?.MainWindow ?? _mainWindow;
            }
            set
            {
                if (_application is not null)
                    _application.MainWindow = value;
                _mainWindow = value;
            }
        }

        public void Shutdown()
        {
            _application?.Shutdown();
        }

        public static UIApplication Current => GetUIApplication();

        private static UIApplication _uiApplication;
        private static UIApplication GetUIApplication()
        {
            if (_uiApplication is null)
                _uiApplication = new UIApplication(Application.Current);
            return _uiApplication;
        }

        public object FindResource(object key)
        {
            object res = null;
            try
            {
                if (_application != null)
                {
                    res = _application.FindResource(key);
                }

                if (res != null) return res;
            }
            catch { }

            res = Resources[key];
            return res;
        }
    }
}
