using Neo4j.Driver;
using ServiceCatalog.Api.Models;

namespace ServiceCatalog.Api.Services;

public class Neo4jService : IDisposable
{
    private readonly IDriver _driver;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Service", "Server", "Database" };
    private static readonly HashSet<string> AllowedRelations = new(StringComparer.OrdinalIgnoreCase)
        { "RUNS_ON", "REQUIRES" };
    private static readonly HashSet<string> AllowedOsValues = new(StringComparer.OrdinalIgnoreCase)
        { "Windows", "Linux" };

    public Neo4jService(IConfiguration config)
    {
        var uri = config["Neo4j:Uri"] ?? "bolt://localhost:7687";
        var user = config["Neo4j:User"] ?? "neo4j";
        var password = config["Neo4j:Password"] ?? "password";
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    }

    private static string SanitizeType(string type)
    {
        if (!AllowedTypes.Contains(type))
            throw new ArgumentException($"Invalid node type: {type}");
        return AllowedTypes.First(t => t.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeOs(string os)
    {
        if (!AllowedOsValues.Contains(os))
            throw new ArgumentException($"Invalid OS value '{os}'. Allowed: Windows, Linux");
        return AllowedOsValues.First(v => v.Equals(os, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeRelation(string rel)
    {
        if (!AllowedRelations.Contains(rel))
            throw new ArgumentException($"Invalid relation type: {rel}");
        return AllowedRelations.First(r => r.Equals(rel, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<NodeDto>> GetAllNodesAsync()
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(
            "MATCH (n) WHERE n:Service OR n:Server OR n:Database " +
            "RETURN elementId(n) AS id, labels(n)[0] AS type, n.name AS name, n.description AS description, properties(n) AS props");
        return await result.ToListAsync(MapNode);
    }

    public async Task<NodeDto?> GetNodeByIdAsync(string id)
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(
            "MATCH (n) WHERE elementId(n) = $id RETURN elementId(n) AS id, labels(n)[0] AS type, n.name AS name, n.description AS description, properties(n) AS props",
            new { id });
        var records = await result.ToListAsync(MapNode);
        return records.FirstOrDefault();
    }

    public async Task<NodeDto> CreateNodeAsync(CreateNodeRequest req)
    {
        var type = SanitizeType(req.Type);
        var props = new Dictionary<string, object> { ["name"] = req.Name, ["description"] = req.Description ?? "" };
        if (type == "Server" && !string.IsNullOrWhiteSpace(req.OperatingSystem))
            props["os"] = SanitizeOs(req.OperatingSystem);
        if (!string.IsNullOrWhiteSpace(req.Owner))
            props["owner"] = req.Owner;
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(
            $"CREATE (n:{type} $props) " +
            "RETURN elementId(n) AS id, labels(n)[0] AS type, n.name AS name, n.description AS description, properties(n) AS props",
            new { props });
        var records = await result.ToListAsync(MapNode);
        return records.First();
    }

    public async Task<NodeDto?> UpdateNodeAsync(string id, UpdateNodeRequest req)
    {
        var os    = (!string.IsNullOrWhiteSpace(req.OperatingSystem)) ? SanitizeOs(req.OperatingSystem) : "";
        var owner = req.Owner?.Trim() ?? "";
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(
            "MATCH (n) WHERE elementId(n) = $id " +
            "SET n.name = $name, n.description = $description " +
            "SET n.owner = CASE WHEN $owner <> '' THEN $owner ELSE null END " +
            "FOREACH (_ IN CASE WHEN n:Server AND $os <> '' THEN [1] ELSE [] END | SET n.os = $os) " +
            "FOREACH (_ IN CASE WHEN n:Server AND $os = '' THEN [1] ELSE [] END | REMOVE n.os) " +
            "RETURN elementId(n) AS id, labels(n)[0] AS type, n.name AS name, n.description AS description, properties(n) AS props",
            new { id, name = req.Name, description = req.Description ?? "", os, owner });
        var records = await result.ToListAsync(MapNode);
        return records.FirstOrDefault();
    }

    public async Task<bool> DeleteNodeAsync(string id)
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(
            "MATCH (n) WHERE elementId(n) = $id DETACH DELETE n RETURN count(n) AS deleted",
            new { id });
        var summary = await result.ConsumeAsync();
        return summary.Counters.NodesDeleted > 0;
    }

    public async Task<IEnumerable<EdgeDto>> GetAllEdgesAsync()
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(
            "MATCH (a)-[r]->(b) WHERE (a:Service OR a:Server OR a:Database) AND (b:Service OR b:Server OR b:Database) " +
            "RETURN elementId(r) AS id, elementId(a) AS fromId, elementId(b) AS toId, type(r) AS relationType");
        return await result.ToListAsync(MapEdge);
    }

    public async Task<EdgeDto> CreateEdgeAsync(CreateEdgeRequest req)
    {
        var rel = SanitizeRelation(req.RelationType);
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(
            $"MATCH (a) WHERE elementId(a) = $fromId " +
            $"MATCH (b) WHERE elementId(b) = $toId " +
            $"CREATE (a)-[r:{rel}]->(b) " +
            "RETURN elementId(r) AS id, elementId(a) AS fromId, elementId(b) AS toId, type(r) AS relationType",
            new { fromId = req.FromId, toId = req.ToId });
        var records = await result.ToListAsync(MapEdge);
        return records.First();
    }

    public async Task<bool> DeleteEdgeAsync(string id)
    {
        await using var session = _driver.AsyncSession();
        var result = await session.RunAsync(
            "MATCH ()-[r]->() WHERE elementId(r) = $id DELETE r RETURN count(r) AS deleted",
            new { id });
        var summary = await result.ConsumeAsync();
        return summary.Counters.RelationshipsDeleted > 0;
    }

    public async Task<GraphDto> GetFullGraphAsync()
    {
        var nodes = await GetAllNodesAsync();
        var edges = await GetAllEdgesAsync();
        return new GraphDto(nodes, edges);
    }

    public async Task<GraphDto> SearchGraphAsync(string query)
    {
        await using var session = _driver.AsyncSession();
        var nodeResult = await session.RunAsync(
            "MATCH (n) WHERE (n:Service OR n:Server OR n:Database) AND toLower(n.name) CONTAINS toLower($q) " +
            "RETURN elementId(n) AS id, labels(n)[0] AS type, n.name AS name, n.description AS description, properties(n) AS props",
            new { q = query });
        var matchedNodes = await nodeResult.ToListAsync(MapNode);

        if (!matchedNodes.Any())
            return new GraphDto([], []);

        var matchedIds = matchedNodes.Select(n => n.Id).ToList();

        var neighborResult = await session.RunAsync(
            "MATCH (n)-[r]-(m) WHERE elementId(n) IN $ids AND (m:Service OR m:Server OR m:Database) " +
            "RETURN elementId(m) AS id, labels(m)[0] AS type, m.name AS name, m.description AS description, properties(m) AS props",
            new { ids = matchedIds });
        var neighborNodes = await neighborResult.ToListAsync(MapNode);

        var allIds = matchedIds.Concat(neighborNodes.Select(n => n.Id)).Distinct().ToList();

        var edgeResult = await session.RunAsync(
            "MATCH (a)-[r]->(b) WHERE elementId(a) IN $ids AND elementId(b) IN $ids " +
            "RETURN elementId(r) AS id, elementId(a) AS fromId, elementId(b) AS toId, type(r) AS relationType",
            new { ids = allIds });
        var edges = await edgeResult.ToListAsync(MapEdge);

        var allNodes = matchedNodes.Concat(neighborNodes)
            .GroupBy(n => n.Id).Select(g => g.First());

        return new GraphDto(allNodes, edges);
    }

    public async Task<GraphDto> GetNeighborsAsync(string nodeId)
    {
        await using var session = _driver.AsyncSession();
        var nodeResult = await session.RunAsync(
            "MATCH (n)-[r]-(m) WHERE elementId(n) = $id AND (m:Service OR m:Server OR m:Database) " +
            "RETURN elementId(m) AS id, labels(m)[0] AS type, m.name AS name, m.description AS description, properties(m) AS props",
            new { id = nodeId });
        var neighbors = await nodeResult.ToListAsync(MapNode);

        var rootResult = await session.RunAsync(
            "MATCH (n) WHERE elementId(n) = $id RETURN elementId(n) AS id, labels(n)[0] AS type, n.name AS name, n.description AS description, properties(n) AS props",
            new { id = nodeId });
        var root = await rootResult.ToListAsync(MapNode);

        var allNodes = root.Concat(neighbors).GroupBy(n => n.Id).Select(g => g.First()).ToList();
        var allIds = allNodes.Select(n => n.Id).ToList();

        var edgeResult = await session.RunAsync(
            "MATCH (a)-[r]->(b) WHERE elementId(a) IN $ids AND elementId(b) IN $ids " +
            "RETURN elementId(r) AS id, elementId(a) AS fromId, elementId(b) AS toId, type(r) AS relationType",
            new { ids = allIds });
        var edges = await edgeResult.ToListAsync(MapEdge);

        return new GraphDto(allNodes, edges);
    }

    private static NodeDto MapNode(IRecord r)
    {
        var props = r["props"].As<IDictionary<string, object>>();
        var extra = props
            .Where(p => p.Key != "name" && p.Key != "description")
            .ToDictionary(p => p.Key, p => p.Value?.ToString() ?? "");
        return new NodeDto(
            r["id"].As<string>(),
            r["type"].As<string>(),
            r["name"].As<string>(),
            r["description"].As<string?>(),
            extra.Count > 0 ? extra : null);
    }

    private static EdgeDto MapEdge(IRecord r) =>
        new(r["id"].As<string>(), r["fromId"].As<string>(), r["toId"].As<string>(), r["relationType"].As<string>());

    public void Dispose() => _driver?.Dispose();
}
