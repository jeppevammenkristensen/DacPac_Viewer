using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DacPac.UI.Infrastructure.LongRunning;

/// <summary>
/// Communicates whether a DacPac installation is running.
/// </summary>
public sealed class InstallationRunningMessage(bool value) : ValueChangedMessage<bool>(value);