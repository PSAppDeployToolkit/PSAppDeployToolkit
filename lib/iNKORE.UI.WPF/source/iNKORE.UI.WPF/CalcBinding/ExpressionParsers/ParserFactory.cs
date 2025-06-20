namespace iNKORE.UI.WPF.CalcBinding.ExpressionParsers
{
    public sealed class ParserFactory
    {
        public IExpressionParser CreateCachedParser(IExpressionParser innerParser = null)
        {
            return new CachedExpressionParser(innerParser ?? new ExpressionParser());
        }
    }
}
