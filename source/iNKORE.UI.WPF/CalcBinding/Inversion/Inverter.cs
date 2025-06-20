using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using iNKORE.UI.WPF.CalcBinding.ExpressionParsers;
using DynamicExpresso;

namespace iNKORE.UI.WPF.CalcBinding.Inversion
{
    /// <summary>
    /// Validate and inverse expression of one parameter
    /// </summary>
    public class Inverter
    {
        private static readonly ExpressionFuncsDictionary<ExpressionType> inversedFuncs = new ExpressionFuncsDictionary<ExpressionType> 
        {
            // res = a+c or c+a => a = res - c
            {ExpressionType.Add, ConstantPlace.Wherever, constant => RES + "-" + constant},
            // res = c-a => a = c - res         
            {ExpressionType.Subtract, ConstantPlace.Left, constant => constant + "-" + RES},
            // res = a-c => a = res + c         
            {ExpressionType.Subtract, ConstantPlace.Right, constant => RES + "+" + constant},
            // res = c*a or a*c => a = res / c  
            {ExpressionType.Multiply, ConstantPlace.Wherever, constant => RES + "/" + constant},
            // res = c/a => a = c / res         
            {ExpressionType.Divide, ConstantPlace.Left, constant => constant + "/" + RES},
            // res = a/c => a = res*c           
            {ExpressionType.Divide, ConstantPlace.Right, constant => RES + "*" + constant},
        };
        
        private static readonly ExpressionFuncsDictionary<String> inversedMathFuncs = new ExpressionFuncsDictionary<string>
        {
            // res = Math.Sin(a) => a = Math.Asin(res)
            {"Math.Sin", ConstantPlace.Wherever, dummy => "Math.Asin" + RES},
            // res = Math.Asin(a) => a = Math.Sin(res)
            {"Math.Asin", ConstantPlace.Wherever, dummy => "Math.Sin" + RES},
            
            // res = Math.Cos(a) => a = Math.Acos(res)
            {"Math.Cos", ConstantPlace.Wherever, dummy => "Math.Acos" + RES},
            // res = Math.Acos(a) => a = Math.Cos(res)
            {"Math.Acos", ConstantPlace.Wherever, dummy => "Math.Cos" + RES},
            
            // res = Math.Tan(a) => a = Math.atan(res)
            {"Math.Tan", ConstantPlace.Wherever, dummy => "Math.Atan" + RES},
            // res = Math.Atan(a) => a = Math.Tan(res)
            {"Math.Atan", ConstantPlace.Wherever, dummy => "Math.Tan" + RES},

            // res = Math.Pow(c, a) => a = Math.Pow(res, 1/c)
            {"Math.Pow", ConstantPlace.Left, constant => "Math.Log(" + RES + ", " + constant + ")"},
            // res = Math.Pow(a, c) => a = Math.Pow(res, 1/c)
            {"Math.Pow", ConstantPlace.Right, constant => "Math.Pow(" + RES + ", 1.0/" + constant + ")"},

            // res = Math.Log(c, a) => a = Math.Pow(c, 1/res)
            {"Math.Log", ConstantPlace.Left, constant => "Math.Pow(" + constant + ", 1.0/" + RES + ")"},
            // res = Math.Log(a, c) => a = Math.Pow(c, res)
            {"Math.Log", ConstantPlace.Right, constant => "Math.Pow(" + constant + ", " + RES + ")"},

        };
        
        public Inverter(IExpressionParser interpreter)
        {
            this._interpreter = interpreter;
        }

