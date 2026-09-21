using CSharpSqliteORM.Test.Tables;

namespace CSharpSqliteORM.Test;

public class InsertTest
{
    [Fact]
    public async Task InsertIgnoreValue()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
            new dbo_BasicAutoKeyedItem() { intTest = 0, stringTest = "test" },
            new dbo_BasicAutoKeyedItem() { intTest = 1, stringTest = "test" },
        ]);

        await db.instance.InsertItem([
            new dbo_BasicAutoKeyedItem() { intTest = -1, stringTest = "IM SPECIAL" },
            new dbo_BasicAutoKeyedItem() { intTest = -1, stringTest = "IM SPECIAL 2" },
        ]);

        dbo_BasicAutoKeyedItem[] confirmedResults = await db.instance.GetItems<dbo_BasicAutoKeyedItem>(SQLFilter.Equal(nameof(dbo_BasicAutoKeyedItem.intTest), -1).OrderAsc(nameof(dbo_BasicAutoKeyedItem.key)));

        Assert.Equal(2, confirmedResults.Length);
        Assert.Equal("IM SPECIAL", confirmedResults[0].stringTest);
        Assert.Equal("IM SPECIAL 2", confirmedResults[1].stringTest);
    }

    [Fact]
    public async Task InsertAndGetValue()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
            new dbo_BasicAutoKeyedItem() { intTest = 0, stringTest = "test" },
            new dbo_BasicAutoKeyedItem() { intTest = 1, stringTest = "test" },
        ]);

        dbo_BasicAutoKeyedItem[] results = await db.instance.InsertItem([
            new dbo_BasicAutoKeyedItem() { intTest = -1, stringTest = "IM SPECIAL" },
            new dbo_BasicAutoKeyedItem() { intTest = -1, stringTest = "IM SPECIAL 2" },
        ]);

        dbo_BasicAutoKeyedItem[] confirmedResults = await db.instance.GetItems<dbo_BasicAutoKeyedItem>(SQLFilter.Equal(nameof(dbo_BasicAutoKeyedItem.intTest), -1).OrderAsc(nameof(dbo_BasicAutoKeyedItem.key)));

        Assert.Equal(confirmedResults.Length, results.Length);
        Assert.Equal(confirmedResults[0].key, results[0].key);
        Assert.Equal(confirmedResults[0].stringTest, results[0].stringTest);

        Assert.Equal(confirmedResults[1].key, results[1].key);
        Assert.Equal(confirmedResults[1].stringTest, results[1].stringTest);
    }
}
