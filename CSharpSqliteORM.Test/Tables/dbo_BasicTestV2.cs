using CSharpSqliteORM.Structure;
using CSharpSqliteORM.Structure.Attributes;

namespace CSharpSqliteORM.Test.Tables;

public class dbo_BasicTestV2 : IDatabase_TableMain
{
    public static string tableName => nameof(dbo_BasicTestV2);

    [Database_Key(true)]
    [Database_Type(Database_ColumnType.INTEGER)]
    [Database_NotNull]
    public int Id { get; set; }

    [Database_Type(Database_ColumnType.TEXT)]
    public string? text { get; set; }

    [Database_NotNull()]
    [Database_Type(Database_ColumnType.TEXT)]
    public string? requiredText { get; set; }

    [Database_Default("test")]
    [Database_Type(Database_ColumnType.TEXT)]
    public string? defaultValue { get; set; }
}
