namespace SerializerFoundation.CodeAnalysis;

/// <summary>
/// Marks a virtual member that is conceptually abstract.
/// Every non-abstract derived type that can see the member must override it, which the bundled analyzer enforces as SF003.
/// Used where a true abstract member would break binary compatibility across target frameworks.
/// The attribute is not inherited: an override satisfies the requirement for everything below it,
/// and an intermediate override that should itself be overridden again applies the attribute anew.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RequireOverrideAttribute : Attribute
{
}
