using DynamicExpresso;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace iNKORE.UI.WPF.CalcBinding.ExpressionParsers
{
    public sealed class CachedExpressionParser : IExpressionParser
    {
        public CachedExpressionParser(IExpressionParser innerParser)
        {
            _innerParser = innerParser;
        }

        public Lambda Parse(string expressionText, Parameter[] parameters)
        {
            var expressionKey = new ExpressionKey(expressionText, parameters);

            var cachedLambda = FindInCache(expressionKey);
            if (cachedLambda != null)
                return cachedLambda;

            var lambda = _innerParser.Parse(expressionText, parameters);
            SaveInCache(expressionKey, lambda);

            return lambda;
        }

        private void SaveInCache(ExpressionKey key, Lambda lambda)
        {
            _cachedExpressions[key] = new WeakReference(lambda);
        }

        private Lambda FindInCache(ExpressionKey expressionKey)
        {
            if (_cachedExpressions.ContainsKey(expressionKey))
            {
                var expressionRef = _cachedExpressions[expressionKey];

                Lambda lambda = expressionRef.Target as Lambda;
                if (lambda != null)
                {
                    return lambda;
                }
                else
                {
                    _cachedExpressions.Remove(expressionKey);
                    RemoveDeadExpressions();
                }
            }

            return null;
        }

        private void RemoveDeadExpressions()
        {
            foreach (var key in _cachedExpressions.Keys.ToList())
            {
                if (!_cachedExpressions[key].IsAlive)
                    _cachedExpressions.Remove(key);
            }
        }

        public void SetReference(IEnumerable<ReferenceType> referencedTypes)
        {
            _innerParser.SetReference(referencedTypes);
        }

        private Dictionary<ExpressionKey, WeakReference> _cachedExpressions = new Dictionary<ExpressionKey, WeakReference>();
        private IExpressionParser _innerParser;

        private struct ExpressionKey : IEquatable<ExpressionKey>
        {
            private readonly string _expressionText;
            private readonly Parameter[] _parameters;

            public ExpressionKey(string expressionText, Parameter[] parameters)
            {
                _expressionText = expressionText;
                _parameters = parameters;
            }

            public override int GetHashCode()
            {
                return (_expressionText.GetHashCode() * 397) ^ (_parameters.Length);
            }

            public bool Equals(ExpressionKey other)
            {
                return string.Equals(_expressionText, other._expressionText)
                    && _parameters.SequenceEqual(other._parameters, _parameterComparer);
            }

            private static ParameterComparer _parameterComparer = new ParameterComparer();
        }

        private class ParameterComparer : IEqualityComparer<Parameter>
        {
            public bool Equals(Parameter x, Parameter y)
            {
                return string.Equals(x.Name, y.Name) && x.Type == y.Type;
            }

            public int GetHashCode(Parameter parameter)
            {
                return (parameter.Name.GetHashCode() * 397) ^ (parameter.Type.GetHashCode());
            }
        }
    }
}