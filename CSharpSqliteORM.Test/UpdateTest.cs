using CSharpSqliteORM.Test.Tables;

namespace CSharpSqliteORM.Test;

public class UpdateTest
{

    [Fact]
    public async Task UpdateTest_AddOrUpdate()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
            new dbo_BasicTestPlatform() { intTest = 0, stringTest = "a" },
        ]);

        dbo_BasicTestPlatform[] changes = [
            new dbo_BasicTestPlatform() { intTest = 0, stringTest = "Changed", stringTest2 = "unchanged" },
            new dbo_BasicTestPlatform() { intTest = 1, stringTest = "a", stringTest2 = "unchanged" },
            new dbo_BasicTestPlatform() { intTest = 2, stringTest = "b", stringTest2 = "unchanged" },
        ];

        await db.instance.AddOrUpdate(changes, c => SQLFilter.Equal(nameof(dbo_BasicTestPlatform.intTest), c.intTest), nameof(dbo_BasicTestPlatform.stringTest));
        dbo_BasicTestPlatform[] results = await db.instance.GetItems<dbo_BasicTestPlatform>();

        Assert.Equal(3, results.Length);

        Assert.Equal("Changed", results.Single(r => r.intTest == 0)!.stringTest);
        Assert.Null(results.Single(r => r.intTest == 0)!.stringTest2);

        Assert.Equal("a", results.Single(r => r.intTest == 1)!.stringTest);
        Assert.Equal("b", results.Single(r => r.intTest == 2)!.stringTest);
    }

    [Fact]
    public async Task UpdateTest_UpdateMultiple()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
            new dbo_BasicTestPlatform() { intTest = 0, stringTest = "a" },
            new dbo_BasicTestPlatform() { intTest = 1, stringTest = "b" },
            new dbo_BasicTestPlatform() { intTest = 2, stringTest = "c" },
            new dbo_BasicTestPlatform() { intTest = 3, stringTest = "d" },
        ]);

        await db.instance.Update([
            new dbo_BasicTestPlatform() { intTest = 1, stringTest = "change", stringTest2 = "unchanged" },
            new dbo_BasicTestPlatform() { intTest = 2, stringTest = "change", stringTest2 = "unchanged" },
            new dbo_BasicTestPlatform() { intTest = 9999999, stringTest = "INVALID", stringTest2 = "INVALID" },
        ], c => SQLFilter.Equal(nameof(dbo_BasicTestPlatform.intTest), c.intTest), [nameof(dbo_BasicTestPlatform.stringTest)]);

        dbo_BasicTestPlatform[] results = await db.instance.GetItems<dbo_BasicTestPlatform>();

        Assert.Equal(4, results.Length);

        Assert.Equal("a", results.Single(r => r.intTest == 0).stringTest);

        Assert.Equal("change", results.Single(r => r.intTest == 1).stringTest);
        Assert.Equal("change", results.Single(r => r.intTest == 2).stringTest);
        Assert.Null(results.Single(r => r.intTest == 1).stringTest2);

        await db.instance.Update([
            new dbo_BasicTestPlatform() { intTest = 0, stringTest = "a", stringTest2 = "b" }
        ], c => SQLFilter.Equal(nameof(dbo_BasicTestPlatform.intTest), c.intTest));

        dbo_BasicTestPlatform result = (await db.instance.GetItem<dbo_BasicTestPlatform>(SQLFilter.Equal(nameof(dbo_BasicTestPlatform.intTest), 0)))!;

        Assert.Equal(0, result.intTest);
        Assert.Equal("a", result.stringTest);
        Assert.Equal("b", result.stringTest2);
    }
}
