using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MessagePack
{
    // simple, tiny JSON reader for MessagePackSerializer.FromJson.
    // this is simple, compact and enough fast but not optimized extremely.

    internal enum TinyJsonToken
    {
        None,
        StartObject,  // {
        EndObject,    // }
        StartArray,   // [
        EndArray,     // ]
        Number,       // -0~9
        String,       // "___"
        True,         // true
        False,        // false
        Null,         // null
    }

    internal enum ValueType : byte
    {
        Null,
        True,
        False,
        Double,
        Long,
        ULong,
        Decimal,
        String
    }

    internal class TinyJsonException : Exception
    {
        public TinyJsonException(string message) : base(message)
        {

        }
    }

    internal class TinyJsonReader : IDisposable
    {
        readonly TextReader reader;
        readonly bool disposeInnerReader;
        StringBuilder reusableBuilder;

        public TinyJsonToken TokenType { get; private set; }
        public ValueType ValueType { get; private set; }
        public double DoubleValue { get; private set; }
        public long LongValue { get; private set; }
        public ulong ULongValue { get; private set; }
        public decimal DecimalValue { get; private set; }
        public string StringValue { get; private set; }

        public TinyJsonReader(TextReader reader, bool disposeInnerReader = true)
        {
            this.reader = reader;
            this.disposeInnerReader = disposeInnerReader;
        }

        public bool Read()
        {
            ReadNextToken();
            ReadValue();
            return TokenType != TinyJsonToken.None;
        }

        public void Dispose()
        {
            if (reader != null && disposeInnerReader)
            {
                reader.Dispose();
            }
            TokenType = TinyJsonToken.None;
            ValueType = ValueType.Null;
        }

        void SkipWhiteSpace()
        {
            var c = reader.Peek();
            while (c != -1 && Char.IsWhiteSpace((char)c))
            {
                reader.Read();
                c = reader.Peek();
            }
        }

        char ReadChar()
        {
            return (char)reader.Read();
        }

        static bool IsWordBreak(char c)
        {
            switch (c)
            {
                case ' ':
                case '{':
                case '}':
                case '[':
                case ']':
                case ',':
                case ':':
                case '\"':
                    return true;
                default:
                    return false;
            }
        }

        void ReadNextToken()
        {
            SkipWhiteSpace();

            var intChar = reader.Peek();
            if (intChar == -1)
            {
                TokenType = TinyJsonToken.None;
                return;
            }

            var c = (char)intChar;
            switch (c)
            {
                case '{':
                    TokenType = TinyJsonToken.StartObject;
                    return;
                case '}':
                    TokenType = TinyJsonToken.EndObject;
                    return;
                case '[':
                    TokenType = TinyJsonToken.StartArray;
                    return;
                case ']':
                    TokenType = TinyJsonToken.EndArray;
                    return;
                case '"':
                    TokenType = TinyJsonToken.String;
                    return;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    TokenType = TinyJsonToken.Number;
                    return;
                case 't':
                    TokenType = TinyJsonToken.True;
                    return;
                case 'f':
                    TokenType = TinyJsonToken.False;
                    return;
                case 'n':
                    TokenType = TinyJsonToken.Null;
                    return;
                case ',':
                case ':':
                    reader.Read();
                    ReadNextToken();
                    return;
                default:
                    throw new TinyJsonException("Invalid String:" + c);
            }
        }

        void ReadValue()
        {
            ValueType = ValueType.Null;

            switch (TokenType)
            {
                case TinyJsonToken.None:
                    break;
                case TinyJsonToken.StartObject:
                case TinyJsonToken.EndObject:
                case TinyJsonToken.StartArray:
                case TinyJsonToken.EndArray:
                    reader.Read();
                    break;
                case TinyJsonToken.Number:
                    ReadNumber();
                    break;
                case TinyJsonToken.String:
                    ReadString();
                    break;
                case TinyJsonToken.True:
                    if (ReadChar() != 't') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'r') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'u') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'e') throw new TinyJsonException("Invalid Token");
                    ValueType = ValueType.True;
                    break;
                case TinyJsonToken.False:
                    if (ReadChar() != 'f') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'a') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'l') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 's') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'e') throw new TinyJsonException("Invalid Token");
                    ValueType = ValueType.False;
                    break;
                case TinyJsonToken.Null:
                    if (ReadChar() != 'n') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'u') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'l') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'l') throw new TinyJsonException("Invalid Token");
                    ValueType = ValueType.Null;
                    break;
                default:
                    throw new ArgumentException("InvalidTokenState:" + TokenType);
            }
        }

        void ReadNumber()
        {
            StringBuilder numberWord;
            if (reusableBuilder == null)
            {
                reusableBuilder = new StringBuilder();
                numberWord = reusableBuilder;
            }
            else
            {
                numberWord = reusableBuilder;
                numberWord.Length = 0; // Clear
            }

            var isDouble = false;
            var intChar = reader.Peek();
            while (intChar != -1 && !IsWordBreak((char)intChar))
            {
                var c = ReadChar();
                numberWord.Append(c);
                if (c == '.') isDouble = true;
                intChar = reader.Peek();
            }

            var number = numberWord.ToString();
            if (isDouble)
            {
                double parsedDouble;
                Double.TryParse(number, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, System.Globalization.CultureInfo.InvariantCulture, out parsedDouble);
                ValueType = ValueType.Double;
                DoubleValue = parsedDouble;
            }
            else
            {
                long parsedInt;
                if (Int64.TryParse(number, NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out parsedInt))
                {
                    ValueType = ValueType.Long;
                    LongValue = parsedInt;
                    return;
                }

                ulong parsedULong;
                if (ulong.TryParse(number, NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out parsedULong))
                {
                    ValueType = ValueType.ULong;
                    ULongValue = parsedULong;
                    return;
                }

                Decimal parsedDecimal;
                if (decimal.TryParse(number, NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out parsedDecimal))
                {
                    ValueType = ValueType.Decimal;
                    DecimalValue = parsedDecimal;
                    return;
                }
            }
        }

        void ReadString()
        {
            reader.Read(); // skip ["]

            StringBuilder sb;
            if (reusableBuilder == null)
            {
                reusableBuilder = new StringBuilder();
                sb = reusableBuilder;
            }
            else
            {
                sb = reusableBuilder;
                sb.Length = 0; // Clear
            }

            while (true)
            {
                if (reader.Peek() == -1) throw new TinyJsonException("Invalid Json String");

                var c = ReadChar();
                switch (c)
                {
                    case '"': // endtoken
                        goto END;
                    case '\\': // escape character
                        if (reader.Peek() == -1) throw new TinyJsonException("Invalid Json String");

                        c = ReadChar();
                        switch (c)
                        {
                            case '"':
                            case '\\':
                            case '/':
                                sb.Append(c);
                                break;
                            case 'b':
                                sb.Append('\b');
                                break;
                            case 'f':
                                sb.Append('\f');
                                break;
                            case 'n':
                                sb.Append('\n');
                                break;
                            case 'r':
                                sb.Append('\r');
                                break;
                            case 't':
                                sb.Append('\t');
                                break;
                            case 'u':
                                var hex = new char[4];
                                hex[0] = ReadChar();
                                hex[1] = ReadChar();
                                hex[2] = ReadChar();
                                hex[3] = ReadChar();
                                sb.Append((char)Convert.ToInt32(new string(hex), 16));
                                break;
                        }
                        break;
                    default: // string
                        sb.Append(c);
                        break;
                }
            }

            END:
            ValueType = ValueType.String;
            StringValue = sb.ToString();
        }
    }
}