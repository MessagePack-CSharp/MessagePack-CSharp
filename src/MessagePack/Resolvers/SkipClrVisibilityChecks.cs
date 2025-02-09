// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Microsoft Corporation. All rights reserved.

/*
 * This class was originally copied from https://github.com/AArnott/vs-mef/blob/master/src/Microsoft.VisualStudio.Composition/Reflection/SkipClrVisibilityChecks.cs
 */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MessagePack;

/// <summary>
/// Gives a dynamic assembly the ability to skip CLR visibility checks,
/// allowing the assembly to access private members of another assembly.
/// </summary>
internal class SkipClrVisibilityChecks
{
    /// <summary>
    /// The <see cref="Attribute.Attribute()"/> constructor.
    /// </summary>
    private static readonly ConstructorInfo AttributeBaseClassCtor = typeof(Attribute).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(ctor => ctor.GetParameters().Length == 0);

    /// <summary>
    /// The <see cref="AttributeUsageAttribute(AttributeTargets)"/> constructor.
    /// </summary>
    private static readonly ConstructorInfo AttributeUsageCtor = typeof(AttributeUsageAttribute).GetConstructor([typeof(AttributeTargets)])!;

    /// <summary>
    /// The <see cref="AttributeUsageAttribute.AllowMultiple"/> property.
    /// </summary>
    private static readonly PropertyInfo AttributeUsageAllowMultipleProperty = typeof(AttributeUsageAttribute).GetProperty(nameof(AttributeUsageAttribute.AllowMultiple))!;

    /// <summary>
    /// The assembly builder that is constructing the dynamic assembly.
    /// </summary>
    private readonly AssemblyBuilder assemblyBuilder;

    /// <summary>
    /// The module builder for the default module of the <see cref="assemblyBuilder"/>.
    /// This is where the special attribute will be defined.
    /// </summary>
    private readonly ModuleBuilder moduleBuilder;

    /// <summary>
    /// The set of assemblies that already have visibility checks skipped for.
    /// </summary>
    private readonly HashSet<string> attributedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The constructor on the special attribute to reference for each skipped assembly.
    /// </summary>
    private ConstructorInfo? magicAttributeCtor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipClrVisibilityChecks"/> class.
    /// </summary>
    /// <param name="assemblyBuilder">The builder for the dynamic assembly.</param>
    /// <param name="moduleBuilder">The builder for the default module defined by <see cref="assemblyBuilder"/>.</param>
    internal SkipClrVisibilityChecks(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder)
    {
        this.assemblyBuilder = assemblyBuilder;
        this.moduleBuilder = moduleBuilder;
    }

    internal static readonly ImmutableHashSet<AssemblyName> EmptySet = ImmutableHashSet.Create<AssemblyName>(AssemblyNameEqualityComparer.Instance);

    /// <summary>
    /// Scans a given type for references to non-public types and adds any assemblies that declare those types
    /// to a given set.
    /// </summary>
    /// <param name="typeInfo">The type which may be internal.</param>
    /// <param name="referencedAssemblies">The set of assemblies to add to where non-public types are found.</param>
    internal static void GetSkipVisibilityChecksRequirements(TypeInfo typeInfo, ImmutableHashSet<AssemblyName>.Builder referencedAssemblies)
    {
        if (typeInfo.IsArray)
        {
            GetSkipVisibilityChecksRequirements(typeInfo.GetElementType()!.GetTypeInfo(), referencedAssemblies);
        }

        AddTypeIfNonPublic(typeInfo);

        foreach (Type arg in typeInfo.GenericTypeArguments)
        {
            AddTypeIfNonPublic(arg);
        }

        // We must walk each base type individually to ensure we don't miss any private members,
        // since even with BindingFlags.NonPublic, GetMembers will not return from base types.
        for (TypeInfo? target = typeInfo; target is not null; target = target.BaseType?.GetTypeInfo())
        {
            ScanDirectType(target);
        }

        void ScanDirectType(TypeInfo typeInfo)
        {
            foreach (MemberInfo member in typeInfo.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                switch (member)
                {
                    case FieldInfo field:
                        if (!field.IsPublic)
                        {
                            referencedAssemblies.Add(typeInfo.Assembly.GetName());
                        }

                        AddTypeIfNonPublic(field.FieldType);
                        break;
                    case PropertyInfo property:
                        if (property.SetMethod?.IsPublic is false || property.GetMethod?.IsPublic is false)
                        {
                            referencedAssemblies.Add(typeInfo.Assembly.GetName());
                        }

                        AddTypeIfNonPublic(property.PropertyType);
                        break;
                    case ConstructorInfo constructorInfo:
                        if (!constructorInfo.IsPublic)
                        {
                            referencedAssemblies.Add(typeInfo.Assembly.GetName());
                        }

                        foreach (ParameterInfo parameter in constructorInfo.GetParameters())
                        {
                            AddTypeIfNonPublic(parameter.ParameterType);
                        }

                        break;
                }
            }
        }

        void AddTypeIfNonPublic(Type type)
        {
            if (type.IsNotPublic || !(type.IsPublic || type.IsNestedPublic))
            {
                referencedAssemblies.Add(type.Assembly.GetName());
            }

            foreach (Type typeArg in type.GenericTypeArguments)
            {
                AddTypeIfNonPublic(typeArg);
            }
        }
    }

