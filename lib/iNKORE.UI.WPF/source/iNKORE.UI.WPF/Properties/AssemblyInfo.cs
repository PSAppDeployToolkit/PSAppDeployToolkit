using iNKORE.UI.WPF.Helpers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]


[assembly: AssemblyTitle("iNKORE.UI.WPF")]
[assembly: AssemblyDescription("Some frequently-used modules and helpers for your WPF applications")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("iNKORE Studios")]
[assembly: AssemblyProduct("iNKORE.UI.WPF")]
[assembly: AssemblyCopyright("Copyright © iNKORE! 2024")]
[assembly: AssemblyTrademark("iNKORE!")]
[assembly: AssemblyVersion("1.2.5")]
[assembly: AssemblyCulture("")]

[assembly: XmlnsPrefix(Extensions.XmlNamespace, "ikw")]
[assembly: XmlnsDefinition(Extensions.XmlNamespace, "iNKORE.UI.WPF")]
[assembly: XmlnsDefinition(Extensions.XmlNamespace, "iNKORE.UI.WPF.Converters")]
[assembly: XmlnsDefinition(Extensions.XmlNamespace, "iNKORE.UI.WPF.Controls")]
[assembly: XmlnsDefinition(Extensions.XmlNamespace, "iNKORE.UI.WPF.ColorPicker")]
[assembly: XmlnsDefinition(Extensions.XmlNamespace, "iNKORE.UI.WPF.TrayIcons")]
[assembly: XmlnsDefinition(Extensions.XmlNamespace, "iNKORE.UI.WPF.DragDrop")]
[assembly: XmlnsDefinition(Extensions.XmlNamespace, "iNKORE.UI.WPF.Helpers")]
