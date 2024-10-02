// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using MessagePack.Formatters;

namespace MessagePack.Internal
{
    internal static class ResolverUtilities
    {
        internal static IMessagePackFormatter ActivateFormatter(Type formatterType, object?[]? args = null)
        {
            if (args == null || args.Length == 0)
            {
                if (FetchSingletonField(formatterType) is FieldInfo instance)
                {
                    return (IMessagePackFormatter)(instance.GetValue(null) ?? throw new InvalidOperationException($"{instance.ReflectedType?.FullName}.{instance.Name} return null."));
                }
                else if (formatterType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder: null, Type.EmptyTypes, Array.Empty<ParameterModifier>()) is ConstructorInfo ctor)
                {
                    return (IMessagePackFormatter)ctor.Invoke(Array.Empty<object>());
                }
                else
                {
                    throw new MessagePackSerializationException($"The {formatterType.FullName} formatter has no public default constructor nor implements the singleton pattern.");
                }
            }
            else
            {
                return (IMessagePackFormatter)Activator.CreateInstance(formatterType, args)!;
            }
        }

        internal static FieldInfo? FetchSingletonField(Type formatterType)
        {
            if (formatterType.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) is FieldInfo fieldInfo && fieldInfo.IsInitOnly)
            {
                return fieldInfo;
            }

            return null;
        }
    }
}
