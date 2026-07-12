using System.Text;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

public abstract class CsharpGenerator
{
    public StringBuilder Build(TSqlObject tSqlObject, StringBuilder? sb = null)
    {
        if (!IsValid(tSqlObject))
        {
            throw new InvalidOperationException($"The provided TSqlObject '{tSqlObject.Name}' is not valid for this generator.");
        }
        
        sb ??= new StringBuilder();
        DoBuild(tSqlObject, sb);
        return sb;
    }

    protected abstract void DoBuild(TSqlObject sqlObject, StringBuilder sb);

    public abstract bool IsValid(TSqlObject tSqlObject);

}