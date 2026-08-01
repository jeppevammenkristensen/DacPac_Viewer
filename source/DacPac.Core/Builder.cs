using System.Text;
using DacPac.Core.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

using SqlObjectWithGenerator = (TSqlObject sqlObject, CsharpGenerator generator);

/// <summary>
/// Coordinates registered C# generators and normalizes their combined output.
/// </summary>
public partial class Builder
{
    private readonly ILogger<Builder> _logger;
    private readonly CsharpGenerator[] _generators;

    /// <summary>
    /// Creates a builder using the supplied object-specific C# generators.
    /// </summary>
    public Builder(IEnumerable<CsharpGenerator> generators, ILogger<Builder> logger)
    {
        _logger = logger;
        _generators = generators.ToArray();
    }


    private CsharpGenerator? FindGenerator(TSqlObject sqlObject)
    {
        return _generators.FirstOrDefault(x => x.IsValid(sqlObject));
    }

    public HashSet<ModelTypeClass> GetSupportedObjectTypes()
    {
        var modelTypeClasses = _generators.SelectMany(x => x.SupportedObjectTypes).ToList();
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (modelTypeClasses.All(x => x == null))
        {
            LogAllSupportedModelTypeClassesAreNullThisCanOccurIfThisMethodIsCalledBeforeADacpac();
            return [];
        }
        
        return [.. modelTypeClasses];
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
                if (!mappedGenerators.ContainsKey(sqlObject.Name.ToString()))
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
        
        var generatedBuilders = new StringBuilder[mappedGenerators.Count];
        Parallel.ForEach(mappedGenerators.Values.Select((mappedGenerator, index) => (mappedGenerator, index)), item =>
        {
            var builder = new StringBuilder();
            item.mappedGenerator.generator.Build(item.mappedGenerator.sqlObject, builder);
            generatedBuilders[item.index] = builder;
        });

        foreach (var builder in generatedBuilders)
            sb.Append(builder);

        return SyntaxFactory.ParseCompilationUnit(sb.ToString()).NormalizeWhitespace().ToFullString();
            
    }

    [LoggerMessage(LogLevel.Warning, "All supported model type classes are null. This can occur if this method is called before a dacpac has been loaded")]
    partial void LogAllSupportedModelTypeClassesAreNullThisCanOccurIfThisMethodIsCalledBeforeADacpac();
}
