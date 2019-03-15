//using Benchmark.Serializers;
//using System.Buffers;
//using Hagar;
//using Hagar.Buffers;
//using Hagar.Session;
//using Microsoft.Extensions.DependencyInjection;
//using System.IO.Pipelines;

//public class Hagar_ : SerializerBase
//{
//    readonly ServiceProvider serviceProvider;

//    public Hagar_()
//    {
//        this.serviceProvider = new ServiceCollection()
//            .AddHagar()
//            .AddISerializableSupport()
//            .AddSerializers(typeof(Hagar_).Assembly) // this assembly
//            .BuildServiceProvider();
//    }

//    public override T Deserialize<T>(object input)
//    {
//        var serializer = serviceProvider.GetRequiredService<Serializer<T>>();
//        var sessionPool = serviceProvider.GetRequiredService<SessionPool>();

//        var pipe = new Pipe();
//        pipe.Writer.WriteAsync((byte[])input).GetAwaiter().GetResult();
//        pipe.Writer.Complete();

//        using (var session = sessionPool.GetSession())
//        {
//            pipe.Reader.TryRead(out var readResult);
//            var reader = new Reader(readResult.Buffer, session);
//            var result = serializer.Deserialize(ref reader);
//            return result;
//        }
//    }

//    public override object Serialize<T>(T input)
//    {
//        var serializer = serviceProvider.GetRequiredService<Serializer<T>>();
//        var sessionPool = serviceProvider.GetRequiredService<SessionPool>();

//        var pipe = new Pipe();

//        using (var session = sessionPool.GetSession())
//        {
//            var writer = pipe.Writer.CreateWriter(session);
//            serializer.Serialize(ref writer, input);
//            pipe.Writer.Complete();
//            pipe.Reader.TryRead(out var result);
//            return result.Buffer.ToArray();
//        }
//    }
//}
