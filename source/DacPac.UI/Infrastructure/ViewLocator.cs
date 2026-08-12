using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace DacPac.UI.Infrastructure;

public class ViewLocator : IDataTemplate
{
    private readonly Dictionary<Type, Func<Control>> _dic;

    public ViewLocator(IEnumerable<ViewLocatorDescriptor> descriptors)
    {
        _dic = descriptors.ToDictionary(x => x.ViewModelType, x => x.Factory);
    }

    public Control Build(object? param)
    {
        return _dic[param!.GetType()]();
    }

    public bool Match(object? data)
    {
        return data is not null && _dic.ContainsKey(data.GetType());
    }
}