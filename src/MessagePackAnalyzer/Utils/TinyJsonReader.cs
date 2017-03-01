using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

// TinyJson is handmade Json reader/writer library.
// It no needs JSON.NET dependency.

namespace MessagePackAnalyzer
{
    public class TinyJsonException : Exception
    {
        public TinyJsonException(string message) : base(message)
        {

        }
    }

    public class KnownTypeSerializer
    {
        readonly Dictionary<Type, Func<object, string>> serializers = new Dictionary<Type, Func<object, string>>();
        readonly Dictionary<Type, Func<string, object>> deserializers = new Dictionary<Type, Func<string, object>>();

        public static KnownTypeSerializer Default = new KnownTypeSerializer();

        public KnownTypeSerializer()
        {
            serializers.Add(typeof(DateTime), x => ((DateTime)x).ToString("o"));
            deserializers.Add(typeof(DateTime), x => DateTime.Parse(x));
            serializers.Add(typeof(DateTimeOffset), x => ((DateTimeOffset)x).ToString("o"));
            deserializers.Add(typeof(DateTimeOffset), x => DateTimeOffset.Parse(x));
            serializers.Add(typeof(Uri), x => ((Uri)x).ToString());
            deserializers.Add(typeof(Uri), x => new Uri(x));
            serializers.Add(typeof(Guid), x => ((Guid)x).ToString());
            deserializers.Add(typeof(Guid), x => new Guid(x));
        }

        public bool Contains(Type type)
        {
            return serializers.ContainsKey(type);
        }

        public void Register(Type type, Func<object, string> serializer, Func<string, object> deserializer)
        {
            serializers[type] = serializer;
            deserializers[type] = deserializer;
        }

