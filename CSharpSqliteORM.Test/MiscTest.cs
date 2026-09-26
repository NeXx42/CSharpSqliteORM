using CSharpSqliteORM.Test.Tables;

namespace CSharpSqliteORM.Test;

public class MiscTest
{
    [Fact]
    public async Task MiscTest_UpdateV2()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
            new dbo_BasicTestV2() { text = "new insert", requiredText = "required" },
            new dbo_BasicTestV2() { text = "new insert 2", requiredText = "required" },
        ]);

        dbo_BasicTestV2[] res = await db.instance.GetItems<dbo_BasicTestV2>(SQLFilter.OrderAsc(nameof(dbo_BasicTestV2.Id)));

        Assert.Equal(2, res.Length);
        Assert.Equal("new insert 2", res[1].text);

        try
        {
            await db.InitWithData([
                new dbo_BasicTestV2() { text = "new insert" }
            ]);

            // expected fail on missing attribute
            Assert.Fail();
        }
        catch
        {
        }
    }
}
