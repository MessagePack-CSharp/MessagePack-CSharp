using System;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MessagePack.AspNetCoreMvcFormatter
{
    /// <summary>
    ///     MVC congiguration helper.
    /// </summary>
    public static class MsgPackMvcCoreBuilderExtensions
    {
        /// <summary>
        ///     Add MsgPack (application/x-message) input and output formatters.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" /></exception>
        public static IMvcCoreBuilder AddMsgPackFormatters(this IMvcCoreBuilder builder,
            Action<MessagePackFormatterOptions> setup = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            var options = new MessagePackFormatterOptions
            {
                FormatterResolver = ContractlessStandardResolverAllowPrivate.Instance
            };

            setup?.Invoke(options);
            builder.Services.Add(ServiceDescriptor.Singleton(options));

            builder.Services.TryAddEnumerable(ServiceDescriptor
                .Transient<IConfigureOptions<MvcOptions>, MessagePackFormatterMvcOptionsSetup>());

            return builder;
        }
    }
}