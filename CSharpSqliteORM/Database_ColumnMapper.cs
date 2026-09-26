using System.Data.SQLite;
using System.Reflection;
using System.Text;
using CSharpSqliteORM.Structure;
using CSharpSqliteORM.Structure.Attributes;

namespace CSharpSqliteORM;

public static class Database_ColumnMapper
{
    public static string CreateTable<T>() where T : IDatabase_TableMain
    {
        Database_Column[] rows = GetColumnsForTable<T>();
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

    public static async Task<T> DeserializeRow<T>(SQLiteDataReader reader) where T : IDatabase_TableMain
    {
        T row = Activator.CreateInstance<T>();
        PropertyInfo[] props = typeof(T).GetProperties();

        Database_Column[] columns = GetColumnsForTable<T>();

        foreach (Database_Column col in columns)
        {
            object? columnResult = null;

            try
            {
                columnResult = reader[col.columnName];
            }
            catch
            {
                throw new Exception($"Column '{col.columnName}' doesn't exist");
            }


            PropertyInfo? prop = props.FirstOrDefault(x => x.Name.Equals(col.columnName));

            if (columnResult != null && prop != null)
            {
                object? realVal = DeserializeColumn(columnResult, col.columnType, prop.PropertyType);
                prop.SetValue(row, realVal);
            }
        }

        return row;
    }

    public static Database_Column[] GetColumnsForTable<T>() where T : IDatabase_TableMain
    {
        if (typeof(IDatabase_Table).IsAssignableFrom(typeof(T)))
        {
            return (Database_Column[])typeof(T)
                .GetProperty(nameof(IDatabase_Table.getColumns))!
                .GetValue(null)!;
        }

        var fields = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        List<Database_Column> columns = new();

        foreach (var field in fields)
        {
            var attributes = field.GetCustomAttributes();
            Database_TypeAttribute? type = TryToGetAttribute<Database_TypeAttribute>(attributes);

            if (type != null)
            {
                Database_KeyAttribute? key = TryToGetAttribute<Database_KeyAttribute>(attributes);

                columns.Add(new Database_Column()
                {
                    columnName = field.Name,
                    columnType = type.Type,

                    isPrimaryKey = key != null,
                    autoIncrement = key?.autoIncrement ?? false,

                    allowNull = !HasAttribute<Database_NotNullAttribute>(attributes),
                    defaultValue = TryToGetAttribute<Database_DefaultAttribute>(attributes)?.defaultValue,
                });
            }
        }

        return columns.ToArray();

        bool HasAttribute<TA>(IEnumerable<Attribute> attributes) where TA : Attribute
        {
            return attributes.Any(a => typeof(TA) == a.GetType());
        }

        TA? TryToGetAttribute<TA>(IEnumerable<Attribute> attributes) where TA : Attribute
        {
            Attribute? type = attributes.FirstOrDefault(a => typeof(TA) == a.GetType());

            if (type != null)
                return (TA)type;

            return default;
        }
    }

    public static object? DeserializeColumn(object val, Database_ColumnType columnType, Type endType)
    {
        if (val == DBNull.Value)
            return null;

        Type endBaseType = Nullable.GetUnderlyingType(endType) ?? endType;

        switch (columnType)
        {
            case Database_ColumnType.GUID:
                return Guid.Parse((string)val);

            case Database_ColumnType.INTEGER:
                if (endBaseType == typeof(long))
                    return Convert.ToInt64(val);

                if (endBaseType.IsEnum)
                    return Enum.ToObject(endBaseType, val);

                return Convert.ToInt32(val);

            case Database_ColumnType.DATETIME:
                return DateTime.Parse((string)val);

            case Database_ColumnType.BIT: return Convert.ToInt64(val) == 1;
            default: return val;
        }
    }

    public static object SerializeColumn<T>(IDatabase_TableMain row, Database_Column column)
    {
        PropertyInfo? prop = typeof(T).GetProperty(column.columnName);
        object? obj = prop?.GetValue(row);

        Type propType = Nullable.GetUnderlyingType(prop!.PropertyType) ?? prop!.PropertyType;

        if (obj != null)
        {
            if (propType == typeof(DateTime))
            {
                obj = ((DateTime)obj).ToString();
            }
            else if (propType == typeof(Guid))
            {
                obj = ((Guid)obj).ToString();
            }
            else if (propType.IsEnum)
            {
                obj = (int)obj;
            }
        }

        return obj ?? DBNull.Value;
    }
}
