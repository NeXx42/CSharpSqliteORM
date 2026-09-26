namespace CSharpSqliteORM.Structure.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class Database_DefaultAttribute : Attribute
{
    public string? defaultValue;

    public Database_DefaultAttribute(string? defaultValue)
    {
        this.defaultValue = defaultValue;
    }
}
