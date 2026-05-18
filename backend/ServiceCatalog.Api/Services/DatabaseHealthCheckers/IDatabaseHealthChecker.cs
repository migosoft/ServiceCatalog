namespace ServiceCatalog.Api.Services.DatabaseHealthCheckers;

public interface IDatabaseHealthChecker
{
    string CheckType { get; }
    Task<(bool available, string? error)> CheckAsync(string connectionString, int retryCount, CancellationToken ct);
}
