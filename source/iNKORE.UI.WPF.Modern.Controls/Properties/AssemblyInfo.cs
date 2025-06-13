using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;
using iNKORE.UI.WPF.Modern;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]


[assembly: AssemblyTitle("iNKORE.UI.WPF.Modern.Controls")]

//[assembly: InternalsVisibleTo("MUXControlsTestApp")]

[assembly: XmlnsDefinition(ThemeManager.XmlNamespace, "iNKORE.UI.WPF.Modern.Controls")]
[assembly: XmlnsDefinition(ThemeManager.XmlNamespace, "iNKORE.UI.WPF.Modern.Controls.Primitives")]
