using System.Windows;

namespace iNKORE.UI.WPF.Modern.Helpers
{
    internal static class ResourceDictionaryHelper
    {
        public static void SealValues(this ResourceDictionary dictionary)
        {
            foreach (var md in dictionary.MergedDictionaries)
            {
                md.SealValues();
            }

            foreach (var value in dictionary.Values)
            {
                if (value is Freezable freezable)
                {
                    if (!freezable.CanFreeze)
                    {
                        var enumerator = freezable.GetLocalValueEnumerator();
                        while (enumerator.MoveNext())
                        {
                            var property = enumerator.Current.Property;
                            if (DependencyPropertyHelper.GetValueSource(freezable, property).IsExpression)
                            {
                                freezable.SetValue(property, freezable.GetValue(property));
                            }
                        }
                    }

                    if (!freezable.IsFrozen && freezable.CanFreeze)
                    {
                        freezable.Freeze();
                    }
                }
                else if (value is Style style)
                {
                    if (!style.IsSealed)
                    {
                        style.Seal();
                    }
                }
            }

            if (dictionary is ResourceDictionaryEx rdEx)
            {
                foreach (var td in rdEx.ThemeDictionaries.Values)
                {
                    td.SealValues();
                }
            }
        }
    }
}
