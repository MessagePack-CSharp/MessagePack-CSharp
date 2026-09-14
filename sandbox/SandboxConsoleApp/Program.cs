using Huga;
using MessagePack;
using MessagePack.Formatters;
using SerializerFoundation;
using SerializerFoundation.CodeAnalysis;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text.Json.Serialization;




//MessagePackSerializer.Serialize<Plain>(new Plain { X = 123 });
Serialize(new Plain { X = 123 });

Console.WriteLine("foo");

static void Serialize<T>(T value)
{
    MessagePackSerializer.Serialize<T>(value);
}


public class Plain { public int X { get; set; } }


//[UnionTag(typeof(Leaf), 0)]
//public abstract class Root2 { } 

[MessagePackSerializable<SandboxPerson[]>]
public partial class RootFactory;


public partial class My<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
{
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value)
    {
        throw new NotImplementedException();
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        throw new NotImplementedException();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value)
    {

        // MessagePackSerializer.Serialize(value);

        // buffer.WriteTimestamp(DateTime.UtcNow);

        throw new NotImplementedException();
    }
}



[MessagePackObject]
public class HugaHuga
{
    // public int MyProperty { get; set; }
}


// [MessagePackObject]



[MessagePackObject]
public class PocoBase
{
    [Key(0)] public virtual int Value { get; set; }
}

[MessagePackObject]
public class Derived : PocoBase
{
    public override int Value { get => base.Value; set => base.Value = value; }
}


public class UserId
{
    public int Value { get; }
    public string Realm { get; }
    public UserId(int value, string realm)
    {
        this.Value = value;
        this.Realm = realm;
    }
}



[MessagePackObject]
public struct UserIdSurrogate : IMessagePackSurrogate<UserId, UserIdSurrogate>
{
    [Key(0)]
    public int Value { get; }

    [Key(1)]
    public string Realm { get; }

    public UserIdSurrogate(int value, string realm)
    {
        this.Value = value;
        this.Realm = realm;
    }

    public UserIdSurrogate ToSurrogate(UserId value)
    {
        return new UserIdSurrogate(value.Value, value.Realm);
    }

    public UserId ToTarget()
    {
        return new UserId(this.Value, this.Realm);
    }
}









//[JsonSerializable(typeof(SandboxPerson[]))]
//public partial class HogeMoge : JsonSerializerContext
//{
//}


//var bytes = MessagePackSerializer.Serialize(person);
//Console.WriteLine($"person  : {Convert.ToHexString(bytes)}");

//var back = MessagePackSerializer.Deserialize<SandboxPerson>(bytes)!;
//Console.WriteLine($"back    : Id={back.Id}, Name={back.Name}, Tags=[{string.Join(", ", back.Tags ?? [])}]");

//ISandboxShape shape = new SandboxCircle { Radius = 2.5 };
//var shapeBytes = MessagePackSerializer.Serialize(shape);
//Console.WriteLine($"union   : {Convert.ToHexString(shapeBytes)}");
//Console.WriteLine($"back    : {MessagePackSerializer.Deserialize<ISandboxShape>(shapeBytes)}");

//var json = MessagePackSerializer.ConvertToJson(bytes);
//Console.WriteLine($"as json : {json}");

[MessagePackObject(KeyNamingPolicy.SnakeCaseLower)]
public class SandboxPerson
{
    public required int Id { get; init; }
    public string? Name { get; set; }
    public string[]? Tags { get; set; }
}

[MessagePackObject]
[UnionTag(typeof(SandboxCircle), 0)]
[UnionTag(typeof(SandboxRectangle), 1)]
public interface ISandboxShape
{
}

[MessagePackObject]
public class SandboxCircle : ISandboxShape
{
    [Key(0)] public double Radius { get; set; }

    public override string ToString() => $"Circle(Radius={Radius})";
}

[MessagePackObject]
public class SandboxRectangle : ISandboxShape
{
    [Key(0)] public double Width { get; set; }
    [Key(1)] public double Height { get; set; }

    public override string ToString() => $"Rectangle({Width}x{Height})";
}




[MessagePackObject]
public class MyTakoYaki
{
    // [MessagePackFormatter(typeof(DotNetOptimizedFormatterFactory))]
    [Key(0)]
    public DateTime Hgoe { get; set; }

    [MessagePackFormatter(typeof(DictionaryFormatterFactory<string, string>), "StringComparer.OrdinalIgnoreCase")]
    [Key(1)]
    public required Dictionary<string, string> MyProperty { get; set; }
}



[MessagePackObject(true)]
public class MyTakoYaki2
{
    public string? MyProperty { get; set; }
    public required string? HogeHogege { get; set; }
}

[MessagePackObject]
public class MyTakoYaki3
{
    [Key(0)]
    public int MyProperty { get; set; }
    [Key(1)]
    public required string Hoge { get; init; }
}

