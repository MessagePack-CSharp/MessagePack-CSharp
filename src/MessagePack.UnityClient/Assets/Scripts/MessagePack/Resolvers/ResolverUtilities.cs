// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using MessagePack.Formatters;

namespace MessagePack.Internal
{
    internal static class ResolverUtilities
    {
        internal static IMessagePackFormatter ActivateFormatter(Type formatterType, object[] args = null)
        {
            if (args == null || args.Length == 0)
            {
                if (formatterType.GetConstructor(Type.EmptyTypes) is ConstructorInfo ctor)
                {
                    return (IMessagePackFormatter)ctor.Invoke(Array.Empty<object>());
                }
                else if (FetchSingletonField(formatterType) is FieldInfo instance)
                {
                    return (IMessagePackFormatter)instance.GetValue(null);
                }
                else
                {
                    throw new MessagePackSerializationException($"The {formatterType.FullName} formatter has no default constructor nor implements the singleton pattern.");
                }
            }
            else
            {
                return (IMessagePackFormatter)Activator.CreateInstance(formatterType, args);
            }
        }

        internal static FieldInfo FetchSingletonField(Type formatterType)
        {
            if (formatterType.GetField("Instance", BindingFlags.Static | BindingFlags.Public) is FieldInfo fieldInfo && fieldInfo.IsInitOnly)
            {
                return fieldInfo;
            }

            return null;
        }
    }
}
