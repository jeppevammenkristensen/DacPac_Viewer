using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Creates registered views for their corresponding view-model types.
/// </summary>
public class ViewLocator : IDataTemplate
{
    private readonly Dictionary<Type, Func<Control>> _dic;

    public ViewLocator(IEnumerable<ViewLocatorDescriptor> descriptors)
    {
        _dic = descriptors.ToDictionary(x => x.ViewModelType, x => x.Factory);
    }

    /// <summary>
    /// Creates the view registered for the supplied view model.
    /// </summary>
    public Control Build(object? param)
    {
        return _dic[param!.GetType()]();
    }

    /// <summary>
    /// Determines whether a view is registered for the supplied data object.
    /// </summary>
    public bool Match(object? data)
    {
        return data is not null && _dic.ContainsKey(data.GetType());
    }
}
