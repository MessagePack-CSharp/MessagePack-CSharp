using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace MessagePack.AspNetCoreMvcFormatter;

/// <summary>The media types the formatters accept and produce.</summary>
public static class MessagePackMediaTypes
{
    /// <summary>The conventional MessagePack media type, and the one v3 used.</summary>
    public const string XMsgPack = "application/x-msgpack";

    /// <summary>The unprefixed spelling some clients send.</summary>
    public const string MsgPack = "application/msgpack";

    /// <summary>The format name for <c>?format=msgpack</c> and <c>[FormatFilter]</c> mappings.</summary>
    public const string FormatName = "msgpack";

    // The formatters route through the serializer's Type-based entries, which are AOT-safe for the types generated code
    // registers (generated types, harvested closures, [MessagePackSerializable] roots) and the built-in scalars. The
    // reflection tier is only reached through MessagePackSerializerOptions.Default, so the constructors that fall back
    // to it carry the Requires* attributes. MVC itself is not supported on Native AOT; the annotations here only say
    // that this package adds no warnings of its own.
    internal const string DefaultOptionsRequiresDynamicCode = "MessagePackSerializerOptions.Default closes generic formatters at runtime; construct the formatter with options built on MessagePackFormatterFactory.DefaultAot for Native AOT.";
    internal const string DefaultOptionsRequiresUnreferencedCode = "MessagePackSerializerOptions.Default ends in a reflection tier; construct the formatter with options built on an explicit chain for trimmed applications.";
}

/// <summary>Reads a request body of <c>application/x-msgpack</c> into the action's model type.</summary>
public sealed class MessagePackInputFormatter : InputFormatter
{
    readonly MessagePackSerializerOptions options;

    /// <summary>A formatter on <see cref="MessagePackSerializerOptions.Default"/>.</summary>
    [RequiresDynamicCode(MessagePackMediaTypes.DefaultOptionsRequiresDynamicCode)]
    [RequiresUnreferencedCode(MessagePackMediaTypes.DefaultOptionsRequiresUnreferencedCode)]
    public MessagePackInputFormatter()
        : this(MessagePackSerializerOptions.Default)
    {
    }

    /// <param name="options">Serializer options for the request bodies.</param>
    public MessagePackInputFormatter(MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options;
        SupportedMediaTypes.Add(MessagePackMediaTypes.XMsgPack);
        SupportedMediaTypes.Add(MessagePackMediaTypes.MsgPack);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.HttpContext.Request;
        try
        {
            // one value, straight off the request pipe; bytes after it stay unread like any other formatter's trailing data
            var model = await MessagePackSerializer.DeserializeAsync(context.ModelType, request.BodyReader, options, context.HttpContext.RequestAborted).ConfigureAwait(false);
            return await InputFormatterResult.SuccessAsync(model).ConfigureAwait(false);
        }
        catch (MessagePackSerializationException exception)
        {
            context.ModelState.TryAddModelError(context.ModelName, exception, context.Metadata);
            return await InputFormatterResult.FailureAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>Writes the action result as <c>application/x-msgpack</c>; a null object is written as nil.</summary>
public sealed class MessagePackOutputFormatter : OutputFormatter
{
    readonly MessagePackSerializerOptions options;

    /// <summary>A formatter on <see cref="MessagePackSerializerOptions.Default"/>.</summary>
    [RequiresDynamicCode(MessagePackMediaTypes.DefaultOptionsRequiresDynamicCode)]
    [RequiresUnreferencedCode(MessagePackMediaTypes.DefaultOptionsRequiresUnreferencedCode)]
    public MessagePackOutputFormatter()
        : this(MessagePackSerializerOptions.Default)
    {
    }

    /// <param name="options">Serializer options for the response bodies.</param>
    public MessagePackOutputFormatter(MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options;
        SupportedMediaTypes.Add(MessagePackMediaTypes.XMsgPack);
        SupportedMediaTypes.Add(MessagePackMediaTypes.MsgPack);
    }

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var writer = context.HttpContext.Response.BodyWriter;
        if (context.Object == null)
        {
            var span = writer.GetSpan(1);
            span[0] = MessagePackCode.Nil;
            writer.Advance(1);
        }
        else
        {
            // the declared type wins over the runtime type unless it says nothing (object), as in v3
            var objectType = context.ObjectType == null || context.ObjectType == typeof(object) ? context.Object.GetType() : context.ObjectType;
            MessagePackSerializer.Serialize(objectType, writer, context.Object, options);
        }
        return writer.FlushAsync(context.HttpContext.RequestAborted).AsTask();
    }
}

/// <summary>Registers the MessagePack formatters with MVC.</summary>
public static class MessagePackMvcBuilderExtensions
{
    /// <summary>Adds the input and output formatters on <see cref="MessagePackSerializerOptions.Default"/> and the <c>msgpack</c> format mapping.</summary>
    [RequiresDynamicCode(MessagePackMediaTypes.DefaultOptionsRequiresDynamicCode)]
    [RequiresUnreferencedCode(MessagePackMediaTypes.DefaultOptionsRequiresUnreferencedCode)]
    public static IMvcBuilder AddMessagePackFormatters(this IMvcBuilder builder)
        => builder.AddMessagePackFormatters(MessagePackSerializerOptions.Default);

    /// <summary>Adds the input and output formatters on <paramref name="options"/> and the <c>msgpack</c> format mapping.</summary>
    public static IMvcBuilder AddMessagePackFormatters(this IMvcBuilder builder, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        builder.Services.Configure<MvcOptions>(mvc => mvc.AddMessagePackFormatters(options));
        return builder;
    }

    /// <inheritdoc cref="AddMessagePackFormatters(IMvcBuilder)"/>
    [RequiresDynamicCode(MessagePackMediaTypes.DefaultOptionsRequiresDynamicCode)]
    [RequiresUnreferencedCode(MessagePackMediaTypes.DefaultOptionsRequiresUnreferencedCode)]
    public static IMvcCoreBuilder AddMessagePackFormatters(this IMvcCoreBuilder builder)
        => builder.AddMessagePackFormatters(MessagePackSerializerOptions.Default);

    /// <inheritdoc cref="AddMessagePackFormatters(IMvcBuilder, MessagePackSerializerOptions)"/>
    public static IMvcCoreBuilder AddMessagePackFormatters(this IMvcCoreBuilder builder, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        builder.Services.Configure<MvcOptions>(mvc => mvc.AddMessagePackFormatters(options));
        return builder;
    }

    /// <summary>Adds the formatters to <paramref name="mvc"/> directly, for callers configuring <see cref="MvcOptions"/> themselves.</summary>
    public static void AddMessagePackFormatters(this MvcOptions mvc, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(mvc);
        ArgumentNullException.ThrowIfNull(options);
        mvc.InputFormatters.Add(new MessagePackInputFormatter(options));
        mvc.OutputFormatters.Add(new MessagePackOutputFormatter(options));
        mvc.FormatterMappings.SetMediaTypeMappingForFormat(MessagePackMediaTypes.FormatName, MessagePackMediaTypes.XMsgPack);
    }
}
