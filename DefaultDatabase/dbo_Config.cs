using System.Data;
using CSharpSqliteORM.Structure;

namespace Logic.db;

public class dbo_Config : IDatabase_Table
{
    public static string tableName => "config";

    public required string key { get; set; }
    public string? value { get; set; }


    public static Database_Column[] getColumns => [
        new Database_Column() { columnName = nameof(key), columnType = Database_ColumnType.TEXT },
        new Database_Column() { columnName = nameof(value), columnType = Database_ColumnType.TEXT },
    ];
}
