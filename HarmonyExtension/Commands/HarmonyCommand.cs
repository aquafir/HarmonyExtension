using HarmonyExtension.Commands;

namespace HarmonyExtension
{
    [Command(PackageIds.AnnotatedPrefixCommand)]
    internal sealed class AnotatedPrefixCommand : BaseCommand<AnotatedPrefixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions { Prefix = true });
        }
    }

    [Command(PackageIds.AnnotatedPostfixCommand)]
    internal sealed class AnotatedPostfixCommand : BaseCommand<AnotatedPostfixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions { Postfix = true});
        }
    }

    [Command(PackageIds.ManualPrefixCommand)]
    internal sealed class ManualPrefixCommand : BaseCommand<ManualPrefixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions { Prefix = true });
        }
    }

    [Command(PackageIds.ManualPostfixCommand)]
    internal sealed class ManualPostfixCommand : BaseCommand<ManualPostfixCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await HarmonyHandler.Instance.Button_CopyAsHarmony(new HarmonyOptions { Postfix = true });
        }
    }

}
