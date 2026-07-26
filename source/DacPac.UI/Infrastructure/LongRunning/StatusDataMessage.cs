using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DacPac.UI.Infrastructure.LongRunning;

public enum StatusType { Info, Error,
    Success
}


public record StatusMessage(string Value, StatusType StatusType);

public class StatusValueDataMessage(StatusMessage value) : ValueChangedMessage<StatusMessage>(value);