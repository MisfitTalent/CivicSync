namespace CivicSync.Core.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public const string SqlServerProvider = "SqlServer";

    public string Provider { get; set; } = SqlServerProvider;
}
