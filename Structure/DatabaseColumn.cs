using System.Text;

namespace CSharpSqliteORM.Structure;

public struct Database_Column
{
    public required string columnName;
    public required Database_ColumnType columnType;

    public bool autoIncrement;
    public bool isPrimaryKey;
    public bool allowNull;

    public Database_Column()
    {
        isPrimaryKey = false;
        allowNull = true;
    }

    public string GenerateColumnSQL()
    {
        string typeName;

        switch (columnType)
        {
            case Database_ColumnType.DATETIME:
                typeName = Database_ColumnType.TEXT.ToString();
                break;

            default:
                typeName = columnType.ToString();
                break;
        }

        StringBuilder sql = new StringBuilder().Append($"{columnName} {typeName}");

        switch (columnType)
        {
            case Database_ColumnType.INTEGER:
                if (isPrimaryKey)
                    sql.Append(" PRIMARY KEY");

                if (autoIncrement)
                    sql.Append(" AUTOINCREMENT");
                break;
        }

        if (!allowNull)
        {
            sql.Append(" NOT NULL");
        }

        return sql.ToString();
    }
}


public enum Database_ColumnType
{
    TEXT,
    BIT,
    INTEGER,
    DATETIME,
}