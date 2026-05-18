using ServiceCatalog.Api.Endpoints;
using ServiceCatalog.Api.Services;
using ServiceCatalog.Api.Services.DatabaseHealthCheckers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDatabaseHealthChecker, MsSqlHealthChecker>();
builder.Services.AddSingleton<Neo4jService>();
builder.Services.AddSingleton<HealthCheckService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<HealthCheckService>());
builder.Services.AddOpenApi();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapNodeEndpoints();
app.MapEdgeEndpoints();
app.MapGraphEndpoints();
app.MapHealthEndpoints();

app.Run();
