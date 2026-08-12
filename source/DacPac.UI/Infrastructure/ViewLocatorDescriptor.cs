using System;
using Avalonia.Controls;

namespace DacPac.UI.Infrastructure;

public record ViewLocatorDescriptor(Type ViewModelType, Func<Control> Factory);