[MessagePackFormatter(typeof(EnumAsStringFormatterFactory<Takosuk>))]
public enum Takosuk { Hoge }





[MessagePackObject]
public record class Cat([property: Key(0)] string Name);
[MessagePackObject]
public record class Dog([property: Key(0)] string Name);
[MessagePackObject]
public record class Bird([property: Key(0)] string Name);

[MessagePackObject]
[UnionTag<Cat>(0)]
[UnionTag<Dog>(1)]
[UnionTag<Bird>(2)]
public union Pet(Cat, Dog, Bird);





[Union]
//[MessagePackObject]
//[UnionTag<int>(0)]
//[UnionTag<string>(1)]
public struct IntOrString : IntOrString.IUnionMembers // : IUnion
{
    private object _value;

    //public IntOrString(int value) => _value = value;
    //public IntOrString(string value) => _value = value;

    public object? Value => _value;

    public interface IUnionMembers
    {
        public static IntOrString Create(int value) => new IntOrString { _value = value };
        public object? Value { get; }
    }
}

[Union]
public class TakoYaki : IUnion
{
    object? _value;
    public object? Value => _value;

    public TakoYaki(int x)
    {
        this._value = x;
    }
}




public closed class HogeHoge
{
}


[MessagePackObject]
[UnionTag<int?>(0)]
[UnionTag<int>(1)]
public union Both(int?, int);


[MessagePackObject(AllowCircularReferences = true)]
public class Node
{
    [Key(0)]
    public Node? Parent { get; set; }
    [Key(1)]
    public List<Node> Children { get; set; } = [];
    [Key(2)]
    public string? Name { get; set; }
}


namespace Hoge
{
    [MessagePackObject]
    public class Hoge
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Name { get; set; } = "";
    }
}


namespace Huga
{
    [MessagePackObject]
    public struct Hoge : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(2)]
        public string? Name { get; set; }

        public MessagePackUnknownMembers? Extra { get; set; }

        void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
        {
            //throw new NotImplementedException();
        }

        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
        {
            //throw new NotImplementedException();
        }
    }


    [MessagePackObject]
    public class TakoyakiX
    {
        [Key(0)]
        public int MyProperty1 { get; set; }
        [Key(1)]
        public int MyProperty2 { get; set; }
        [Key(2)]
        public int MyProperty3 { get; set; }
    }


    public class Moge
    {
    }

    public abstract class FooHoge
    {
        [RequireOverride]
        public virtual void Nano()
        {
        }
    }



    public partial class MyTakoyakiXFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TakoyakiX>
    {
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, int> intFormatter = default!;

        public void Initialize(MessagePackFormatterResolver resolver)
        {
            intFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, int>();
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TakoyakiX value)
        {
            buffer.WriteArrayHeader(3);
            state.Enter();





            state.Exit();
        }






        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TakoyakiX value)
        {
            throw new NotImplementedException();
        }
    }



    [MessagePackObject]
    [UnionTag<HogeZ1>(0)]
    public closed class TakoyakiZ
    {
    }

    [MessagePackObject]
    public class HogeZ1 : TakoyakiZ
    {
    }

    //[MessagePackObject]
    //public class HogeZ2 : TakoyakiZ
    //{
    //}




    public struct MyBuffer : IWriteBuffer
    {
        public long BytesWritten => throw new NotImplementedException();

        public void Advance(int bytesWritten)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            throw new NotImplementedException();
        }
    }




    [MessagePackObject]
    [UnionTag<Tako1>(0)]
    public interface ITako
    {
    }

    [MessagePackObject]
    public class Tako1 : ITako
    {
    }

    [MessagePackObject]
    [UnionTag<TakoDerived>(0)]
    public closed class TakoBase
    {
    }

    [MessagePackObject]
    public class TakoDerived : TakoBase
    {
    }


    // [MessagePackSerializable<Testoariaezu[]>]
    // [MessagePackSerializable(typeof(List<>))]
    public partial class MyFactory;


    //[MessagePackObject(AllowCircularReferences = true)]
    //public class CtorNode
    //{
    //    [Key(0)] public int Value { get; init; }

    //    public CtorNode(int value) { Value = value; }
    //}



    [MessagePackObject(keyAsPropertyName: true, AllowPrivate = true)]
    public partial class Testoariaezu
    {
        [MessagePackFormatter<ForceSizeFormatterFactory>]
        public int MyProperty { get; set; }

        [MessagePackFormatter<DictionaryFormatterFactory<string, int>>("StringComparer.OrdinalIgnoreCase")]
        public Dictionary<string, int>? My_Property1 { get; set; }

        int Hoge;

        [SerializationConstructor]
        public Testoariaezu()
        {
            // EqualityComparer<string> comparer = StringComparer.OrdinalIgnoreCase;
        }
    }
}


