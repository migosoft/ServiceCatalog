namespace ServiceCatalog.Api.Models;

public record EdgeDto(string Id, string FromId, string ToId, string RelationType);
public record CreateEdgeRequest(string FromId, string ToId, string RelationType);
public record GraphDto(IEnumerable<NodeDto> Nodes, IEnumerable<EdgeDto> Edges);
