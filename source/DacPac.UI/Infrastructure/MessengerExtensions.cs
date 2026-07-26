using CommunityToolkit.Mvvm.Messaging;
using DacPac.UI.Infrastructure.LongRunning;

namespace DacPac.UI.Infrastructure;

public static class MessengerExtensions
{
    extension(IMessenger messenger)
    {
        public void SendInformation(string message)
        {
            messenger.Send(new StatusValueDataMessage(new StatusMessage(message, StatusType.Info)));
        }

        public void SendError(string message)
        {
            messenger.Send(new StatusValueDataMessage(new StatusMessage(message, StatusType.Error)));
        }
        
        public void SendSuccess(string message)
        {
            messenger.Send(new StatusValueDataMessage(new StatusMessage(message, StatusType.Success)));
        }
    }
}