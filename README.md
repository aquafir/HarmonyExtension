# HarmonyExtension
This is a Visual Studio extension that adds commands for copying a Harmony patch implementation to clipboard an IMethod or IProperty symbol.

### Todo

* Templating

  * Variables available for templating:

    * Usage: `$varToInsert` -  *Pre**$methodName**A*
    * `methodName`
    * `typeName`
    * `originalMethodName` - Method name, using *nameof* if accessible and preferred in options: *nameof(Type.MethodName) | "PrivateMethodName"*
    * `methodDeclarationName` - Default naming of a created method that uses patch and method type: *PreGet_Method* | *PostMethod*
    * `harmonyParamSignature` - Type array: *new Type[] { typeof(bool) }*
    * `returnType` - *void* | *bool*

  * Manual

    * Standard template:
      *var original = AccessTools.Method(typeof({{typeName}}), {{originalMethodName}});*
      *var patchMethod = AccessTools.Method(typeof(PatchClass), nameof(PatchClass.{{methodDeclarationName}}), {{harmonyParamSignature}});*
      *var patch = new HarmonyMethod(patchMethod);*
      *harmony.Patch(original, {{ (options.Postfix ? "postfix" : "prefix")}}: patch);*

  * Annotations

    * Pre template:
      ***Annotations***
      ***Method declaration*** {

      *//Return false to override*
      *//return false;*

      *//Return true to execute original*
      *return true;*

      }

    * Post template:
      ***Annotations***
      ***Method declaration*** {

      *//Your code here*

      }

  * Add methods for pasting 

* [Visual Studio Extension](https://learn.microsoft.com/en-us/visualstudio/extensibility/vsix/get-started/get-started-guide?view=vs-2022)

  * ~~Options menu~~
  * Submenu for commands
  * Context

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
