namespace CSharpSqliteORM.Structure.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class Database_TypeAttribute : Attribute
{
    public Database_ColumnType Type { get; }

    public Database_TypeAttribute(Database_ColumnType type)
    {
        Type = type;
    }
}
