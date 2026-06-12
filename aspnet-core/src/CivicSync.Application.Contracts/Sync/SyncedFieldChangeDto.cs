using System.ComponentModel.DataAnnotations;

namespace CivicSync.Application.Contracts.Sync;

public sealed class SyncedFieldChangeDto
{
    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string NewValue { get; set; } = string.Empty;
}
