using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MessagePack.AspNetCoreMvcFormatter
{
    /// <summary>
    ///     Formatting options setup.
    /// </summary>
    internal class MessagePackFormatterMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly MessagePackFormatterOptions _messagePackFormatterOptions;

        /// <inheritdoc />
        public MessagePackFormatterMvcOptionsSetup(MessagePackFormatterOptions messagePackFormatterOptions)
        {
            _messagePackFormatterOptions = messagePackFormatterOptions ??
                throw new ArgumentNullException(nameof(messagePackFormatterOptions));
        }

        /// <inheritdoc />
        public void Configure(MvcOptions options)
        {
            options.InputFormatters.Add(new MessagePackInputFormatter(_messagePackFormatterOptions.FormatterResolver));
            options.OutputFormatters.Add(
                new MessagePackOutputFormatter(_messagePackFormatterOptions.FormatterResolver));
        }
    }
}