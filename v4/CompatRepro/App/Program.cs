
using ThirdPartyLib;
using UltraMessagePack;

var resolver = new MessagePackFormatterResolver(new ThirdPartyFactory());
resolver.CompatibilityFallback += type => Console.WriteLine($"compat fallback: {type}");

var options = new MessagePackSerializerOptions(resolver);
var bin = MessagePackSerializer.Serialize(new MyPerson { MyProperty = 99 }, options);
