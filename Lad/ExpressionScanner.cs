using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lad
{
    public class ExpressionScanner
    {
        private readonly TextReader reader;
        private int? savedChar;
        private ScannerMode mode;

        public ExpressionScanner(TextReader reader)
        {
            this.reader = reader;
        }

        internal Token Read()
        {
            while(Peek(out char ch))
            {
                Get();
                if(mode == ScannerMode.Starting)
                {
                    // Ignore leading white space.
                    if(ch == '\n')
                        return Token.End;
                    if(char.IsWhiteSpace(ch))
                        continue;
                    mode = ScannerMode.Normal;
                }
                if(ch == '\\')
                    return new Token { Symbol = ExpressionParser.Symbol, Value = (char)GetEscaped() };
                switch(mode)
                {
                case ScannerMode.InClass:
                    switch(ch)
                    {
                    case ']':
                        mode = ScannerMode.Normal;
                        return new Token { Symbol = ch };
                    case '-':
                        return new Token { Symbol = ch };
                    case '\n':
                        ReportError("unbalanced brackets");
                        return Token.End;
                    }
                    return new Token { Symbol = ExpressionParser.Symbol, Value = (char)ch };
                case ScannerMode.InCount:
                    if(char.IsDigit(ch))
                    {
                        int value = 0;
                        for(; ; )
                        {
                            value *= 10;
                            value += ch - '0';
                            if(!Peek(out ch) || !char.IsDigit(ch))
                                break;
                            Get();
                        }
                        return new Token { Symbol = ExpressionParser.Number, Value = value };
                    }
                    if(ch == '\n')
                    {
                        ReportError("unbalanced braces");
                        return Token.End;
                    }
                    if(ch == '}')
                        mode = ScannerMode.Normal;
                    return new Token { Symbol = ch };
                case ScannerMode.InNamedExpression:
                    mode = ScannerMode.Normal;
                    if(ch == '_' || char.IsLetter(ch))
                    {
                        var sb = new StringBuilder();
                        while(char.IsLetterOrDigit(ch))
                        {
                            sb.Append(ch);
                            ch = (char)Get();
                        }
                        if(ch == '}')
                            return new Token { Symbol = ExpressionParser.NamedExpression, Value = sb.ToString() };
                    }
                    ReportError("invalid named expression reference");
                    return Token.End;
                case ScannerMode.InQuotes:
                    switch(ch)
                    {
                    case '\n':
                        ReportError("unterminated string constant");
                        return Token.End;
                    case '"':
                        mode = ScannerMode.Normal;
                        break;
                    default:
                        return new Token { Symbol = ExpressionParser.Symbol, Value = (char)ch };
                    }
                    break;
                case ScannerMode.Normal:
                    switch(ch)
                    {
                    case '[':
                        mode = ScannerMode.InClass;
                        if(Peek() == '^')
                        {
                            Get();
                            return new Token { Symbol = ExpressionParser.OSBC };
                        }
                        break;
                    case '{':
                        if(char.IsDigit((char)Peek()))
                        {
                            mode = ScannerMode.InCount;
                            break;
                        }
                        else
                        {
                            mode = ScannerMode.InNamedExpression;
                            continue;
                        }
                    case '"':
                        mode = ScannerMode.InQuotes;
                        continue;
                    case '(':
                    case ')':
                    case '.':
                    case '|':
                    case '?':
                    case '+':
                    case '*':
                        break;
                    default:
                        if(char.IsWhiteSpace(ch))
                            return Token.End;
                        else
                            return new Token { Symbol = ExpressionParser.Symbol, Value = (char)ch };
                    }
                    return new Token { Symbol = ch };
                }
            }
            return Token.End;
        }

        private void ReportError(string message)
        {
            throw new Exception(message);
        }

        private int GetEscaped()
        {
            int ch = Get();
            switch(ch)
            {
            case 'a':
                ch = '\a';
                break;
            case 'b':
                ch = '\b';
                break;
            case 'f':
                ch = '\f';
                break;
            case 'n':
                ch = '\n';
                break;
            case 'r':
                ch = '\r';
                break;
            case 't':
                ch = '\t';
                break;
            case 'u':
                return GetUnicode();
            case 'v':
                ch = '\v';
                break;
            case 'x':
                return GetHex();
            }
            return ch == '0' ? GetOctal() : ch;
        }

        private int GetUnicode()
        {
            if (GetHexDigit(out int x1) && GetHexDigit(out int x2) && GetHexDigit(out int x3) && GetHexDigit(out int x4))
                return (x1 << 12) | (x2 << 8) | (x3 << 4) | x4;
            ReportError("invalid Unicode escape");
            return -1;
        }

        private int GetHex()
        {
            if (GetHexDigit(out int x1) && GetHexDigit(out int x2))
                return (x1 << 4) | x2;
            ReportError("invalid hex escape");
            return -1;
        }

        private bool GetHexDigit(out int value)
        {
            value = Get();
            if(value < 0)
                return false;
            char ch = (char)value;
            if(char.IsDigit(ch))
                value -= '0';
            else if(ch >= 'A' && ch <= 'F')
                value -= 'A' + 10;
            else if(ch >= 'a' && ch <= 'f')
                value -= 'a' + 10;
            else
                return false;
            return true;
        }

        private int GetOctal()
        {
            int value = 0;
            while(Peek(out char ch))
            {
                if(ch >= '0' && ch < '8')
                    value += ch - '0';
                else
                {
                    ReportError("invalid octal escape");
                    break;
                }
                Get();
            }
            return value;
        }

        private int Get()
        {
            if(savedChar == null)
                return ReadCooked();
            int ch = savedChar.Value;
            if(ch >= 0)
                savedChar = null;
            return ch;
        }

        private int Peek()
        {
            if(savedChar == null)
                savedChar = ReadCooked();
            return savedChar.Value;
        }

        private bool Peek(out char ch)
        {
            int i = Peek();
            ch = (char)i;
            return i >= 0;
        }

        private int ReadCooked()
        {
            int ch = reader.Read();
            return ch == '\r' ? ReadCooked() : ch;
        }

        private enum ScannerMode { Starting, Normal, InClass, InCount, InNamedExpression, InQuotes }
    }
}
