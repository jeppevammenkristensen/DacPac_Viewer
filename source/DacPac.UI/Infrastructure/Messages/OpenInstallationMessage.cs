using CommunityToolkit.Mvvm.Messaging.Messages;
using TruePath;

namespace DacPac.UI.Infrastructure.Messages;

public class OpenInstallationMessage(AbsolutePath[] paths) : AsyncRequestMessage<bool>()
{
    public AbsolutePath[] Paths { get; } = paths;
}