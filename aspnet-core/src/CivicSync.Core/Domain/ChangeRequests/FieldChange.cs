using CivicSync.Core.Domain.Common;

namespace CivicSync.Core.Domain.ChangeRequests;

public sealed class FieldChange : EntityBase
{
    private FieldChange()
    {
    }

    public FieldChange(Guid changeRequestId, string fieldName, string oldValue, string newValue)
    {
        ChangeRequestId = changeRequestId;
        FieldName = fieldName;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public Guid ChangeRequestId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}
