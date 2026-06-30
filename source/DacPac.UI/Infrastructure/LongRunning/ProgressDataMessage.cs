using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DacPac.UI.Infrastructure.LongRunning;

public class ProgressDataMessage(double value) : ValueChangedMessage<double>(value);