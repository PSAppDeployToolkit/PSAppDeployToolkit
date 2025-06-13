using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Helpers;
using System;
using System.ComponentModel;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Themes.DesignTime
{
    public abstract class IntellisenseResourcesBase : ResourceDictionary, ISupportInitialize
    {
        protected IntellisenseResourcesBase()
        {
        }

        public new Uri Source
        {
            get => base.Source;
            set
            {
                if (DesignMode.DesignModeEnabled)
                {
                    base.Source = value;
                }
            }
        }

        public new void EndInit()
        {
            Clear();
            MergedDictionaries.Clear();
            base.EndInit();
        }

        void ISupportInitialize.EndInit()
        {
            EndInit();
        }
    }
}