        public bool TrySerialize(Type type, object obj, out string result)
        {
            Func<object, string> serializer;
            if (type != null && serializers.TryGetValue(type, out serializer))
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
            if (type != null && deserializers.TryGetValue(type, out deserializer))
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
        readonly TextReader reader;
        readonly bool disposeInnerReader;

        public TinyJsonToken TokenType { get; private set; }
        public object Value { get; private set; }

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
            Value = null;
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
            Value = null;

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
                    Value = true;
                    break;
                case TinyJsonToken.False:
                    if (ReadChar() != 'f') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'a') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'l') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 's') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'e') throw new TinyJsonException("Invalid Token");
                    Value = false;
                    break;
                case TinyJsonToken.Null:
                    if (ReadChar() != 'n') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'u') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'l') throw new TinyJsonException("Invalid Token");
                    if (ReadChar() != 'l') throw new TinyJsonException("Invalid Token");
                    Value = null;
                    break;
                default:
                    throw new ArgumentException("InvalidTokenState:" + TokenType);
            }
        }

        void ReadNumber()
        {
            var numberWord = new StringBuilder();

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
                Double.TryParse(number, out parsedDouble);
                Value = parsedDouble;
            }
            else
            {
                long parsedInt;
                if (Int64.TryParse(number, out parsedInt))
                {
                    Value = parsedInt;
                    return;
                }

                ulong parsedULong;
                if (ulong.TryParse(number, out parsedULong))
                {
                    Value = parsedULong;
                    return;
                }

                Decimal parsedDecimal;
                if (decimal.TryParse(number, out parsedDecimal))
                {
                    Value = parsedDecimal;
                    return;
                }
            }
        }

        void ReadString()
        {
            reader.Read(); // skip ["]

            var sb = new StringBuilder();
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
            Value = sb.ToString();
        }
    }

    public class TinyJsonWriter : IDisposable
    {
        enum WritingState
        {
            Value, ArrayStart, ObjectStart, Array, Object, ObjectPropertyName
        }

        readonly TextWriter writer;
        readonly Stack<WritingState> state;
        readonly bool disposeInnerWriter;

        public TinyJsonWriter(TextWriter writer, bool disposeInnerWriter = true)
        {
            this.writer = writer;
            this.disposeInnerWriter = disposeInnerWriter;
            this.state = new Stack<WritingState>();
            state.Push(WritingState.Value);
        }

        public void WriteStartObject()
        {
            WritePrefix();
            writer.Write('{');
            state.Push(WritingState.ObjectStart);
        }

        public void WriteEndObject()
        {
            writer.Write('}');
            state.Pop();
        }

        public void WriteStartArray()
        {
            WritePrefix();
            writer.Write('[');
            state.Push(WritingState.ArrayStart);
        }

        public void WriteEndArray()
        {
            writer.Write(']');
            state.Pop();
        }

        public void WritePropertyName(string name)
        {
            WritePrefix();
            state.Push(WritingState.ObjectPropertyName);
            WriteString(name);
        }

        public void WriteValue(object obj)
        {
            WriteValue(obj, KnownTypeSerializer.Default);
        }

        public void WriteValue(object obj, KnownTypeSerializer serializer)
        {
            WritePrefix();

            // write value
            if (obj == null)
            {
                writer.Write("null");
            }
            else if (obj is string)
            {
                WriteString((string)obj);
            }
            else if (obj is bool)
            {
                writer.Write(((bool)obj) ? "true" : "false");
            }
            else
            {
                var t = obj.GetType();
                if (t.GetTypeInfo().IsEnum)
                {
                    var eValue = Convert.ChangeType(obj, Enum.GetUnderlyingType(t));
                    writer.Write(eValue); // Enum as WriteNumber
                    return;
                }

                if (t == typeof(sbyte))
                {
                    writer.Write((sbyte)obj);
                }
                else if (t == typeof(byte))
                {
                    writer.Write((byte)obj);
                }
                else if (t == typeof(Int16))
                {
                    writer.Write((Int16)obj);
                }
                else if (t == typeof(UInt16))
                {
                    writer.Write((UInt16)obj);
                }
                else if (t == typeof(Int32))
                {
                    writer.Write((Int32)obj);
                }
                else if (t == typeof(UInt32))
                {
                    writer.Write((UInt32)obj);
                }
                else if (t == typeof(Int64))
                {
                    writer.Write((Int64)obj);
                }
                else if (t == typeof(UInt64))
                {
                    writer.Write((UInt64)obj);
                }
                else if (t == typeof(Single))
                {
                    writer.Write((Single)obj);
                }
                else if (t == typeof(Double))
                {
                    writer.Write((Double)obj);
                }
                else if (t == typeof(Decimal))
                {
                    writer.Write((Decimal)obj);
                }
                else
                {
                    string result;
                    if (serializer.TrySerialize(t, obj, out result))
                    {
                        WriteString(result);
                    }
                    else
                    {
                        WriteString(obj.ToString());
                    }
                }
            }
        }

        void WritePrefix()
        {
            // write prefix by state
            var currentState = state.Peek();
            switch (currentState)
            {
                case WritingState.Value:
                    break;
                case WritingState.ArrayStart:
                    state.Pop();
                    state.Push(WritingState.Array);
                    break;
                case WritingState.ObjectStart:
                    state.Pop();
                    state.Push(WritingState.Object);
                    break;
                case WritingState.Array:
                case WritingState.Object:
                    writer.Write(',');
                    break;
                case WritingState.ObjectPropertyName:
                    state.Pop();
                    writer.Write(':');
                    break;
                default:
                    break;
            }
        }

        void WriteString(string o)
        {
            writer.Write('\"');

            for (int i = 0; i < o.Length; i++)
            {
                var c = o[i];
                switch (c)
                {
                    case '"':
                        writer.Write("\\\"");
                        break;
                    case '\\':
                        writer.Write("\\\\");
                        break;
                    case '\b':
                        writer.Write("\\b");
                        break;
                    case '\f':
                        writer.Write("\\f");
                        break;
                    case '\n':
                        writer.Write("\\n");
                        break;
                    case '\r':
                        writer.Write("\\r");
                        break;
                    case '\t':
                        writer.Write("\\t");
                        break;
                    default:
                        writer.Write(c);
                        break;
                }
            }

            writer.Write('\"');
        }

        public void Dispose()
        {
            if (writer != null && disposeInnerWriter)
            {
                writer.Dispose();
            }
        }
    }
}