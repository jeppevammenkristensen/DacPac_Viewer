using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Infrastructure;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public abstract partial class DisplayViewModel : ViewModelBase, IDisplayViewModel
{
    protected TSqlObject Model { get; }

    public DisplayViewModel(TSqlObject model)
    {
        Model = model;
    }
}

public partial class DefaultDisplayViewModel : ViewModelBase, IDisplayViewModel
{
    
    
    [ObservableProperty] public partial string ShortName { get; set; }
    [ObservableProperty] public partial string FullName { get; set; }
    [ObservableProperty] public partial string Script { get; set; }
    [ObservableProperty] public partial string Type { get; set; }
    [ObservableProperty] public partial ObservableCollection<string> Referencing { get; set; }
    [ObservableProperty] public partial IEnumerable<string> Referenced { get; set; }

    public DefaultDisplayViewModel(TSqlObject model)
    {
        ShortName = model.Name.Parts.Last();
        FullName = model.Name.ToString();
        Type = model.ObjectType.Name;

        Script = model.GetScript();
        Referencing = [..model.GetReferencing().Select(x => x.Name.ToString())];
        Referenced = model.GetReferenced().Select(x => x.Name.ToString());
    }
    
    
}
