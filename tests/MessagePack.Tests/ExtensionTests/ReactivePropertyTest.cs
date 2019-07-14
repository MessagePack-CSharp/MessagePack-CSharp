// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.ReactivePropertyExtension;
using MessagePack.Resolvers;
using Reactive.Bindings;
using Xunit;

namespace MessagePack.Tests.ExtensionTests
{
    public class ReactivePropertyTest
    {
        private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Default.WithResolver(new WithRxPropDefaultResolver());

        private T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, Options), Options);
        }

        [Fact(Skip = "assembly reference")]
        public void ViewModelTest()
        {
            var vm = new ViewModel(10, 20, 30);

            ViewModel deserialized = this.Convert(vm);
            deserialized.Prop1.Value.Is(10);
            deserialized.Prop2.Value.Is(20);
            deserialized.Prop3.Value.Is(30);
            deserialized.Prop4.Value.Is(60);

            // dump serialized
            var data = MessagePackSerializer.SerializeToJson(vm, Options);

            // +3 = ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe
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
            this.Convert(rxCol).Is(1, 10, 100);
            this.Convert(Unit.Default).Is(Unit.Default);
            Unit? nullUnit = null;
            this.Convert(nullUnit).Is(Unit.Default);
        }

        public class WithRxPropDefaultResolver : IFormatterResolver
        {
            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                return ReactivePropertyResolver.Instance.GetFormatter<T>()
                     ?? StandardResolver.Instance.GetFormatter<T>();
            }
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
            this.Prop1 = x;
            this.Prop2 = y;
            this.Prop3 = z;
            this.OnAfterDeserialize();
        }

        public void OnAfterDeserialize()
        {
            this.Prop4 = this.Prop1.CombineLatest(this.Prop2, (x, y) => (x + y) * 2).ToReadOnlyReactiveProperty();
        }

        public void OnBeforeSerialize()
        {
        }
    }
}
