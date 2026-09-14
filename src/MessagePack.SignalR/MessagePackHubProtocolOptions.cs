using System.Diagnostics.CodeAnalysis;
using MessagePack.Formatters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MessagePack.SignalR;

/// <summary>Options for <see cref="MessagePackHubProtocol"/>.</summary>
public sealed class MessagePackHubProtocolOptions
{
    internal const string RequiresDynamicCodeMessage = "The default serializer options end in the contractless reflection tier, which closes generic formatters at runtime. Set SerializerOptions to options built on MessagePackFormatterFactory.DefaultAot for Native AOT.";
    internal const string RequiresUnreferencedCodeMessage = "The default serializer options end in the contractless reflection tier. Set SerializerOptions to options built on an explicit chain for trimmed applications.";

    MessagePackSerializerOptions? serializerOptions;

    /// <summary>
    /// The serializer options for arguments, results and stream items. The default matches the official protocol's
    /// defaults: attribute-less types as maps of member names (contractless), enums as their names, hash-flooding
    /// resistant collections.
    /// </summary>
    public MessagePackSerializerOptions SerializerOptions
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get => serializerOptions ??= CreateDefaultSerializerOptions();
        set => serializerOptions = value;
    }

    /// <summary>The default options: enum names first, then the default chain with the contractless tier appended.</summary>
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static MessagePackSerializerOptions CreateDefaultSerializerOptions()
        => new MessagePackSerializerOptions(new MessagePackFormatterResolver([new GenericEnumAsStringFormatterFactory(), MessagePackFormatterFactory.Default.WithContractless()]));
}

/// <summary>Registers the protocol with a SignalR server or client builder.</summary>
public static class MessagePackHubProtocolSignalRBuilderExtensions
{
    /// <summary>Adds the "messagepack" hub protocol on MessagePack v4 with the default serializer options.</summary>
    [RequiresDynamicCode(MessagePackHubProtocolOptions.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackHubProtocolOptions.RequiresUnreferencedCodeMessage)]
    public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder) where TBuilder : ISignalRBuilder
        => builder.AddMessagePackProtocol(static _ => { });

    /// <summary>Adds the "messagepack" hub protocol on MessagePack v4, with <paramref name="configure"/> applied to its options.</summary>
    [RequiresDynamicCode(MessagePackHubProtocolOptions.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackHubProtocolOptions.RequiresUnreferencedCodeMessage)]
    public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder, Action<MessagePackHubProtocolOptions> configure) where TBuilder : ISignalRBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        builder.Services.TryAddEnumerable(Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>());
        builder.Services.Configure(configure);
        return builder;
    }

    /// <summary>Adds the protocol over explicit serializer options, the overload for trimmed and AOT applications.</summary>
    public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder, MessagePackSerializerOptions serializerOptions) where TBuilder : ISignalRBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serializerOptions);
        builder.Services.TryAddEnumerable(Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<IHubProtocol>(new MessagePackHubProtocol(serializerOptions)));
        return builder;
    }
}
