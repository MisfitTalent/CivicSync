using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Application.Services.Nodes;
using CivicSync.Node.Api.Contracts.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CivicSync.Node.Api.Controllers;

[ApiController]
[Route("api/node")]
public sealed class NodeController : ControllerBase
{
    private readonly IDepartmentNodeService _departmentNodeService;
    private readonly NodeOptions _nodeOptions;

    public NodeController(
        IDepartmentNodeService departmentNodeService,
        IOptions<NodeOptions> nodeOptions)
    {
        _departmentNodeService = departmentNodeService;
        _nodeOptions = nodeOptions.Value;
    }

    [HttpGet]
    public ActionResult<NodeInfoDto> GetNodeInfo()
    {
        var peers = _nodeOptions.Peers
            .Select(peer => new PeerNodeDto(peer.DepartmentCode, peer.ApiBaseUrl))
            .ToList();

        return Ok(new NodeInfoDto(_nodeOptions.DepartmentCode, _nodeOptions.ApiBaseUrl, peers));
    }

    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyCollection<DepartmentNodeDto>>> GetRegisteredDepartmentsAsync(
        CancellationToken cancellationToken)
    {
        var nodes = await _departmentNodeService.GetRegisteredNodesAsync(cancellationToken);

        return Ok(nodes);
    }

    [HttpPost("departments")]
    public async Task<ActionResult<DepartmentNodeDto>> RegisterDepartmentAsync(
        RegisterDepartmentNodeRequest request,
        CancellationToken cancellationToken)
    {
        var node = await _departmentNodeService.RegisterAsync(request, cancellationToken);

        return Created($"/api/node/departments/{node.Id}", node);
    }
}
