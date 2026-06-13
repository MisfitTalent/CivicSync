using CivicSync.Application.Services.Nodes;
using CivicSync.Application.Contracts.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace CivicSync.Web.Host.Controllers;

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

    [HttpPost]
    public async Task<ActionResult<DepartmentUserDto>> CreateForCurrentNodeAsync(
        CreateDepartmentUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _departmentUserService.CreateForCurrentNodeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = user.Id }, user);
    }
}
