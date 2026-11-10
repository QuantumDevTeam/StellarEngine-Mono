namespace Stellar.Sdk.Analyzers.Internal;

/// <summary>
/// Checks that a text is well-formed JSON, and says where it is not. Nothing is built: the analyzers only need to
/// point at the mistake, and they cannot rely on a JSON library inside the compiler.
/// </summary>
internal sealed class JsonSyntaxChecker
{
    private readonly string _text;
    private int _position;

    private JsonSyntaxChecker(string text)
    {
        _text = text;
    }

    /// <summary>
    /// Checks a text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="position">Where the first mistake is.</param>
    /// <param name="message">What it is.</param>
    /// <returns><c>true</c> if the text is well-formed.</returns>
    public static bool TryCheck(string text, out int position, out string message)
    {
        var checker = new JsonSyntaxChecker(text);

        try
        {
            checker.SkipWhitespace();
            checker.Value();
            checker.SkipWhitespace();

            if (checker._position != text.Length)
                throw new JsonSyntaxError(checker._position, "Unexpected text after the end of the document");

            position = 0;
            message = string.Empty;
            return true;
        }
        catch (JsonSyntaxError error)
        {
            position = error.Position;
            message = error.Message;
            return false;
        }
    }

    private void Value()
    {
        if (_position >= _text.Length)
            throw new JsonSyntaxError(_position, "Unexpected end of the document");

        switch (_text[_position])
        {
            case '{': Object(); break;
            case '[': Array(); break;
            case '"': String(); break;
            case 't': Literal("true"); break;
            case 'f': Literal("false"); break;
            case 'n': Literal("null"); break;
            default: Number(); break;
        }
    }

    private void Object()
    {
        _position++;
        SkipWhitespace();

        if (Peek() == '}')
        {
            _position++;
            return;
        }

        while (true)
        {
            SkipWhitespace();
            if (Peek() != '"')
                throw new JsonSyntaxError(_position, "A property name in double quotes is expected");

            String();
            SkipWhitespace();
            Expect(':');
            SkipWhitespace();
            Value();
            SkipWhitespace();

            if (Peek() == ',')
            {
                _position++;
                continue;
            }

            Expect('}');
            return;
        }
    }

    private void Array()
    {
        _position++;
        SkipWhitespace();

        if (Peek() == ']')
        {
            _position++;
            return;
        }

        while (true)
        {
            SkipWhitespace();
            Value();
            SkipWhitespace();

            if (Peek() == ',')
            {
                _position++;
                continue;
            }

            Expect(']');
            return;
        }
    }

    private void String()
    {
        var start = _position++;

        while (_position < _text.Length)
        {
            var c = _text[_position++];
            if (c == '\\')
                _position++;
            else if (c == '"')
                return;
            else if (c == '\n')
                break;
        }

        throw new JsonSyntaxError(start, "The string is not closed");
    }

    private void Literal(string literal)
    {
        if (string.CompareOrdinal(_text, _position, literal, 0, literal.Length) != 0)
            throw new JsonSyntaxError(_position, $"'{literal}' is expected");

        _position += literal.Length;
    }

    private void Number()
    {
        var start = _position;

        while (_position < _text.Length && "+-0123456789.eE".IndexOf(_text[_position]) >= 0)
            _position++;

        if (_position == start)
            throw new JsonSyntaxError(_position, $"Unexpected character '{_text[_position]}'");
    }

    private void Expect(char expected)
    {
        if (Peek() != expected)
            throw new JsonSyntaxError(_position, $"'{expected}' is expected");

        _position++;
    }

    private char Peek()
    {
        return _position < _text.Length ? _text[_position] : '\0';
    }

    private void SkipWhitespace()
    {
        while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            _position++;
    }

    private sealed class JsonSyntaxError(int position, string message) : System.Exception(message)
    {
        public int Position { get; } = position;
    }
}
