void Test(string text)
{
    var parser = new Parser();
    try
    {
        parser.Parse(text);
        Console.WriteLine($"\"{text}\"\t OK");
    }
    catch (ParserException e)
    {
        Console.WriteLine($"\"{text}\"\t {e.Message}\n");
    }
}


Test("1+2+3+4");
Test("1*2*3*4");
Test("1-2-3-4");
Test("1/2/3/4");
Test("1*2+3*4");
Test("1+2*3+4");
Test("(1+2)*(3+4)");
Test("1+(2*3)*(4+5)");
Test("1+(2*3)/4+5");
Test("5/(4+3)/2");
Test("1 + 2.5");
Test("125");
Test("-1");
Test("-1+(-2)");
Test("-1+(-2.0)");

Test("   1*2,5");
Test("   1*2.5e2");
Test("M1 + 2.5");
Test("1 + 2&5");
Test("1 * 2.5.6");
Test("1 ** 2.5");
Test("*1 / 2.5");


class Parser
{
    private Token _crtToken;
    private string _text = "";
    private int _idx;

    private void Expression()
    {
        Term();
        Expression1();
    }

    private void Expression1()
    {
        switch (_crtToken.Type)
        {
            case TokenType.Plus:
                GetNextToken();
                Term();
                Expression1();
                break;
            case TokenType.Minus:
                GetNextToken();
                Term();
                Expression1();
                break;
        }
    }

    private void Term()
    {
        Factor();
        Term1();
    }

    private void Term1()
    {
        switch (_crtToken.Type)
        {
            case TokenType.Mul:
                GetNextToken();
                Factor();
                Term1();
                break;
            case TokenType.Div:
                GetNextToken();
                Factor();
                Term1();
                break;
        }
    }

    private void Factor()
    {
        switch (_crtToken.Type)
        {
            case TokenType.OpenParenthesis: 
                GetNextToken();
                Expression();
                Match(')');
                break;
            case TokenType.Minus:
                GetNextToken();
                Factor();
                break;
            case TokenType.Number:
                GetNextToken();
                break;
            default:
                string error = $"Unexpected token \"{_crtToken.Symbol}\" at position {_idx}";
                throw new ParserException(error, _idx);
        }
    }

    private void Match(char expected)
    {
        if (_text[_idx-1] == expected)
        {
            GetNextToken();
        }
        else
        {
            string error = $"Expected token \"{expected}\" at position {_idx}";
            throw new ParserException(error, _idx);
        }
    }
    private enum TokenType
    {
        Error,
        Plus,
        Minus,
        Mul,
        Div,
        EndOfText,
        OpenParenthesis,
        CloseParenthesis,
        Number
    }

    private struct Token(TokenType type = TokenType.Error, double value = 0, char symbol = '0')
    {
        public TokenType Type = type;
        public double Value = value;
        public char Symbol = symbol;
    }

    private void SkipWhitespaces()
    {
        while (_idx < _text.Length && char.IsWhiteSpace(_text[_idx]))
        {
            _idx++;
        }
    }

    private void GetNextToken()
    {
        SkipWhitespaces();
        _crtToken.Value = 0;
        _crtToken.Symbol = '0';
        if (_idx >= _text.Length)
        {
            _crtToken.Type = TokenType.EndOfText;
            return;
        }

        if (char.IsDigit(_text[_idx]))
        {
            _crtToken.Type = TokenType.Number;
            _crtToken.Value = GetNumber();
            return;
        }

        _crtToken.Type = TokenType.Error;

        switch (_text[_idx])
        {
            case '+':
                _crtToken.Type = TokenType.Plus;
                break;
            case '-':
                _crtToken.Type = TokenType.Minus;
                break;
            case '*':
                _crtToken.Type = TokenType.Mul;
                break;
            case '/':
                _crtToken.Type = TokenType.Div;
                break;
            case '(':
                _crtToken.Type = TokenType.OpenParenthesis;
                break;
            case ')':
                _crtToken.Type = TokenType.CloseParenthesis;
                break;
        }

        if (_crtToken.Type != TokenType.Error)
        {
            _crtToken.Symbol = _text[_idx];
            _idx++;
        }
        else
        {
            string error = $"Unexpected token \"{_text[_idx]}\" at position {_idx}";
            throw new ParserException(error, _idx);
        }
    }

    private double GetNumber()
    {
        SkipWhitespaces();
        int index = _idx;
        while (_idx < _text.Length && char.IsDigit(_text[_idx]))
        {
            _idx++;
        }

        if (_idx < _text.Length && _text[_idx] == '.')
        {
            _idx++;
        }

        while (_idx < _text.Length && char.IsDigit(_text[_idx]))
        {
            _idx++;
        }

        if (_idx - index == 0)
        {
            throw new ParserException("Number expected but not found!", _idx);
        }

        return double.Parse(_text.Substring(index, _idx - index));
    }

    public void Parse(string text)
    {
        _text = text;
        _idx = 0;
        GetNextToken();
        Expression();
    }

    
}
class ParserException : Exception
{
    private int _pos;

    public ParserException(string message, int pos)
    {
        Console.WriteLine(message);
        _pos = pos;
    }
}

