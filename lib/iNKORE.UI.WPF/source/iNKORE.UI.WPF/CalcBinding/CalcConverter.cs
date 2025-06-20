using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using iNKORE.UI.WPF.CalcBinding.ExpressionParsers;
using iNKORE.UI.WPF.CalcBinding.Inversion;
using iNKORE.UI.WPF.CalcBinding.Trace;
using DynamicExpresso;
using Expression = System.Linq.Expressions.Expression;

namespace iNKORE.UI.WPF.CalcBinding
{
    /// <summary>
    /// Converter that supports expression evaluate
    /// </summary>
    public class CalcConverter : IValueConverter, IMultiValueConverter
    {
        public bool StringFormatDefined { get; set; }

        public FalseToVisibility FalseToVisibility { get; set; } = FalseToVisibility.Collapsed;

        #region Init

        public CalcConverter(IExpressionParser parser, object fallbackValue, Dictionary<string, Type> enums)
        {
            _parser = parser;
            _fallbackValue = fallbackValue;

            if (parser != null && enums != null && enums.Any())
            {
                parser.SetReference(enums.Select(ep => new ReferenceType(ep.Key, ep.Value)));
            }
        }

        #endregion

        #region IValueConverter
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(new [] { value }, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (_compiledExpression == null)
            {
                if ((_compiledExpression = CompileExpression(null, (string)parameter, true, new List<Type>{targetType})) == null)
                    return null;
            }

            if (_compiledInversedExpression == null)
            {
                //try convert back expression
                try
                {
                    var resType = _compiledExpression.Expression.Type;
                    var param = Expression.Parameter(resType, "Path");
                    _compiledInversedExpression = new Inverter(_parser).InverseExpression(_compiledExpression.Expression, param);
                }
                catch (Exception e)
                {
                    Tracer.TraceError("Can't convert back expression " + parameter + ": " + e.Message);
                }
            }

            if (_compiledInversedExpression != null)
            {
                try
                {
                    if (targetType == typeof(bool) && value.GetType() == typeof(Visibility))
                        value = new BoolToVisibilityConverter(FalseToVisibility)
                            .ConvertBack(value, targetType, null, culture);

                    if (value is string && _compiledExpression.Expression.Type != value.GetType())
                        value = ParseStringToObject((string)value, _compiledExpression.Expression.Type);

                    var source = _compiledInversedExpression.Invoke(value);
                    return source;
                }
                catch (Exception e)
                {
                    Tracer.TraceError("Can't invoke back expression " + parameter + ": " + e.Message);
                }
            }
            return null;
        }

        private object ParseStringToObject(string value, Type type)
        {
            var res = System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            return res;
        } 

        #endregion
        
        #region IMultiValueConverter
        
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                return null;

            if (_sourceValuesTypes == null)
            {
                _sourceValuesTypes = GetTypes(values);
            }
            else
            {
                var currentValuesTypes = GetTypes(values);

                if (!_sourceValuesTypes.SequenceEqual(currentValuesTypes))
                {
                    _sourceValuesTypes = currentValuesTypes;

                    _compiledExpression = null;
                    _compiledInversedExpression = null;
                }
            }

            if (_compiledExpression == null)
            {
                if ((_compiledExpression = CompileExpression(values, (string)parameter)) == null)
                    return _fallbackValue;
            }

            try
            {
                var result = _compiledExpression.Invoke(values);

                if (!StringFormatDefined)
                {
                    if (targetType == typeof(Visibility))
                    {
                        if (!(result is Visibility))
                            result = new BoolToVisibilityConverter(FalseToVisibility)
                                        .Convert(result, targetType, null, culture);
                    }

                    if (targetType == typeof(String))
                        result = String.Format(CultureInfo.InvariantCulture, "{0}", result);
                }
                return result;
            }
            catch (Exception e)
            {
                Tracer.TraceError("Can't invoke expression " + _compiledExpression.ExpressionText + ": " + e.Message);
                return null;
            }
        }

        private Type[] GetTypes(object[] values)
        {
            return values.Select(v => v != null ? v.GetType() : null).ToArray();
        }

        private Lambda CompileExpression(Object[] values, string expressionTemplate, bool convertBack = false, List<Type> targetTypes = null)
        {
            try
            {
                Lambda res = null;

                var needCompile = false;
                // we can't determine value type if value is null
                // so, binding Path = (a == null) ? "a" : "b" is permitted
                if (convertBack)
                    needCompile = true;
                else
                    if (values.Contains(DependencyProperty.UnsetValue))
                    {
                        Tracer.TraceError("One of source fields is Unset");
                    }
                    else
                    {
                        needCompile = true;
                    }

                if (needCompile)
                {
                    var argumentsTypes = convertBack ? targetTypes : _sourceValuesTypes.Select(t => t ?? typeof(Object)).ToList();
                    res = CompileExpression(argumentsTypes, expressionTemplate);
                }

                return res;
            }
            catch (Exception e)
            {
                Tracer.TraceError("Can't convert expression " + expressionTemplate + ": " + e.Message);
                return null;
            }
        }

        private Lambda CompileExpression(List<Type> argumentsTypes, string expressionTemplate)
        {
            var parametersDefinition = new List<Parameter>();
            
            for (int i = 0; i < argumentsTypes.Count(); i++)
            {
                var paramName = GetVariableName(i);
                
                expressionTemplate = expressionTemplate.Replace("{" + i + "}", paramName);
                parametersDefinition.Add(new Parameter(paramName, argumentsTypes[i]));
            }

            var compiledExpression = _parser.Parse(expressionTemplate, parametersDefinition.ToArray());

            return compiledExpression;
        }

        /// <summary>
        /// Returns string of one char, following from 'a' on i positions (1 -> b, 2 -> c)
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private string GetVariableName(int i)
        {
            //p1 p2 etc
            return String.Format("p{0}", ++i);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion

        private IExpressionParser _parser;
        private readonly object _fallbackValue;
        private Lambda _compiledExpression;
        private Lambda _compiledInversedExpression;
        private Type[] _sourceValuesTypes;
        private static readonly Tracer Tracer = new Tracer(TraceComponent.CalcConverter);
    }
}
