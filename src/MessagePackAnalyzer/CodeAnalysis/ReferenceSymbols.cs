// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace MessagePackAnalyzer.CodeAnalysis;

public record ReferenceSymbols(
    INamedTypeSymbol MessagePackObjectAttribute,
    INamedTypeSymbol UnionAttribute,
    INamedTypeSymbol SerializationConstructorAttribute,
    INamedTypeSymbol KeyAttribute,
    INamedTypeSymbol IgnoreAttribute,
    INamedTypeSymbol FormatterAttribute,
    INamedTypeSymbol MessagePackFormatter,
    INamedTypeSymbol? IgnoreDataMemberAttribute,
    INamedTypeSymbol IMessagePackSerializationCallbackReceiver)
{
    public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out ReferenceSymbols? instance)
    {
        instance = null;

        INamedTypeSymbol? messagePackObjectAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackObjectAttribute");
        if (messagePackObjectAttribute is null)
        {
            return false;
        }

        INamedTypeSymbol? unionAttribute = compilation.GetTypeByMetadataName("MessagePack.UnionAttribute");
        if (unionAttribute is null)
        {
            return false;
        }

        INamedTypeSymbol? serializationConstructor = compilation.GetTypeByMetadataName("MessagePack.SerializationConstructorAttribute");
        if (serializationConstructor is null)
        {
            return false;
        }

        INamedTypeSymbol? keyAttribute = compilation.GetTypeByMetadataName("MessagePack.KeyAttribute");
        if (keyAttribute is null)
        {
            return false;
        }

        INamedTypeSymbol? ignoreAttribute = compilation.GetTypeByMetadataName("MessagePack.IgnoreMemberAttribute");
        if (ignoreAttribute is null)
        {
            return false;
        }

        INamedTypeSymbol? formatterAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackFormatterAttribute");
        if (formatterAttribute is null)
        {
            return false;
        }

        INamedTypeSymbol? messageFormatter = compilation.GetTypeByMetadataName("MessagePack.Formatters.IMessagePackFormatter");
        if (messageFormatter is null)
        {
            return false;
        }

        INamedTypeSymbol? ignoreDataMemberAttribute = compilation.GetTypeByMetadataName("System.Runtime.Serialization.IgnoreDataMemberAttribute");

        INamedTypeSymbol? messagePackSerializationCallbackReceiver = compilation.GetTypeByMetadataName("MessagePack.IMessagePackSerializationCallbackReceiver");
        if (messagePackSerializationCallbackReceiver is null)
        {
            return false;
        }

        instance = new ReferenceSymbols(
            messagePackObjectAttribute,
            unionAttribute,
            serializationConstructor,
            keyAttribute,
            ignoreAttribute,
            formatterAttribute,
            messageFormatter,
            ignoreDataMemberAttribute,
            messagePackSerializationCallbackReceiver);
        return true;
    }
}
