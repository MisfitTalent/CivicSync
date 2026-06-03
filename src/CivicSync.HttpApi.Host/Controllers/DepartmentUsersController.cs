using CivicSync.Node.Api.Application.Services.Nodes;
using CivicSync.Node.Api.Contracts.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace CivicSync.Node.Api.Controllers;

[ApiController]
[Route("api/department-users")]
public sealed class DepartmentUsersController : ControllerBase
{
    private readonly IDepartmentUserService _departmentUserService;

    public DepartmentUsersController(IDepartmentUserService departmentUserService)
    {
        _departmentUserService = departmentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DepartmentUserDto>>> GetCurrentNodeUsersAsync(
        CancellationToken cancellationToken)
    {
        var users = await _departmentUserService.GetCurrentNodeUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DepartmentUserDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _departmentUserService.GetByIdAsync(id, cancellationToken);
        return Ok(user);
    }
}
