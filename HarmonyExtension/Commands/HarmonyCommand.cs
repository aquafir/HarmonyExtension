using HarmonyExtension.Commands;
using Microsoft.VisualStudio.TextManager.Interop;

namespace HarmonyExtension
{
    [Command(PackageIds.PrefixCommand)]
    internal sealed class HarmonyPrefixCommand : BaseCommand<HarmonyPrefixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions { Prefix = true });
        }
    }

    [Command(PackageIds.PostfixCommand)]
    internal sealed class HarmonyPostfixCommand : BaseCommand<HarmonyPostfixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions { Postfix = true});
        }
    }
}
