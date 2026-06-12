using System.ComponentModel.DataAnnotations;

namespace CivicSync.Application.Contracts.ChangeRequests;

public sealed class SubmitFieldChangeRequest
{
    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string NewValue { get; set; } = string.Empty;
}
