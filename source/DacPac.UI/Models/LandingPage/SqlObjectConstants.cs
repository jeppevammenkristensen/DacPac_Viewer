using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Defines SQL object types supported by the landing page tree.
/// </summary>
public static class SqlObjectConstants
{
    /// <summary>
    /// Gets the SQL object types displayed at the tree root.
    /// </summary>
    public static readonly ModelTypeClass[] RootModelTypes =
        [Table.TypeClass, View.TypeClass, Procedure.TypeClass, TableType.TypeClass];
}