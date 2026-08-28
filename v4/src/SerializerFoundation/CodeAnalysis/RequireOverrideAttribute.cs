// deliberately quarantined in SerializerFoundation.CodeAnalysis: this is analyzer support
// machinery (close to internal), not part of the buffer surface, and should not surface
// in completion next to the real API

namespace SerializerFoundation.CodeAnalysis;

/// <summary>
/// Marks a virtual member that is conceptually abstract.
/// Every non-abstract derived type that can see the member must override it, which the bundled analyzer enforces as SF003.
/// Used where a true abstract member would break binary compatibility across target frameworks.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RequireOverrideAttribute : Attribute
{
}
