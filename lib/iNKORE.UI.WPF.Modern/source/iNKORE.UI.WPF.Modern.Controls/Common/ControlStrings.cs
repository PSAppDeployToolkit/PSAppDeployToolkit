using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Common
{
    internal class ControlStrings : ResourceAccessor
    {
        public ControlStrings(Type controlType, ModernControlCategory category) : base(GetControlBaseName(controlType, category), GetControlAssembly(controlType))
        {
            
        }


        internal static string GetControlBaseName(Type controlType, ModernControlCategory category)
        {
            var root = controlType.Assembly.GetName().Name;

            root = root + "." + category.ToString() + "." + controlType.Name;
            root = root + "." + "Strings.Resources";

            return root;
        }

        internal static Assembly GetControlAssembly(Type controlType)
        {
            return controlType.Assembly;
        }
    }

    internal enum ModernControlCategory
    {
        Windows,
        Community,
        Extended
    }

}
