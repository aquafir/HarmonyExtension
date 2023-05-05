namespace HarmonyExtension.Commands;

internal class HarmonyOptions
{
    public bool Prefix { get; set; } = false;
    public bool Postfix { get; set; } = false;
}

public enum PatchType
{
    Prefix, Postfix
}