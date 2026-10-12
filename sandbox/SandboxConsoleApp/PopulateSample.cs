using System.Collections.ObjectModel;
using MessagePack;

// Populate contract sample (README §3): a plain Deserialize constructs the object and refills, in place, whatever the
// member initializers put there; an explicit `ref` populate does the same on the caller's instance.
// Generated formatter: obj/Debug/net11.0/generated/MessagePack.SourceGenerator/.../PopulateSample_UserFormatter.g.cs
// (build with -p:EmitCompilerGeneratedFiles=true)
namespace PopulateSample;

[MessagePackObject]
public class Settings
{
    [Key(0)] public string? Name { get; set; }
}

// populate shape: parameterless ctor + settable members
[MessagePackObject]
public class User
{
    [Key(0)] public int Id { get; set; }                                   // Direct: read straight into the setter
    [Key(1)] public List<int> Items { get; set; } = new();                 // cleared and refilled: one allocation
    [Key(2)] public Settings Settings { get; set; } = new() { Name = "x" }; // refilled in place
    [Key(3)] public ObservableCollection<int> Observed { get; set; } = new(); // refilled: its handlers fire during the read
    [Key(4)] public object[] Tags { get; set; } = new string[2];           // covariant: replaced, never refilled
}

// construction shape (init-only member): always built afresh, populate never applies
[MessagePackObject]
public class Account
{
    [Key(0)] public int Id { get; init; }
    [Key(1)] public string? Name { get; set; }
}

public static class Demo
{
    public static void Run()
    {
        var bytes = MessagePackSerializer.Serialize(new User { Id = 1, Items = [1, 2], Settings = new Settings { Name = "custom" }, Observed = [1, 2], Tags = [1, 2] });

        var user = MessagePackSerializer.Deserialize<User>(bytes);
        Console.WriteLine($"plain:    Items={string.Join(",", user.Items)} Settings.Name={user.Settings.Name} Tags={user.Tags.GetType().Name}");

        var target = new User();
        var items = target.Items;
        var child = target.Settings;
        MessagePackSerializer.Deserialize(bytes, ref target);
        Console.WriteLine($"populate: sameItems={ReferenceEquals(items, target.Items)} sameChild={ReferenceEquals(child, target.Settings)} child.Name={child.Name}");

        var account = new Account { Id = 1, Name = "old" };
        var original = account;
        MessagePackSerializer.Deserialize(MessagePackSerializer.Serialize(new Account { Id = 2, Name = "new" }), ref account);
        Console.WriteLine($"init-only: replaced={!ReferenceEquals(original, account)} account.Id={account.Id}");
    }
}
