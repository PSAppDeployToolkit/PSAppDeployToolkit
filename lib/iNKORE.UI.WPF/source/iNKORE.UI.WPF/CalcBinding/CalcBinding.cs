using iNKORE.UI.WPF.CalcBinding.ExpressionParsers;
using iNKORE.UI.WPF.CalcBinding.PathAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace iNKORE.UI.WPF.CalcBinding
{
    /// <summary>
    /// Binding with advantages
    /// </summary>
    public class Binding : MarkupExtension
    {
        // We cannot use PropertyPath instead of string (such as standart Binding) because transformation from xaml value string to Property path 
        // is doing automatically by PropertyPathConverter and result PropertyPath object could have form, that cannot retranslate to normal string.
        // e.g.: (local:MyStaticVM.Prop) -> PropertyPath.Path = (0), Converted to string = MyStaticVM.Prop (but we need to analyze static class with xaml namespace prefix)
        public string Path { get; set; }

        /// <summary>
        /// False to visibility. Default: False = Collapsed
        /// </summary>
        public FalseToVisibility FalseToVisibility { get; set; } = FalseToVisibility.Collapsed;

        /// <summary>
        /// If true then single quotes and double quotes are considered as single quotes, otherwise - both are considerent as double quotes
        /// </summary>
        /// <remarks>
        /// Use this flag if you need to use char is path expresion
        /// </remarks>
        public bool SingleQuotes { get; set; } = false;

        /// <summary> Value to use when source cannot provide a value </summary>
        /// <remarks>
        ///     Initialized to DependencyProperty.UnsetValue; if FallbackValue is not set, BindingExpression
        ///     will return target property's default when Binding cannot get a real value.
        /// </remarks>
        public object FallbackValue { get; set; } = DependencyProperty.UnsetValue;

        public Binding()
        {
            Mode = BindingMode.Default;
        }

        public Binding(String path)
            : this()
        {
            Path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var targetPropertyType = GetPropertyType(serviceProvider);
            var typeResolver = (IXamlTypeResolver)serviceProvider.GetService(typeof(IXamlTypeResolver));
            var typeDescriptor = serviceProvider as ITypeDescriptorContext;

            var normalizedPath = NormalizePath(Path);
            var pathes = GetSourcePathes(normalizedPath, typeResolver);

            var expressionTemplate = GetExpressionTemplate(normalizedPath, pathes, out Dictionary<string, Type> enumParameters);

            var mathConverter = new CalcConverter(_parser.Value, FallbackValue, enumParameters)
            {
                FalseToVisibility = FalseToVisibility,
                StringFormatDefined = StringFormat != null,
            };

            var bindingPathes = pathes
                .Where(p => p.PathId.PathType == PathTokenType.Property || 
                            p.PathId.PathType == PathTokenType.StaticProperty).ToList();

            BindingBase resBinding;

            if (bindingPathes.Count == 1)
            {
                // todo: can enums be binded ? What if one value is Enum? bug..
                var binding = new System.Windows.Data.Binding()
                {
                    Mode = Mode,
                    NotifyOnSourceUpdated = NotifyOnSourceUpdated,
                    NotifyOnTargetUpdated = NotifyOnTargetUpdated,
                    NotifyOnValidationError = NotifyOnValidationError,
                    UpdateSourceExceptionFilter = UpdateSourceExceptionFilter,
                    UpdateSourceTrigger = UpdateSourceTrigger,
                    ValidatesOnDataErrors = ValidatesOnDataErrors,
                    ValidatesOnExceptions = ValidatesOnExceptions,
                    FallbackValue = FallbackValue,
#if NET45
                    ValidatesOnNotifyDataErrors = ValidatesOnNotifyDataErrors,
#endif
            };

                var pathId = bindingPathes.Single().PathId;
                // we need to use convert from string for support of static properties
                var pathValue = pathId.Value;

                if (pathId.PathType == PathTokenType.StaticProperty)
                {
                    pathValue = string.Format("({0})", pathValue);  // need to use brackets for Static property recognition in standart binding
                }
                var resPath = (PropertyPath)new PropertyPathConverter().ConvertFromString(typeDescriptor, pathValue);
                binding.Path = resPath;

                if (Source != null)
                    binding.Source = Source;

                if (ElementName != null)
                    binding.ElementName = ElementName;

                if (RelativeSource != null)
                    binding.RelativeSource = RelativeSource;

                if (StringFormat != null)
                    binding.StringFormat = StringFormat;

                // we don't use converter if binding is trivial - {0}, except type convertion from bool to visibility

                //todo: use more smart recognition for template (with analysing brackets ({1}) any count )
                // trivial binding, CalcBinding converter is not needed
                if ((expressionTemplate != "{0}" && expressionTemplate != "({0})") || targetPropertyType == typeof(Visibility))
                {
                    binding.Converter = mathConverter;
                    binding.ConverterParameter = expressionTemplate;
                    binding.ConverterCulture = ConverterCulture;
                }
                resBinding = binding;
            }
            else
            {
                var mBinding = new MultiBinding
                {
                    Converter = mathConverter,
                    ConverterParameter = expressionTemplate,
                    ConverterCulture = ConverterCulture,
                    Mode = BindingMode.OneWay,
                    NotifyOnSourceUpdated = NotifyOnSourceUpdated,
                    NotifyOnTargetUpdated = NotifyOnTargetUpdated,
                    NotifyOnValidationError = NotifyOnValidationError,
                    UpdateSourceExceptionFilter = UpdateSourceExceptionFilter,
                    UpdateSourceTrigger = UpdateSourceTrigger,
                    ValidatesOnDataErrors = ValidatesOnDataErrors,
                    ValidatesOnExceptions = ValidatesOnExceptions,
                    FallbackValue = FallbackValue,
#if NET45
                    ValidatesOnNotifyDataErrors = ValidatesOnNotifyDataErrors,
#endif
                };

                if (StringFormat != null)
                    mBinding.StringFormat = StringFormat;

                foreach (var path in bindingPathes)
                {
                    var binding = new System.Windows.Data.Binding();

                    // we need to use convert from string for support of static properties
                    var pathValue = path.PathId.Value;

                    if (path.PathId.PathType == PathTokenType.StaticProperty)
                    {
                        pathValue = string.Format("({0})", pathValue);  // need to use brackets for Static property recognition in standart binding
                    }

                    var resPath = (PropertyPath)new PropertyPathConverter().ConvertFromString(typeDescriptor, pathValue);

                    binding.Path = resPath;

                    if (Source != null)
                        binding.Source = Source;

                    if (ElementName != null)
                        binding.ElementName = ElementName;

                    if (RelativeSource != null)
                        binding.RelativeSource = RelativeSource;

                    mBinding.Bindings.Add(binding);
                }

                resBinding = mBinding;
            }

            return resBinding.ProvideValue(serviceProvider);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void ReplaceExpressionParser(IExpressionParser expressionParser)
        {
            if (expressionParser == null)
                throw new ArgumentNullException(nameof(expressionParser));

            _parser = new Lazy<IExpressionParser>(() => expressionParser);
        }

        private Type GetPropertyType(IServiceProvider serviceProvider)
        {
            //provider of target object and it's property
            var targetProvider = (IProvideValueTarget)serviceProvider
                .GetService(typeof(IProvideValueTarget));

            if (targetProvider.TargetProperty is DependencyProperty)
            {
                return ((DependencyProperty)targetProvider.TargetProperty).PropertyType;
            }

            return targetProvider.TargetProperty.GetType();
        }

        /// <summary>
        /// Replace source properties pathes by its numbers
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pathes"></param>
        /// <returns></returns>
        private string GetExpressionTemplate(string path, List<PathAppearances> properties, out Dictionary<string, Type> enumParameters)
        {
            var result = "";
            var sourceIndex = 0;

            var passedProps = new Dictionary<PathTokenId, string>();
            var enumNames = new Dictionary<PathTokenId, string>();

            enumParameters = new Dictionary<string, Type>();

            while (sourceIndex < path.Length)
            {
                var replaced = false;
                for (int index = 0; index < properties.Count(); index++)
                {
                    var propGroup = properties[index];
                    var propId = propGroup.PathId;
                    var targetProp = propGroup.Pathes.FirstOrDefault(token => token.Start == sourceIndex);

                    if (targetProp != null)
                    {
                        var propPath = propId.Value;

                        if (propId.PathType == PathTokenType.Property || propId.PathType == PathTokenType.StaticProperty)
                        {
                            string replace = null;
                            if (passedProps.ContainsKey(propId))
                            {
                                replace = passedProps[propId];
                            }
                            else
                            {
                                replace = (passedProps.Count).ToString("{0}");
                                passedProps.Add(propId, replace);
                            }

                            result += replace;
                            sourceIndex += propPath.Length;
                            replaced = true;
                        }
                        else if (propId.PathType == PathTokenType.Enum)
                        {
                            var enumPath = propGroup.Pathes.First() as EnumToken;

                            string enumTypeName = null;
                            if (enumNames.ContainsKey(propId))
                            {
                                enumTypeName = enumNames[propId];
                            }
                            else
                            {
                                enumTypeName = GetEnumName(enumNames.Count);
                                enumNames.Add(propId, enumTypeName);
                                enumParameters.Add(enumTypeName, enumPath.Enum);
                            }

                            var replace = string.Join(".", enumTypeName, enumPath.EnumMember);

                            result += replace;
                            sourceIndex += propPath.Length;
                            replaced = true;
                        }
                        if (replaced)
                            break;
                    }
                }

                if (!replaced)
                {
                    result += path[sourceIndex];
                    sourceIndex++;
                }
            }

            return result;
        }

        private string GetEnumName(int i)
        {
            // Enum1, Enum2, etc
            return string.Format("Enum{0}", ++i);
        }

        /// <summary>
        /// Find all sourceProperties pathes in Path string
        /// </summary>
        /// <param name="normPath"></param>
        /// <returns>List of pathes and its start positions</returns>
        private List<PathAppearances> GetSourcePathes(string normPath, IXamlTypeResolver typeResolver)
        {
            var propertyPathAnalyzer = new PropertyPathAnalyzer();

            var pathes = propertyPathAnalyzer.GetPathes(normPath, typeResolver);

            var propertiesGroups = pathes.GroupBy(p => p.Id).Select(p => new PathAppearances(p.Key, p.ToList())).ToList();

            return propertiesGroups;
        }

        /// <summary>
        /// Replace operators labels to operators names (ex. and -> &&), remove excess spaces
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string NormalizePath(string path)
        {
            var replaceDict = new Dictionary<String, String>
            {
                {" and ",     " && "},
                {")and ",     ")&& "},
                {" and(",     " &&("},
                {")and(",     ")&&("},

                {" or ",      " || "},
                {")or ",      ")|| "},
                {" or(",      " ||("},
                {")or(",      ")||("},

                {" less ",    " < "},
                {")less ",    ")< "},
                {" less(",    " <("},
                {")less(",    ")<("},

                {" less=",   " <="}, 
                {")less=",   ")<="}, 

                {"not ",    "!"}
            };

            if (!SingleQuotes)
                replaceDict.Add("\'", "\"");
            else
                replaceDict.Add("\"", "\'");

            var normPath = path;
            foreach (var pair in replaceDict)
                normPath = normPath.Replace(pair.Key, pair.Value);

            return normPath;
        }

        #region Binding Properties

        //
        // Summary:
        //     Gets or sets the converter to use to convert the source values to or from
        //     the target value.
        //
        // Returns:
        //     A value of type System.Windows.Data.IMultiValueConverter that indicates the
        //     converter to use. The default value is null.
        [DefaultValue("")]
        public IMultiValueConverter Converter { get; set; }
        //
        // Summary:
        //     Gets or sets the System.Globalization.CultureInfo object that applies to
        //     any converter assigned to bindings wrapped by the System.Windows.Data.MultiBinding
        //     or on the System.Windows.Data.MultiBinding itself.
        //
        // Returns:
        //     A valid System.Globalization.CultureInfo.
        [DefaultValue("")]
        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
        public CultureInfo ConverterCulture { get; set; }
        //
        // Summary:
        //     Gets or sets an optional parameter to pass to a converter as additional information.
        //
        // Returns:
        //     A parameter to pass to a converter. The default value is null.
        [DefaultValue("")]
        public object ConverterParameter { get; set; }
        //
        // Summary:
        //     Gets or sets a value that indicates the direction of the data flow of this
        //     binding.
        //
        // Returns:
        //     One of the System.Windows.Data.BindingMode values. The default value is System.Windows.Data.BindingMode.Default,
        //     which returns the default binding mode value of the target dependency property.
        //     However, the default value varies for each dependency property. In general,
        //     user-editable control properties, such as System.Windows.Controls.TextBox.Text,
        //     default to two-way bindings, whereas most other properties default to one-way
        //     bindings.A programmatic way to determine whether a dependency property binds
        //     one-way or two-way by default is to get the property metadata of the property
        //     using System.Windows.DependencyProperty.GetMetadata(System.Type) and then
        //     check the Boolean value of the System.Windows.FrameworkPropertyMetadata.BindsTwoWayByDefault
        //     property.
        [DefaultValue(BindingMode.Default)]
        public BindingMode Mode { get; set; }
        //
        // Summary:
        //     Gets or sets a value that indicates whether to raise the System.Windows.FrameworkElement.SourceUpdated
        //     event when a value is transferred from the binding target to the binding
        //     source.
        //
        // Returns:
        //     true if the System.Windows.FrameworkElement.SourceUpdated event will be raised
        //     when the binding source value is updated; otherwise, false. The default value
        //     is false.
        [DefaultValue(false)]
        public bool NotifyOnSourceUpdated { get; set; }
        //
        // Summary:
        //     Gets or sets a value that indicates whether to raise the System.Windows.FrameworkElement.TargetUpdated
        //     event when a value is transferred from the binding source to the binding
        //     target.
        //
        // Returns:
        //     true if the System.Windows.FrameworkElement.TargetUpdated event will be raised
        //     when the binding target value is updated; otherwise, false. The default value
        //     is false.
        [DefaultValue(false)]
        public bool NotifyOnTargetUpdated { get; set; }
        //
        // Summary:
        //     Gets or sets a value that indicates whether to raise the System.Windows.Controls.Validation.Error attached
        //     event on the bound element.
        //
        // Returns:
        //     true if the System.Windows.Controls.Validation.Error attached event will
        //     be raised on the bound element when there is a validation error during source
        //     updates; otherwise, false. The default value is false.
        [DefaultValue(false)]
        public bool NotifyOnValidationError { get; set; }
        //
        // Summary:
        //     Gets or sets a handler you can use to provide custom logic for handling exceptions
        //     that the binding engine encounters during the update of the binding source
        //     value. This is only applicable if you have associated the System.Windows.Controls.ExceptionValidationRule
        //     with your System.Windows.Data.MultiBinding object.
        //
        // Returns:
        //     A method that provides custom logic for handling exceptions that the binding
        //     engine encounters during the update of the binding source value.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UpdateSourceExceptionFilterCallback UpdateSourceExceptionFilter { get; set; }
        //
        // Summary:
        //     Gets or sets a value that determines the timing of binding source updates.
        //
        // Returns:
        //     One of the System.Windows.Data.UpdateSourceTrigger values. The default value
        //     is System.Windows.Data.UpdateSourceTrigger.Default, which returns the default
        //     System.Windows.Data.UpdateSourceTrigger value of the target dependency property.
        //     However, the default value for most dependency properties is System.Windows.Data.UpdateSourceTrigger.PropertyChanged,
        //     while the System.Windows.Controls.TextBox.Text property has a default value
        //     of System.Windows.Data.UpdateSourceTrigger.LostFocus.A programmatic way to
        //     determine the default System.Windows.Data.Binding.UpdateSourceTrigger value
        //     of a dependency property is to get the property metadata of the property
        //     using System.Windows.DependencyProperty.GetMetadata(System.Type) and then
        //     check the value of the System.Windows.FrameworkPropertyMetadata.DefaultUpdateSourceTrigger
        //     property.
        public UpdateSourceTrigger UpdateSourceTrigger { get; set; }
        //
        // Summary:
        //     Gets or sets a value that indicates whether to include the System.Windows.Controls.DataErrorValidationRule.
        //
        // Returns:
        //     true to include the System.Windows.Controls.DataErrorValidationRule; otherwise,
        //     false.
        [DefaultValue(false)]
        public bool ValidatesOnDataErrors { get; set; }

        // Summary:
        //     Gets or sets a value that indicates whether to include the System.Windows.Controls.ExceptionValidationRule.
        //
        // Returns:
        //     true to include the System.Windows.Controls.ExceptionValidationRule; otherwise,
        //     false.
        [DefaultValue(false)]
        public bool ValidatesOnExceptions { get; set; }

#if NET45
        //
        // Summary:
        //     Gets or sets a value that indicates whether to include the System.Windows.Controls.NotifyDataErrorValidationRule.
        //
        // Returns:
        //     true to include the System.Windows.Controls.NotifyDataErrorValidationRule;
        //     otherwise, false. The default is true.
        [DefaultValue(true)]
        public bool ValidatesOnNotifyDataErrors { get; set; }
#endif
        //
        // Summary:
        //     Gets or sets the binding source by specifying its location relative to the
        //     position of the binding target.
        //
        // Returns:
        //     A System.Windows.Data.RelativeSource object specifying the relative location
        //     of the binding source to use. The default is null.
        [DefaultValue("")]
        public RelativeSource RelativeSource { get; set; }
        //
        // Summary:
        //     Gets or sets the object to use as the binding source.
        //
        // Returns:
        //     The object to use as the binding source.
        public object Source { get; set; }

        //
        // Summary:
        //     Gets or sets the name of the element to use as the binding source object.
        //
        // Returns:
        //     The value of the Name property or x:Name Directive of the element of interest.
        //     You can refer to elements in code only if they are registered to the appropriate
        //     System.Windows.NameScope through RegisterName. For more information, see
        //     WPF XAML Namescopes.The default is null.
        [DefaultValue("")]
        public string ElementName { get; set; }

        //
        // Summary:
        //     Gets or sets a string that specifies how to format the binding if it displays
        //     the bound value as a string.
        //
        // Returns:
        //     A string that specifies how to format the binding if it displays the bound
        //     value as a string.
        [DefaultValue("")]
        public string StringFormat { get; set; }

        #endregion

        private static Lazy<IExpressionParser> _parser = new Lazy<IExpressionParser>(() => new ParserFactory().CreateCachedParser());

        class PathAppearances
        {
            public PathTokenId PathId { get; private set; }

            public IEnumerable<PathToken> Pathes { get; private set; }

            public PathAppearances(PathTokenId id, List<PathToken> pathes)
            {
                PathId = id;
                Pathes = pathes;
            }
        }
    }
}
