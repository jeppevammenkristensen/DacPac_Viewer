using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Infrastructure;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public partial class DefaultDisplayViewModel : ViewModelBase, IDisplayViewModel
{
    
    
    [ObservableProperty] public partial string ShortName { get; set; }
    [ObservableProperty] public partial string FullName { get; set; }
    [ObservableProperty] public partial string Script { get; set; }
    [ObservableProperty] public partial string Type { get; set; }

    public DefaultDisplayViewModel(TSqlObject model)
    {
        ShortName = model.Name.Parts.Last();
        FullName = model.Name.ToString();
        Type = model.ObjectType.Name;

        Script = model.GetScript();
    }
}
