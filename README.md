# HarmonyExtension
This is a Visual Studio extension that adds commands for copying a selected symbol as a Harmony patch, customized by the options.



* Templating

  * Insert a variable by using a `$` follow by its case-insensitive name: `$varToInsert`

  * Variables/examples available:

    * `Annotations`	[HarmonyPatch(typeof(TypeName), nameof(TypeName.MyMethod), MethodType.Getter), new Type[] { typeof(string) }]
    * `AnnotatedPatchType`	[HarmonyPrefix]
    * `HarmonyMethodName`	nameof(TypeName.MyMethod)
    * `HarmonyParamSignature`	new Type[] { typeof(string) }
    * `ManualPatchType`	prefix
    * `MethodDeclarationName`	PrefixGetMyMethod
    * `MethodName`	MyMethod
    * `MethodSignature`	string foo, ref Bar __instance, ref bool __result
    * `PatchTarget`	Get
    * `PatchType`	Prefix
    * `ReturnType`	bool
    * `TypeName`	TypeName

  * Default Manual Template

    > var originalMethod = AccessTools.Method(typeof(\$typeName), \$methodName);
    > var patchMethod = AccessTools.Method(typeof(PatchClass), nameof(PatchClass.\$methodDeclarationName), \$harmonyParamSignature);
    > var patch = new HarmonyMethod(patchMethod);
    > harmony.Patch(original, $manualPatchType: patch);
    >
    > 
    >
    > public static \$returnType  \$methodDeclarationName(\$methodSignature) {
    > //Return false to override
    > //return false;
    >
    > //Return true to execute original
    > //return true;
    > }

  * Default  Annotated Template

    > \$annotatedPatchType
    > \$annotations
    > public static \$returnType  \$methodDeclarationName(\$methodSignature) {
    > //Return false to override
    > //return false;
    >
    > //Return true to execute original
    > //return true;
    > }



### Todo

* Templating
  * Add support for context-aware insertions (e.g., template variables for destination Type)
  * Better xaml design
  * Conditionals / templates for each combination of:
    * Return type
    * Type
    * Target
    * Style
* [Visual Studio Extension](https://learn.microsoft.com/en-us/visualstudio/extensibility/vsix/get-started/get-started-guide?view=vs-2022)

  * ~~Options menu~~
  * Submenu
  * [Context](https://stackoverflow.com/questions/37819010/how-to-customize-the-context-menu-in-the-solution-explorer-for-a-specific-projec)
* Harmony support
  * ~~Manual patching~~
  * Cascading patches, if applicable
  * Add default values?
  * Annotations
    * [Categories](https://harmony.pardeike.net/articles/annotations.html#basic-annotations)
    * [Argument](https://harmony.pardeike.net/articles/annotations.html#basic-annotations)
    * [Generics](https://harmony.pardeike.net/articles/annotations.html#generic-methods)
  * [Priority](https://harmony.pardeike.net/articles/priorities.html)
  * Patch types
    * [Finalizers](https://harmony.pardeike.net/articles/patching-finalizer.html)
    * [Transpilers](https://harmony.pardeike.net/articles/patching-transpiler.html)
    * [Reverse patches](https://harmony.pardeike.net/articles/reverse-patching.html)
  * Type
    * Patch everything in a type
    * Inject fields from a type
