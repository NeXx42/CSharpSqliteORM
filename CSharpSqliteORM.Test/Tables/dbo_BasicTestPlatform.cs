using CSharpSqliteORM.Structure;

namespace CSharpSqliteORM.Test.Tables;

public class dbo_BasicTestPlatform : IDatabase_Table
{
    public static string tableName => "test";

    public int intTest { get; set; }
    public string? stringTest { get; set; }
    public string? stringTest2 { get; set; }

    public static Database_Column[] getColumns => [
        new Database_Column() { columnName = nameof(intTest), columnType = Database_ColumnType.INTEGER, allowNull = true },
        new Database_Column() { columnName = nameof(stringTest), columnType = Database_ColumnType.TEXT, allowNull = true },
        new Database_Column() { columnName = nameof(stringTest2), columnType = Database_ColumnType.TEXT, allowNull = true },
    ];
}
