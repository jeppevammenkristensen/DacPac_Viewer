using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DacPac.UI.Infrastructure;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DacPac.UI.ViewModels.GeneratedCode;

/// <summary>
/// Displays editable C# source produced from DacPac objects.
/// </summary>
public partial class GeneratedCodePageViewModel(IClipboardService clipboard) : ScreenPage
{
    /// <summary>
    /// Gets the title displayed by the generated-code tab.
    /// </summary>
    public override string Title => "Generated code";

    [NotifyPropertyChangedFor(nameof(ClassCount))]
    [ObservableProperty]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ClipboardMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets the number of class declarations in the current C# source.
    /// </summary>
    public int ClassCount => SyntaxFactory.ParseCompilationUnit(Code)
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Count();

    /// <summary>
    /// Loads newly generated code and records that the initial source was copied.
    /// </summary>
    public void Load(string code)
    {
        Code = code;
        ClipboardMessage = "Generated code copied to clipboard.";
    }

    /// <summary>
    /// Copies the current editable source to the system clipboard.
    /// </summary>
    [RelayCommand]
    private async Task CopyCode()
    {
        await clipboard.SetTextAsync(Code);
        ClipboardMessage = "Current code copied to clipboard.";
        SetStatusMessage("Current generated code copied to the clipboard.");
    }
}
