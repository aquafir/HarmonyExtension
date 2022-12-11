global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using HarmonyExtension.Commands;
using System.Runtime.InteropServices;
using System.Threading;

namespace HarmonyExtension;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideOptionPage(typeof(OptionsProvider.ExtensionOptionsOptions), "Harmony", "General", 0, 0, true, SupportsProfiles = true)]
[ProvideProfile(typeof(OptionsProvider.ExtensionOptionsOptions), "Harmony", "General", 0, 0, true)]
[Guid(PackageGuids.HarmonyExtensionString)]
public sealed class HarmonyExtensionPackage : ToolkitPackage
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        HarmonyHandler.Initialize(this);
        await this.RegisterCommandsAsync();
    }
}