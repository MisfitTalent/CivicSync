using System.ComponentModel.DataAnnotations;

namespace CivicSync.Node.Api.Contracts.ChangeRequests;

public sealed class SubmitChangeRequest
{
    [Required]
    public Guid CitizenId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<SubmitFieldChangeRequest> FieldChanges { get; set; } = [];
}
