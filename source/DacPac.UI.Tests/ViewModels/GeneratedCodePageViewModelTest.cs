using System.Threading.Tasks;
using DacPac.UI.Infrastructure;
using DacPac.UI.ViewModels.GeneratedCode;
using Xunit;

namespace DacPac.UI.Tests.ViewModels;

public class GeneratedCodePageViewModelTest
{
    [Fact]
    public void ClassCount_CountsEveryClassDeclaration()
    {
        var viewModel = new GeneratedCodePageViewModel(new ClipboardService());

        viewModel.Load("public class First { } internal class Second { class Nested { } }");

        Assert.Equal(3, viewModel.ClassCount);
    }

    [Fact]
    public async Task CopyCode_CopiesTheCurrentEditableSource()
    {
        var clipboard = new ClipboardService();
        var viewModel = new GeneratedCodePageViewModel(clipboard);
        viewModel.Load("public class Original { }");
        viewModel.Code = "public class Edited { }";

        await viewModel.CopyCodeCommand.ExecuteAsync(null);

        Assert.Equal("public class Edited { }", clipboard.Text);
        Assert.Equal("Current code copied to clipboard.", viewModel.ClipboardMessage);
    }

    private sealed class ClipboardService : IClipboardService
    {
        public string? Text { get; private set; }

        public Task SetTextAsync(string text)
        {
            Text = text;
            return Task.CompletedTask;
        }
    }
}
