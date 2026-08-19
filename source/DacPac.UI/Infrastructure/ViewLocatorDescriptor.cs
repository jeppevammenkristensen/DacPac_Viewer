using System;
using Avalonia.Controls;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Describes how to create a view for a view-model type.
/// </summary>
public record ViewLocatorDescriptor(Type ViewModelType, Func<Control> Factory);
