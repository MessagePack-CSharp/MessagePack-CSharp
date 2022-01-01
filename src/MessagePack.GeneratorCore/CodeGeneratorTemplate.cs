// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using MessagePackCompiler.CodeAnalysis;
using MessagePackCompiler.Generator;
using Microsoft.CodeAnalysis;

namespace MessagePackCompiler;

public partial class CodeGenerator
{
    private async ValueTask GenerateMultipleFileAsync(string output, string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo)
    {
        string GetNamespace(INamespaceInfo x)
        {
            if (x.Namespace == null)
            {
                return namespaceDot + "Formatters";
            }

            return namespaceDot + "Formatters." + x.Namespace;
        }

#if NET6_0_OR_GREATER
        var objectTask = Parallel.ForEachAsync(objectInfo, cancellationToken, async (x, token) =>
        {
            var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
            ITemplate template;
            if (x.IsStringKey)
            {
                template = new StringKeyFormatterTemplate(ns, new[] { x }, cancellationToken);
            }
            else
            {
                template = new FormatterTemplate(ns, new[] { x }, cancellationToken);
            }

            await OutputToDirAsync(output, x.Name + "Formatter", template).ConfigureAwait(false);
        });
        var enumTask = Parallel.ForEachAsync(enumInfo, cancellationToken, async (x, token) =>
        {
            var template = new EnumTemplate(GetNamespace(x), new[] { x }, cancellationToken);
            await OutputToDirAsync(output, x.Name + "Formatter", template).ConfigureAwait(false);
        });
        var unionTask = Parallel.ForEachAsync(unionInfo, cancellationToken, async (x, token) =>
        {
            var template = new UnionTemplate(GetNamespace(x), new[] { x }, cancellationToken);
            await OutputToDirAsync(output, x.Name + "Formatter", template).ConfigureAwait(false);
        });

        var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray(), cancellationToken);
        await OutputToDirAsync(output, resolverTemplate.ResolverName, resolverTemplate).ConfigureAwait(false);
        var waitingTasks = new Task[3]
        {
            objectTask,
            enumTask,
            unionTask,
        };
#else
        var waitingTasks = new Task[objectInfo.Length + enumInfo.Length + unionInfo.Length + 1];
        var waitingIndex = 0;
        foreach (var x in objectInfo)
        {
            var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
            ITemplate template;
            if (x.IsStringKey)
            {
                template = new StringKeyFormatterTemplate(ns, new[] { x }, cancellationToken);
            }
            else
            {
                template = new FormatterTemplate(ns, new[] { x }, cancellationToken);
            }

            waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template).AsTask();
        }

        foreach (var x in enumInfo)
        {
            var template = new EnumTemplate(GetNamespace(x), new[] { x }, cancellationToken);
            waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template).AsTask();
        }

        foreach (var x in unionInfo)
        {
            var template = new UnionTemplate(GetNamespace(x), new[] { x }, cancellationToken);
            waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template).AsTask();
        }

        var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray(), cancellationToken);
        waitingTasks[waitingIndex] = OutputToDirAsync(output, resolverTemplate.ResolverName, resolverTemplate).AsTask();
#endif
        await Task.WhenAll(waitingTasks).ConfigureAwait(false);
    }

    private async ValueTask GenerateMultipleFileAsync(string output, string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo, string[] multioutSymbols)
    {
        string GetNamespace(INamespaceInfo x)
        {
            if (x.Namespace == null)
            {
                return namespaceDot + "Formatters";
            }

            return namespaceDot + "Formatters." + x.Namespace;
        }

#if NET6_0_OR_GREATER
        var objectTask = Parallel.ForEachAsync(objectInfo, cancellationToken, async (x, token) =>
        {
            var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
            ITemplate template;
            if (x.IsStringKey)
            {
                template = new StringKeyFormatterTemplate(ns, new[] { x }, cancellationToken);
            }
            else
            {
                template = new FormatterTemplate(ns, new[] { x }, cancellationToken);
            }

            await OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).ConfigureAwait(false);
        });
        var enumTask = Parallel.ForEachAsync(enumInfo, cancellationToken, async (x, token) =>
        {
            var template = new EnumTemplate(GetNamespace(x), new[] { x }, cancellationToken);
            await OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).ConfigureAwait(false);
        });
        var unionTask = Parallel.ForEachAsync(unionInfo, cancellationToken, async (x, token) =>
        {
            var template = new UnionTemplate(GetNamespace(x), new[] { x }, cancellationToken);
            await OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).ConfigureAwait(false);
        });

        var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray(), cancellationToken);
        await OutputToDirAsync(output, resolverTemplate.ResolverName, resolverTemplate, multioutSymbols).ConfigureAwait(false);
        var waitingTasks = new Task[3]
        {
            objectTask,
            enumTask,
            unionTask,
        };
#else
        var waitingTasks = new Task[objectInfo.Length + enumInfo.Length + unionInfo.Length + 1];
        var waitingIndex = 0;
        foreach (var x in objectInfo)
        {
            var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
            ITemplate template;
            if (x.IsStringKey)
            {
                template = new StringKeyFormatterTemplate(ns, new[] { x }, cancellationToken);
            }
            else
            {
                template = new FormatterTemplate(ns, new[] { x }, cancellationToken);
            }

            waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).AsTask();
        }

        foreach (var x in enumInfo)
        {
            var template = new EnumTemplate(GetNamespace(x), new[] { x }, cancellationToken);
            waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).AsTask();
        }

        foreach (var x in unionInfo)
        {
            var template = new UnionTemplate(GetNamespace(x), new[] { x }, cancellationToken);
            waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).AsTask();
        }

        var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray(), cancellationToken);
        waitingTasks[waitingIndex] = OutputToDirAsync(output, resolverTemplate.ResolverName, resolverTemplate, multioutSymbols).AsTask();
#endif
        await Task.WhenAll(waitingTasks).ConfigureAwait(false);
    }
}
