using System.Reflection;
using System.Runtime.ExceptionServices;

namespace MessagePack;

/// <summary>
/// Reflection invocation whose target's exception surfaces as itself. <c>MethodBase.Invoke</c> wraps whatever the
/// invoked member throws in a <see cref="TargetInvocationException"/>, so the resolver's
/// <see cref="MessagePackSerializationException"/> for a formatter that cannot be built (an object or Typeless slot
/// closing a formatter over the runtime type, a union case, a reflection member slot) and a user constructor's own
/// exception reached the caller wrapped, where the typed entry points throw them directly.
/// <c>BindingFlags.DoNotWrapExceptions</c> would do the same, but netstandard2.0 lacks it and .NET Framework ignores it.
/// </summary>
static class ReflectionInvoke
{
    public static object? InvokeUnwrapped(this MethodInfo method, object? target, object?[]? arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw; // not reached: Throw does not return
        }
    }

    public static object InvokeUnwrapped(this ConstructorInfo constructor, object?[]? arguments)
    {
        try
        {
            return constructor.Invoke(arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw; // not reached: Throw does not return
        }
    }
}
