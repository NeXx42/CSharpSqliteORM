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

    [Fact]
    public async Task MiscTest_EnumConversion()
    {
        using DatabaseHelper db = new DatabaseHelper();
        await db.InitWithData([
            new dbo_ConversionTest() { value = dbo_ConversionTest_Enum.One },
            new dbo_ConversionTest() { value = dbo_ConversionTest_Enum.Two },
        ]);

        var res = await db.instance.GetItem<dbo_ConversionTest>(SQLFilter.Equal(nameof(dbo_ConversionTest.value), dbo_ConversionTest_Enum.One));
        Assert.Equal(dbo_ConversionTest_Enum.One, res!.value!.Value);

        await db.instance.InsertItem(new dbo_ConversionTest { value = null });

        var res2 = await db.instance.GetItems<dbo_ConversionTest>(SQLFilter.IsNull(nameof(dbo_ConversionTest.value)));

        Assert.Equal(1, res2.Length);
        Assert.Equal(null, res2[0].value);
    }
}
