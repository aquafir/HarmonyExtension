using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HarmonyExtension.Commands;

internal class HarmonyHandler
{
    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly Package _package;

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static HarmonyHandler Instance { get; private set; } = default!;

    /// <summary>
    /// Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    public static void Initialize(Package package) => Instance = new HarmonyHandler(package);

    /// <summary>
    /// Gets the service provider from the owner package.
    /// </summary>
    private IServiceProvider ServiceProvider => _package;

    /// <summary>
    /// The status bar
    /// </summary>
    private readonly IVsStatusbar _statusBar;

    /// <summary>
    /// COM
    /// </summary>
    private readonly IComponentModel _componentModel;

    /// <summary>
    /// The editor adapters factory
    /// </summary>
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;

    /// <summary>
    /// COM
    /// </summary>
    //private readonly IComponentModel _componentModel;

    /// <summary>
    /// EnvDTE service
    /// </summary>
    //private readonly DTE _dte;

    public HarmonyHandler(Package package)
    {
        try
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {

            }

            _statusBar = (IVsStatusbar)ServiceProvider.GetService(typeof(SVsStatusbar));
            Assumes.Present(_statusBar);

            _componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));
            Assumes.Present(_componentModel);

            _editorAdaptersFactory = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            Assumes.Present(_editorAdaptersFactory);

            //_dte = _componentModel.GetService<DTE>();
            //Assumes.Present(_dte);
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show($"Some error in Harmony extension.\n Please take screenshot and create issue on github with this error\n{ex}", "[HarmonyExtension] Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    public async Task Button_CopyAsHarmony(HarmonyOptions options)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        try
        {
            var textView = GetTextView();
            if (textView == null)
            {
                _statusBar.SetText("Can't get a text view, please open the file and execute the command while a document window is active.");
                return;
            }
            SnapshotPoint caretPosition = textView.Caret.Position.BufferPosition;

            Microsoft.CodeAnalysis.Document document = caretPosition.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                _statusBar.SetText("You should execute the command while a document window is active.");
                return;
            }

            SyntaxNode? rootSyntaxNode = await document.GetSyntaxRootAsync().ConfigureAwait(true);
            if (rootSyntaxNode == null)
            {
                _statusBar.SetText("Can't get a syntax root node");
                return;
            }

            SyntaxToken st = rootSyntaxNode.FindToken(caretPosition);

            SemanticModel? semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(true);
            if (semanticModel == null)
            {
                _statusBar.SetText("Can't get a semantic model");
                return;
            }

            //Try to get the method(s)
//            var node = st.Parent;
            List<IMethodSymbol> methodSymbols = new();

            if (!TryGetMethodSymbols(semanticModel, st.Parent, ref methodSymbols) || methodSymbols.Count == 0)
            {
                _statusBar.SetText("Can't find methods");
                return;
            }

            var sb = new StringBuilder();
            foreach (var methodSymbol in methodSymbols)
            {
                if (methodSymbol is null)
                {
                    _statusBar.SetText("Can't get parent node");
                    return;
                }

                GetHarmonyPatch(methodSymbol, options, sb);
            }

            System.Windows.Forms.Clipboard.SetText(sb.ToString());
        }
        catch (Exception ex)
        {
            _statusBar.SetText(ex.Message);
        }
    }

    private void GetHarmonyPatch(IMethodSymbol symbol, HarmonyOptions options, StringBuilder sb)
    {
        var _options = ExtensionOptions.Instance;
        var parameters = symbol.Parameters;
        var typeName = symbol.ContainingType.GetFriendlyName();
        var methodName = symbol.GetHarmonyName();

        //Type array to match methods
        var harmonyParamSignature = $"new Type[] {{ {String.Join(",", parameters.Select(p => $"typeof({p.Type.GetFriendlyName()})"))} }}";

        //Use nameof if accessible and preferred
        var originalMethodName = (symbol.DeclaredAccessibility.HasFlag(Accessibility.Private) || !_options.PreferNameOf) ?
            $"\"{methodName}\"" : $"nameof({typeName}.{methodName})";

        //What the generated method gets called
        var methodDeclarationName = options.Postfix switch
        {
            true when symbol.IsConstructor() => "PostCtor_" + methodName,
            true when symbol.IsGetter() => "PostGet_" + methodName,
            true when symbol.IsSetter() => "PostSet_" + methodName,
            false when symbol.IsConstructor() => "PreCtor_" + methodName,
            false when symbol.IsGetter() => "PreGet_" + methodName,
            false when symbol.IsSetter() => "PreSet_" + methodName,
            true => "Post" + methodName,
            false => "Pre" + methodName,            
        };           

        //Optional injected parameters for method signature
        List<string> injections = new();
        if (_options.AddInstance)
            injections.Add($"ref {typeName} __instance");

        if (_options.AddResult && !symbol.ReturnsVoid) //options.Postfix && 
            injections.Add($"ref {symbol.ReturnType.GetFriendlyName()} __result");

        //Add either manual patching usage or annotations
        if (_options.ManualPatch)
        {
            sb.AppendLine(
                $$"""
                var original = AccessTools.Method(typeof({{typeName}}), {{originalMethodName}});
                var patchMethod = AccessTools.Method(typeof(PatchClass), nameof(PatchClass.{{methodDeclarationName}}), {{harmonyParamSignature}});
                var patch = new HarmonyMethod(patchMethod);
                harmony.Patch(original, {{ (options.Postfix ? "postfix" : "prefix")}}: patch);
                """);
        }
        else
        {
            //Patch type
            sb.AppendLine(options.Postfix ? "[HarmonyPostfix]" : "[HarmonyPrefix]");

            //Build annotations
            List<string> annotations = new() {
                $"typeof({typeName})"
            };

            //Method type annotation.  Todo: verify get/set/ctor
            if (symbol.IsGetter())
                annotations.Add("MethodType.Getter");
            if (symbol.IsSetter())
                annotations.Add("MethodType.Setter");
            if (symbol.IsConstructor())
                annotations.Add("MethodType.Constructor");
            //MethodType.Normal, MethodType.StaticConstructor, MethodType.Enumerator

            if (!symbol.IsConstructor())
                annotations.Add(originalMethodName);

            if (parameters.Count() != 0 || _options.IncludeEmptyParameterAnnotation)
                annotations.Add($"{harmonyParamSignature}");

            if (_options.UseSeparateAnnotations)
            {
                foreach (var annotation in annotations)
                {
                    sb.AppendLine($"[HarmonyPatch({annotation})]");
                }
            }
            else
            {
                var mergedAnnotations = String.Join(", ", annotations.Select(x => x));
                sb.AppendLine($"[HarmonyPatch({mergedAnnotations})]");
            }
        }

        //Method body
        //Types and names in the method parameters
        var methodSignature = parameters.Select(p => $"{p.Type.GetFriendlyName()} {p.Name}").ToList();
        methodSignature.AddRange(injections);
        var formattedParams = String.Join(",", methodSignature);

        var last = parameters[parameters.Count() - 1];

        //Harmony return
        string returnType = options.Postfix || !_options.PreferOverride ? "void" : "bool";
        var a = last.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var b = last.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var parts = last.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat);

        //Boilerplate for the method
        string bodyAndComments = options.Postfix || !_options.PreferOverride ?
            "//Your code here" :
            """
            //Return false to override
            //return false;

            //Return true to execute original
            return true;
            """;

        sb.AppendLine(
            $$"""
            public static {{returnType}} {{methodDeclarationName}}({{formattedParams}}) {
            {{bodyAndComments}}
            }
            """);
    }

    private bool TryGetMethodSymbols(SemanticModel semanticModel, SyntaxNode st, ref List<IMethodSymbol> methodSymbols, int depth = 2)
    {
        ISymbol symbol = semanticModel.GetDeclaredSymbol(st);

        if (symbol is null)
            return false;

        var propSymbol = symbol as IPropertySymbol;

        if (propSymbol != null)
        {
            if (propSymbol.GetMethod != null)
                methodSymbols.Add(propSymbol.GetMethod);

            if (propSymbol.SetMethod != null)
                methodSymbols.Add(propSymbol.SetMethod);

            return true;
        }

        var methodSymbol = symbol as IMethodSymbol;
        if (methodSymbol != null)
        {
            methodSymbols.Add(methodSymbol);

            return true;
        }

        if (depth < 0 || symbol.ContainingSymbol is null)
            return false;

        return TryGetMethodSymbols(semanticModel, st.Parent, ref methodSymbols, depth - 1);
    }

    private IWpfTextView? GetTextView()
    {
        var textManager = (IVsTextManager)ServiceProvider.GetService(typeof(VsTextManagerClass));
        Assumes.Present(textManager);
        if (textManager.GetActiveView(fMustHaveFocus: 0, null, out IVsTextView textView) != 0)
            return null;
        return _editorAdaptersFactory.GetWpfTextView(textView);
    }
}