        /// <summary>
        /// Inverse expression of one parameter
        /// </summary>
        /// <param name="expression">Expression Y=F(X)</param>
        /// <param name="parameter">Type and name of Y parameter</param>
        /// <returns>Inverted expression X = F_back(Y)</returns>
        public Lambda InverseExpression(Expression expression, ParameterExpression parameter)
        {
            var recInfo = new RecursiveInfo();
            String dummy = null;
            InverseExpressionInternal(expression, recInfo, ref dummy);

            if (recInfo.FoundedParamName == null)
                throw new InverseException(String.Format("Parameter was not found in expression '{0}'!", expression));

            // difficult with constant subtrees: we write to string all constant subtrees,
            // but some of them can take Convert operator, which converted to string as Convert(arg).
            // when we try to parse this string, an error occured, because "Convert(arg)" is not
            // a valid expression
            // Solution: remove all Convert(arg) substrings from result string using regex
            // Big problem: we can't remove Convert because it play important role: 1/2 = 0, ((double)1)/2 = 0.5 !!
            // Solution № 2: as Convert element looks bad in ToString() we need to generate substring by constant subtree manually,
            // this is not very hard task. Good.
            // Other solution: switch to Expression based inverse, where we no need to generate string by Expression,
            // only expressions. But I don't wish to do this, because 

            var paramName = parameter.Name;

            var invertedExp = String.Format(recInfo.InvertedExp, paramName);

            var res = _interpreter.Parse(invertedExp, new Parameter(parameter.Name, parameter.Type));                       
            Debug.WriteLine(res.ExpressionText);          
            return res;
        }

        /// <summary>
        /// Generate inversed expression tree from original expression tree of one parameter 
        /// using recursion
        /// </summary>
        /// <param name="expr">Original expression</param>
        /// <param name="recInfo">Out expression</param>
        /// <returns>NodeType - const or variable</returns>
        private NodeType InverseExpressionInternal(Expression expr, RecursiveInfo recInfo, ref string constantExpression)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                    {
                        var binExp = expr as BinaryExpression;

                        string leftConstant = null, rightConstant = null;
                        var leftOperandType = InverseExpressionInternal(binExp.Left, recInfo, ref leftConstant);
                        var rightOperandType = InverseExpressionInternal(binExp.Right, recInfo, ref rightConstant);

                        var nodeType = (leftOperandType == NodeType.Variable || rightOperandType == NodeType.Variable)
                                        ? NodeType.Variable
                                        : NodeType.Constant;

                        if (nodeType == NodeType.Variable)
                        {
                            var constantPlace = leftOperandType == NodeType.Constant ? ConstantPlace.Left : ConstantPlace.Right;
                            var constant = leftOperandType == NodeType.Constant ? leftConstant :rightConstant;
                            recInfo.InvertedExp = String.Format(recInfo.InvertedExp, inversedFuncs[expr.NodeType, constantPlace](constant));
                        }
                        else
                            constantExpression = String.Format("({0}{1}{2})", leftConstant, NodeTypeToString(binExp.NodeType), rightConstant);

                        return nodeType;
                    }
                case ExpressionType.Parameter:
                    {
                        var parameter = expr as ParameterExpression;

                        if (recInfo.FoundedParamName == null)
                        {
                            recInfo.FoundedParamName = parameter.Name;
                            recInfo.InvertedExp = RES;
                            return NodeType.Variable;
                        }

                        if (recInfo.FoundedParamName == parameter.Name)
                            throw new InverseException(String.Format("Variable {0} is defined more than one time!", recInfo.FoundedParamName));
                        else
                            throw new InverseException(String.Format("More than one variables are defined in expression: {0} and {1}", recInfo.FoundedParamName, parameter.Name));
                    }

