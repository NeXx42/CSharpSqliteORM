using System.Data;

namespace CSharpSqliteORM.Structure;

public interface IDatabase_Table
{
    public abstract static string tableName { get; }
    public abstract static Database_Column[] getColumns { get; }
}
