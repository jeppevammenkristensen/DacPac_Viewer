using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

public class Builder
{
    private readonly CsharpGenerator[] _generators;

    public Builder(IEnumerable<CsharpGenerator> generators)
    {
        _generators = generators.ToArray();
    }

    public string Build(TSqlObject[] sqlObjects)
    {
        var sb = new StringBuilder();
        
        foreach (var sqlObject in sqlObjects)
        {
            foreach (var generator in _generators)
            {
                if (generator.IsValid(sqlObject))
                {
                    generator.Build(sqlObject, sb);
                }
                else
                {
                    sb.AppendLine($"// No generator found for {sqlObject.Name} of type {sqlObject.ObjectType}");
                }
            }
        }

        return SyntaxFactory.ParseCompilationUnit(sb.ToString()).NormalizeWhitespace().ToFullString();
            
    }
}