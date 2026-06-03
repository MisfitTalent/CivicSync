namespace CivicSync.Node.Api.Contracts.ChangeRequests;

public sealed class FieldChangeDto
{
    public Guid Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}
