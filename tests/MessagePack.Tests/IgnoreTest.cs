using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MessagePack.Tests
{
    [MessagePackObject(true)]
    public class ViewModel
    {
        [IgnoreDataMember]
        public int NoModel { get; set; }

        public int MyProperty1 { get; set; }
        public int MyProperty2 { get; set; }
    }

    public class IgnoreTest
    {
        [Fact]
        public void DataContract()
        {
            var vm = new ViewModel() { NoModel = 999, MyProperty1 = 10, MyProperty2 = 100 };
            var bin = MessagePackSerializer.Serialize(vm);
            var d = MessagePackSerializer.Deserialize<ViewModel>(bin);
            var json = MessagePackSerializer.ToJson(bin);

            d.NoModel.Is(0);
            d.MyProperty1.Is(10);
            d.MyProperty2.Is(100);
            json.Is(@"{""MyProperty1"":10,""MyProperty2"":100}");
        }
    }
}
