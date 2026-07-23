using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Messaging.Messages;
using TruePath;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Notifies recipients that the persisted recent dacpac paths have changed.
/// </summary>
public sealed class StoredPathsChangedMessage : ValueChangedMessage<ImmutableArray<AbsolutePath[]>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoredPathsChangedMessage"/> class.
    /// </summary>
    public StoredPathsChangedMessage(IEnumerable<AbsolutePath[]> paths) : base([..paths])
    {
    }
}
