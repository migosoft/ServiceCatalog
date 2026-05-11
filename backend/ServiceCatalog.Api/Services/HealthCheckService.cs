using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Threading.Channels;
using ServiceCatalog.Api.Models;

namespace ServiceCatalog.Api.Services;

public sealed class HealthCheckService : BackgroundService
{
    private readonly Neo4jService _neo4j;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly ILogger<HealthCheckService> _log;

    private readonly ConcurrentDictionary<string, HealthCheckConfig> _configs  = new();
    private readonly ConcurrentDictionary<string, HealthStatusDto>   _statuses = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset>    _nextCheck= new();

    private readonly object _subLock = new();
    private readonly List<ChannelWriter<HealthStatusDto>> _subscribers = new();

    public HealthCheckService(Neo4jService neo4j, ILogger<HealthCheckService> log)
    {
        _neo4j = neo4j;
        _log   = log;
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public IEnumerable<HealthStatusDto>   GetAllStatuses() => _statuses.Values;
    public IEnumerable<HealthCheckConfig> GetAllConfigs()  => _configs.Values;

    public void Subscribe(ChannelWriter<HealthStatusDto> writer)
    {
        lock (_subLock) _subscribers.Add(writer);
    }

    public void Unsubscribe(ChannelWriter<HealthStatusDto> writer)
    {
        lock (_subLock) { _subscribers.Remove(writer); writer.TryComplete(); }
    }

    public async Task RefreshConfigAsync(string nodeId)
    {
        var cfg = await _neo4j.GetHealthConfigAsync(nodeId);
        if (cfg != null)
        {
            _configs[nodeId]   = cfg;
            _nextCheck[nodeId] = DateTimeOffset.UtcNow; // trigger immediate check
        }
        else
        {
            _configs.TryRemove(nodeId, out _);
            _statuses.TryRemove(nodeId, out _);
            _nextCheck.TryRemove(nodeId, out _);
        }
    }

    // ── Background loop ───────────────────────────────────────────────────────
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            foreach (var c in await _neo4j.GetAllHealthConfigsAsync())
            {
                _configs[c.NodeId]   = c;
                _nextCheck[c.NodeId] = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex) { _log.LogWarning(ex, "Could not load health configs on startup"); }

        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var due = _configs.Values
                .Where(c => _nextCheck.GetValueOrDefault(c.NodeId, DateTimeOffset.MinValue) <= now)
                .ToList();

            if (due.Count > 0)
                await Task.WhenAll(due.Select(c => RunCheckAsync(c, ct)));

            await Task.Delay(1000, ct);
        }
    }

    // ── Single check with retry + jitter ─────────────────────────────────────
    private async Task RunCheckAsync(HealthCheckConfig cfg, CancellationToken ct)
    {
        _nextCheck[cfg.NodeId] = DateTimeOffset.UtcNow.AddSeconds(cfg.IntervalSeconds);

        var (available, error) = cfg.CheckType == "ping"
            ? await RunPingAsync(cfg.CheckTarget, cfg.RetryCount, ct)
            : await RunHttpAsync(cfg.CheckTarget, cfg.RetryCount, ct);

        var status = new HealthStatusDto(cfg.NodeId, available, DateTimeOffset.UtcNow, error);
        _statuses[cfg.NodeId] = status;
        Broadcast(status);
    }

    private async Task<(bool available, string? error)> RunPingAsync(string target, int retryCount, CancellationToken ct)
    {
        bool   available = false;
        string? error    = null;

        for (int attempt = 0; attempt <= retryCount; attempt++)
        {
            try
            {
                using var ping  = new Ping();
                var reply = await ping.SendPingAsync(target, 5000);
                if (reply.Status == IPStatus.Success)
                {
                    available = true;
                    error     = null;
                    break;
                }
                error = reply.Status.ToString();
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            if (attempt < retryCount && !ct.IsCancellationRequested)
            {
                var baseMs   = (int)Math.Pow(2, attempt) * 1000;
                var jitterMs = Random.Shared.Next(0, 500);
                await Task.Delay(baseMs + jitterMs, ct);
            }
        }

        return (available, error);
    }

    private async Task<(bool available, string? error)> RunHttpAsync(string target, int retryCount, CancellationToken ct)
    {
        bool   available = false;
        string? error    = null;

        for (int attempt = 0; attempt <= retryCount; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                var resp = await _http.GetAsync(target, cts.Token);
                available = (int)resp.StatusCode < 500;
                error     = null;
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

    private void Broadcast(HealthStatusDto status)
    {
        lock (_subLock)
        {
            var dead = _subscribers.Where(w => !w.TryWrite(status)).ToList();
            foreach (var w in dead) _subscribers.Remove(w);
        }
    }
}
