using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using Microsoft.VisualBasic.CompilerServices;

void Test(string text)
{
    var parser = new Parser();
    try
    {
        var ast = parser.Parse(text);
        try
        {
            var eval = new Evaluator();
            double val = eval.Evaluate(ast);

            Console.WriteLine($"{text} = {val}");
        }
        catch(EvaluatorException e)
        {
            Console.WriteLine($"{text}\t {e.Msg}");
        }

    }
    catch (ParserException e)
    {
        Console.WriteLine($"\"{text}\"\t {e.Msg}\n");
        Console.WriteLine(e.Ptr.PadLeft(e.Pos + e.Ptr.Length + 1));
    }
}


/*Test("1+2+3+4");
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
Test("*1 / 2.5");*/
while (true)
{ 
    Console.WriteLine("Write an arithmetic expression or \"exit\" to close.");
    string input = Console.ReadLine() ?? "";
    switch (input)
    {
        case "":
            Console.WriteLine("Empty input. Please try again.");
            break;
        case "exit":
            Console.WriteLine("Exiting. Have a good day.");
            Environment.Exit(0);
            break;
        default:
            Test(input);
            break;
    }
}
public enum AstNodeType
{
    Undefined,
    OperatorPlus,
    OperatorMinus,
    OperatorMul,
    OperatorDiv,
    UnaryMinus,
    NumberValue
}

public class AstNode
{
    public AstNodeType Type = AstNodeType.Undefined;
    public double Value = 0;
    public AstNode? Left = null;
    public AstNode? Right = null;
}

class Parser
{
    private Token _crtToken;
    private string _text = "";
    private int _idx;

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

    private AstNode Expression()
    {
        AstNode tNode =  Term();
        AstNode e1Node =  Expression1();
        return CreateNode(AstNodeType.OperatorPlus, tNode, e1Node);
    }

    private AstNode Expression1()
    {
        var tNode = new AstNode();
        var e1Node = new AstNode();

        switch (_crtToken.Type)
        {
            case TokenType.Plus:
                GetNextToken();
                tNode = Term();
                e1Node = Expression1();
                return CreateNode(AstNodeType.OperatorPlus, e1Node, tNode);
            case TokenType.Minus:
                GetNextToken();
                tNode = Term();
                e1Node = Expression1();
                return CreateNode(AstNodeType.OperatorMinus, e1Node, tNode);
        }

        return CreateNodeNumber(0);
    }

    private AstNode Term()
    {
        var fNode = Factor();
        var t1Node = Term1();

        return CreateNode(AstNodeType.OperatorMul, fNode, t1Node);
    }

    private AstNode Term1()
    {
        var fNode = new AstNode();
        var t1Node = new AstNode();
        switch (_crtToken.Type)
        {
            case TokenType.Mul:
                GetNextToken();
                fNode = Factor();
                t1Node = Term1();
                return CreateNode(AstNodeType.OperatorMul, t1Node, fNode);
            case TokenType.Div:
                GetNextToken();
                fNode = Factor();
                t1Node = Term1();
                return CreateNode(AstNodeType.OperatorDiv, t1Node, fNode);
        }

        return CreateNodeNumber(1);
    }

    private AstNode Factor()
    {
        var node = new AstNode();
        switch (_crtToken.Type)
        {
            case TokenType.OpenParenthesis: 
                GetNextToken();
                node = Expression();
                Match(')');
                return node;
            case TokenType.Minus:
                GetNextToken();
                node = Factor();
                return node;
            case TokenType.Number:
                double value = _crtToken.Value;
                GetNextToken();
                return CreateNodeNumber(value);
            default:
                string error = $"Unexpected token \"{_crtToken.Symbol}\" at position {_idx-1}";
                throw new ParserException(error, _idx-1);
        }
    }

    AstNode CreateNode(AstNodeType type, AstNode left, AstNode right)
    {
        var node = new AstNode
        {
            Type = type,
            Left = left,
            Right = right
        };
        return node;
    }

    AstNode CreateUnaryNode(AstNode left)
    {
        var node = new AstNode
        {
            Type = AstNodeType.UnaryMinus,
            Left = left,
            Right = null
        };

        return node;
    }

    AstNode CreateNodeNumber(double value)
    {
        var node = new AstNode
        {
            Type = AstNodeType.NumberValue,
            Value = value
        };
        return node;
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

    public AstNode Parse(string text)
    {
        _text = text;
        _idx = 0;
        GetNextToken();
        return Expression();
    }

    
}
class ParserException(string message, int pos) : Exception
{
    public int Pos { get;} = pos;
    public string Msg { get;} = message;
    public string Ptr = "^---Here";

}

class Evaluator
{
    double EvaluateSubtree(AstNode ast)
    {
        if (ast == null)
        {
            throw new EvaluatorException("Incorrect AST");
        }

        if (ast.Type == AstNodeType.NumberValue)
        {
            return ast.Value;
        }
        else if (ast.Type == AstNodeType.UnaryMinus)
        {
            return EvaluateSubtree(ast.Left);
        }
        else
        {
            double v1 = EvaluateSubtree(ast.Left);
            double v2 = EvaluateSubtree(ast.Right);
            switch (ast.Type)
            {
                case AstNodeType.OperatorPlus: return v1 + v2;
                case AstNodeType.OperatorMinus: return v1 - v2;
                case AstNodeType.OperatorMul: return v1 * v2;
                case AstNodeType.OperatorDiv: return v1 / v2;
            }
        }

        throw new EvaluatorException("Incorrect AST");
    }

    public double Evaluate(AstNode ast)
    {
        if (ast == null)
        {
            throw new EvaluatorException("Incorrect AST");
        }

        return EvaluateSubtree(ast);
    }
}

 public class EvaluatorException(string message) : Exception
{
    public string Msg { get;} = message;
}

