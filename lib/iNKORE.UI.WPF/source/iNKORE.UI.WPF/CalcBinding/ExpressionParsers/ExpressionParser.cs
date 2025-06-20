using System.Collections.Generic;
using DynamicExpresso;

namespace iNKORE.UI.WPF.CalcBinding.ExpressionParsers
{
    public sealed class ExpressionParser : IExpressionParser
    {
        public ExpressionParser()
        {
            _interpreter = new Interpreter();
        }

        public Lambda Parse(string expressionText, Parameter[] parameters)
        {
            return _interpreter.Parse(expressionText, parameters);
        }

        public void SetReference(IEnumerable<ReferenceType> referencedTypes)
        {
            _interpreter.Reference(referencedTypes);
        }

        private Interpreter _interpreter;
    }
}
