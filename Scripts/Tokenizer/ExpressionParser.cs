using System;
using System.Linq.Expressions;
using System.Numerics;

namespace ExpressionToGLSL
{
    public static class ExpressionParser
    {
        public static string ConvertExpressionToGlsl(string expression, bool useC)
        {
            var tokenizer = new Tokenizer(expression, useC);
            var tokens = tokenizer.Tokenize();
            var parser = new Parser(tokens);
            var ast = parser.ParseExpression();
            return ast.ToGlsl();
        }

        public static Func<Complex, Complex, Complex> CompileToFunc(string expression, bool useC = false)
        {
            var tokenizer = new Tokenizer(expression, useC);
            var tokens = tokenizer.Tokenize();
            var parser = new Parser(tokens);
            var ast = parser.ParseExpression();

            var zParam = Expression.Parameter(typeof(Complex), "z");
            var cParam = Expression.Parameter(typeof(Complex), "c");
            var body = ast.ToExpression(zParam, cParam);

            return Expression.Lambda<Func<Complex, Complex, Complex>>(body, zParam, cParam).Compile();
        }
    }
}
