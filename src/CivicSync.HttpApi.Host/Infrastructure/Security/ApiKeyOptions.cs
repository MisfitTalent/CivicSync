namespace CivicSync.Node.Api.Infrastructure.Security;

public sealed class ApiKeyOptions
{
    public const string SectionName = "Security";

    public string ApiKey { get; set; } = string.Empty;
}
