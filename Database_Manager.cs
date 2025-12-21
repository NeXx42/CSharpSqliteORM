using System.Data.Common;
using System.Data.SQLite;
using System.Reflection;
using System.Text;
using CSharpSqliteORM.Structure;

namespace CSharpSqliteORM;

public static class Database_Manager
{
    private static string? dbPath;
    private static string GetConnectionString() => $"Data Source={dbPath};Version=3;";

    private static SQLiteConnection? connection;

    public static async Task Init(string location)
    {
        if (string.IsNullOrEmpty(location))
            throw new Exception("Invalid path");

        dbPath = location;

        if (!File.Exists(dbPath))
        {
            SQLiteConnection.CreateFile(dbPath);
            connection = new SQLiteConnection(GetConnectionString());
        }

        connection ??= new SQLiteConnection(GetConnectionString());

        await GenerateTables();
    }

    private static async Task GenerateTables()
    {
        // cannot add or modify existing columns. way too advanced for this

        Type[] tables = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(IDatabase_Table).IsAssignableFrom(t))).ToArray();
        await connection!.OpenAsync();

        var tableCreator = typeof(Database_ColumnMapper).GetMethod(nameof(Database_ColumnMapper.CreateTable));

        foreach (Type tableType in tables)
        {
            var invoker = tableCreator!.MakeGenericMethod(tableType);
            string sql = (string)invoker.Invoke(null, null)!;

            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        await connection.CloseAsync();
    }



    public static async Task<T[]> GetItems<T>(SQLFilter.InternalSQLFilter? filter) where T : IDatabase_Table
    {
        if (filter != null)
        {
            filter.Build(T.tableName, out string sql, out SQLiteParameter[] args);
            return await ExecuteSQLQuery(sql, Database_ColumnMapper.DeserializeRow<T>, args);
        }
        else
        {
            return await ExecuteSQLQuery($"SELECT * FROM {T.tableName}", Database_ColumnMapper.DeserializeRow<T>);
        }
    }

    public static async Task InsertItem<T>(params T[] entries) where T : IDatabase_Table
    {
        StringBuilder sql = new StringBuilder($"INSERT INTO {T.tableName} VALUES ");
        List<SQLiteParameter> sqlParams = new List<SQLiteParameter>();

        foreach (T row in entries)
        {
            List<string> paramNames = new List<string>();
            Database_Column[] columns = T.getColumns;

            foreach (Database_Column col in columns)
            {
                string paramName = sqlParams.Count.ToString();

                paramNames.Add($"@{paramName}");
                sqlParams.Add(new SQLiteParameter(paramName, Database_ColumnMapper.SerializeColumn<T>(row, col)));
            }

            sql.Append($"({string.Join(",", paramNames)})");
        }

        await ExecuteSQLNonQuery(sql.ToString(), sqlParams.ToArray());
    }


    /*
        DB LOGIC
    */


    public static async Task ExecuteSQLNonQuery(string sql, params SQLiteParameter[] args)
    {
        try
        {
            await connection!.OpenAsync();

            using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
            {
                cmd.Parameters.AddRange(args);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch
        {
        }
        finally
        {
            await connection!.CloseAsync();
        }
    }

    public static async Task<T[]> ExecuteSQLQuery<T>(string sql, Func<SQLiteDataReader, Task<T>> deserializer, params SQLiteParameter[]? args)
    {
        List<T> res = new List<T>();

        try
        {
            await connection!.OpenAsync();

            using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
            {
                if (args?.Length > 0)
                    cmd.Parameters.AddRange(args);

                using (SQLiteDataReader reader = (SQLiteDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        T deserializedResult = await deserializer(reader);
                        res.Add(deserializedResult);
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            await connection!.CloseAsync();
        }

        return res.ToArray();
    }
}

/*
     Filtering
  */


public static class SQLFilter
{
    public static InternalSQLFilter Equal(string columnName, object val) => new InternalSQLFilter().Equal(columnName, val);

    public class InternalSQLFilter
    {
        public List<string> whereClauses = new List<string>();
        public List<string> orderClauses = new List<string>();
        public List<SQLiteParameter> arguments = new List<SQLiteParameter>();

        public InternalSQLFilter Equal(string columnName, object val)
        {
            SQLiteParameter arg = new SQLiteParameter(arguments.Count.ToString(), val);
            whereClauses.Add($"{columnName} = @{arg.ParameterName}");
            arguments.Add(arg);

            return this;
        }



        public void Build(string tableName, out string resultSql, out SQLiteParameter[] args)
        {
            StringBuilder sql = new StringBuilder($"SELECT _t.* FROM {tableName} _t");

            if (whereClauses.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", whereClauses));
            }

            if (orderClauses.Count > 0)
            {
                sql.Append(" ORDER BY ");
                sql.Append(string.Join(" , ", orderClauses));
            }

            args = arguments.ToArray();
            resultSql = sql.ToString();
        }
    }
}
