namespace CSharpSqliteORM.Structure.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class Database_KeyAttribute : Attribute
{
    public bool autoIncrement;

    public Database_KeyAttribute(bool autoIncrement)
    {
        this.autoIncrement = autoIncrement;
    }
}
