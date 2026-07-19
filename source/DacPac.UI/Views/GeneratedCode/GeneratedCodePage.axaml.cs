using System;
using Avalonia.Controls;
using DacPac.UI.ViewModels.GeneratedCode;

namespace DacPac.UI.Views.GeneratedCode;

public partial class GeneratedCodePage : UserControl
{
    public GeneratedCodePage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Editor.TextChanged += OnEditorTextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is GeneratedCodePageViewModel viewModel)
            Editor.Text = viewModel.Code;
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is GeneratedCodePageViewModel viewModel && viewModel.Code != Editor.Text)
            viewModel.Code = Editor.Text;
    }
}
