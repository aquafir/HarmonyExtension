using Microsoft.Internal.VisualStudio.PlatformUI;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace HarmonyExtension
{
    internal partial class OptionsProvider
    {
        // Register the options with this attribute on your package class:
        [ComVisible(true)]
        public class GeneralOptions : BaseOptionPage<General> { }

        [ComVisible(true)]
        public class TemplateOptions : BaseOptionPage<Template> { }
    }

    //https://www.vsixcookbook.com/recipes/settings-and-options.html
    public class General : BaseOptionModel<General>
    {
        [Category("Harmony")]
        [DisplayName("Manual Patch")]
        [Description("If enabled AccessTools will be used to get the methods for manual patching.  If disabled annotations will be used")]
        [DefaultValue(false)]
        public bool ManualPatch { get; set; } = false;

        [Category("Harmony")]
        [DisplayName("NameOf")]
        [Description("Prefers using nameof method names when available")]
        [DefaultValue(true)]
        public bool PreferNameOf { get; set; } = true;

        [Category("Harmony")]
        [DisplayName("Override")]
        [Description("Defaults to using bool prefixes that can override the method")]
        [DefaultValue(true)]
        public bool PreferOverride { get; set; } = true;

        [Category("Harmony")]
        [DisplayName("Split Annotations")]
        [Description("Split annotations into separate HarmonyPatch attributes")]
        [DefaultValue(false)]
        public bool UseSeparateAnnotations { get; set; } = false;

        [Category("Harmony")]
        [DisplayName("Empty Parameters")]
        [Description("Include annotations for empty parameter lists")]
        [DefaultValue(false)]
        public bool IncludeEmptyParameterAnnotation { get; set; } = false;

        //Injections: https://harmony.pardeike.net/articles/patching-injections.html
        [Category("Harmony")]
        [DisplayName("Inject Instance")]
        [Description("Inject a reference to the instance (this)")]
        [DefaultValue(true)]
        public bool AddInstance { get; set; } = true;

        [Category("Harmony")]
        [DisplayName("Inject Result")]
        [Description("Inject a reference to the return value")]
        [DefaultValue(true)]
        public bool AddResult { get; set; } = true;
    }

    //Custom options UI for templates: https://www.vsixcookbook.com/recipes/settings-and-options.html#Walkthrough-Create-individual-Options-with-Checkboxes
    public class Template : BaseOptionModel<Template>
    {
        [Category("Template")]
        [DisplayName("Manual Template")]
        [Description("Template used for manual patching.")]
        [DefaultValue("")]
        public string ManualTemplate { get; set; } = $$"""
            public static returnType methodDeclarationName(formattedParams) {
            bodyAndComments
            }
            """;

        [Category("Template")]
        [DisplayName("Attribute Template")]
        [Description("Template used for attribute-based patching.")]
        [DefaultValue("")]
        public string AttributeTemplate { get; set; } = $$"""
            public static returnType methodDeclarationName(formattedParams) {
            bodyAndComments
            }
            """;


    }
}
