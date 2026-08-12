using CommunityToolkit.Mvvm.Messaging;
using DacPac.UI.ApplicationLayer.Infrastructure;
using DacPac.UI.Infrastructure.LongRunning;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Provides helpers for publishing application status messages.
/// </summary>
public static class MessengerExtensions
{
    extension(IMessenger messenger)
    {
        /// <summary>
        /// Publishes an informational status message.
        /// </summary>
        public void SendInformation(string message)
        {
            messenger.Send(new StatusValueDataMessage(new StatusMessage(message, StatusType.Info)));
        }

        /// <summary>
        /// Publishes an error status message.
        /// </summary>
        public void SendError(string message)
        {
            messenger.Send(new StatusValueDataMessage(new StatusMessage(message, StatusType.Error)));
        }

        /// <summary>
        /// Publishes an exception error along with an associated error message.
        /// </summary>
        /// <param name="message">The error message to be sent. This will be displayed to the user</param>
        /// <param name="exception">The exception object containing details of the error.</param>
        public void SendException(string message, Exception exception)
        {
            messenger.SendError(message + "(exception recorded)");
            messenger.Send(new ExceptionMessageValueMessage(exception));
        }

        /// <summary>
        /// Publishes a success status message.
        /// </summary>
        public void SendSuccess(string message)
        {
            messenger.Send(new StatusValueDataMessage(new StatusMessage(message, StatusType.Success)));
        }
    }
}