using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DacPac.UI.Infrastructure.LongRunning;

public sealed class ThemeChangedMessage() : ValueChangedMessage<bool>(true);