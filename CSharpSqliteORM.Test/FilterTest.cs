using CSharpSqliteORM.Test.Tables;

namespace CSharpSqliteORM.Test;

public class FilterTest
{
    [Fact]
    public async Task FilterTest_Equals()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.Init([
            new dbo_BasicTestPlatform() { intTest = 0, stringTest = "a" },
            new dbo_BasicTestPlatform() { intTest = 1, stringTest = "b" },
            new dbo_BasicTestPlatform() { intTest = 2, stringTest = "c" },
            new dbo_BasicTestPlatform() { intTest = 3, stringTest = "d" },
            new dbo_BasicTestPlatform() { intTest = 4, stringTest = "e" },
            new dbo_BasicTestPlatform() { intTest = 5, stringTest = "f" },
            new dbo_BasicTestPlatform() { intTest = 6, stringTest = "g" },
            new dbo_BasicTestPlatform() { intTest = 7, stringTest = "h" },
            new dbo_BasicTestPlatform() { intTest = 8, stringTest = "i" },
        ]);

        dbo_BasicTestPlatform? item = await db.instance.GetItem<dbo_BasicTestPlatform>(SQLFilter.Equal(nameof(dbo_BasicTestPlatform.intTest), 2));

        Assert.NotNull(item);
        Assert.Equal("c", item!.stringTest);
    }

    [Fact]
    public async Task FilterTest_In()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.Init([
            new dbo_BasicTestPlatform() { intTest = 0, stringTest = "a" },
            new dbo_BasicTestPlatform() { intTest = 1, stringTest = "b" },
            new dbo_BasicTestPlatform() { intTest = 2, stringTest = "c" },
            new dbo_BasicTestPlatform() { intTest = 3, stringTest = "d" },
            new dbo_BasicTestPlatform() { intTest = 4, stringTest = "e" },
            new dbo_BasicTestPlatform() { intTest = 5, stringTest = "f" },
            new dbo_BasicTestPlatform() { intTest = 6, stringTest = "g" },
            new dbo_BasicTestPlatform() { intTest = 7, stringTest = "h" },
            new dbo_BasicTestPlatform() { intTest = 8, stringTest = "i" },
        ]);

        dbo_BasicTestPlatform[] items = await db.instance.GetItems<dbo_BasicTestPlatform>(SQLFilter.In(nameof(dbo_BasicTestPlatform.intTest), [1, 2, 3, 50]));

        Assert.NotNull(items);
        Assert.Equal(3, items.Length);

        Assert.Equal("b", items[0].stringTest);
        Assert.Equal("c", items[1].stringTest);
        Assert.Equal("d", items[2].stringTest);

        items = await db.instance.GetItems<dbo_BasicTestPlatform>(SQLFilter.In(nameof(dbo_BasicTestPlatform.stringTest), ["h", "i"]));

        Assert.NotNull(items);
        Assert.Equal(2, items.Length);

        Assert.Equal(7, items[0].intTest);
        Assert.Equal(8, items[1].intTest);
    }
}
