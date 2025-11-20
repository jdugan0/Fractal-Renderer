using System;
using System.Collections.Generic;

namespace ExpressionToGLSL
{
    internal class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _pos = 0;
        }

        private Token Current => _tokens[_pos];

        public AstNode ParseExpression()
        {
            AstNode left = ParseTerm();

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                var op = Current.Type == TokenType.Plus ? BinOpType.Add : BinOpType.Subtract;
                _pos++;
                AstNode right = ParseTerm();
                left = new AstBinOp(left, right, op);
            }

            return left;
        }

        private AstNode ParseTerm()
        {
            AstNode left = ParseFactor();

            while (Match(TokenType.Asterisk, TokenType.Slash))
            {
                var op = Current.Type == TokenType.Asterisk ? BinOpType.Multiply : BinOpType.Divide;
                _pos++;
                AstNode right = ParseFactor();
                left = new AstBinOp(left, right, op);
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
                AstNode exponent = ParseFactor();
                baseNode = new AstBinOp(baseNode, exponent, BinOpType.Power);
            }

            return baseNode;
        }

        private AstNode ParseBase()
        {
            if (Match(TokenType.Identifier))
            {
                string funcName = Eat(TokenType.Identifier).Text;
                Eat(TokenType.LParen);
                AstNode arg = ParseExpression();
                Eat(TokenType.RParen);
                return new AstFunctionCall(funcName, arg);
            }

            if (Match(TokenType.LParen))
            {
                Eat(TokenType.LParen);
                AstNode expr = ParseExpression();
                Eat(TokenType.RParen);

                if (Match(TokenType.EndIdentifier))
                {
                    string funcName = Eat(TokenType.EndIdentifier).Text;
                    return new AstFunctionCall(funcName, expr);
                }

                return expr;
            }

            if (Match(TokenType.Number))
            {
                Token t = Eat(TokenType.Number);
                return new AstNumber(t.Text, t.IsImaginary);
            }

            if (Match(TokenType.Z))
            {
                Token t = Eat(TokenType.Z);
                return new AstVariable(t.Text);
            }

            throw new Exception($"Unexpected token {Current.Type} at position {_pos}");
        }

        private Token Eat(TokenType type)
        {
            if (Current.Type != type)
            {
                throw new Exception($"Expected {type}, got {Current.Type} at position {_pos}");
            }

            Token t = Current;
            _pos++;
            return t;
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Current.Type == type) return true;
            }
            return false;
        }
    }
}
