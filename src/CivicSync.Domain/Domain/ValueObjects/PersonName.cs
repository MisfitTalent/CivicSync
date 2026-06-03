namespace CivicSync.Node.Api.Domain.ValueObjects;

public sealed record PersonName(string FirstName, string LastName)
{
    public string DisplayName => $"{FirstName} {LastName}".Trim();
}
