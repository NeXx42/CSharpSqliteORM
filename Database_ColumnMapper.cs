using System.Data.SQLite;
using System.Reflection;
using System.Text;
using CSharpSqliteORM.Structure;

namespace CSharpSqliteORM;

public static class Database_ColumnMapper
{
    public static string CreateTable<T>() where T : IDatabase_Table
    {
        Database_Column[] rows = T.getColumns;
        StringBuilder sql = new StringBuilder($"CREATE TABLE IF NOT EXISTS {T.tableName} ( ");

        for (int i = 0; i < rows.Length; i++)
        {
            sql.Append(rows[i].GenerateColumnSQL());

            if (i < rows.Length - 1)
                sql.Append(",");
        }

        sql.Append(")");
        return sql.ToString();
    }

    public static async Task<T> DeserializeRow<T>(SQLiteDataReader reader) where T : IDatabase_Table
    {
        T row = Activator.CreateInstance<T>();
        PropertyInfo[] props = typeof(T).GetProperties();

        Database_Column[] columns = T.getColumns;

        foreach (Database_Column col in columns)
        {
            object? columnResult = reader[col.columnName];
            PropertyInfo? prop = props.FirstOrDefault(x => x.Name.Equals(col.columnName));

            if (columnResult != null && prop != null)
            {
                object? realVal = DeserializeColumn(columnResult, col.columnType, prop.PropertyType);
                prop.SetValue(row, realVal);
            }
        }

        return row;
    }

    public static object? DeserializeColumn(object val, Database_ColumnType columnType, Type endType)
    {
        if (val == DBNull.Value)
            return null;

        switch (columnType)
        {
            case Database_ColumnType.INTEGER:
                if (endType == typeof(long))
                    return Convert.ToInt64(val);

                return Convert.ToInt32(val);

            case Database_ColumnType.BIT: return Convert.ToInt64(val) == 1;
            default: return val;
        }
    }

    public static object SerializeColumn<T>(IDatabase_Table row, Database_Column column)
    {
        PropertyInfo? prop = typeof(T).GetProperty(column.columnName);
        return prop?.GetValue(row) ?? DBNull.Value;
    }
}
