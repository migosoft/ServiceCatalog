namespace ServiceCatalog.Api.Models;

public record HealthCheckConfig(string NodeId, string CheckType, string CheckTarget, int IntervalSeconds, int RetryCount);
public record SetHealthConfigRequest(string CheckType, string CheckTarget, int IntervalSeconds = 30, int RetryCount = 3);
public record HealthStatusDto(string NodeId, bool IsAvailable, DateTimeOffset CheckedAt, string? Error = null);
