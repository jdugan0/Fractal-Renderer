using System;
using System.Collections.Generic;

namespace ExpressionToGLSL
{
    internal class Tokenizer
    {
        private readonly string _input;
        private readonly bool _useC;
        private int _pos;

        public Tokenizer(string input, bool useC)
        {
            _input = input;
            _useC = useC;
            _pos = 0;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (_pos < _input.Length)
            {
                char c = _input[_pos];

                if (char.IsWhiteSpace(c))
                {
                    _pos++;
                }
                else if (TryTokenizeOperator(c, tokens) ||
                         TryTokenizeVariable(c, tokens) ||
                         TryTokenizeStandaloneI(c, tokens) ||
                         TryTokenizeFunction(tokens) ||
                         TryTokenizeNumeric(c, tokens))
                {
                    continue;
                }
                else
                {
                    throw new Exception($"Unexpected character '{c}' at position {_pos}");
                }
            }

            tokens.Add(new Token(TokenType.EOF, ""));
            return tokens;
        }

        private bool TryTokenizeOperator(char c, List<Token> tokens)
        {
            TokenType? type = c switch
            {
                '(' => TokenType.LParen,
                ')' => TokenType.RParen,
                '!' => TokenType.EndIdentifier,
                '+' => TokenType.Plus,
                '-' => TokenType.Minus,
                '*' => TokenType.Asterisk,
                '/' => TokenType.Slash,
                '^' => TokenType.Caret,
                _ => null
            };

            if (!type.HasValue) return false;

            tokens.Add(new Token(type.Value, c.ToString()));
            _pos++;

            if (c == ')' && _pos < _input.Length && _input[_pos] == '(')
            {
                tokens.Add(new Token(TokenType.Asterisk, "*"));
            }

            return true;
        }

        private bool TryTokenizeVariable(char c, List<Token> tokens)
        {
            if (!IsNextLetterSeparate()) return false;

            char lower = char.ToLower(c);
            if (lower == 'z' || (lower == 'c' && _useC) || (lower == 'p' && _useC))
            {
                tokens.Add(new Token(TokenType.Z, lower.ToString()));
                _pos++;
                return true;
            }

            return false;
        }

        private bool TryTokenizeStandaloneI(char c, List<Token> tokens)
        {
            if (c == 'i' && IsNextLetterSeparate())
            {
                tokens.Add(new Token(TokenType.Number, "1.0", true));
                _pos++;
                return true;
            }
            return false;
        }

        private bool TryTokenizeNumeric(char c, List<Token> tokens)
        {
            if (!char.IsDigit(c) && c != '.') return false;

            ParseNumber(tokens);
            return true;
        }

        private void ParseNumber(List<Token> tokens)
        {
            int start = _pos;
            bool hasDot = false;
            bool hasDigits = false;
            bool hasI = false;

            while (_pos < _input.Length)
            {
                char c = _input[_pos];

                if (char.IsDigit(c))
                {
                    hasDigits = true;
                    _pos++;
                }
                else if (c == '.' && !hasDot)
                {
                    hasDot = true;
                    _pos++;
                }
                else if (c == 'i')
                {
                    hasI = true;
                    _pos++;
                    break;
                }
                else
                {
                    break;
                }
            }

            if (!hasDigits && _input[start] != '.')
            {
                throw new Exception($"Invalid numeric token near '{_input.Substring(start)}'");
            }

            string numText = _input.Substring(start, _pos - start - (hasI ? 1 : 0));
            if (numText.EndsWith(".")) numText += "0";

            tokens.Add(new Token(TokenType.Number, numText, hasI));
        }

        private bool TryTokenizeFunction(List<Token> tokens)
        {
            if (!char.IsLetter(_input[_pos])) return false;

            var functionMap = new Dictionary<string, (string token, int length)>
            {
                { "acos", ("acos", 4) },
                { "abs", ("abs", 3) },
                { "bar", ("bar", 3) },
                { "real", ("real", 4) },
                { "imag", ("imag", 4) },
                { "sin", ("sin", 3) },
                { "cos", ("cos", 3) },
                { "tan", ("tan", 3) },
                { "ln", ("ln", 2) },
                { "pi", (Math.PI.ToString(), 2) },
                { "e", (Math.E.ToString(), 1) }
            };

            foreach (var (key, (token, length)) in functionMap)
            {
                if (_input.Substring(_pos).StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    var tokenType = key == "pi" || key == "e" ? TokenType.Number : TokenType.Identifier;
                    tokens.Add(new Token(tokenType, token));
                    _pos += length;
                    return true;
                }
            }

            throw new Exception($"Unknown function at position {_pos}");
        }

        private bool IsNextLetterSeparate()
        {
            return _pos >= _input.Length - 1 || !char.IsLetter(_input[_pos + 1]);
        }
    }
}