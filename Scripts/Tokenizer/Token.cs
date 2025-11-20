namespace ExpressionToGLSL
{
    internal enum TokenType
    {
        Number,
        Z,
        Plus,
        Minus,
        Asterisk,
        Slash,
        Caret,
        LParen,
        RParen,
        Identifier,
        EndIdentifier,
        EOF
    }

    internal class Token
    {
        public TokenType Type { get; }
        public string Text { get; }
        public bool IsImaginary { get; }

        public Token(TokenType type, string text, bool isImaginary = false)
        {
            Type = type;
            Text = text;
            IsImaginary = isImaginary;
        }

        public override string ToString() => 
            $"{Type}('{Text}'){(IsImaginary ? "[i]" : "")}";
    }
}
