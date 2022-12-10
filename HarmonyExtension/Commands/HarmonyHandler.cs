using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
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

            var symbol = semanticModel.LookupSymbols(caretPosition.Position, name: st.Text).FirstOrDefault();
            var node = st.Parent;
            
            //Try to get the method
            List<IMethodSymbol> methodSymbols = new();
            //IPropertySymbol propertySymbol = st.Parent as IPropertySymbol;
            //propertySymbol = symbol as IPropertySymbol;


            if(!TryGetMethodSymbols(semanticModel, node, ref methodSymbols) || methodSymbols.Count == 0) {
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



                string? memberName = null;
                MemberType memberType = 0;

                if (symbol == null || !TryHandleAsMember(symbol, out var typeSymbol, out memberName, out memberType))
                {
                    var msg = $"{st.Text} is not a valid identifier. token: {st}, Kind: {st.Kind()}";
                    _statusBar.SetText(msg);
                    return;
                }
                //else if (parentKind == SyntaxKind.ConstructorDeclaration)
                //{

                //}


                if (typeSymbol == null)
                    return;

                //string typeNamespace = GetFullNamespace(typeSymbol);
                //string typeName = typeNamespace + "." + typeSymbol.MetadataName;
                var typeName = typeSymbol.FriendlyName();

                if (!(memberType == MemberType.Property || memberType == MemberType.Method))
                {
                    _statusBar.SetText($"Harmony only creates a template for properties or methods.  {typeName}.{memberName} is {memberType}");
                    return;
                }


                //Use nameof if accessible
                var harmonyMemberName = (symbol.DeclaredAccessibility.HasFlag(Accessibility.Private) || !options.PreferNameOf) ?
                    $"\"{memberName}\"" : $"nameof({typeName}.{memberName})";


                var parameters = methodSymbol.Parameters;
                var paramSignature = String.Join(",", parameters.Select(p => $"typeof({p.Type.FriendlyName()})"));
                var paramNames = String.Join(",", parameters.Select(p => $"{p.Type.FriendlyName()} {p.Name}"));

                var returnSignature = !methodSymbol.ReturnsVoid;

                //Set type
                sb.AppendLine(options.Postfix ? "[HarmonyPostfix]" : "[HarmonyPrefix]");
                sb.AppendLine($"[HarmonyPatch(typeof({typeName}), {harmonyMemberName}, new Type[] {{ {paramSignature} }})]");

                var optParams = "";
                if (options.AddInstance)
                    optParams += $", ref {typeName} __instance";

                if (options.Postfix && options.AddResult && !methodSymbol.ReturnsVoid)
                    optParams += $", ref {methodSymbol.ReturnType.FriendlyName()} __result";

                if (parameters.Count() == 0)
                    optParams = optParams.TrimStart(',');

                //Method declaration
                string returnName, bodyAndComments;

                if (options.Postfix || !options.PreferOverride)
                {
                    returnName = "void";
                    bodyAndComments = "//Your code here";
                }
                else
                {
                    returnName = "bool";
                    bodyAndComments = """
                    //Return false to override
                    //return false;

                    //Return true to execute original
                    return true;
                    """;

                }
                sb.AppendLine($"public static {returnName} {(options.Postfix ? "Post" : "Pre")}{methodSymbol.Name}({paramNames}{optParams}) {{");

                sb.AppendLine(bodyAndComments);
                sb.AppendLine("}");
            }

            System.Windows.Forms.Clipboard.SetText(sb.ToString());
        }
        catch (Exception ex)
        {
            _statusBar.SetText(ex.Message);
        }
    }

    private bool TryGetMethodSymbols(SemanticModel semanticModel, SyntaxNode node, ref List<IMethodSymbol> methodSymbols, int depth = 2)
    {

        var propSymbol = node as IPropertySymbol;

        if(propSymbol  != null)
        {
            if(propSymbol.GetMethod != null)
                methodSymbols.Add(propSymbol.GetMethod);

            if (propSymbol.SetMethod != null)
                methodSymbols.Add(propSymbol.SetMethod);

            return true;
        }

        var methodSymbol = node as IMethodSymbol;
        if (methodSymbol != null)
        {
            methodSymbols.Add(methodSymbol);

            return true;
        }

        if (depth < 0 || node.Parent is null)
            return false;

        return TryGetMethodSymbols(node.Parent, ref methodSymbols, depth - 1);
    }

    private static bool TryHandleAsMember(ISymbol symbol, out INamedTypeSymbol? type, out string? memberName, out MemberType memberType)
    {        
        if (symbol is IFieldSymbol fieldSymbol)
        {
            type = fieldSymbol.ContainingType;
            memberName = fieldSymbol.Name;
            memberType = MemberType.Field;
        }
        else if (symbol is IPropertySymbol propSymbol)
        {
            type = propSymbol.ContainingType;
            memberName = propSymbol.Name;
            memberType = MemberType.Property;
        }
        else if (symbol is IMethodSymbol methodSymbol)
        {
            type = methodSymbol.ContainingType;
            memberName = methodSymbol.Name;
            memberType = MemberType.Method;
        }
        else if (symbol is IEventSymbol eventSymbol)
        {
            type = eventSymbol.ContainingType;
            memberName = eventSymbol.Name;
            memberType = MemberType.Event;
        }
        else
        {
            type = null;
            memberName = null;
            memberType = 0;
        }

        return type != null;
    }

    private IWpfTextView? GetTextView()
    {
        var textManager = (IVsTextManager)ServiceProvider.GetService(typeof(VsTextManagerClass));
        Assumes.Present(textManager);
        if (textManager.GetActiveView(fMustHaveFocus: 0, null, out IVsTextView textView) != 0)
            return null;
        return _editorAdaptersFactory.GetWpfTextView(textView);
    }

    private static string GetFullNamespace(INamedTypeSymbol typeSymbol)
    {
        INamespaceSymbol nsSym = typeSymbol.ContainingNamespace;
        var sb = new StringBuilder();
        while (nsSym?.IsGlobalNamespace == false)
        {
            if (sb.Length == 0)
                sb.Append(nsSym.Name);
            else
                sb.Insert(0, '.').Insert(0, nsSym.Name);
            nsSym = nsSym.ContainingNamespace;
        }

        return sb.ToString();
    }

    internal enum MemberType
    {
        Field = 0,
        Property = 1,
        Method = 2,
        Event = 3,
    }
}
