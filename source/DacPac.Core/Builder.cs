using System.Text;
using DacPac.Core.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

using SqlObjectWithGenerator = (TSqlObject sqlObject, CsharpGenerator generator);

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


    private CsharpGenerator? FindGenerator(TSqlObject sqlObject)
    {
        return _generators.FirstOrDefault(x => x.IsValid(sqlObject));
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
        sb.AppendLine("using Dapper;");

        Dictionary<string, SqlObjectWithGenerator> mappedGenerators = new();
        
        foreach (var sqlObject in sqlObjects)
        {
            if (FindGenerator(sqlObject) is { } generator)
            {
                mappedGenerators.Add(sqlObject.Name.ToString(), (sqlObject, generator));

                foreach (var requiredObject in generator.RequiredObjects(sqlObject))
                {
                    if (!mappedGenerators.ContainsKey(requiredObject.Name.ToString()))
                    {
                        if (FindGenerator(requiredObject) is { } requiredGenerator)
                        {
                            mappedGenerators.Add(requiredObject.Name.ToString(), (requiredObject, requiredGenerator));    
                        }
                    }
                }
            }
            else
            {
                mappedGenerators.Add(sqlObject.Name.ToString(), (sqlObject, new NotFoundGenerator()));
            }
        }
        
        foreach (var mappedGenerator in mappedGenerators.Values)
        {
            mappedGenerator.generator.Build(mappedGenerator.sqlObject,sb);
        }

        return SyntaxFactory.ParseCompilationUnit(sb.ToString()).NormalizeWhitespace().ToFullString();
            
    }
}
