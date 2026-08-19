using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Publishes an exception through the application messenger.
/// </summary>
public class ExceptionMessageValueMessage(Exception Exception)
    : ValueChangedMessage<ExceptionMessage>(new ExceptionMessage(Exception));
    
/// <summary>
/// Wraps an exception for application messaging.
/// </summary>
public record ExceptionMessage(Exception Exception)
{
    
}

public interface IErrorCollector
{
    ImmutableArray<Exception> Errors { get; }
    void Receive(Exception exception);
}

/// <summary>
/// Collects exceptions received through the application messenger.
/// </summary>
public sealed class ErrorCollector : ObservableRecipient, IRecipient<ExceptionMessageValueMessage>, IErrorCollector
{
    public ErrorCollector()
    {
        this.OnActivated();
    }
    
    /// <summary>
    /// Gets the exceptions collected during the current application session.
    /// </summary>
    public ImmutableArray<Exception> Errors { get; private set; } = [];

    /// <summary>
    /// Adds an exception received through the messenger.
    /// </summary>
    public void Receive(ExceptionMessageValueMessage message)
    {
        Receive(message.Value.Exception);
    }

    /// <summary>
    /// Adds an exception to the collection.
    /// </summary>
    public void Receive(Exception exception) => Errors = Errors.Add(exception);
}
