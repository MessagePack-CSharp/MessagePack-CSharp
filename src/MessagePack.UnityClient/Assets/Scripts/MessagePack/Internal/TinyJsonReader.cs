// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack
{
    /* simple, tiny JSON reader for MessagePackSerializer.FromJson.
     * this is simple, compact and enough fast but not optimized extremely. */

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
        String,
    }

    [Serializable]
    public class TinyJsonException : MessagePackSerializationException
    {
        public TinyJsonException(string message)
            : base(message)
        {
        }

        protected TinyJsonException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    internal class TinyJsonReader : IDisposable
    {
        private readonly TextReader reader;
        private readonly bool disposeInnerReader;
        private StringBuilder reusableBuilder;

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
            this.ReadNextToken();
            this.ReadValue();
            return this.TokenType != TinyJsonToken.None;
        }

        public void Dispose()
        {
            if (this.reader != null && this.disposeInnerReader)
            {
                this.reader.Dispose();
            }

            this.TokenType = TinyJsonToken.None;
            this.ValueType = ValueType.Null;
        }

        private void SkipWhiteSpace()
        {
            var c = this.reader.Peek();
            while (c != -1 && Char.IsWhiteSpace((char)c))
            {
                this.reader.Read();
                c = this.reader.Peek();
            }
        }

        private char ReadChar()
        {
            return (char)this.reader.Read();
        }

        private static bool IsWordBreak(char c)
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

        private void ReadNextToken()
        {
            this.SkipWhiteSpace();

            var intChar = this.reader.Peek();
            if (intChar == -1)
            {
                this.TokenType = TinyJsonToken.None;
                return;
            }

            var c = (char)intChar;
            switch (c)
            {
                case '{':
                    this.TokenType = TinyJsonToken.StartObject;
                    return;
                case '}':
                    this.TokenType = TinyJsonToken.EndObject;
                    return;
                case '[':
                    this.TokenType = TinyJsonToken.StartArray;
                    return;
                case ']':
                    this.TokenType = TinyJsonToken.EndArray;
                    return;
                case '"':
                    this.TokenType = TinyJsonToken.String;
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
                    this.TokenType = TinyJsonToken.Number;
                    return;
                case 't':
                    this.TokenType = TinyJsonToken.True;
                    return;
                case 'f':
                    this.TokenType = TinyJsonToken.False;
                    return;
                case 'n':
                    this.TokenType = TinyJsonToken.Null;
                    return;
                case ',':
                case ':':
                    this.reader.Read();
                    this.ReadNextToken();
                    return;
                default:
                    throw new TinyJsonException("Invalid String:" + c);
            }
        }

        private void ReadValue()
        {
            this.ValueType = ValueType.Null;

            switch (this.TokenType)
            {
                case TinyJsonToken.None:
                    break;
                case TinyJsonToken.StartObject:
                case TinyJsonToken.EndObject:
                case TinyJsonToken.StartArray:
                case TinyJsonToken.EndArray:
                    this.reader.Read();
                    break;
                case TinyJsonToken.Number:
                    this.ReadNumber();
                    break;
                case TinyJsonToken.String:
                    this.ReadString();
                    break;
                case TinyJsonToken.True:
                    if (this.ReadChar() != 't')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 'r')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 'u')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 'e')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    this.ValueType = ValueType.True;
                    break;
                case TinyJsonToken.False:
                    if (this.ReadChar() != 'f')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 'a')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 'l')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 's')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 'e')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    this.ValueType = ValueType.False;
                    break;
                case TinyJsonToken.Null:
                    if (this.ReadChar() != 'n')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 'u')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 'l')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    if (this.ReadChar() != 'l')
                    {
                        throw new TinyJsonException("Invalid Token");
                    }

                    this.ValueType = ValueType.Null;
                    break;
                default:
                    throw new MessagePackSerializationException("InvalidTokenState:" + this.TokenType);
            }
        }

        private void ReadNumber()
        {
            StringBuilder numberWord;
            if (this.reusableBuilder == null)
            {
                this.reusableBuilder = new StringBuilder();
                numberWord = this.reusableBuilder;
            }
            else
            {
                numberWord = this.reusableBuilder;
                numberWord.Length = 0; // Clear
            }

            var isDouble = false;
            var intChar = this.reader.Peek();
            while (intChar != -1 && !IsWordBreak((char)intChar))
            {
                var c = this.ReadChar();
                numberWord.Append(c);
                if (c == '.' || c == 'e' || c == 'E')
                {
                    isDouble = true;
                }

                intChar = this.reader.Peek();
            }

            var number = numberWord.ToString();
            if (isDouble)
            {
                double parsedDouble;
                Double.TryParse(number, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, System.Globalization.CultureInfo.InvariantCulture, out parsedDouble);
                this.ValueType = ValueType.Double;
                this.DoubleValue = parsedDouble;
            }
            else
            {
                long parsedInt;
                if (Int64.TryParse(number, NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out parsedInt))
                {
                    this.ValueType = ValueType.Long;
                    this.LongValue = parsedInt;
                    return;
                }

                ulong parsedULong;
                if (ulong.TryParse(number, NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out parsedULong))
                {
                    this.ValueType = ValueType.ULong;
                    this.ULongValue = parsedULong;
                    return;
                }

                Decimal parsedDecimal;
                if (decimal.TryParse(number, NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out parsedDecimal))
                {
                    this.ValueType = ValueType.Decimal;
                    this.DecimalValue = parsedDecimal;
                    return;
                }
            }
        }

        private void ReadString()
        {
            this.reader.Read(); // skip ["]

            StringBuilder sb;
            if (this.reusableBuilder == null)
            {
                this.reusableBuilder = new StringBuilder();
                sb = this.reusableBuilder;
            }
            else
            {
                sb = this.reusableBuilder;
                sb.Length = 0; // Clear
            }

            while (true)
            {
                if (this.reader.Peek() == -1)
                {
                    throw new TinyJsonException("Invalid Json String");
                }

                var c = this.ReadChar();
                switch (c)
                {
                    case '"': // endtoken
                        goto END;
                    case '\\': // escape character
                        if (this.reader.Peek() == -1)
                        {
                            throw new TinyJsonException("Invalid Json String");
                        }

                        c = this.ReadChar();
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
                                hex[0] = this.ReadChar();
                                hex[1] = this.ReadChar();
                                hex[2] = this.ReadChar();
                                hex[3] = this.ReadChar();
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
            this.ValueType = ValueType.String;
            this.StringValue = sb.ToString();
        }
    }
}
