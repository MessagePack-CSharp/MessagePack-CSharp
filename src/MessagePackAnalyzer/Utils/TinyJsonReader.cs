// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

/* TinyJson is handmade Json reader/writer library.
 * It no needs JSON.NET dependency. */

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePackAnalyzer
{
    public class TinyJsonException : Exception
    {
        public TinyJsonException(string message)
            : base(message)
        {
        }
    }

    public class KnownTypeSerializer
    {
        private readonly Dictionary<Type, Func<object, string>> serializers = new Dictionary<Type, Func<object, string>>();
        private readonly Dictionary<Type, Func<string, object>> deserializers = new Dictionary<Type, Func<string, object>>();

        public static readonly KnownTypeSerializer Default = new KnownTypeSerializer();

        public KnownTypeSerializer()
        {
            this.serializers.Add(typeof(DateTime), x => ((DateTime)x).ToString("o"));
            this.deserializers.Add(typeof(DateTime), x => DateTime.Parse(x));
            this.serializers.Add(typeof(DateTimeOffset), x => ((DateTimeOffset)x).ToString("o"));
            this.deserializers.Add(typeof(DateTimeOffset), x => DateTimeOffset.Parse(x));
            this.serializers.Add(typeof(Uri), x => ((Uri)x).ToString());
            this.deserializers.Add(typeof(Uri), x => new Uri(x));
            this.serializers.Add(typeof(Guid), x => ((Guid)x).ToString());
            this.deserializers.Add(typeof(Guid), x => new Guid(x));
        }

        public bool Contains(Type type)
        {
            return this.serializers.ContainsKey(type);
        }

        public void Register(Type type, Func<object, string> serializer, Func<string, object> deserializer)
        {
            this.serializers[type] = serializer;
            this.deserializers[type] = deserializer;
        }

        public bool TrySerialize(Type type, object obj, out string result)
        {
            Func<object, string> serializer;
            if (type != null && this.serializers.TryGetValue(type, out serializer))
            {
                result = serializer(obj);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public bool TryDeserialize(Type type, string json, out object result)
        {
            Func<string, object> deserializer;
            if (type != null && this.deserializers.TryGetValue(type, out deserializer))
            {
                result = deserializer(json);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }

    public enum TinyJsonToken
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

    public class TinyJsonReader : IDisposable
    {
        private readonly TextReader reader;
        private readonly bool disposeInnerReader;

        public TinyJsonToken TokenType { get; private set; }

        public object Value { get; private set; }

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
            this.Value = null;
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
            this.Value = null;

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

                    this.Value = true;
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

                    this.Value = false;
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

                    this.Value = null;
                    break;
                default:
                    throw new ArgumentException("InvalidTokenState:" + this.TokenType);
            }
        }

        private void ReadNumber()
        {
            var numberWord = new StringBuilder();

            var isDouble = false;
            var intChar = this.reader.Peek();
            while (intChar != -1 && !IsWordBreak((char)intChar))
            {
                var c = this.ReadChar();
                numberWord.Append(c);
                if (c == '.')
                {
                    isDouble = true;
                }

                intChar = this.reader.Peek();
            }

            var number = numberWord.ToString();
            if (isDouble)
            {
                double parsedDouble;
                Double.TryParse(number, out parsedDouble);
                this.Value = parsedDouble;
            }
            else
            {
                long parsedInt;
                if (Int64.TryParse(number, out parsedInt))
                {
                    this.Value = parsedInt;
                    return;
                }

                ulong parsedULong;
                if (ulong.TryParse(number, out parsedULong))
                {
                    this.Value = parsedULong;
                    return;
                }

                Decimal parsedDecimal;
                if (decimal.TryParse(number, out parsedDecimal))
                {
                    this.Value = parsedDecimal;
                    return;
                }
            }
        }

        private void ReadString()
        {
            this.reader.Read(); // skip ["]

            var sb = new StringBuilder();
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
            this.Value = sb.ToString();
        }
    }

    public class TinyJsonWriter : IDisposable
    {
        private enum WritingState
        {
            Value,
            ArrayStart,
            ObjectStart,
            Array,
            Object,
            ObjectPropertyName,
        }

        private readonly TextWriter writer;
        private readonly Stack<WritingState> state;
        private readonly bool disposeInnerWriter;

        public TinyJsonWriter(TextWriter writer, bool disposeInnerWriter = true)
        {
            this.writer = writer;
            this.disposeInnerWriter = disposeInnerWriter;
            this.state = new Stack<WritingState>();
            this.state.Push(WritingState.Value);
        }

        public void WriteStartObject()
        {
            this.WritePrefix();
            this.writer.Write('{');
            this.state.Push(WritingState.ObjectStart);
        }

        public void WriteEndObject()
        {
            this.writer.Write('}');
            this.state.Pop();
        }

        public void WriteStartArray()
        {
            this.WritePrefix();
            this.writer.Write('[');
            this.state.Push(WritingState.ArrayStart);
        }

        public void WriteEndArray()
        {
            this.writer.Write(']');
            this.state.Pop();
        }

        public void WritePropertyName(string name)
        {
            this.WritePrefix();
            this.state.Push(WritingState.ObjectPropertyName);
            this.WriteString(name);
        }

        public void WriteValue(object obj)
        {
            this.WriteValue(obj, KnownTypeSerializer.Default);
        }

        public void WriteValue(object obj, KnownTypeSerializer serializer)
        {
            this.WritePrefix();

            // write value
            if (obj == null)
            {
                this.writer.Write("null");
            }
            else if (obj is string)
            {
                this.WriteString((string)obj);
            }
            else if (obj is bool)
            {
                this.writer.Write(((bool)obj) ? "true" : "false");
            }
            else
            {
                Type t = obj.GetType();
                if (t.GetTypeInfo().IsEnum)
                {
                    var eValue = Convert.ChangeType(obj, Enum.GetUnderlyingType(t));
                    this.writer.Write(eValue); // Enum as WriteNumber
                    return;
                }

                if (t == typeof(sbyte))
                {
                    this.writer.Write((sbyte)obj);
                }
                else if (t == typeof(byte))
                {
                    this.writer.Write((byte)obj);
                }
                else if (t == typeof(Int16))
                {
                    this.writer.Write((Int16)obj);
                }
                else if (t == typeof(UInt16))
                {
                    this.writer.Write((UInt16)obj);
                }
                else if (t == typeof(Int32))
                {
                    this.writer.Write((Int32)obj);
                }
                else if (t == typeof(UInt32))
                {
                    this.writer.Write((UInt32)obj);
                }
                else if (t == typeof(Int64))
                {
                    this.writer.Write((Int64)obj);
                }
                else if (t == typeof(UInt64))
                {
                    this.writer.Write((UInt64)obj);
                }
                else if (t == typeof(Single))
                {
                    this.writer.Write((Single)obj);
                }
                else if (t == typeof(Double))
                {
                    this.writer.Write((Double)obj);
                }
                else if (t == typeof(Decimal))
                {
                    this.writer.Write((Decimal)obj);
                }
                else
                {
                    string result;
                    if (serializer.TrySerialize(t, obj, out result))
                    {
                        this.WriteString(result);
                    }
                    else
                    {
                        this.WriteString(obj.ToString());
                    }
                }
            }
        }

        private void WritePrefix()
        {
            // write prefix by state
            WritingState currentState = this.state.Peek();
            switch (currentState)
            {
                case WritingState.Value:
                    break;
                case WritingState.ArrayStart:
                    this.state.Pop();
                    this.state.Push(WritingState.Array);
                    break;
                case WritingState.ObjectStart:
                    this.state.Pop();
                    this.state.Push(WritingState.Object);
                    break;
                case WritingState.Array:
                case WritingState.Object:
                    this.writer.Write(',');
                    break;
                case WritingState.ObjectPropertyName:
                    this.state.Pop();
                    this.writer.Write(':');
                    break;
                default:
                    break;
            }
        }

        private void WriteString(string o)
        {
            this.writer.Write('\"');

            for (int i = 0; i < o.Length; i++)
            {
                var c = o[i];
                switch (c)
                {
                    case '"':
                        this.writer.Write("\\\"");
                        break;
                    case '\\':
                        this.writer.Write("\\\\");
                        break;
                    case '\b':
                        this.writer.Write("\\b");
                        break;
                    case '\f':
                        this.writer.Write("\\f");
                        break;
                    case '\n':
                        this.writer.Write("\\n");
                        break;
                    case '\r':
                        this.writer.Write("\\r");
                        break;
                    case '\t':
                        this.writer.Write("\\t");
                        break;
                    default:
                        this.writer.Write(c);
                        break;
                }
            }

            this.writer.Write('\"');
        }

        public void Dispose()
        {
            if (this.writer != null && this.disposeInnerWriter)
            {
                this.writer.Dispose();
            }
        }
    }
}
