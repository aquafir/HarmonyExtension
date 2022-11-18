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

using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


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
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            if(ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService) {

            }

            _statusBar = (IVsStatusbar)ServiceProvider.GetService(typeof(SVsStatusbar));
            Assumes.Present(_statusBar);

            _componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));
            Assumes.Present(_componentModel);

            _editorAdaptersFactory = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            Assumes.Present(_editorAdaptersFactory);
        }
        catch { }
    }

    public async Task Button_CopyAsHarmony(object sender, EventArgs e)
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
            if (st.Parent == null)
            {
                _statusBar.SetText("Can't get parent node");
                return;
            }
            ISymbol? symbol = null;
            var parentKind = st.Parent.Kind();

            if (st.Kind() == SyntaxKind.IdentifierToken && (
                   parentKind == SyntaxKind.PropertyDeclaration
                || parentKind == SyntaxKind.FieldDeclaration
                || parentKind == SyntaxKind.MethodDeclaration
                || parentKind == SyntaxKind.NamespaceDeclaration
                || parentKind == SyntaxKind.DestructorDeclaration
                || parentKind == SyntaxKind.ConstructorDeclaration
                || parentKind == SyntaxKind.OperatorDeclaration
                || parentKind == SyntaxKind.ConversionOperatorDeclaration
                || parentKind == SyntaxKind.EnumDeclaration
                || parentKind == SyntaxKind.EnumMemberDeclaration
                || parentKind == SyntaxKind.ClassDeclaration
                || parentKind == SyntaxKind.EventDeclaration
                || parentKind == SyntaxKind.EventFieldDeclaration
                || parentKind == SyntaxKind.InterfaceDeclaration
                || parentKind == SyntaxKind.StructDeclaration
                || parentKind == SyntaxKind.DelegateDeclaration
                || parentKind == SyntaxKind.IndexerDeclaration
                || parentKind == SyntaxKind.VariableDeclarator
                ))
            {
                symbol = semanticModel.LookupSymbols(caretPosition.Position, name: st.Text).FirstOrDefault();
            }
            else
            {
                SymbolInfo si = semanticModel.GetSymbolInfo(st.Parent);
                symbol = si.Symbol ?? si.CandidateSymbols.FirstOrDefault();
            }
            if (symbol == null)
            {
                _statusBar.SetText($"Can't find symbol");
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

            if (typeSymbol == null)
                return;

            string typeNamespace = GetFullNamespace(typeSymbol);
            //string typeName = typeNamespace + "." + typeSymbol.MetadataName;
            var typeName = typeSymbol.FriendlyName();

            if (!(memberType == MemberType.Property || memberType == MemberType.Method))
            {
                _statusBar.SetText($"Harmony only creates a template for properties or methods.  {typeName}.{memberName} is {memberType}");
                return;
            }

            //Get type of command
            var s = sender as MenuCommand;

            if (s is null)
                return;

            bool postfix = s.CommandID.ID == PackageIds.PrefixCommand;

            //Set type
            var sb = new StringBuilder();
            sb.AppendLine(postfix ? "[HarmonyPostfix]" : "[HarmonyPrefix]");

            //Use nameof if accessible
            var harmonyMemberName = symbol.DeclaredAccessibility.HasFlag(Accessibility.Private) ? $"\"{memberName}\"" : $"nameof({typeName}.{memberName})";
            var methodSymbol = symbol as IMethodSymbol;

            if (methodSymbol is null)
                return;

            var paramSignature = String.Join(",", methodSymbol.Parameters.Select(p => $"typeof({p.Type.FriendlyName()})"));
            var paramMethodSignature = String.Join(",", methodSymbol.Parameters.Select(p => $"{p.Type.FriendlyName()} {p.Name}"));

            if (postfix)
                paramMethodSignature += $", ref {typeName} __instance, ref {methodSymbol.ReturnType.FriendlyName()} __result";
            else
                paramMethodSignature += $", ref {typeName} __instance";

            sb.AppendLine($"[HarmonyPatch(typeof({typeName}), {harmonyMemberName}, new Type[] {{ {paramSignature} }}]");
            sb.AppendLine($"public static {(postfix ? "void" : "bool")} {(postfix ? "Post" : "Pre")}{methodSymbol.Name}({paramMethodSignature}) {{\r\n" +
                $"//Your code here" +
                $"{(postfix ? "" : "\r\nreturn true; //Return false to override")}" +
                $"\r\n}}");

            //System.Windows.Forms.Clipboard.SetText(sb.ToString());
        }
        catch (Exception ex)
        {
            _statusBar.SetText(ex.Message);
        }
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
