using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyExtension.Commands;

internal class HarmonyOptions
{
    public bool Prefix { get; set; } = false;
    public bool Postfix { get; set; } = false;

    public bool PreferNameOf { get; set; } = true;      //Uses nameof when the accessibility isn't private
    public bool PreferOverride { get; set; } = true;    //Uses bool for prefixes

    //public bool UseSeparateAnnotations { get; set; } = false;
    public bool SkipEmptySignature { get; set; } = true;

    //Injections: https://harmony.pardeike.net/articles/patching-injections.html
    public bool AddInstance { get; set; } = true;
    public bool AddResult { get; set; } = true;
    //State, Fields, Args ignored
}
