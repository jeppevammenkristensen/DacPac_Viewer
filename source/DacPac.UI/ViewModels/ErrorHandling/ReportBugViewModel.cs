using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DacPac.Core;
using DacPac.UI.ApplicationLayer.Infrastructure;
using DacPac.UI.Infrastructure;
using JetBrains.Annotations;

namespace DacPac.UI.ViewModels.ErrorHandling;

[UsedImplicitly]
public partial class ReportBugViewModel : ValidatingScreenPage
{
    private readonly IErrorCollector _collector;
    private readonly IClipboardService _clipboardService;
    private readonly IApplicationInfoService _infoService;

    public ReportBugViewModel(
        IErrorCollector collector,
        IClipboardService clipboardService,
        IApplicationInfoService infoService)
    {
        _collector = collector;
        _clipboardService = clipboardService;
        _infoService = infoService;
        ReportMessage = string.Empty;
        Exceptions = [.. _collector.Errors.Select(x => new ExceptionWrapper(x))];
    }

    public override Task OnActivatedAsync()
    {
        Exceptions = [.. _collector.Errors.Select(x => new ExceptionWrapper(x))];
        return base.OnActivatedAsync();
    }

    private bool CanExecuteCreate()
    {
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCreate))]
    private async Task Create()
    {
        ValidateAllProperties();
        if (HasErrors)
            return;

        await GenerateMarkdownForGithubIssue();
    }

    private async Task GenerateMarkdownForGithubIssue()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder
            .AppendLine("# Bug Report")
            .AppendLine()
            .AppendLine("## Description");

        stringBuilder.AppendLine(ReportMessage.EscapeMarkdown());

        var exceptions = Exceptions.Where(x => x.Selected).ToList();
        if (exceptions.Any())
        {
            stringBuilder.AppendLine().AppendLine("## Exceptions");

            foreach (var exceptionWrapper in exceptions)
            {
                stringBuilder.AppendLine("```");
                stringBuilder.AppendLine(exceptionWrapper.Exception.ToString());
                stringBuilder.AppendLine("```");
            }
        }

        await _clipboardService.SetTextAsync(stringBuilder.ToString());
        Process.Start(new ProcessStartInfo(_infoService.NewIssueLink.AbsoluteUri) { UseShellExecute = true });
    }


    public override string Title => "Report a Bug";

    [ObservableProperty]
    [Required(ErrorMessage = "Describe the problem before creating a bug report.")]
    public partial string ReportMessage { get; set; }

    [ObservableProperty] public partial ObservableCollection<ExceptionWrapper> Exceptions { get; set; }

    partial void OnReportMessageChanged(string value) => ValidateProperty(value, nameof(ReportMessage));
}