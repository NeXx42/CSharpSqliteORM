using CSharpSqliteORM.Test.Tables;

namespace CSharpSqliteORM.Test;

public class FilterTest
{
    [Fact]
    public async Task FilterTest_Equals()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
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
        await db.InitWithData([
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

    [Fact]
    public async Task FilterTest_Skip()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
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

        dbo_BasicTestPlatform[] items = await db.instance.GetItems<dbo_BasicTestPlatform>(SQLFilter.Skip(5).OrderAsc(nameof(dbo_BasicTestPlatform.intTest)));

        Assert.NotNull(items);
        Assert.Equal(4, items.Length);

        Assert.Equal("f", items[0].stringTest);
        Assert.Equal("g", items[1].stringTest);
        Assert.Equal("h", items[2].stringTest);
        Assert.Equal("i", items[3].stringTest);
    }

    [Fact]
    public async Task FilterTest_Take()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
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

        dbo_BasicTestPlatform[] items = await db.instance.GetItems<dbo_BasicTestPlatform>(SQLFilter.Limit(5).OrderAsc(nameof(dbo_BasicTestPlatform.intTest)));

        Assert.NotNull(items);
        Assert.Equal(5, items.Length);

        Assert.Equal("a", items[0].stringTest);
        Assert.Equal("b", items[1].stringTest);
        Assert.Equal("c", items[2].stringTest);
        Assert.Equal("d", items[3].stringTest);
        Assert.Equal("e", items[4].stringTest);
    }

    [Fact]
    public async Task FilterTest_SkipTake()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
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

        dbo_BasicTestPlatform[] items = await db.instance.GetItems<dbo_BasicTestPlatform>(SQLFilter.Skip(5).Limit(2).OrderAsc(nameof(dbo_BasicTestPlatform.intTest)));

        Assert.NotNull(items);
        Assert.Equal(2, items.Length);

        Assert.Equal("f", items[0].stringTest);
        Assert.Equal("g", items[1].stringTest);
    }
}
