using MessagePack.Formatters;
using MessagePack.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections.ObjectModel;

namespace MessagePack.Resolvers
{
    public class DynamicGenericResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DynamicGenericResolver();

        DynamicGenericResolver()
        {

        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IMessagePackFormatter<T>)DynamicGenericResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class DynamicGenericResolverGetFormatterHelper
    {
        // Reduce IL2CPP code generate size(don't write long code in <T>)
        internal static object GetFormatter(Type t)
        {
            var ti = t.GetTypeInfo();

            if (t.IsArray)
            {
                // byte[] is including BuitinResolver
                return Activator.CreateInstance(typeof(ArrayFormatter<>).MakeGenericType(t.GetElementType()));
            }
            else if (ti.IsGenericType)
            {
                var genericType = ti.GetGenericTypeDefinition();
                var genericTypeInfo = genericType.GetTypeInfo();
                var isNullable = genericTypeInfo.IsNullable();
                var nullableElementType = isNullable ? ti.GenericTypeArguments[0] : null;

                if (genericType == typeof(KeyValuePair<,>))
                {
                    return CreateInstance(typeof(KeyValuePairFormatter<,>), ti.GenericTypeArguments);
                }
                else if (isNullable && nullableElementType.IsConstructedGenericType && nullableElementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                }

                // Tuple
                else if (ti.FullName.StartsWith("System.Tuple"))
                {
                    Type tupleFormatterType = null;
                    switch (ti.GenericTypeArguments.Length)
                    {
                        case 1:
                            tupleFormatterType = typeof(TupleFormatter<>);
                            break;
                        case 2:
                            tupleFormatterType = typeof(TupleFormatter<,>);
                            break;
                        case 3:
                            tupleFormatterType = typeof(TupleFormatter<,,>);
                            break;
                        case 4:
                            tupleFormatterType = typeof(TupleFormatter<,,,>);
                            break;
                        case 5:
                            tupleFormatterType = typeof(TupleFormatter<,,,,>);
                            break;
                        case 6:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,>);
                            break;
                        case 7:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,>);
                            break;
                        case 8:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,,>);
                            break;
                        default:
                            break;
                    }

                    return CreateInstance(tupleFormatterType, new[] { t });
                }

                // ArraySegement
                else if (genericType == typeof(ArraySegment<>))
                {
                    if (ti.GenericTypeArguments[0] == typeof(byte))
                    {
                        return new ByteArraySegmentFormatter();
                    }
                    else
                    {
                        return CreateInstance(typeof(ArraySegmentFormatter<>), ti.GenericTypeArguments);
                    }
                }

                // Standard Collections
                else if (genericType == typeof(List<>))
                {
                    return CreateInstance(typeof(ListFormatter<>), ti.GenericTypeArguments);
                }
                else if (genericType == typeof(LinkedList<>))
                {
                    return CreateInstance(typeof(LinkedListFormatter<>), ti.GenericTypeArguments);
                }
                else if (genericType == typeof(Stack<>))
                {
                    return CreateInstance(typeof(StackFormatter<>), ti.GenericTypeArguments);
                }
                else if (genericType == typeof(Queue<>))
                {
                    return CreateInstance(typeof(QeueueFormatter<>), ti.GenericTypeArguments);
                }
                else if (genericType == typeof(HashSet<>))
                {
                    return CreateInstance(typeof(HashSetFormatter<>), ti.GenericTypeArguments);
                }
                else if (genericType == typeof(ObservableCollection<>))
                {
                    return CreateInstance(typeof(ObservableCollectionFormatter<>), ti.GenericTypeArguments);
                }
                else if (genericType == typeof(ReadOnlyObservableCollection<>))
                {
                    return CreateInstance(typeof(ReadOnlyObservableCollectionFormatter<>), ti.GenericTypeArguments);
                }

                // Interface Collection
                else if (genericType == typeof(IList<>))
                {
                    return CreateInstance(typeof(InterfaceListFormatter<>), ti.GenericTypeArguments);
                }

                // TODO:other genericcollection...(ICollection and new)

                // TODO:Dictionary
                else if (genericType == typeof(Dictionary<,>))
                {
                    return CreateInstance(typeof(DictionaryFormatter<,>), ti.GenericTypeArguments);
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    return CreateInstance(typeof(InterfaceDictionaryFormatter<,>), ti.GenericTypeArguments);
                }
            }

            return null;
        }

        static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
        }
    }
}