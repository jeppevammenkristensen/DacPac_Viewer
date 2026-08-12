using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DacPac.UI.ViewModels.ErrorHandling;

public partial class ExceptionWrapper : ObservableObject
{
    public Exception Exception { get; }
    [ObservableProperty] public partial bool Selected { get; set; }
    [ObservableProperty] public partial string ExceptionMessage { get; set; }

    public string FullError => this.Exception.ToString();

    public ExceptionWrapper(Exception exception)
    {
        Selected = false;
        Exception = exception;
        ExceptionMessage = exception.Message;
    }
}