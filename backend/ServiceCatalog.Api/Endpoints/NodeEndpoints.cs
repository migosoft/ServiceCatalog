using ServiceCatalog.Api.Models;
using ServiceCatalog.Api.Services;

namespace ServiceCatalog.Api.Endpoints;

public static class NodeEndpoints
{
    public static void MapNodeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/nodes");

        group.MapGet("/", async (Neo4jService svc) =>
            Results.Ok(await svc.GetAllNodesAsync()));

        group.MapGet("/{id}", async (string id, Neo4jService svc) =>
        {
            var node = await svc.GetNodeByIdAsync(id);
            return node is null ? Results.NotFound() : Results.Ok(node);
        });

        group.MapPost("/", async (CreateNodeRequest req, Neo4jService neo4j, HealthCheckService healthSvc) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest("Name is required.");
            try
            {
                var node = await neo4j.CreateNodeAsync(req);
                await SyncMssqlHealthAsync(node.Id, req.Type, req.DbType, req.DatabaseAddress, neo4j, healthSvc);
                return Results.Created($"/api/nodes/{node.Id}", node);
            }
            catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
        });

        group.MapPut("/{id}", async (string id, UpdateNodeRequest req, Neo4jService neo4j, HealthCheckService healthSvc) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest("Name is required.");
            var node = await neo4j.UpdateNodeAsync(id, req);
            if (node is null) return Results.NotFound();
            await SyncMssqlHealthAsync(id, node.Type, req.DbType, req.DatabaseAddress, neo4j, healthSvc);
            return Results.Ok(node);
        });

        group.MapDelete("/{id}", async (string id, Neo4jService svc) =>
        {
            var deleted = await svc.DeleteNodeAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapGet("/{id}/neighbors", async (string id, Neo4jService svc) =>
            Results.Ok(await svc.GetNeighborsAsync(id)));
    }

    private static async Task SyncMssqlHealthAsync(
        string nodeId, string nodeType, string? dbType, string? databaseAddress,
        Neo4jService neo4j, HealthCheckService healthSvc)
    {
        if (!nodeType.Equals("Database", StringComparison.OrdinalIgnoreCase))
            return;

        var isMssql = dbType?.Equals("Microsoft SQL Server", StringComparison.OrdinalIgnoreCase) == true;
        var hasDatabaseAddress = !string.IsNullOrWhiteSpace(databaseAddress);

        if (isMssql && hasDatabaseAddress)
        {
            var existing = await neo4j.GetHealthConfigAsync(nodeId);
            var interval = existing?.IntervalSeconds ?? 30;
            var retries  = existing?.RetryCount      ?? 3;
            await neo4j.SetHealthConfigAsync(nodeId, new SetHealthConfigRequest("mssql", databaseAddress!, interval, retries));
            await healthSvc.RefreshConfigAsync(nodeId);
        }
        else
        {
            var existing = await neo4j.GetHealthConfigAsync(nodeId);
            if (existing?.CheckType.Equals("mssql", StringComparison.OrdinalIgnoreCase) == true)
            {
                await neo4j.DeleteHealthConfigAsync(nodeId);
                await healthSvc.RefreshConfigAsync(nodeId);
            }
        }
    }
}