                case ExpressionType.Constant:
                    {
                        var constant = expr as ConstantExpression;
                        constantExpression = String.Format(CultureInfo.InvariantCulture, "({0})", constant.Value);
                        return NodeType.Constant;
                    }
                case ExpressionType.Convert:
                    {
                        var convertExpr = expr as UnaryExpression;
                        string constant = null;
                        var operandType = InverseExpressionInternal(convertExpr.Operand, recInfo, ref constant);

                        if (operandType == NodeType.Constant)
                            constantExpression = "((" + convertExpr.Type.Name + ")" + constant + ")";
                        else
                            recInfo.InvertedExp = String.Format(recInfo.InvertedExp, "((" + convertExpr.Operand.Type.Name + ")" + RES + ")");
                        return operandType;
                    }
                case ExpressionType.Negate:
                    {
                        var negateExpr = expr as UnaryExpression;
                        string constant = null;
                        var operandType = InverseExpressionInternal(negateExpr.Operand, recInfo, ref constant);

                        if (operandType == NodeType.Constant)
                            constantExpression = "(-" + constant + ")";
                        else
                            recInfo.InvertedExp = String.Format(recInfo.InvertedExp, "(-" + RES + ")");
                        return operandType;
                    }
                case ExpressionType.Not:
                    {
                        var convertExpr = expr as UnaryExpression;

                        string constant = null;
                        var operandType = InverseExpressionInternal(convertExpr.Operand, recInfo, ref constant);

                        if (operandType == NodeType.Constant)
                            constantExpression = "(" + NodeTypeToString(ExpressionType.Not) + constant + ")";
                        else
                            recInfo.InvertedExp = String.Format(recInfo.InvertedExp, "(" + NodeTypeToString(ExpressionType.Not) + RES + ")");
                        return operandType;
                    }
                case ExpressionType.Call:
                    {
                        var methodExpr = expr as MethodCallExpression;

                        var methodName = methodExpr.Method.DeclaringType.Name + "." + methodExpr.Method.Name;
                        if (!inversedMathFuncs.ContainsKey(methodName))
                        {
                            throw new InverseException(String.Format("Unsupported method call expression: {0}", expr));
                        }

                        string leftConstant = null, rightConstant = null;
                        var leftOperandType = InverseExpressionInternal(methodExpr.Arguments[0], recInfo, ref leftConstant);
                        NodeType? rightOperandType = null;
                        Expression leftOperand, rightOperand = null;

                        leftOperand = methodExpr.Arguments[0];

                        if (methodExpr.Arguments.Count == 2)
                        {
                            rightOperandType = InverseExpressionInternal(methodExpr.Arguments[1], recInfo, ref rightConstant);
                            rightOperand = methodExpr.Arguments[1];
                        }

                        string inversedRes = null;
                        if (leftOperandType == NodeType.Variable)
                            inversedRes = inversedMathFuncs[methodName, ConstantPlace.Right](rightConstant);
                        else
                            if (rightOperandType.HasValue && rightOperandType.Value == NodeType.Variable)
                                inversedRes = inversedMathFuncs[methodName, ConstantPlace.Left](leftConstant);
                            else
                            {
                                //constant
                                constantExpression = methodName + "(" + leftConstant;
                                if (rightOperandType != null)
                                    constantExpression += ", " + rightConstant;
                                constantExpression += ")";
                            }

                        if (inversedRes != null)
                            recInfo.InvertedExp = String.Format(recInfo.InvertedExp, inversedRes);

                        return inversedRes == null ? NodeType.Constant : NodeType.Variable;
                    }
                case ExpressionType.MemberAccess:
                    {
                        var memberExpr = expr as MemberExpression;

                        if (memberExpr.Member.DeclaringType.Name == "Math")
                        {
                            constantExpression = String.Format(CultureInfo.InvariantCulture, "({0})", memberExpr.Member.DeclaringType.Name + "." + memberExpr.Member.Name);
                            return NodeType.Constant;
                        }
                        else
                        {
                            throw new InverseException(String.Format("Unsupported method call expression: {0}", expr));
                        }

                    }
                default:
                    throw new InverseException(String.Format("Unsupported expression: {0}", expr));
            }
        }

        private string NodeTypeToString(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Subtract:
                    return "-";
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.Not:
                    return "!";
                default:
                    throw new Exception("Unkwnown binary node type: " + nodeType + "!");
            }
        }

        private const string RES = "({0})";

        private IExpressionParser _interpreter;

        #region Types for recursion func work

        internal enum NodeType
        {
            Variable,
            Constant
        }

        internal enum ConstantPlace
        {
            Left,
            Right,
            Wherever
        }

        private class RecursiveInfo
        {
            public string FoundedParamName;
            public string InvertedExp;
        }

        private delegate String FuncExpressionDelegate(String constant);    
        
        /// <summary>
        /// Dictionary for inversed funcs static initialize
        /// </summary>
        private class ExpressionFuncsDictionary<T> : Dictionary<T, ConstantPlace, FuncExpressionDelegate>
        {
            public override FuncExpressionDelegate this[T key1, ConstantPlace key2]
            {
                get
                {
                    var dict = this[key1];

                    if (dict.ContainsKey(key2))
                        return dict[key2];

                    if (dict.ContainsKey(ConstantPlace.Wherever))
                        return dict[ConstantPlace.Wherever];

                    return dict[key2];
                }
            }
        }
        
        #endregion    
    }
}
