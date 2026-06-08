using System.ComponentModel.DataAnnotations;
using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.Nodes;

public sealed class RegisterDepartmentNodeRequest
{
    [Required]
    public DepartmentCode DepartmentCode { get; set; }

    [Required]
    [MaxLength(500)]
    public string ApiBaseUrl { get; set; } = string.Empty;

    public bool RegisterAsPeerOfCurrentNode { get; set; } = true;
}
