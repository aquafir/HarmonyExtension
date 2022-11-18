using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyExtension
{
    public static class Helpers
    {
        //Todo: something better than hardcode: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types?redirectedfrom=MSDN
        public static string FriendlyName(this ISymbol symbol) => symbol.Name switch
        {
            "Boolean" => "bool",
            "Byte" => "byte",
            "SByte" => "sbyte",
            "Char" => "char",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "Int32" => "int",
            "UInt32" => "uint",
            "IntPtr" => "nint",
            "UIntPtr" => "nuint",
            "Int64" => "long",
            "UInt64" => "ulong",
            "Int16" => "short",
            "UInt16" => "ushort",
            "String" => "string",
            "Object" => "object",
            _ => symbol.Name,
        };
    }
}
