using System.Data.Common;
using System.Data.SQLite;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using CSharpSqliteORM.Structure;
using Logic.db;

namespace CSharpSqliteORM;

public static class Database_Manager
{
    // dont want to use this static instance but runix / tuxpaper rely on it. need to convert those to use the nuget package
    private static DatabaseInstance? instance;

    public static async Task Init(string location, Action<Exception, string?>? errorCallback = null)
    {
        if (instance != null)
            throw new Exception("Database instance already exists");

        instance = new DatabaseInstance();
        await instance.Init(location, errorCallback);
    }

    public static string GetGenericParameterName() => Guid.NewGuid().ToString().Replace("-", "");

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task<int> GetCount<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? cancellationToken = null) where T : IDatabase_Table
            => await instance!.GetCount<T>(filter, cancellationToken);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task Delete<T>(SQLFilter.InternalSQLFilter? filter = null) where T : IDatabase_Table
               => await instance!.Delete<T>(filter);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task AddOrUpdate<T>(T obj, SQLFilter.InternalSQLFilter? match, params string[] columns) where T : IDatabase_Table
               => await instance!.AddOrUpdate<T>(obj, match, columns);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task AddOrUpdate<T>(IEnumerable<T> objs, Func<T, SQLFilter.InternalSQLFilter>? match, params string[] columns) where T : IDatabase_Table
               => await instance!.AddOrUpdate<T>(objs, match, columns);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task Update<T>(T obj, SQLFilter.InternalSQLFilter? match, params string[] columns) where T : IDatabase_Table
               => await instance!.Update<T>(obj, match, columns);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task InsertItem<T>(params IEnumerable<T> entries) where T : IDatabase_Table
               => await instance!.InsertItem<T>(entries);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task<T[]> GetItems<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? token = null) where T : IDatabase_Table
               => await instance!.GetItems<T>(filter, token);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task<T[]> GetItemsGeneric<T>(string sql, Func<SQLiteDataReader, Task<T>> deserializer, CancellationToken? cancellationToken = null)
               => await instance!.GetItemsGeneric<T>(sql, deserializer, cancellationToken);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task<(T[], int)> GetItemsWithCount<T>(string sql) where T : IDatabase_Table
               => await instance!.GetItemsWithCount<T>(sql);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task<T?> GetItem<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? token = null) where T : IDatabase_Table
               => await instance!.GetItem<T>(filter, token);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task<bool> Exists<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? token = null) where T : IDatabase_Table
               => await instance!.Exists<T>(filter, token);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task<T[]> ExecuteSQLQuery<T>(string sql, Func<SQLiteDataReader, Task<T>> deserializer, CancellationToken? cancellationToken, params SQLiteParameter[]? args)
               => await instance!.ExecuteSQLQuery<T>(sql, deserializer, cancellationToken, args);

    [Obsolete("Use the instance version and handle static instances outside of here. New functions will not be added here")]
    public static async Task ExecuteSQLNonQuery(string sql, CancellationToken? cancellationToken, params SQLiteParameter[] args)
               => await instance!.ExecuteSQLNonQuery(sql, cancellationToken, args);



    public class DatabaseInstance : IDisposable
    {
        private string? dbPath;
        private string GetConnectionString() => $"Data Source={dbPath};Version=3;";

        private SQLiteConnection? connection;
        private Action<Exception, string?>? errorCallback;

        private SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

        public async Task Init(string location, Action<Exception, string?>? errorCallback = null, Assembly[]? customAssemblies = null)
        {
            this.errorCallback = errorCallback;

            if (string.IsNullOrEmpty(location))
                throw new Exception("Invalid path");

            dbPath = location;

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                connection = new SQLiteConnection(GetConnectionString());
            }

            connection ??= new SQLiteConnection(GetConnectionString());

            customAssemblies ??= AppDomain.CurrentDomain.GetAssemblies();

            await GenerateTables(customAssemblies);
            await HandleMigrations(customAssemblies);
        }

        /* Database setup */

        private async Task GenerateTables(Assembly[] assemblies)
        {
            // cannot add or modify existing columns. way too advanced for this

            Type[] tables = assemblies.SelectMany(x => x.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(IDatabase_Table).IsAssignableFrom(t))).ToArray();
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

        private async Task HandleMigrations(Assembly[] assemblies)
        {
            Type[] migrations = assemblies.SelectMany(x => x.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(IDatabase_Migration).IsAssignableFrom(t))).ToArray();
            long? lastMigration = null;

            string? id = (await GetItem<dbo_Config>(SQLFilter.Equal(nameof(dbo_Config.key), IDatabase_Migration.CONFIG_MIGRATIONID)))?.value ?? null;

            if (!string.IsNullOrEmpty(id))
            {
                lastMigration = long.Parse(id);
            }

            IDatabase_Migration[] migrationsToApply = migrations.Select(x => (IDatabase_Migration)Activator.CreateInstance(x)!)
                .Where(x => x.migrationId > (lastMigration ?? 0))
                .OrderBy(x => x.migrationId).ToArray();


            if (lastMigration.HasValue)
            {
                lastMigration = null;
                await connection!.OpenAsync();

                foreach (IDatabase_Migration migration in migrationsToApply)
                {
                    lastMigration = migration.migrationId;

                    using (SQLiteCommand command = new SQLiteCommand(migration.Up(), connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                await connection.CloseAsync();
            }
            else
            {
                // migrations in this context are only to update existing database TABLES,
                // as migrations are only for amending tables there is no need to do migration on a database that is has just been created
                lastMigration = migrationsToApply.Length == 0 ? 0 : migrationsToApply[migrations.Length - 1].migrationId;
            }

            if (lastMigration.HasValue)
            {
                await Delete<dbo_Config>(SQLFilter.Equal(nameof(dbo_Config.key), IDatabase_Migration.CONFIG_MIGRATIONID));
                await InsertItem(new dbo_Config() { key = IDatabase_Migration.CONFIG_MIGRATIONID, value = lastMigration.Value.ToString() });
            }
        }

        /* Database interaction */

        public async Task<bool> Exists<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? token = null) where T : IDatabase_Table
            => (await GetItems<T>(filter, token))?.Length > 0; // replace with actual sql

        public async Task<T?> GetItem<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? token = null) where T : IDatabase_Table
            => (await GetItems<T>(filter?.Limit(1) ?? SQLFilter.Limit(1), token)).FirstOrDefault();

        public async Task<(T[], int)> GetItemsWithCount<T>(string sql) where T : IDatabase_Table
        {
            int? rowCount = null;
            return (await ExecuteSQLQuery<T>(sql, DeserializeRow, null), rowCount ?? 0);

            async Task<T> DeserializeRow(SQLiteDataReader reader)
            {
                if (rowCount == null)
                {
                    rowCount = Convert.ToInt32(reader["total_count"]);
                }

                return await Database_ColumnMapper.DeserializeRow<T>(reader);
            }
        }

        public async Task<T[]> GetItemsGeneric<T>(string sql, Func<SQLiteDataReader, Task<T>> deserializer, CancellationToken? cancellationToken = null)
        {
            return await ExecuteSQLQuery<T>(sql, deserializer, cancellationToken);
        }

        public async Task<T[]> GetItems<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? token = null) where T : IDatabase_Table
        {
            if (filter != null)
            {
                filter.Build(T.tableName, out string sql, out List<SQLiteParameter> args);
                return await ExecuteSQLQuery(sql, Database_ColumnMapper.DeserializeRow<T>, token, args.ToArray());
            }
            else
            {
                return await ExecuteSQLQuery($"SELECT * FROM {T.tableName}", Database_ColumnMapper.DeserializeRow<T>, token);
            }
        }

        public async Task InsertItem<T>(params IEnumerable<T> entries) where T : IDatabase_Table
        {
            if (entries.Count() == 0)
                return;

            Database_Column[] columns = T.getColumns.Where(x => !x.autoIncrement).ToArray();
            List<string> rows = new List<string>();

            List<SQLiteParameter> sqlParams = new List<SQLiteParameter>();

            foreach (T row in entries)
            {
                List<string> paramNames = new List<string>();

                foreach (Database_Column col in columns)
                {
                    if (col.autoIncrement)
                        continue;

                    string paramName = GetGenericParameterName();

                    paramNames.Add($"@{paramName}");
                    sqlParams.Add(new SQLiteParameter(paramName, Database_ColumnMapper.SerializeColumn<T>(row, col)));
                }

                rows.Add($"({string.Join(",", paramNames)})");
            }

            StringBuilder sql = new StringBuilder($"INSERT INTO {T.tableName} ({string.Join(",", columns.Select(x => x.columnName))}) VALUES");
            sql.Append(string.Join(",", rows));

            await ExecuteSQLNonQuery(sql.ToString(), null, sqlParams.ToArray());
        }

        public async Task Update<T>(IEnumerable<T> objs, Func<T, SQLFilter.InternalSQLFilter> match, params string[] columns) where T : IDatabase_Table
        {
            await Task.WhenAll(objs.Select(o => Update(o, match(o), columns)));
        }

        public async Task Update<T>(T obj, SQLFilter.InternalSQLFilter? match, params string[] columns) where T : IDatabase_Table
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

            if (match != null)
            {
                match.BuildGeneric(out string addition, out List<SQLiteParameter> extraArgs);
                sqlParams.AddRange(extraArgs);

                sql.Append(addition);
            }

            await ExecuteSQLNonQuery(sql.ToString(), null, sqlParams.ToArray());
        }

        public async Task AddOrUpdate<T>(IEnumerable<T> objs, Func<T, SQLFilter.InternalSQLFilter>? match, params string[] columns) where T : IDatabase_Table
        {
            foreach (T obj in objs)
            {
                await AddOrUpdate(obj, match == null ? null : match(obj), columns);
            }
        }


        public async Task AddOrUpdate<T>(T obj, SQLFilter.InternalSQLFilter? match, params string[] columns) where T : IDatabase_Table
        {
            if (await Exists<T>(match))
            {
                await Update(obj, match, columns);
            }
            else
            {
                await InsertItem(obj);
            }
        }

        public async Task Delete<T>(SQLFilter.InternalSQLFilter? filter = null) where T : IDatabase_Table
        {
            StringBuilder sql = new StringBuilder($"DELETE FROM {T.tableName} ");
            if (filter != null)
            {
                filter.BuildGeneric(out string where, out List<SQLiteParameter> args);
                await ExecuteSQLNonQuery(sql.Append(where).ToString(), null, args.ToArray());
            }
            else
            {
                await ExecuteSQLNonQuery(sql.ToString(), null);
            }
        }

        public async Task<int> GetCount<T>(SQLFilter.InternalSQLFilter? filter = null, CancellationToken? cancellationToken = null) where T : IDatabase_Table
        {
            const string countName = "cnt";
            StringBuilder sql = new StringBuilder($"select Count(*) as {countName} FROM {T.tableName}");

            if (filter != null)
            {
                filter.BuildGeneric(out string clauses, out List<SQLiteParameter> args);
                sql.Append(clauses);

                return (await ExecuteSQLQuery(sql.ToString(), Parse, cancellationToken, args.ToArray()))[0];
            }
            else
            {
                return (await ExecuteSQLQuery(sql.ToString(), Parse, cancellationToken))[0];
            }

            Task<int> Parse(SQLiteDataReader reader) => Task.FromResult(Convert.ToInt32(reader[countName]));
        }


        /*
            DB LOGIC
        */


        public async Task ExecuteSQLNonQuery(string sql, CancellationToken? cancellationToken, params SQLiteParameter[] args)
        {
            cancellationToken ??= CancellationToken.None;

            try
            {
                await _mutex.WaitAsync(cancellationToken.Value);
                await connection!.OpenAsync(cancellationToken.Value);

                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                {
                    cmd.Parameters.AddRange(args);
                    await cmd.ExecuteNonQueryAsync(cancellationToken.Value);
                }
            }
            catch (SQLiteException e) { HandleException(e); }
            catch (Exception e) { HandleException(e); }
            finally
            {
                await connection!.CloseAsync();
                _mutex.Release();
            }
        }

        public async Task<T[]> ExecuteSQLQuery<T>(string sql, Func<SQLiteDataReader, Task<T>> deserializer, CancellationToken? cancellationToken, params SQLiteParameter[]? args)
        {
            cancellationToken ??= CancellationToken.None;
            List<T> res = new List<T>();

            try
            {
                await _mutex.WaitAsync(cancellationToken.Value);
                await connection!.OpenAsync(cancellationToken.Value);

                using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                {
                    if (args?.Length > 0)
                        cmd.Parameters.AddRange(args);

                    using (SQLiteDataReader reader = (SQLiteDataReader)await cmd.ExecuteReaderAsync(cancellationToken.Value))
                    {
                        while (await reader.ReadAsync(cancellationToken.Value))
                        {
                            T deserializedResult = await deserializer(reader);
                            res.Add(deserializedResult);
                        }
                    }
                }
            }
            catch (SQLiteException e) { HandleException(e); }
            catch (Exception e) { HandleException(e); }
            finally
            {
                await connection!.CloseAsync();
                _mutex.Release();
            }

            return res.ToArray();
        }

        private void HandleException(SQLiteException e)
        {
            if (errorCallback == null)
                throw e;

            errorCallback?.Invoke(e, null);
        }

        private void HandleException(Exception e)
        {
            if (errorCallback == null)
                throw e;

            errorCallback?.Invoke(e, null);
        }

        public void Dispose()
        {
            connection!.Close();
        }
    }
}