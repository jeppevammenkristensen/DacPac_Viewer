namespace DacPac.Core;

public record DotnetType(string Name, bool IsNullable)
{
    public override string ToString()
    {
        return Name + (IsNullable ? "?" : "");
    }
}