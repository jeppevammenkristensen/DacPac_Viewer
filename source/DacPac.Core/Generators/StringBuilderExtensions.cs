using System.Text;

namespace DacPac.Core.Generators;

public class SummaryBuilder
{
    private readonly StringBuilder _stringBuilder;


    /// <summary>
    /// 
    /// </summary>
    /// <param name="summary"></param>
    /// <param name="builder"></param>
    public SummaryBuilder(string summary, StringBuilder builder)
    {
        _stringBuilder = builder;
        _stringBuilder.AppendLine("/// <summary>");
        _stringBuilder.AppendLine($"/// {summary} ");
        _stringBuilder.AppendLine("/// </summary>");
    }

    public SummaryBuilder WithParameter(string parameter, string text)
    {
        _stringBuilder.AppendLine($"<param name=\"{parameter}\">{text}</param>");
        return this;
    }
    
    public SummaryBuilder WithRemarks(string remarks)
    {
        
            _stringBuilder.AppendLine("/// <remarks>");
            foreach (var line in remarks.Split('\n'))
            {
                _stringBuilder.AppendLine($"/// {line.TrimEnd('\r')}");
            }

            _stringBuilder.AppendLine("/// </remarks>");
            return this;
    }

    public StringBuilder Builder()
    {
        return _stringBuilder;
    }
}

/// <summary>
/// Provides helpers for writing common C# declarations and XML documentation to generated source.
/// </summary>
public static class StringBuilderExtensions
{
    /// <summary>
    /// Appends an XML documentation summary and, optionally, remarks.
    /// </summary>
    public static StringBuilder AppendSummary(this StringBuilder builder, string summary, string? remarks = null)
    {
        builder.AppendLine("/// <summary>");
        builder.AppendLine($"/// {summary}");
        builder.AppendLine("/// </summary>");

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            builder.AppendLine("/// <remarks>");
            foreach (var line in remarks.Split('\n'))
            {
                builder.AppendLine($"/// {line.TrimEnd('\r')}");
            }

            builder.AppendLine("/// </remarks>");
        }

        return builder;
    }

    /// <summary>
    /// Appends a C# class declaration and its opening brace.
    /// </summary>
    public static StringBuilder AppendClass(this StringBuilder builder, string className, string modifiers = "public")
    {
        builder.AppendLine($"{modifiers} class {className}");
        builder.AppendLine("{");
        return builder;
    }

    /// <summary>
    /// Appends an auto-implemented C# property declaration.
    /// </summary>
    public static StringBuilder AppendProperty(this StringBuilder builder, string type, string propertyName, string modifiers = "public")
    {
        builder.AppendLine($"{modifiers} {type} {propertyName} {{ get; set; }}");
        return builder;
    }
}
