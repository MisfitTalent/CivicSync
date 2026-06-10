using Microsoft.OpenApi;

namespace CivicSync.Web.Core.Infrastructure.Swagger;

public static class CivicSyncSwaggerDocumentation
{
    public static void Apply(string httpMethod, string relativePath, OpenApiOperation operation)
    {
        var key = $"{httpMethod.ToUpperInvariant()} {relativePath.TrimStart('/')}";
        if (!EndpointDocumentation.TryGetValue(key, out var documentation))
        {
            return;
        }

        operation.Summary = documentation.Summary;
        operation.Description = documentation.Description;
    }

    private static readonly IReadOnlyDictionary<string, SwaggerEndpointDocumentation> EndpointDocumentation =
        new Dictionary<string, SwaggerEndpointDocumentation>(StringComparer.OrdinalIgnoreCase)
        {
            ["POST api/citizens"] = new(
                "Create a local citizen record",
                "Creates the citizen copy owned by the current department node. Use this first before submitting a change request on that node."),
            ["GET api/citizens"] = new(
                "List local citizen records",
                "Returns citizen records stored by the current department node."),
            ["POST api/change-requests"] = new(
                "Submit a citizen change request",
                "Creates a draft request for one or more shared citizen fields. The request captures the citizen record version for conflict detection."),
            ["POST api/change-requests/{id}/approvals"] = new(
                "Assign a department approval",
                "Adds the department node and approver user that must review the requested citizen change."),
            ["POST api/change-requests/{id}/decisions"] = new(
                "Record an approval decision",
                "Records the assigned approver's final decision. Once all required approvals are approved, the change request becomes ready to commit."),
            ["POST api/change-requests/{id}/commit"] = new(
                "Commit an approved change request",
                "Applies approved field changes to the local citizen record, writes an append-only ledger entry, and creates an outbox event for peer sync."),
            ["POST api/sync/outbox/publish-pending"] = new(
                "Publish pending outbox events",
                "Sends committed ledger entries from this node to configured peer department nodes."),
            ["POST api/sync/inbox/apply-pending"] = new(
                "Apply pending inbox entries",
                "Applies validated incoming ledger entries that were received from peer department nodes."),
            ["POST api/sync/ledger-entries"] = new(
                "Receive a peer ledger entry",
                "Receives a signed ledger entry from another department node. This endpoint validates the node HMAC signature before storing the inbox entry.")
        };

    private sealed record SwaggerEndpointDocumentation(string Summary, string Description);
}
