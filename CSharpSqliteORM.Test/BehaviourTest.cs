using System.Data.SQLite;

namespace CSharpSqliteORM.Test;

public class BehaviourTest
{
    [Fact]
    public async Task BehaviourTest_Exceptions()
    {
        using (DatabaseHelper db = new DatabaseHelper())
        {
            Exception? error = null;

            await db.Init((e, _) => error = e);
            await db.instance.ExecuteSQLNonQuery("INSERT INTO NOT_A_TABLE VALUES ('Fake')", CancellationToken.None);

            Assert.NotNull(error);
        }

        using (DatabaseHelper db = new DatabaseHelper())
        {
            Exception? error = null;
            await db.Init();

            try
            {
                await db.instance.ExecuteSQLNonQuery("INSERT INTO NOT_A_TABLE VALUES ('Fake')", CancellationToken.None);
            }
            catch (SQLiteException e)
            {
                error = e;
            }

            Assert.NotNull(error);
        }
    }
}
