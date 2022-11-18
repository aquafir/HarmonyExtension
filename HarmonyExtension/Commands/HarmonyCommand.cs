using HarmonyExtension.Commands;
using Microsoft.VisualStudio.TextManager.Interop;

namespace HarmonyExtension
{
    [Command(PackageIds.PrefixCommand)]
    internal sealed class HarmonyPrefixCommand : BaseCommand<HarmonyPrefixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await HarmonyHandler.Instance.Button_CopyAsHarmony(this, e);
        }
    }

    [Command(PackageIds.PostfixCommand)]
    internal sealed class HarmonyPostfixCommand : BaseCommand<HarmonyPostfixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await HarmonyHandler.Instance.Button_CopyAsHarmony(this, e);
            //var textManager = await this.Package.GetServiceAsync(typeof(VsTextManagerClass));
            //await VS.MessageBox.ShowWarningAsync("HarmonyExtension", "Button clicked");
        }
    }


}
