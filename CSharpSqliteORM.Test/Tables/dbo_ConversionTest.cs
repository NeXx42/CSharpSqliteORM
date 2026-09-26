using CSharpSqliteORM.Structure;
using CSharpSqliteORM.Structure.Attributes;

namespace CSharpSqliteORM.Test.Tables;

public enum dbo_ConversionTest_Enum
{
    One,
    Two,
    Three
}

public class dbo_ConversionTest : IDatabase_TableMain
{
    public static string tableName => nameof(dbo_ConversionTest);

    [Database_Type(Database_ColumnType.INTEGER)]
    public dbo_ConversionTest_Enum? value { get; set; }
}
