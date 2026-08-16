using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

public class ExceptionMessageValueMessage(Exception Exception)
    : ValueChangedMessage<ExceptionMessage>(new ExceptionMessage(Exception));
    
public record ExceptionMessage(Exception Exception)
{
    
}

public interface IErrorCollector
{
    ImmutableArray<Exception> Errors { get; }
    void Receive(Exception exception);
}

public sealed class ErrorCollector : ObservableRecipient, IRecipient<ExceptionMessageValueMessage>, IErrorCollector
{
    public ErrorCollector()
    {
        this.OnActivated();
    }
    
    public ImmutableArray<Exception> Errors { get; private set; } = [];

    public void Receive(ExceptionMessageValueMessage message)
    {
        Receive(message.Value.Exception);
    }

    public void Receive(Exception exception) => Errors = Errors.Add(exception);
}