using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace HarmonyExtension;

public static class TemplateHelpers
{
    /// <summary>
    /// Replaces all occurrences of a template variable with a substitution
    /// </summary>
    public static string Substitute(this string template, Dictionary<TemplateName, string> substitutions)
    {
        foreach (var s in substitutions)
        {
            template = Regex.Replace(template, $@"\${s.Key.ToString()}", s.Value, RegexOptions.IgnoreCase);
        }

        return template;
    }

    /// <summary>
    /// Returns a template string with default replacements
    /// </summary>
    public static string PreviewTemplate(this string template) => template.Substitute(templateDefaults);

    private static readonly Dictionary<TemplateName, string> templateDefaults = new()
    {
        [TemplateName.Annotations] = "[HarmonyPatch(typeof(TypeName), nameof(TypeName.MyMethod), MethodType.Getter), new Type[] { typeof(string) }]",
        [TemplateName.AnnotatedPatchType] = "[HarmonyPrefix]",
        [TemplateName.HarmonyMethodName] = "nameof(TypeName.MyMethod)",
        [TemplateName.HarmonyParamSignature] = "new Type[] { typeof(string) }",
        [TemplateName.HarmonyArgumentTypes] = "new ArgumentType[] { ArgumentType.Normal }",
        [TemplateName.ManualPatchType] = "prefix",
        [TemplateName.MethodDeclarationName] = "PrefixGetMyMethod",
        [TemplateName.MethodName] = "MyMethod",
        [TemplateName.MethodSignature] = "string foo, ref Bar __instance, ref bool __result",
        [TemplateName.PatchTarget] = "Get",
        [TemplateName.PatchType] = "Prefix",
        [TemplateName.ReturnType] = "bool",
        [TemplateName.TypeName] = "TypeName",
    };

    public const string ManualTemplateDefault = $$"""
            var originalMethod = AccessTools.Method(typeof($typeName), $methodName);
            var patchMethod = AccessTools.Method(typeof(PatchClass), nameof(PatchClass.$methodDeclarationName), $harmonyParamSignature);
            var patch = new HarmonyMethod(patchMethod);
            harmony.Patch(original, $manualPatchType: patch);


            public static $returnType  $methodDeclarationName($methodSignature) {
            //Return false to override
            //return false;

            //Return true to execute original
            //return true;
            }
            """;

    public const string AnnotatedTemplateDefault = $$"""
            $annotatedPatchType
            $annotations
            public static $returnType  $methodDeclarationName($methodSignature) {
            //Return false to override
            //return false;

            //Return true to execute original
            //return true;
            }
            """;
}

//Todo: decide if there should be a map of substitution type of matching template string
public enum TemplateName
{
    Annotations,            //Single-line/split annotations as determined by options
    AnnotatedPatchType,     //[HarmonyPrefix]
    HarmonyMethodName,      //nameof(TypeName.MyMethod) or "MyMethod"
    HarmonyParamSignature,  //new Type[] { ... }
    ManualPatchType,        //Variable used in Harmony.Patch: prefix|postfix
    MethodDeclarationName,  //Method name generated for harmony: patchType + patchTarget + methodName
    MethodName,             //MyMethod
    MethodSignature,        //Method signature with injections: string foo, ref Bar __instance
    PatchTarget,            //Ctor|Get|Set|<empty>
    PatchType,              //Prefix|Postfix
    ReturnType,             //void|bool
    TypeName,               //TypeName.MyMethod
    HarmonyArgumentTypes,
}