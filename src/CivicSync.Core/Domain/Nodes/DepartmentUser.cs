using CivicSync.Core.Domain.Common;

namespace CivicSync.Core.Domain.Nodes;

public sealed class DepartmentUser : EntityBase
{
    private DepartmentUser()
    {
    }

    public DepartmentUser(
        Guid departmentNodeId,
        string fullName,
        string role,
        string emailAddress)
    {
        DepartmentNodeId = departmentNodeId;
        FullName = fullName;
        Role = role;
        EmailAddress = emailAddress;
        IsActive = true;
    }

    public Guid DepartmentNodeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
