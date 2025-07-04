using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Linq.Expressions;
using System.Reflection;
using Meta.Numerics.Functions;
using MNComplex = Meta.Numerics.Complex;


namespace ExpressionToGLSL
{

    public static class ExpressionParser
    {

        static readonly ConstructorInfo ComplexCtor =
    typeof(Complex).GetConstructor(new[] { typeof(double), typeof(double) });
        static readonly MethodInfo PowCC = typeof(Complex).GetMethod(
            nameof(Complex.Pow),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Complex), typeof(Complex) },
            modifiers: null)!;
        static readonly MethodInfo GammaMN =
typeof(AdvancedComplexMath).GetMethod(
    nameof(AdvancedComplexMath.Gamma),
    BindingFlags.Public | BindingFlags.Static,
    binder: null,
    types: new[] { typeof(MNComplex) },
    modifiers: null)!;
        static Expression ToMN(Expression e) =>
        Expression.Convert(e, typeof(MNComplex));

        // Meta.Numerics.Complex    ->  System.Numerics.Complex
        static Expression ToSys(Expression e) =>
            Expression.Convert(e, typeof(Complex));
        static readonly MethodInfo MathAbs =
typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(double) });

        /// <summary>
        /// Convert a user-typed expression like "(1/z)^18 + z^3"
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// into a GLSL snippet such as:
        /// complexAdd(
        ///    complex_pow_complex(complexDivide(vec2(1.0, 0.0), z), vec2(18.0, 0.0)),
        ///    complex_pow_complex(z, vec2(3.0, 0.0))
        /// )
        /// 
        /// Also supports imaginary parts via 'i' and negative numbers like "-3.14" or "-2.5i".
        public static string ConvertExpressionToGlsl(string expression, bool useC)
        {
            // 1) Tokenize the input
            List<Token> tokens = Tokenize(expression, useC);

            // 2) Parse into an abstract syntax tree (AST)
            Parser parser = new Parser(tokens);
            AstNode ast = parser.ParseExpression();

            // 3) Convert the AST to GLSL code
            return ast.ToGlsl();
        }

        public static Func<Complex, Complex, Complex> CompileToFunc(
        string expression, bool useC = false)
        {
            var tokens = Tokenize(expression, useC);
            var ast = new Parser(tokens).ParseExpression();
            var zParam = Expression.Parameter(typeof(Complex), "z");
            var cParam = Expression.Parameter(typeof(Complex), "c");
            Expression body = ast.ToExpression(zParam, cParam);
            return Expression.Lambda<Func<Complex, Complex, Complex>>(body, zParam, cParam)
                             .Compile();
        }

        #region Tokenization

        private enum TokenType
        {
            Number,     // e.g. "123", "3.14", or might be something with 'i' like "2.5i"
            Z,          // 'z' or 'Z'
            Plus,       // '+'
            Minus,      // '-'
            Asterisk,   // '*'
            Slash,      // '/'
            Caret,      // '^'
            LParen,     // '('
            RParen, // ')'
            Identifier,
            EndIdentifier,
            EOF
        }

        private class Token
        {
            public TokenType Type;
            public string Text;
            public bool IsImag;  // Whether this token is purely imaginary (e.g. "2.5i")

            public Token(TokenType type, string text, bool isImag = false)
            {
                Type = type;
                Text = text;
                IsImag = isImag;
            }

            public override string ToString() => $"{Type}('{Text}'){(IsImag ? "[i]" : "")}";
        }


        public static bool checkNextLetter(string input, int id)
        {
            if (id < input.Length - 1)
            {
                if (char.IsLetter(input[id + 1]))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Convert a raw string into a list of tokens.
        /// For example "(1/z)^18 + z^3" => LParen, Number("1"), Slash, Z, RParen, Caret, Number("18"), Plus, Z, Caret, Number("3"), EOF
        /// Also supports something like "-3.14i" or "1.2i" or just "i" for 1i.
        /// </summary>
        private static List<Token> Tokenize(string input, bool useC)
        {
            List<Token> tokens = new List<Token>();
            int pos = 0;

            while (pos < input.Length)
            {
                char c = input[pos];
                if (char.IsWhiteSpace(c))
                {
                    // Ignore whitespace
                    pos++;
                }
                else if (c == '(')
                {
                    tokens.Add(new Token(TokenType.LParen, "("));
                    pos++;
                }
                else if (c == ')')
                {
                    tokens.Add(new Token(TokenType.RParen, ")"));
                    if (pos < input.Length - 1)
                    {
                        if (input[pos + 1] == '(')
                        {
                            tokens.Add(new Token(TokenType.Asterisk, "*"));
                        }
                    }
                    pos++;
                }
                else if (c == '!')
                {
                    tokens.Add(new Token(TokenType.EndIdentifier, "!"));
                    pos++;
                }
                else if (c == '+')
                {
                    tokens.Add(new Token(TokenType.Plus, "+"));
                    pos++;
                }
                else if (c == '-')
                {
                    // Check for a "negative number" or just a minus operator
                    // We'll handle that more gracefully in the parser (unary minus or subtraction).
                    tokens.Add(new Token(TokenType.Minus, "-"));
                    pos++;
                }
                else if (c == '*')
                {
                    tokens.Add(new Token(TokenType.Asterisk, "*"));
                    pos++;
                }
                else if (c == '/')
                {
                    tokens.Add(new Token(TokenType.Slash, "/"));
                    pos++;
                }
                else if (c == '^')
                {
                    tokens.Add(new Token(TokenType.Caret, "^"));
                    pos++;
                }
                else if ((c == 'z' || c == 'Z') && checkNextLetter(input, pos))
                {
                    tokens.Add(new Token(TokenType.Z, "z"));
                    pos++;
                }
                else if ((c == 'c' || c == 'C') && useC && checkNextLetter(input, pos))
                {
                    tokens.Add(new Token(TokenType.Z, "c"));
                    pos++;
                }
                else if ((c == 'p' || c == 'P') && useC && checkNextLetter(input, pos))
                {
                    tokens.Add(new Token(TokenType.Z, "p"));
                    pos++;
                }
                else if (char.IsDigit(c) || c == '.' || (c == 'i' && checkNextLetter(input, pos)))
                {
                    // Parse a numeric literal, possibly with an 'i' for imaginary.
                    ParseNumberOrImag(input, ref pos, tokens);
                }
                else if (char.IsLetter(c))
                {
                    if (input.Substring(pos).ToLower().StartsWith("ln"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "ln"));
                        pos += 2;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("sin"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "sin"));
                        pos += 3;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("cos"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "cos"));
                        pos += 3;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("tan"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "tan"));
                        pos += 3;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("e"))
                    {
                        tokens.Add(new Token(TokenType.Number, Math.E.ToString()));
                        pos += 1;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("pi"))
                    {
                        tokens.Add(new Token(TokenType.Number, Math.PI.ToString()));
                        pos += 2;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("acos"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "acos"));
                        pos += 4;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("abs"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "abs"));
                        pos += 3;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("bar"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "bar"));
                        pos += 3;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("real"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "real"));
                        pos += 4;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("imag"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "imag"));
                        pos += 4;
                    }
                    else
                    {
                        throw new Exception($"unknown function at {pos}");
                    }
                }
                else
                {
                    throw new Exception($"Unexpected character '{c}' at position {pos}");
                }
            }

            tokens.Add(new Token(TokenType.EOF, ""));
            return tokens;
        }

        /// <summary>
        /// Handle numeric or imaginary tokens, e.g. "3.14", "2.5i", ".5i", "i" alone, etc.
        /// </summary>
        private static void ParseNumberOrImag(string input, ref int pos, List<Token> tokens)
        {
            int startPos = pos;
            bool hasDot = false;
            bool hasDigits = false;
            bool hasI = false;

            // If the token is literally just 'i', treat it like "1i".
            if (input[pos] == 'i')
            {
                // e.g. "i" => "1.0" with imaginary = true
                tokens.Add(new Token(TokenType.Number, "1.0", true));
                pos++;
                return;
            }

            // Otherwise, gather digits, possibly one dot, and an optional trailing 'i'.
            while (pos < input.Length)
            {
                char nc = input[pos];
                if (char.IsDigit(nc))
                {
                    hasDigits = true;
                    pos++;
                }
                else if (nc == '.' && !hasDot)
                {
                    hasDot = true;
                    pos++;
                }
                else if (nc == 'i')
                {
                    hasI = true;
                    pos++;
                    break; // consume the 'i' and then stop
                }
                else
                {
                    // done
                    break;
                }
            }

            if (!hasDigits && !hasI && input[startPos] != '.')
            {
                throw new Exception($"Invalid numeric/imag token near '{input.Substring(startPos)}'");
            }

            // e.g. "3.14" or "2.5" or ".75"
            string numberText = input.Substring(startPos, pos - startPos - (hasI ? 1 : 0));
            if (numberText.EndsWith("."))
            {
                numberText += "0";  // e.g. "3." => "3.0"
            }
            tokens.Add(new Token(TokenType.Number, numberText, hasI));
        }

        #endregion

        #region Parsing
        private class Parser
        {
            private readonly List<Token> _tokens;
            private int _pos;

            public Parser(List<Token> tokens)
            {
                _tokens = tokens;
                _pos = 0;
            }

            private Token Current => _tokens[_pos];

            private Token Eat(TokenType type)
            {
                if (Current.Type == type)
                {
                    Token t = Current;
                    _pos++;
                    return t;
                }
                else
                {
                    throw new Exception($"Expected token {type}, got {Current.Type} at position {_pos}");
                }
            }

            private bool Match(params TokenType[] types)
            {
                foreach (var t in types)
                {
                    if (Current.Type == t) return true;
                }
                return false;
            }

            public AstNode ParseExpression()
            {
                AstNode left = ParseTerm();

                while (Match(TokenType.Plus, TokenType.Minus))
                {
                    Token op = Current;
                    _pos++;
                    AstNode right = ParseTerm();
                    if (op.Type == TokenType.Plus)
                        left = new AstBinOp(left, right, BinOpType.Add);
                    else
                        left = new AstBinOp(left, right, BinOpType.Subtract);
                }

                return left;
            }

            private AstNode ParseTerm()
            {
                AstNode left = ParseFactor();

                while (Match(TokenType.Asterisk, TokenType.Slash))
                {
                    Token op = Current;
                    _pos++;
                    AstNode right = ParseFactor();
                    if (op.Type == TokenType.Asterisk)
                        left = new AstBinOp(left, right, BinOpType.Multiply);
                    else
                        left = new AstBinOp(left, right, BinOpType.Divide);
                }

                return left;
            }
            private AstNode ParseFactor()
            {
                if (Match(TokenType.Minus))
                {
                    Eat(TokenType.Minus);
                    AstNode child = ParseFactor();
                    return new AstUnaryOp(child, true);
                }
                AstNode baseNode = ParseBase();
                while (Match(TokenType.Caret))
                {
                    Eat(TokenType.Caret);
                    // Parse the exponent as another Factor
                    AstNode exponent = ParseFactor();
                    baseNode = new AstBinOp(baseNode, exponent, BinOpType.Power);
                }

                return baseNode;
            }


            /// <summary>
            /// Allow a leading unary minus (or plus) before a base,
            /// e.g. "-z", "+(3+2)", "-(1 + i2.5)", etc.
            /// </summary>
            private AstNode ParseUnary()
            {
                // If there's a leading minus, parse one
                // If there's a leading plus, skip it
                if (Match(TokenType.Minus))
                {
                    // This is a unary minus
                    Eat(TokenType.Minus);
                    AstNode child = ParseBase();
                    return new AstUnaryOp(child, true);
                }
                else if (Match(TokenType.Plus))
                {
                    // If a leading plus, just skip it
                    Eat(TokenType.Plus);
                    AstNode child = ParseBase();
                    return child;
                }

                return ParseBase();
            }

            /// <summary>
            /// Parse a Base -> Number | z | "(" Expression ")"
            /// </summary>
            private AstNode ParseBase()
            {
                if (Current.Type == TokenType.Identifier)
                {
                    Token funcName = Eat(TokenType.Identifier); // e.g. "ln"
                    Eat(TokenType.LParen); // consume '('
                    AstNode arg = ParseExpression(); // the function argument
                    Eat(TokenType.RParen); // consume ')'
                    return new AstFunctionCall(funcName.Text, arg);
                }
                else if (Match(TokenType.LParen))
                {
                    Eat(TokenType.LParen);
                    AstNode expr = ParseExpression();
                    Eat(TokenType.RParen);
                    if (Current.Type == TokenType.EndIdentifier)
                    {
                        Token funcName = Eat(TokenType.EndIdentifier);
                        return new AstFunctionCall(funcName.Text, expr);
                    }
                    return expr;
                }
                else if (Match(TokenType.Number))
                {
                    Token t = Eat(TokenType.Number);
                    return new AstNumber(t.Text, t.IsImag);
                }
                else if (Match(TokenType.Z))
                {
                    Token t = Eat(TokenType.Z);
                    return new AstVariableZ(t.Text);
                }
                else
                {
                    throw new Exception($"Unexpected token {Current.Type} at position {_pos} when expecting a base element");
                }
            }
        }
        private class AstFunctionCall : AstNode
        {
            public string FunctionName { get; }
            public AstNode Argument { get; }

            public AstFunctionCall(string functionName, AstNode argument)
            {
                FunctionName = functionName;
                Argument = argument;
            }

            public override string ToGlsl()
            {
                // Convert the argument to GLSL
                string argCode = Argument.ToGlsl();
                switch (FunctionName.ToLower())
                {
                    case "ln":
                        return $"complexLn({argCode})";
                    case "sin":
                        return $"complexSin({argCode})";
                    case "cos":
                        return $"complexCos({argCode})";
                    case "tan":
                        return $"complexTan({argCode})";
                    case "!":
                        return $"complexGamma({argCode})";
                    case "acos":
                        return $"complexAcos({argCode})";
                    case "abs":
                        return $"complexAbs({argCode})";
                    case "bar":
                        return $"complexConjugate({argCode})";
                    case "real":
                        return $"real({argCode})";
                    case "imag":
                        return $"imag({argCode})";
                    default:
                        throw new Exception($"Unknown function {FunctionName}");
                }
            }

            public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
            {
                var arg = Argument.ToExpression(z, c);

                return FunctionName.ToLowerInvariant() switch
                {
                    "!" => ToSys(Expression.Call(GammaMN, ToMN(arg))),
                    "ln" => Call(nameof(Complex.Log)),
                    "sin" => Call(nameof(Complex.Sin)),
                    "cos" => Call(nameof(Complex.Cos)),
                    "tan" => Call(nameof(Complex.Tan)),
                    "abs" => absCC(arg),
                    "bar" => Call(nameof(Complex.Conjugate)),
                    "real" => Expression.New(typeof(Complex)
                               .GetConstructor(new[] { typeof(double), typeof(double) }),
                               Expression.PropertyOrField(arg, "Real"),
                               Expression.Constant(0.0)),
                    "imag" => Expression.New(typeof(Complex)
                               .GetConstructor(new[] { typeof(double), typeof(double) }),
                               Expression.PropertyOrField(arg, "Imaginary"),
                               Expression.Constant(0.0)),
                    _ => throw new NotSupportedException($"func {FunctionName}")
                };

                Expression absCC(Expression arg)
                {
                    var real = Expression.Property(arg, nameof(Complex.Real));
                    var imag = Expression.Property(arg, nameof(Complex.Imaginary));
                    var absReal = Expression.Call(MathAbs, real);
                    var absImag = Expression.Call(MathAbs, imag);

                    // new Complex(|Re|, |Im|)
                    return Expression.New(ComplexCtor, absReal, absImag);
                }


                MethodCallExpression Call(string method)
                    => Expression.Call(typeof(Complex).GetMethod(method, new[] { typeof(Complex) })!, arg);
            }

        }


        #endregion

        #region AST
        private abstract class AstNode
        {
            public abstract string ToGlsl();
            public abstract Expression ToExpression(ParameterExpression z, ParameterExpression c);
        }
        private class AstUnaryOp : AstNode
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
                // If it’s a negation, multiply the child by -1.0
                if (_isNegative)
                {
                    return $"complexMult(vec2(-1.0, 0.0), {_child.ToGlsl()})";
                }
                return _child.ToGlsl();
            }

            public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
            {
                var child = _child.ToExpression(z, c);
                return _isNegative
                    ? Expression.Multiply(
                          Expression.Constant(new Complex(-1, 0)),   // keep types consistent
                          child)
                    : child;
            }


        }
        private class AstNumber : AstNode
        {
            public double Value;
            public bool IsImag;

            public AstNumber(string text, bool isImag)
            {
                IsImag = isImag;
                Value = double.Parse(
                    text.StartsWith(".") ? "0" + text : text,
                    CultureInfo.InvariantCulture
                );
            }

            public override string ToGlsl()
            {
                if (IsImag)
                {
                    // e.g. "2.5i" => vec2(0.0, 2.5)
                    return $"vec2(0.0, {Value.ToString("G", CultureInfo.InvariantCulture)})";
                }
                else
                {
                    // e.g. "3.14" => vec2(3.14, 0.0)
                    return $"vec2({Value.ToString("G", CultureInfo.InvariantCulture)}, 0.0)";
                }
            }
            public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
            {
                Complex value;
                if (IsImag)
                {
                    // e.g. "2.5i" => vec2(0.0, 2.5)
                    value = new Complex(0, Value);
                }
                else
                {
                    // e.g. "3.14" => vec2(3.14, 0.0)
                    value = new Complex(Value, 0);
                }
                return Expression.Constant(value, typeof(Complex));
            }
        }

        /// <summary>
        /// The variable 'z', treated as a complex vec2 in the shader.
        /// </summary>
        private class AstVariableZ : AstNode
        {
            string character = "z";
            public AstVariableZ(string character)
            {
                this.character = character;
            }
            public AstVariableZ() { }
            public override string ToGlsl()
            {
                return character;
            }
            public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
                => character switch
                {
                    "z" => z,
                    "c" => c,
                    _ => throw new NotSupportedException($"unknown var {character}")
                };
        }

        private enum BinOpType { Add, Subtract, Multiply, Divide, Power }
        private class AstBinOp : AstNode
        {
            private readonly AstNode _left;
            private readonly AstNode _right;
            private readonly BinOpType _op;

            public AstBinOp(AstNode left, AstNode right, BinOpType op)
            {
                _left = left;
                _right = right;
                _op = op;
            }
            public override Expression ToExpression(ParameterExpression z, ParameterExpression c)
            {
                var L = _left.ToExpression(z, c);
                var R = _right.ToExpression(z, c);
                return _op switch
                {
                    BinOpType.Add => Expression.Add(L, R),
                    BinOpType.Subtract => Expression.Subtract(L, R),
                    BinOpType.Multiply => Expression.Multiply(L, R),
                    BinOpType.Divide => Expression.Divide(L, R),
                    BinOpType.Power => Expression.Call(PowCC, L, R),
                    _ => throw new ArgumentOutOfRangeException()
                };


            }

            public override string ToGlsl()
            {
                string leftCode = _left.ToGlsl();
                string rightCode = _right.ToGlsl();

                switch (_op)
                {
                    case BinOpType.Add:
                        return $"complexAdd({leftCode}, {rightCode})";
                    case BinOpType.Subtract:
                        return $"complexSub({leftCode}, {rightCode})";
                    case BinOpType.Multiply:
                        return $"complexMult({leftCode}, {rightCode})";
                    case BinOpType.Divide:
                        return $"complexDivide({leftCode}, {rightCode})";
                    case BinOpType.Power:
                        return $"complex_pow_complex({leftCode}, {rightCode})";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion
    }
    public static class ComplexDiff
    {
        public static Complex DfDz(Func<Complex, Complex, Complex> f,
                           Complex z, Complex c, double h = 1e-8)
        {
            var hC = new Complex(h, 0);
            return (f(z + hC, c) - f(z - hC, c)) / (2 * h);
        }

        // central finite difference w.r.t. the *second* argument (c)
        public static Complex DfDc(Func<Complex, Complex, Complex> f,
                                   Complex z, Complex c, double h = 1e-8)
        {
            var hC = new Complex(h, 0);
            return (f(z, c + hC) - f(z, c - hC)) / (2 * h);
        }
    }
}
