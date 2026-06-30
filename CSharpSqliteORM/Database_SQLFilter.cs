using System.Data.SQLite;
using System.Text;

namespace CSharpSqliteORM;

public static class SQLFilter
{
    public static InternalSQLFilter Equal(string columnName, object val) => new InternalSQLFilter().Equal(columnName, val);
    public static InternalSQLFilter IsNull(string columnName) => new InternalSQLFilter().IsNull(columnName);
    public static InternalSQLFilter Limit(int to) => new InternalSQLFilter().Limit(to);
    public static InternalSQLFilter OrderDesc(string columnName) => new InternalSQLFilter().OrderDesc(columnName);
    public static InternalSQLFilter OrderAsc(string columnName) => new InternalSQLFilter().OrderAsc(columnName);

    public class InternalSQLFilter
    {
        public List<string> whereClauses = new List<string>();
        public List<string> orderClauses = new List<string>();

        public int? limitAmount;

        public List<SQLiteParameter> arguments = new List<SQLiteParameter>();

        public InternalSQLFilter Equal(string columnName, object val)
        {
            SQLiteParameter arg = new SQLiteParameter(Database_Manager.GetGenericParameterName(), val);
            whereClauses.Add($"{columnName} = @{arg.ParameterName}");
            arguments.Add(arg);

            return this;
        }

        public InternalSQLFilter IsNull(string columnName)
        {
            whereClauses.Add($"{columnName} IS NULL");
            return this;
        }

        public InternalSQLFilter Limit(int to)
        {
            limitAmount = to;
            return this;
        }

        public InternalSQLFilter OrderDesc(string columnName)
        {
            orderClauses.Add($"{columnName} Desc");
            return this;
        }

        public InternalSQLFilter OrderAsc(string columnName)
        {
            orderClauses.Add($"{columnName} Asc");
            return this;
        }


        public void Build(string tableName, out string resultSql, out List<SQLiteParameter> args)
        {
            StringBuilder sql = new StringBuilder($"SELECT _t.* FROM {tableName} _t");

            BuildGeneric(out string addition, out args);
            resultSql = sql.Append(addition).ToString();
        }

        public void BuildGeneric(out string addition, out List<SQLiteParameter> args)
        {
            StringBuilder sql = new StringBuilder();

            if (whereClauses.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", whereClauses));
            }

            if (orderClauses.Count > 0)
            {
                sql.Append(" ORDER BY ");
                sql.Append(string.Join(" , ", orderClauses));
            }

            if (limitAmount != null)
            {
                sql.Append($" LIMIT {limitAmount.Value}");
            }

            args = this.arguments;
            addition = $"{sql};";
        }
    }
}