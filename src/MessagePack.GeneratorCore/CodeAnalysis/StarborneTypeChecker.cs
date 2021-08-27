using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MessagePackCompiler.CodeAnalysis
{
    public static class StarborneTypeChecker
    {
        public const string AssemblyNameForStarborne = "Starborne";
        public const string MessagePackAttributeName = "MessagePackObjectAttribute";
        public const string ControllerProducesMessagePackAttributeName = "ProducesAttribute";

        public static bool IsSignalRClass(INamedTypeSymbol? named)
        {
            if (named == null)
            {
                return false;
            }

            if (named.Name == "Hub" || named.Name == "IHubClient")
            {
                return true;
            }

            return IsSignalRClass(named.BaseType) || named.Interfaces.Any(IsSignalRClass);
        }

        public static bool IsController(INamedTypeSymbol? named)
        {
            if (named == null)
            {
                return false;
            }

            if (named.Name == "ControllerBase")
            {
                return true;
            }

            return IsController(named.BaseType);
        }

        public static bool IsMessagePackMethod(IMethodSymbol method)
        {
            if (method == null || method.MethodKind != MethodKind.Ordinary)
            {
                return false;
            }

            if (IsSignalRClass(method.ContainingType))
            {
                return method.MethodKind != MethodKind.Constructor;
            }
            else if (IsController(method.ContainingType))
            {
                // [Produces("application/x-msgpack")]
                // Task SomeControllerMethod(SomeArgClass asd) => ...
                return method.GetAttributes().Any(x =>
                    x.AttributeClass?.Name == ControllerProducesMessagePackAttributeName
                    && x.ConstructorArguments.FirstOrDefault().Value is "application/x-msgpack");
            }

            return false;
        }
    }
}
