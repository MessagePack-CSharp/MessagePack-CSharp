using MessagePack.Formatters;
using System.Reactive.Linq;
using MessagePack.ReactivePropertyExtension;
using MessagePack.Resolvers;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Reactive;

namespace MessagePack.Tests.ExtensionTests
{
    public class WithRxPropDefaultResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return (ReactivePropertyResolver.Instance.GetFormatter<T>()
                 ?? StandardResolver.Instance.GetFormatter<T>());
        }
    }

    public class ReactivePropertyTest
    {
        private static readonly MessagePackSerializerOptions options = MessagePackSerializerOptions.Default.WithResolver(new WithRxPropDefaultResolver());

        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, options), options);
        }

        [Fact(Skip = "assembly reference")]
        public void ViewModelTest()
        {
            var vm = new ViewModel(10, 20, 30);

            var deserialized = Convert(vm);
            deserialized.Prop1.Value.Is(10);
            deserialized.Prop2.Value.Is(20);
            deserialized.Prop3.Value.Is(30);
            deserialized.Prop4.Value.Is(60);

            // dump serialized
            var data = MessagePackSerializer.SerializeToJson(vm, options);

            //  3 = ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe
            // -1 = UIDispatcherScheduler.Default
            // Prop1:[3, -1, 10]
            // Prop2:[3, -1, 10]
            // Prop3:[3, -1, 10]
            data.Is("[[3,-1,10],[3,-1,20],[3,-1,30]]");
        }

        [Fact(Skip = "assembly reference")]
        public void MiscTest()
        {
            var rxCol = new ReactiveCollection<int> { 1, 10, 100 };
            Convert(rxCol).Is(1, 10, 100);
            Convert(Unit.Default).Is(Unit.Default);
            Unit? nullUnit = null;
            Convert(nullUnit).Is(Unit.Default);
        }
    }

    [MessagePackObject]
    public class ViewModel : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public ReactiveProperty<int> Prop1 { get; private set; }
        [Key(1)]
        public IReactiveProperty<int> Prop2 { get; private set; }
        [Key(2)]
        public IReadOnlyReactiveProperty<int> Prop3 { get; private set; }
        [IgnoreMember]
        public IReadOnlyReactiveProperty<int> Prop4 { get; private set; }

        public ViewModel(int x, int y, int z)
            : this(new ReactiveProperty<int>(x), new ReactiveProperty<int>(y), new ReactiveProperty<int>(z))
        {

        }

        [SerializationConstructor]
        public ViewModel(ReactiveProperty<int> x, IReactiveProperty<int> y, IReadOnlyReactiveProperty<int> z)
        {
            Prop1 = x;
            Prop2 = y;
            Prop3 = z;
            OnAfterDeserialize();
        }

        public void OnAfterDeserialize()
        {
            Prop4 = Prop1.CombineLatest(Prop2, (x, y) => (x + y) * 2).ToReadOnlyReactiveProperty();
        }

        public void OnBeforeSerialize()
        {

        }
    }
}
