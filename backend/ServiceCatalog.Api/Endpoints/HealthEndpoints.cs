using System.Text.Json;
using System.Threading.Channels;
using ServiceCatalog.Api.Models;
using ServiceCatalog.Api.Services;

namespace ServiceCatalog.Api.Endpoints;

public static class HealthEndpoints
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public static void MapHealthEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/health");

        // SSE stream
        g.MapGet("/stream", async (HealthCheckService svc, HttpContext ctx, CancellationToken ct) =>
        {
            ctx.Response.Headers.Append("Content-Type",      "text/event-stream");
            ctx.Response.Headers.Append("Cache-Control",     "no-cache");
            ctx.Response.Headers.Append("X-Accel-Buffering", "no");

            await ctx.Response.WriteAsync("retry: 3000\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);

            foreach (var s in svc.GetAllStatuses())
                await WriteEventAsync(ctx, s, ct);

            var channel = Channel.CreateUnbounded<HealthStatusDto>();
            svc.Subscribe(channel.Writer);
            try
            {
                await foreach (var update in channel.Reader.ReadAllAsync(ct))
                    await WriteEventAsync(ctx, update, ct);
            }
            catch (OperationCanceledException) { }
            finally { svc.Unsubscribe(channel.Writer); }
        });

        g.MapGet("/status",  (HealthCheckService svc) => Results.Ok(svc.GetAllStatuses()));
        g.MapGet("/configs", (HealthCheckService svc) => Results.Ok(svc.GetAllConfigs()));

        g.MapPut("/configs/{nodeId}", async (string nodeId, SetHealthConfigRequest req,
            Neo4jService neo4j, HealthCheckService svc) =>
        {
            await neo4j.SetHealthConfigAsync(nodeId, req);
            await svc.RefreshConfigAsync(nodeId);
            return Results.Ok();
        });

        g.MapDelete("/configs/{nodeId}", async (string nodeId,
            Neo4jService neo4j, HealthCheckService svc) =>
        {
            await neo4j.DeleteHealthConfigAsync(nodeId);
            await svc.RefreshConfigAsync(nodeId);
            return Results.NoContent();
        });
    }

    private static async Task WriteEventAsync(HttpContext ctx, HealthStatusDto s, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(s, _json);
        await ctx.Response.WriteAsync($"data: {json}\n\n", ct);
        await ctx.Response.Body.FlushAsync(ct);
    }
}
