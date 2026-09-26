using System.Data;

namespace CSharpSqliteORM.Structure;

public interface IDatabase_Table : IDatabase_TableMain
{
    public abstract static Database_Column[] getColumns { get; }
}
