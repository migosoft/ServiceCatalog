namespace ServiceCatalog.Api.Models;

public record NodeDto(string Id, string Type, string Name, string? Description, Dictionary<string, string>? Properties);
public record CreateNodeRequest(string Type, string Name, string? Description, string? OperatingSystem, string? Owner, Dictionary<string, string>? Properties);
public record UpdateNodeRequest(string Name, string? Description, string? OperatingSystem, string? Owner, Dictionary<string, string>? Properties);
