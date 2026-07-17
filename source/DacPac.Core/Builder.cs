using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

/// <summary>
/// Coordinates registered C# generators and normalizes their combined output.
/// </summary>
public class Builder
{
    private readonly CsharpGenerator[] _generators;

    /// <summary>
    /// Creates a builder using the supplied object-specific C# generators.
    /// </summary>
    public Builder(IEnumerable<CsharpGenerator> generators)
    {
        _generators = generators.ToArray();
    }

    /// <summary>
    /// Generates normalized C# source for the supplied DacPac objects.
    /// </summary>
    /// <remarks>
    /// Objects without a supporting generator are retained as explanatory comments in the output.
    /// </remarks>
    public string Build(TSqlObject[] sqlObjects)
    {
        var sb = new StringBuilder();
        
        foreach (var sqlObject in sqlObjects)
        {
            bool generatorFound = false;
            
            foreach (var generator in _generators)
            {
                if (generator.IsValid(sqlObject))
                {
                    generator.Build(sqlObject, sb);
                    generatorFound = true;
                }
            }

            if (!generatorFound)
            {
                sb.AppendLine($"// No generator found for {sqlObject.Name} of type {sqlObject.ObjectType}");
            }
        }

        return SyntaxFactory.ParseCompilationUnit(sb.ToString()).NormalizeWhitespace().ToFullString();
            
    }
}
