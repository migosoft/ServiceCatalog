using Npgsql;

namespace ServiceCatalog.Api.Services.DatabaseHealthCheckers;

public sealed class PostgresHealthChecker : IDatabaseHealthChecker
{
    public string CheckType => "postgres";

    public async Task<(bool available, string? error)> CheckAsync(string connectionString, int retryCount, CancellationToken ct)
    {
        bool available = false;
        string? error = null;

        for (int attempt = 0; attempt <= retryCount; attempt++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                await conn.OpenAsync(cts.Token);
                available = true;
                error = null;
                break;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                if (attempt < retryCount && !ct.IsCancellationRequested)
                {
                    var baseMs   = (int)Math.Pow(2, attempt) * 1000;
                    var jitterMs = Random.Shared.Next(0, 500);
                    await Task.Delay(baseMs + jitterMs, ct);
                }
            }
        }

        return (available, error);
    }
}
