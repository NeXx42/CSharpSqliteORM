using CSharpSqliteORM.Structure;

namespace CSharpSqliteORM.Test;

public class DatabaseHelper : IDisposable
{
    private string path;
    public Database_Manager.DatabaseInstance instance { private set; get; }

    public DatabaseHelper()
    {
        path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
        instance = new Database_Manager.DatabaseInstance();
    }

    public async Task InitWithData<T>(T[] testData) where T : IDatabase_Table
    {
        await Init();
        await instance.InsertItem(testData);
    }

    public async Task Init(Action<Exception, string?> thrower) => await instance.Init(path, thrower);
    public async Task Init() => await instance.Init(path, Thrower);

    private void Thrower(Exception e, string? msg)
    {
        Console.WriteLine("Database failure - " + msg);
        throw e;
    }

    public void Dispose()
    {
        instance.Dispose();

        if (File.Exists(path))
            File.Delete(path);
    }

}
