using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.Audit;

public sealed class PeerHealthDto
{
    public DepartmentCode DepartmentCode { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string Message { get; set; } = string.Empty;
}
