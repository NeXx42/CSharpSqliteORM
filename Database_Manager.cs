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


    public static string GetGenericParameterName() => Guid.NewGuid().ToString().Replace("-", "");

    public static async Task<bool> Exists<T>(SQLFilter.InternalSQLFilter? filter = null) where T : IDatabase_Table => (await GetItems<T>(filter))?.Length > 0; // replace with actual sql

    public static async Task<T[]> GetItems<T>(SQLFilter.InternalSQLFilter? filter = null) where T : IDatabase_Table
    {
        if (filter != null)
        {
            filter.Build(T.tableName, out string sql, out List<SQLiteParameter> args);
            return await ExecuteSQLQuery(sql, Database_ColumnMapper.DeserializeRow<T>, args.ToArray());
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
                string paramName = GetGenericParameterName();

                paramNames.Add($"@{paramName}");
                sqlParams.Add(new SQLiteParameter(paramName, Database_ColumnMapper.SerializeColumn<T>(row, col)));
            }

            sql.Append($"({string.Join(",", paramNames)})");
        }

        await ExecuteSQLNonQuery(sql.ToString(), sqlParams.ToArray());
    }

    public static async Task AddOrUpdate<T>(T obj, Func<T, SQLFilter.InternalSQLFilter>? match, params string[] columns) where T : IDatabase_Table
    {
        SQLFilter.InternalSQLFilter? whereClause = match != null ? match(obj) : null;

        if (await Exists<T>(whereClause))
        {
            StringBuilder sql = new StringBuilder($"UPDATE {T.tableName} SET ");

            List<string> updates = new List<string>();
            List<SQLiteParameter> sqlParams = new List<SQLiteParameter>();

            Database_Column[] cols = T.getColumns;

            foreach (Database_Column col in cols)
            {
                if (columns?.Length > 0 && !columns.Contains(col.columnName))
                    continue;

                SQLiteParameter param = new SQLiteParameter(GetGenericParameterName(), Database_ColumnMapper.SerializeColumn<T>(obj, col));

                updates.Add($"{col.columnName} = @{param.ParameterName}");
                sqlParams.Add(param);
            }

            sql.Append(string.Join(",", updates));

            if (whereClause != null)
            {
                whereClause.BuildGeneric(out string addition, out List<SQLiteParameter> extraArgs);
                sqlParams.AddRange(extraArgs);

                sql.Append(addition);
            }

            await ExecuteSQLNonQuery(sql.ToString(), sqlParams.ToArray());
        }
        else
        {
            await InsertItem(obj);
        }
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
            SQLiteParameter arg = new SQLiteParameter(Database_Manager.GetGenericParameterName(), val);
            whereClauses.Add($"{columnName} = @{arg.ParameterName}");
            arguments.Add(arg);

            return this;
        }



        public void Build(string tableName, out string resultSql, out List<SQLiteParameter> args)
        {
            StringBuilder sql = new StringBuilder($"SELECT _t.* FROM {tableName} _t");

            BuildGeneric(out string addition, out args);
            resultSql = sql.Append(addition).ToString();
        }

        public void BuildGeneric(out string addition, out List<SQLiteParameter> args)
        {
            StringBuilder sql = new StringBuilder();

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

            args = this.arguments;
            addition = sql.ToString();
        }
    }
}
