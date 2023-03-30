using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;

namespace HarmonyExtension;

public static class Helpers {
    //Todo: something better than hardcode: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types?redirectedfrom=MSDN
    /// <summary>
    /// Map Types to the way they would appear in C#
    /// </summary>
    public static string GetFriendlyName(this ISymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);   
    //      => {
    //    symbol.Name switch {        
    //    "Boolean" => "bool",
    //    "Byte" => "byte",
    //    "SByte" => "sbyte",
    //    "Char" => "char",
    //    "Decimal" => "decimal",
    //    "Double" => "double",
    //    "Single" => "float",
    //    "Int32" => "int",
    //    "UInt32" => "uint",
    //    "IntPtr" => "nint",
    //    "UIntPtr" => "nuint",
    //    "Int64" => "long",
    //    "UInt64" => "ulong",
    //    "Int16" => "short",
    //    "UInt16" => "ushort",
    //    "String" => "string",
    //    "Object" => "object",
    //    _ => symbol.Name,
    //};

    public static string GetHarmonyName(this IMethodSymbol symbol) => symbol switch {
        _ when symbol.IsGetter() => symbol.Name.Substring(4),
        _ when symbol.IsSetter() => symbol.Name.Substring(4),
        _ when symbol.IsConstructor() => symbol.ContainingType.Name, //+ "Ctor",
        _ => symbol.Name,
    };

    public static string GetFullNamespace(this INamedTypeSymbol typeSymbol) {
        INamespaceSymbol nsSym = typeSymbol.ContainingNamespace;
        var sb = new StringBuilder();
        while (nsSym?.IsGlobalNamespace == false) {
            if (sb.Length == 0)
                sb.Append(nsSym.Name);
            else
                sb.Insert(0, '.').Insert(0, nsSym.Name);
            nsSym = nsSym.ContainingNamespace;
        }

        return sb.ToString();
    }

    public static bool IsConstructor(this IMethodSymbol symbol) => symbol.Name.StartsWith(".ctor");
    public static bool IsSetter(this IMethodSymbol symbol) => symbol.Name.StartsWith("set_");
    public static bool IsGetter(this IMethodSymbol symbol) => symbol.Name.StartsWith("get_");
    public static bool IsProperty(this IMethodSymbol symbol) => symbol.IsSetter() || symbol.IsGetter();
}
