namespace CSharpSqliteORM;

public interface IDatabase_Migration
{
    public const string CONFIG_MIGRATIONID = "MIGRATIONID";

    public long migrationId { get; }
    public string Up();
}
