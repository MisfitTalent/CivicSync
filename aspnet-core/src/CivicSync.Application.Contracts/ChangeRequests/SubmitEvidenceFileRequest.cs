using System.ComponentModel.DataAnnotations;

namespace CivicSync.Application.Contracts.ChangeRequests;

public sealed class SubmitEvidenceFileRequest
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public string ContentBase64 { get; set; } = string.Empty;
}
