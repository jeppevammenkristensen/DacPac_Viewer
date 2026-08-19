using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Models.LandingPage;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Views.LandingPage;

public partial class SimpleTreeItem : ObservableObject, ISqlObjectTreeItem
{
    public SimpleTreeItem(string name, string? iconId, TSqlObject obj, IEnumerable<ITreeItem>? children = null)
    {
        Name = name;
        IconId = iconId;
        ToolTip = obj.ObjectType.Name;
        Source = obj;
        Children = children ?? [];
    }

    public string Name { get; }
    public string? IconId { get; }
    public string? ToolTip { get; }
    public IEnumerable<ITreeItem> Children { get; }
    public TSqlObject Source { get; }

    [ObservableProperty] public partial bool IsExpanded { get; set; }
    [ObservableProperty] public partial bool IsHidden { get; set; }
    [ObservableProperty] public partial bool IsMatch { get; set; }
}
