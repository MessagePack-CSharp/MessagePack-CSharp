using System.Net;
using System.Net.Http.Headers;
using MessagePack;
using MessagePack.AspNetCoreMvcFormatter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessagePack.Tests.AspNetCore;

// MessagePack.AspNetCoreMvcFormatter through a real MVC pipeline on TestServer: content negotiation in both
// directions, the null-to-nil rule, the ?format= mapping, and a malformed body turning into a 400 instead of a 500.
public class MvcFormatterTests : IAsyncLifetime
{
    IHost host = default!;
    HttpClient client = default!;

    public async ValueTask InitializeAsync()
    {
        host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers(mvc =>
                    {
                        // MVC answers a null result with 204 before any formatter runs; drop that formatter so the
                        // MessagePack formatter's own nil path is what the null test exercises
                        mvc.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.HttpNoContentOutputFormatter>();
                    }).AddApplicationPart(typeof(EchoController).Assembly).AddMessagePackFormatters();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .StartAsync();
        client = host.GetTestServer().CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await host.StopAsync();
        host.Dispose();
    }

    static readonly Person Alice = new() { Name = "Alice", Age = 30, Tags = ["admin", "ops"] };

    [Fact]
    public async Task Post_MsgPackIn_MsgPackOut()
    {
        using var content = new ByteArrayContent(MessagePackSerializer.Serialize(Alice));
        content.Headers.ContentType = new MediaTypeHeaderValue(MessagePackMediaTypes.XMsgPack);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/echo") { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MessagePackMediaTypes.XMsgPack));
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MessagePackMediaTypes.XMsgPack, response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(MessagePackSerializer.Serialize(Alice), body); // byte-identical echo: the generated formatter on both sides
    }

    [Fact]
    public async Task Post_MsgPackIn_JsonOut_NegotiatesPerAcceptHeader()
    {
        using var content = new ByteArrayContent(MessagePackSerializer.Serialize(Alice));
        content.Headers.ContentType = new MediaTypeHeaderValue(MessagePackMediaTypes.MsgPack); // the unprefixed spelling is accepted too
        using var request = new HttpRequestMessage(HttpMethod.Post, "/echo") { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("\"name\":\"Alice\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FormatQuery_SelectsMsgPack()
    {
        using var response = await client.GetAsync("/alice?format=msgpack");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MessagePackMediaTypes.XMsgPack, response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(MessagePackSerializer.Serialize(Alice), await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task NullResult_IsNil()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/nobody");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MessagePackMediaTypes.XMsgPack));
        using var response = await client.SendAsync(request);
        // with HttpNoContentOutputFormatter removed in the host setup, a null result reaches the formatter and is nil
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MessagePackMediaTypes.XMsgPack, response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(new byte[] { 0xc0 }, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task MalformedBody_IsBadRequest()
    {
        using var content = new ByteArrayContent([0xc1, 0xc1, 0xc1]); // 0xc1 is the never-used code
        content.Headers.ContentType = new MediaTypeHeaderValue(MessagePackMediaTypes.XMsgPack);
        using var response = await client.PostAsync("/echo", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_DeclaredTypeWins_OverRuntimeType()
    {
        // the action declares Person but returns a derived instance: the declared type's formatter is used, as in v3
        using var request = new HttpRequestMessage(HttpMethod.Get, "/derived");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MessagePackMediaTypes.XMsgPack));
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(MessagePackSerializer.Serialize<Person>(new Employee { Name = "Bob", Age = 41, Tags = [], Title = "CTO" }), body);
        Assert.Equal(0x93, body[0]); // Person's three keys, not Employee's four
    }
}

[MessagePackObject]
public partial class Person
{
    [Key(0)] public string? Name { get; set; }
    [Key(1)] public int Age { get; set; }
    [Key(2)] public string[]? Tags { get; set; }
}

[MessagePackObject]
public partial class Employee : Person
{
    [Key(3)] public string? Title { get; set; }
}

[ApiController]
public class EchoController : ControllerBase
{
    static readonly Person Alice = new() { Name = "Alice", Age = 30, Tags = ["admin", "ops"] };

    [HttpPost("/echo")]
    public Person Echo([FromBody] Person person) => person;

    [HttpGet("/alice")]
    [FormatFilter]
    public Person GetAlice() => Alice;

    [HttpGet("/nobody")]
    public Person? Nobody() => null;

    [HttpGet("/derived")]
    public Person Derived() => new Employee { Name = "Bob", Age = 41, Tags = [], Title = "CTO" };
}
