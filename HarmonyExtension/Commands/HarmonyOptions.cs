namespace HarmonyExtension.Commands;

internal class HarmonyOptions
{
    public PatchType Type { get; set; }
    public PatchStyle Style { get; set; }
}

public enum PatchType
{
    Prefix, Postfix
}

public enum PatchStyle
{
    Manual,
    Annotated
}

public enum PatchTarget
{
    Constructor,
    Getter,
    Setter,
    Method
}
