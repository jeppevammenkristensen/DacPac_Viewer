using System.Runtime.CompilerServices;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

public static class WrongSqlType
{
    /// <summary>
    /// Throws an exception if the <param name="model"></param> is not of the correct type <param name="typeClass"></param>.
    /// </summary>
    /// <param name="model"></param>    
    /// <param name="typeClass"></param>
    /// <param name="paramName"></param>
    public static void ThrowIfIncorrectType(this TSqlObject model, ModelTypeClass typeClass, [CallerArgumentExpression(nameof(model))] string? paramName = null)
    {
        if (model.ObjectType != typeClass)
        {
            throw new ArgumentException($"The provided TSqlObject is not of the expected type. Expected type: {typeClass.Name}, but got: {model.ObjectType.Name}", paramName);
        }
    }
}