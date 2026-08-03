using ThirdPartyLib;
using UltraMessagePack;


var a = new MessagePackSerializerOptions(new ThirdPartyFactory());


var bin = MessagePackSerializer.Serialize(10, a);
