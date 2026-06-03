using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Contracts.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CivicSync.Node.Api.Controllers;

[ApiController]
[Route("api/node")]
public sealed class NodeController : ControllerBase
{
    private readonly NodeOptions _nodeOptions;

    public NodeController(IOptions<NodeOptions> nodeOptions)
    {
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
}
