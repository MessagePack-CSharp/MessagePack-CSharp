using MessagePack;
using MessagePack.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessagePack.Tests.AspNetCore;

// A hub on TestServer and a HubConnection client, both on the v4 protocol: invocations with generated and
// contractless arguments, results, server-to-client streaming, and client-to-server streaming. The client is wired
// through the same AddMessagePackProtocol extension (HubConnectionBuilder is an ISignalRBuilder).
public class SignalREndToEndTests : IAsyncLifetime
{
    IHost host = default!;
    HubConnection connection = default!;

    public async ValueTask InitializeAsync()
    {
        host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddSignalR().AddMessagePackProtocol())
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapHub<TestHub>("/hub"));
                }))
            .StartAsync();
        var server = host.GetTestServer();
        connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "hub"), options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .AddMessagePackProtocol()
            .Build();
        await connection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
        await host.StopAsync();
        host.Dispose();
    }

    [Fact]
    public async Task Invoke_GeneratedPoco_RoundTrips()
    {
        var alice = new Person { Name = "Alice", Age = 30, Tags = ["admin"] };
        var back = await connection.InvokeAsync<Person>("Echo", alice);
        Assert.Equal((alice.Name, alice.Age), (back.Name, back.Age));
        Assert.Equal(alice.Tags, back.Tags);
    }

    [Fact]
    public async Task Invoke_ContractlessDtoAndEnum_RoundTrips()
    {
        var back = await connection.InvokeAsync<PlainDto>("EchoPlain", new PlainDto { Id = 9, Level = Level.High });
        Assert.Equal((9, Level.High), (back.Id, back.Level));
    }

    [Fact]
    public async Task Invoke_Primitives_AndVoid()
    {
        Assert.Equal(7, await connection.InvokeAsync<int>("Add", 3, 4));
        Assert.Equal("hello, v4", await connection.InvokeAsync<string>("Greet", "v4"));
        await connection.InvokeAsync("Nop");
        Assert.Null(await connection.InvokeAsync<Person?>("Nobody"));
    }

    [Fact]
    public async Task Invoke_HubException_SurfacesAsError()
    {
        var exception = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Fail"));
        Assert.Contains("boom", exception.Message);
    }

    [Fact]
    public async Task ServerToClient_Streaming()
    {
        var items = new List<Person>();
        await foreach (var person in connection.StreamAsync<Person>("People", 3))
        {
            items.Add(person);
        }
        Assert.Equal(["p0", "p1", "p2"], items.Select(p => p.Name));
    }

    [Fact]
    public async Task ClientToServer_Streaming()
    {
        static async IAsyncEnumerable<int> Numbers()
        {
            for (var i = 1; i <= 4; i++)
            {
                yield return i;
                await Task.Yield();
            }
        }
        Assert.Equal(10, await connection.InvokeAsync<int>("Sum", Numbers()));
    }

    [Fact]
    public async Task ServerPush_ClientHandler()
    {
        var received = new TaskCompletionSource<Person>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<Person>("Pushed", p => received.TrySetResult(p));
        await connection.InvokeAsync("PushBack", new Person { Name = "Carol", Age = 5, Tags = [] });
        var pushed = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Carol", pushed.Name);
    }
}

public class TestHub : Hub
{
    public Person Echo(Person person) => person;

    public PlainDto EchoPlain(PlainDto dto) => dto;

    public int Add(int a, int b) => a + b;

    public string Greet(string name) => "hello, " + name;

    public Task Nop() => Task.CompletedTask;

    public Person? Nobody() => null;

    public Task Fail() => throw new HubException("boom");

    public async IAsyncEnumerable<Person> People(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return new Person { Name = "p" + i, Age = i, Tags = [] };
            await Task.Yield();
        }
    }

    public async Task<int> Sum(IAsyncEnumerable<int> numbers)
    {
        var total = 0;
        await foreach (var n in numbers)
        {
            total += n;
        }
        return total;
    }

    public Task PushBack(Person person) => Clients.Caller.SendAsync("Pushed", person);
}
