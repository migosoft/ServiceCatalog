using ServiceCatalog.Api.Services;

namespace ServiceCatalog.Api.Endpoints;

public static class GraphEndpoints
{
    public static void MapGraphEndpoints(this WebApplication app)
    {
        app.MapGet("/api/graph", async (Neo4jService svc) =>
            Results.Ok(await svc.GetFullGraphAsync()));

        app.MapGet("/api/search", async (string q, Neo4jService svc) =>
        {
            if (string.IsNullOrWhiteSpace(q)) return Results.BadRequest("Query parameter 'q' is required.");
            return Results.Ok(await svc.SearchGraphAsync(q));
        });
    }
}
