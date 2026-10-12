using MessagePack;
using MessagePack.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace MessagePack.Tests.AspNetCore;

// AddMessagePackProtocol() registered the protocol by type; an application that also registers MessagePackSerializerOptions
// as a service then left the container with two satisfiable single-argument constructors (IOptions<...> and
// MessagePackSerializerOptions): an InvalidOperationException at resolution, at startup under ValidateOnBuild.
public class HubProtocolRegistrationTests
{
    [Fact]
    public void AddMessagePackProtocol_WithSerializerOptionsRegistered_Resolves()
    {
        var services = new ServiceCollection();
        services.AddSingleton(MessagePackSerializerOptions.Default); // the app shares its options through DI
        services.AddSignalR().AddMessagePackProtocol();
        using var provider = services.BuildServiceProvider();

        var protocol = Assert.Single(provider.GetServices<IHubProtocol>().OfType<MessagePackHubProtocol>());
        Assert.Equal("messagepack", protocol.Name);
    }

    [Fact]
    public void AddMessagePackProtocol_Twice_RegistersOnce()
    {
        var services = new ServiceCollection();
        services.AddSignalR().AddMessagePackProtocol().AddMessagePackProtocol(static _ => { });
        using var provider = services.BuildServiceProvider();

        Assert.Single(provider.GetServices<IHubProtocol>().OfType<MessagePackHubProtocol>());
    }
}
