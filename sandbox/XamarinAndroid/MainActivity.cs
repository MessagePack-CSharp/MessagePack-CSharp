using Android.App;
using Android.Widget;
using Android.OS;
using MessagePack;

namespace XamarinAndroid
{
    [MessagePackObject]
    public class MyClass
    {
        [Key(0)]
        public int MyProperty { get; set; }
    }

    [Activity(Label = "XamarinAndroid", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            var hoge = MessagePackSerializer.Serialize(new MyClass());
            var huga = MessagePackSerializer.Deserialize<MyClass>(hoge);

            System.Console.WriteLine(huga.MyProperty);

            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            // SetContentView (Resource.Layout.Main);

            
        }
    }
}