    /// <summary>
    /// Add attributes to a dynamic assembly so that the CLR will skip visibility checks
    /// for the assemblies with the specified names.
    /// </summary>
    /// <param name="assemblyNames">The names of the assemblies to skip visibility checks for.</param>
    internal void SkipVisibilityChecksFor(IEnumerable<AssemblyName> assemblyNames)
    {
        foreach (var assemblyName in assemblyNames)
        {
            this.SkipVisibilityChecksFor(assemblyName);
        }
    }

    /// <summary>
    /// Add an attribute to a dynamic assembly so that the CLR will skip visibility checks
    /// for the assembly with the specified name.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to skip visibility checks for.</param>
    internal void SkipVisibilityChecksFor(AssemblyName assemblyName)
    {
        string? assemblyNameArg = assemblyName.Name;
        if (assemblyNameArg is not null && this.attributedAssemblyNames.Add(assemblyNameArg))
        {
            var cab = new CustomAttributeBuilder(this.GetMagicAttributeCtor(), [assemblyNameArg]);
            this.assemblyBuilder.SetCustomAttribute(cab);
        }
    }

    /// <summary>
    /// Gets the constructor to the IgnoresAccessChecksToAttribute, generating the attribute if necessary.
    /// </summary>
    /// <returns>The constructor to the IgnoresAccessChecksToAttribute.</returns>
    private ConstructorInfo GetMagicAttributeCtor()
    {
        if (this.magicAttributeCtor == null)
        {
            TypeInfo magicAttribute = this.EmitMagicAttribute();
            this.magicAttributeCtor = magicAttribute.GetConstructor([typeof(string)])!;
        }

        return this.magicAttributeCtor;
    }

    /// <summary>
    /// Defines the special IgnoresAccessChecksToAttribute type in the <see cref="moduleBuilder"/>.
    /// </summary>
    /// <returns>The generated attribute type.</returns>
    private TypeInfo EmitMagicAttribute()
    {
        TypeBuilder tb = this.moduleBuilder.DefineType(
            "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
            TypeAttributes.NotPublic,
            typeof(Attribute));

        CustomAttributeBuilder attributeUsage = new(
            AttributeUsageCtor,
            new object[] { AttributeTargets.Assembly },
            new PropertyInfo[] { AttributeUsageAllowMultipleProperty },
            new object[] { false });
        tb.SetCustomAttribute(attributeUsage);

        ConstructorBuilder cb = tb.DefineConstructor(
            MethodAttributes.Public |
            MethodAttributes.HideBySig |
            MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName,
            CallingConventions.Standard,
            new Type[] { typeof(string) });
        cb.DefineParameter(1, ParameterAttributes.None, "assemblyName");

        ILGenerator il = cb.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, AttributeBaseClassCtor);
        il.Emit(OpCodes.Ret);

        return tb.CreateTypeInfo()!;
    }

    private class AssemblyNameEqualityComparer : IEqualityComparer<AssemblyName>
    {
        internal static readonly AssemblyNameEqualityComparer Instance = new();

        private AssemblyNameEqualityComparer()
        {
        }

        public bool Equals(AssemblyName? x, AssemblyName? y)
        {
            if (x is null || y is null)
            {
                return x == y;
            }

            return string.Equals(x.FullName, y.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode([DisallowNull] AssemblyName obj) => obj.FullName?.GetHashCode() ?? 0;
    }
}
