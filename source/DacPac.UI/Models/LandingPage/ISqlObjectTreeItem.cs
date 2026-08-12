using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a tree item backed by a DACPAC SQL object.
/// </summary>
public interface ISqlObjectTreeItem : ITreeItem
{
    TSqlObject Source { get; }
}