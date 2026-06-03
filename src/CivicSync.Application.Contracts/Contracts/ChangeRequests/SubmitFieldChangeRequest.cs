using System.ComponentModel.DataAnnotations;

namespace CivicSync.Node.Api.Contracts.ChangeRequests;

public sealed class SubmitFieldChangeRequest
{
    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string NewValue { get; set; } = string.Empty;
}
