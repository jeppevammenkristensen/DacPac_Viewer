using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Infrastructure;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

/// <summary>
/// Provides common display data for a DACPAC SQL object.
/// </summary>
public abstract partial class DisplayViewModel : ViewModelBase, IDisplayViewModel
{
    /// <summary>
    /// Gets the SQL object represented by this view model.
    /// </summary>
    protected TSqlObject Model { get; }

    /// <summary>
    /// Gets or sets the unqualified name of the SQL object.
    /// </summary>
    [ObservableProperty] public partial string ShortName { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified name of the SQL object.
    /// </summary>
    [ObservableProperty] public partial string FullName { get; set; }

    /// <summary>
    /// Gets or sets the SQL definition script for the object.
    /// </summary>
    [ObservableProperty] public partial string Script { get; set; }

    /// <summary>
    /// Gets or sets the DAC object type name.
    /// </summary>
    [ObservableProperty] public partial string Type { get; set; }

    /// <summary>
    /// Gets or sets the objects that reference this object.
    /// </summary>
    [ObservableProperty] public partial ObservableCollection<string> Referencing { get; set; }

    /// <summary>
    /// Gets or sets the objects referenced by this object.
    /// </summary>
    [ObservableProperty] public partial ObservableCollection<string> Referenced { get; set; }

    /// <summary>
    /// Initializes common display data from a SQL object.
    /// </summary>
    public DisplayViewModel(TSqlObject model)
    {
        Model = model;
        ShortName = model.Name.Parts.Last();
        FullName = model.Name.ToString();
        Type = model.ObjectType.Name;
        Script = model.GetScript();
        Referencing = [..model.GetReferencing()
            .Where(FilterReferencing)
            .Select(RenderReferencing).Distinct()];
        Referenced = [..model.GetReferenced()
            .Where(FilterReferenced)
            .Select(RenderReferenced).Distinct()];
    }

    /// <summary>
    /// Determines whether an object is included in the referenced-object list.
    /// </summary>
    /// <remarks>Defaults to true</remarks>
    protected virtual bool FilterReferenced(TSqlObject arg)
    {
        return true;
    }

    /// <summary>
    /// Determines whether an object is included in the referencing-object list. (referenced by)
    /// </summary>
    /// <remarks>Defaults to true</remarks>
    protected virtual bool FilterReferencing(TSqlObject arg)
    {
        return true;
    }

    /// <summary>
    /// Formats an object for the referencing-object list.
    /// </summary>
    protected virtual string RenderReferencing(TSqlObject model)
    {
        return $"{model.Name} {model.ObjectType.Name}";
    }
    
    /// <summary>
    /// Formats an object for the referenced-object list.
    /// </summary>
    protected virtual string RenderReferenced(TSqlObject model)
    {
        return $"{model.Name} {model.ObjectType.Name}";
    }

}

/// <summary>
/// Displays common information for SQL objects without a specialized view model.
/// </summary>
public partial class DefaultDisplayViewModel : DisplayViewModel
{
    /// <summary>
    /// Initializes a default display for a SQL object.
    /// </summary>
    public DefaultDisplayViewModel(TSqlObject model) : base(model)
    {
    }
}
