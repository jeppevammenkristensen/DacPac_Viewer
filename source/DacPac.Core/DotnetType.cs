namespace DacPac.Core;

/// <summary>
/// Represents a C# type name and whether its generated declaration is nullable.
/// </summary>
/// <param name="Name">The C# type name without a nullable suffix.</param>
/// <param name="IsNullable">Whether to append a nullable suffix when rendering the type.</param>
public record DotnetType(string Name, bool IsNullable)
{
    /// <summary>
    /// Renders the type name with a nullable suffix when required.
    /// </summary>
    public override string ToString()
    {
        return Name + (IsNullable ? "?" : "");
    }
}
