using MessagePack;
using MessagePack.Formatters;
using SerializerFoundation;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;


var person = new SandboxPerson
{
    Id = 1,
    Name = "Yamaoka",
    Tags = ["msgpack", "sandbox"],
};

SandboxPerson[] array = [person];
var bytes = MessagePackSerializer.Serialize(array, MessagePackSerializerOptions.DefaultAot);
Console.WriteLine($"person  : {Convert.ToHexString(bytes)}");




[MessagePackSerializable<SandboxPerson[]>]
public partial class RootFactory;



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

    public static UserIdSurrogate ToSurrogate(UserId value)
    {
        return new UserIdSurrogate(value.Value, value.Realm);
    }

    public static UserId FromSurrogate(UserIdSurrogate surrogate)
    {
        return new UserId(surrogate.Value, surrogate.Realm);
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
    public int MyProperty { get; set; }
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
}