using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Meta.Numerics.Functions;
using MNComplex = Meta.Numerics.Complex;

namespace ExpressionToGLSL
{
    internal abstract class AstNode
    {
        public abstract string ToGlsl();
        public abstract Expression ToExpression(ParameterExpression z, ParameterExpression c);
    }

    internal enum BinOpType { Add, Subtract, Multiply, Divide, Power }

    internal class AstBinOp : AstNode
    {
        private static readonly MethodInfo PowCC = typeof(Complex).GetMethod(
            nameof(Complex.Pow),
            BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(Complex), typeof(Complex) }, null)!;

        private readonly AstNode _left;
        private readonly AstNode _right;
        private readonly BinOpType _op;

        public AstBinOp(AstNode left, AstNode right, BinOpType op)
        {
            _left = left;
            _right = right;
            _op = op;
        }

        public override string ToGlsl()
        {
            string left = _left.ToGlsl();
            string right = _right.ToGlsl();

            return _op switch
            {
                BinOpType.Add => $"complexAdd({left}, {right})",
                BinOpType.Subtract => $"complexSub({left}, {right})",
                BinOpType.Multiply => $"complexMult({left}, {right})",
                BinOpType.Divide => $"complexDivide({left}, {right})",
                BinOpType.Power => $"complex_pow_complex({left}, {right})",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
        {
            var left = _left.ToExpression(z, c);
            var right = _right.ToExpression(z, c);

            return _op switch
            {
                BinOpType.Add => Expression.Add(left, right),
                BinOpType.Subtract => Expression.Subtract(left, right),
                BinOpType.Multiply => Expression.Multiply(left, right),
                BinOpType.Divide => Expression.Divide(left, right),
                BinOpType.Power => Expression.Call(PowCC, left, right),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    internal class AstUnaryOp : AstNode
    {
        private readonly AstNode _child;
        private readonly bool _isNegative;

        public AstUnaryOp(AstNode child, bool isNegative)
        {
            _child = child;
            _isNegative = isNegative;
        }

        public override string ToGlsl()
        {
            return _isNegative
                ? $"complexMult(vec4(splitFloat(-1.0), splitFloat(0.0)), {_child.ToGlsl()})"
                : _child.ToGlsl();
        }

        public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
        {
            var child = _child.ToExpression(z, c);
            return _isNegative
                ? Expression.Multiply(Expression.Constant(new Complex(-1, 0)), child)
                : child;
        }
    }

    internal class AstNumber : AstNode
    {
        private readonly double _value;
        private readonly bool _isImaginary;

        public AstNumber(string text, bool isImaginary)
        {
            _isImaginary = isImaginary;
            _value = double.Parse(
                text.StartsWith(".") ? "0" + text : text,
                CultureInfo.InvariantCulture);
        }

        public override string ToGlsl()
        {
            string val = _value.ToString("G", CultureInfo.InvariantCulture);
            return _isImaginary
                ? $"vec4(splitFloat(0.0), splitFloat({val}))"
                : $"vec4(splitFloat({val}), splitFloat(0.0))";
        }

        public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
        {
            var value = _isImaginary ? new Complex(0, _value) : new Complex(_value, 0);
            return Expression.Constant(value, typeof(Complex));
        }
    }

    internal class AstVariable : AstNode
    {
        private readonly string _name;

        public AstVariable(string name)
        {
            _name = name;
        }

        public override string ToGlsl() => _name;

        public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
        {
            return _name switch
            {
                "z" => z,
                "c" => c,
                _ => throw new NotSupportedException($"Unknown variable {_name}")
            };
        }
    }

    internal class AstFunctionCall : AstNode
    {
        private static readonly MethodInfo GammaMN = typeof(AdvancedComplexMath).GetMethod(
            nameof(AdvancedComplexMath.Gamma),
            BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(MNComplex) }, null)!;

        private static readonly MethodInfo MathAbs =
            typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(double) });

        private static readonly ConstructorInfo ComplexCtor =
            typeof(Complex).GetConstructor(new[] { typeof(double), typeof(double) });

        private readonly string _functionName;
        private readonly AstNode _argument;

        public AstFunctionCall(string functionName, AstNode argument)
        {
            _functionName = functionName;
            _argument = argument;
        }

        public override string ToGlsl()
        {
            string arg = _argument.ToGlsl();
            return _functionName.ToLower() switch
            {
                "ln" => $"complexLn({arg})",
                "sin" => $"complexSin({arg})",
                "cos" => $"complexCos({arg})",
                "tan" => $"complexTan({arg})",
                "!" => $"complexGamma({arg})",
                "acos" => $"complexAcos({arg})",
                "abs" => $"complexAbs({arg})",
                "bar" => $"complexConjugate({arg})",
                "real" => $"real({arg})",
                "imag" => $"imag({arg})",
                _ => throw new Exception($"Unknown function {_functionName}")
            };
        }

        public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
        {
            var arg = _argument.ToExpression(z, c);

            return _functionName.ToLowerInvariant() switch
            {
                "!" => ToSys(Expression.Call(GammaMN, ToMN(arg))),
                "ln" => CallComplexMethod(nameof(Complex.Log), arg),
                "sin" => CallComplexMethod(nameof(Complex.Sin), arg),
                "cos" => CallComplexMethod(nameof(Complex.Cos), arg),
                "tan" => CallComplexMethod(nameof(Complex.Tan), arg),
                "abs" => CreateComplexAbs(arg),
                "bar" => CallComplexMethod(nameof(Complex.Conjugate), arg),
                "real" => Expression.New(ComplexCtor,
                    Expression.PropertyOrField(arg, "Real"),
                    Expression.Constant(0.0)),
                "imag" => Expression.New(ComplexCtor,
                    Expression.PropertyOrField(arg, "Imaginary"),
                    Expression.Constant(0.0)),
                _ => throw new NotSupportedException($"Function {_functionName}")
            };
        }

        private static Expression ToMN(Expression e) => Expression.Convert(e, typeof(MNComplex));
        private static Expression ToSys(Expression e) => Expression.Convert(e, typeof(Complex));

        private static MethodCallExpression CallComplexMethod(string methodName, Expression arg)
        {
            return Expression.Call(
                typeof(Complex).GetMethod(methodName, new[] { typeof(Complex) })!,
                arg);
        }

        private static Expression CreateComplexAbs(Expression arg)
        {
            var real = Expression.Property(arg, nameof(Complex.Real));
            var imag = Expression.Property(arg, nameof(Complex.Imaginary));
            var absReal = Expression.Call(MathAbs, real);
            var absImag = Expression.Call(MathAbs, imag);
            return Expression.New(ComplexCtor, absReal, absImag);
        }
    }
}