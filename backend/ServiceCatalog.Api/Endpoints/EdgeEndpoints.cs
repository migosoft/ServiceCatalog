using ServiceCatalog.Api.Models;
using ServiceCatalog.Api.Services;

namespace ServiceCatalog.Api.Endpoints;

public static class EdgeEndpoints
{
    public static void MapEdgeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/edges");

        group.MapGet("/", async (Neo4jService svc) =>
            Results.Ok(await svc.GetAllEdgesAsync()));

        group.MapPost("/", async (CreateEdgeRequest req, Neo4jService svc) =>
        {
            try
            {
                var edge = await svc.CreateEdgeAsync(req);
                return Results.Created($"/api/edges/{edge.Id}", edge);
            }
            catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
            catch (InvalidOperationException) { return Results.BadRequest("One or both nodes not found."); }
        });

        group.MapDelete("/{id}", async (string id, Neo4jService svc) =>
        {
            var deleted = await svc.DeleteEdgeAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });
    }
}
