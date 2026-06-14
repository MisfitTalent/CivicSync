namespace CivicSync.Core.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public const string SqlServerProvider = "SqlServer";

    public const string PostgreSqlProvider = "PostgreSql";

    public string Provider { get; set; } = SqlServerProvider;

    public bool IsPostgreSql() =>
        string.Equals(Provider, PostgreSqlProvider, StringComparison.OrdinalIgnoreCase);
}
