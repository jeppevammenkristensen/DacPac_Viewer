using System.Collections.Generic;
using System.Linq;
using DacPac.UI.Models.LandingPage;
using DacPac.UI.ViewModels.LandingPage;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Infrastructure;

public class TreeDisplayService 
{
    public IEnumerable<ITreeItem> GetRoots(IEnumerable<TSqlModel> models)
    {
        var sqlObjects = models.SelectMany(x => x.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass, View.TypeClass, Procedure.TypeClass)).ToList();

        foreach (var grouping in sqlObjects.GroupBy(x => x.GetSchema(), new ObjectIdentifierComparer()).Where(x => x.Key is not null).OrderBy(x => x.Key!.Parts.Last()))
        {
            yield return new SchemaTreeItem(grouping.Key!, grouping.OrderBy(x => x.ObjectType.Name));
        }
    }
}