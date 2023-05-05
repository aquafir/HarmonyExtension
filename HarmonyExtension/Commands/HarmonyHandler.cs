using EnvDTE;
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

    /// <summary>
    /// Inserts a copied Harmony patch into the current document with context-aware replacements
    /// </summary>
    public async Task Button_InsertAsHarmony(HarmonyOptions harmonyOptions)
    {
        //https://stackoverflow.com/questions/71102005/vsix-exstension-how-to-paste-the-text-from-clipboard-to-the-environment
        var template = "//Not yet implemented\r\n" + Clipboard.GetText();

        DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
        var d = dte.ActiveDocument;
        TextSelection selectedText = (TextSelection)dte.ActiveDocument.Selection; // gets all the selected text (if not, it gets the current cursor)
        selectedText.Insert(template); //Insert text
    }

    /// <summary>
    /// Copy current symbol as a Harmony patch specified by extension and context options
    /// </summary>
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
        var _options = General.Instance;
        var parameters = symbol.Parameters;
        var typeName = symbol.ContainingType.GetFriendlyName();
        var methodName = symbol.GetHarmonyName();

        //Type array to match methods
        var harmonyParamSignature = $"new Type[] {{ {String.Join(",", parameters.Select(p => $"typeof({p.Type.GetFriendlyName()})"))} }}";

        //Method name, using nameof if accessible and preferred in options
        var originalMethodName = (symbol.DeclaredAccessibility.HasFlag(Accessibility.Private) || !_options.PreferNameOf) ?
            $"\"{methodName}\"" : $"nameof({typeName}.{methodName})";


        var patchType = options.Type switch
        {
            PatchType.Postfix => "Post",
            PatchType.Prefix => "Pre",
        };

        PatchTarget target = PatchTarget.Method;
        if (symbol.IsConstructor())
            target = PatchTarget.Constructor;
        else if (symbol.IsGetter())
            target = PatchTarget.Getter;
        else if (symbol.IsSetter())
            target = PatchTarget.Setter;
        var patchTarget = target switch
        {
            PatchTarget.Constructor => "Ctor",
            PatchTarget.Getter => "Get",
            PatchTarget.Setter => "Set",
            PatchTarget.Method => "",
        };

        //What the generated method gets called
        var methodDeclarationName = patchType + patchTarget + methodName;

        //Optional injected parameters for method signature
        List<string> injections = new();
        if (_options.AddInstance)
            injections.Add($"ref {typeName} __instance");

        if (_options.AddResult && !symbol.ReturnsVoid) //options.Postfix && 
            injections.Add($"ref {symbol.ReturnType.GetFriendlyName()} __result");

        //Add either manual patching usage or annotations
        if (options.Style == PatchStyle.Manual)
        {
            sb.AppendLine(
                $$"""
                var original = AccessTools.Method(typeof({{typeName}}), {{originalMethodName}});
                var patchMethod = AccessTools.Method(typeof(PatchClass), nameof(PatchClass.{{methodDeclarationName}}), {{harmonyParamSignature}});
                var patch = new HarmonyMethod(patchMethod);
                harmony.Patch(original, {{ (options.Type == PatchType.Postfix ? "postfix" : "prefix")}}: patch);
                """);
        }
        else
        {
            //Patch type
            sb.AppendLine(options.Type == PatchType.Postfix ? "[HarmonyPostfix]" : "[HarmonyPrefix]");

            //Build annotations
            List<string> annotations = new() {
                $"typeof({typeName})"
            };

            if (!symbol.IsConstructor())
                annotations.Add(originalMethodName);
            
            //Method type annotation.
            //Todo: verify get/set/ctor
            if (symbol.IsGetter())
                annotations.Add("MethodType.Getter");
            if (symbol.IsSetter())
                annotations.Add("MethodType.Setter");
            if (symbol.IsConstructor())
                annotations.Add("MethodType.Constructor");
            //MethodType.Normal, MethodType.StaticConstructor, MethodType.Enumerator

            if(parameters.Count() > 0)
                annotations.Add($"{harmonyParamSignature}");
            else if(_options.IncludeEmptyParameterAnnotation && !(symbol.IsGetter() || symbol.IsSetter()))
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

        //Parameters?
        //var last = parameters[parameters.Count() - 1];
        //var parts = last.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat);

        //Harmony return
        string returnType = (options.Type == PatchType.Postfix || !_options.PreferOverride) ? "void" : "bool";

        //Boilerplate for the method
        string bodyAndComments = (options.Type == PatchType.Postfix || !_options.PreferOverride) ?
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
