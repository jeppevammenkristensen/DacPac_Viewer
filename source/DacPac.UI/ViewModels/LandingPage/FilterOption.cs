namespace DacPac.UI.ViewModels.LandingPage;

/// <summary>
/// Represents a selectable object-type filter and whether at least one matching result supports code generation.
/// </summary>
public sealed record FilterOption(string Type, bool CanGenerateCode)
{
    /// <summary>Gets the label shown in the filter dropdown.</summary>
    public string DisplayName => CanGenerateCode ? $"{Type} (can generate code)" : Type;
}