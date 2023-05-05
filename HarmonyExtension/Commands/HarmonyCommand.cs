using HarmonyExtension.Commands;

namespace HarmonyExtension
{
    [Command(PackageIds.AnnotatedPostfixCommand)]
    internal sealed class AnnotatedPostfixCommand : BaseCommand<AnnotatedPostfixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) => 
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions { Style = PatchStyle.Annotated, Type = PatchType.Postfix});
    }

    [Command(PackageIds.AnnotatedPrefixCommand)]
    internal sealed class AnnotatedPrefixCommand : BaseCommand<AnnotatedPrefixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) =>
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions {  Style = PatchStyle.Annotated, Type = PatchType.Prefix});
    }

    [Command(PackageIds.ManualPostfixCommand)]
    internal sealed class ManualPostfixCommand : BaseCommand<ManualPostfixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) =>
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions { Style = PatchStyle.Manual, Type = PatchType.Postfix });
    }

    [Command(PackageIds.ManualPrefixCommand)]
    internal sealed class ManualPrefixCommand : BaseCommand<ManualPrefixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) =>
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions { Style = PatchStyle.Manual, Type = PatchType.Prefix });
    }

    [Command(PackageIds.HarmonyPasteCommand)]
    internal sealed class HarmonyPasteCommand : BaseCommand<HarmonyPasteCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) =>
            await HarmonyHandler.Instance.Button_InsertAsHarmony(new HarmonyOptions {  });
    }
}
